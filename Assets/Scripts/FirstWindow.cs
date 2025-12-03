using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MinorShift._Library;

/// <summary>
/// First window displayed on application startup.
/// Shows a list of available ERA games to select from.
/// </summary>
public class FirstWindow : MonoBehaviour
{
    /// <summary>
    /// PlayerPrefs key for storing custom game directory path.
    /// </summary>
    public const string CUSTOM_DIR_KEY = "CustomGameDirectory";
    
    /// <summary>
    /// Singleton instance for accessing FirstWindow from other scripts.
    /// </summary>
    public static FirstWindow instance { get; private set; }
    
    /// <summary>
    /// Shows the first window by loading it from resources.
    /// </summary>
    public static void Show()
    {
        var obj = Resources.Load<GameObject>("Prefab/FirstWindow");
        obj = GameObject.Instantiate(obj);
        obj.name = "FirstWindow";
    }
    
    /// <summary>
    /// Runs the selected ERA game.
    /// </summary>
    /// <param name="workspace">The workspace directory path.</param>
    /// <param name="era">The ERA game folder name.</param>
    static System.Collections.IEnumerator Run(string workspace, string era)
    {
        var async = Resources.UnloadUnusedAssets();
        while(!async.isDone)
            yield return null;

        var ow = EmueraContent.instance.option_window;
        ow.gameObject.SetActive(true);
        ow.ShowGameButton(true);
        ow.ShowInProgress(true);
        yield return null;

        System.GC.Collect();
        SpriteManager.Init();

        Sys.SetWorkFolder(workspace);
        Sys.SetSourceFolder(era);
        uEmuera.Utils.ResourcePrepare();

        async = Resources.UnloadUnusedAssets();
        while(!async.isDone)
            yield return null;

        EmueraContent.instance.SetNoReady();
        var emuera = GameObject.FindObjectOfType<EmueraMain>();
        emuera.Run();
    }

    void Awake()
    {
        instance = this;
    }
    
    void OnDestroy()
    {
        if(instance == this)
            instance = null;
    }

    void Start()
    {
        if(!string.IsNullOrEmpty(MultiLanguage.FirstWindowTitlebar))
            titlebar.text = MultiLanguage.FirstWindowTitlebar;  

        scroll_rect_ = GenericUtils.FindChildByName<ScrollRect>(gameObject, "ScrollRect");
        item_ = GenericUtils.FindChildByName(gameObject, "Item", true);
        setting_ = GenericUtils.FindChildByName(gameObject, "optionbtn", true);
        GenericUtils.SetListenerOnClick(setting_, OnOptionClick);

        GenericUtils.FindChildByName<Text>(gameObject, "version")
            .text = Application.version + " ";

        GetList(Application.persistentDataPath);
        setting_.SetActive(true);

#if UNITY_EDITOR
        var main_entry = GameObject.FindObjectOfType<MainEntry>();
        if(!string.IsNullOrEmpty(main_entry.era_path))
            GetList(main_entry.era_path);
        // In editor, also allow standalone directory logic for testing
        InitStandaloneDirectory();
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android: Use predefined paths only
        GetList("storage/emulated/0/emuera");
        GetList("storage/emulated/1/emuera");
        GetList("storage/emulated/2/emuera");

        GetList("storage/sdcard0/emuera");
        GetList("storage/sdcard1/emuera");
        GetList("storage/sdcard2/emuera");
#endif
#if UNITY_STANDALONE && !UNITY_EDITOR
        // Standalone (Windows, Linux, macOS): Allow custom directory selection
        GetList(Path.GetFullPath(Application.dataPath + "/.."));
        InitStandaloneDirectory();
#endif
    }
    
    /// <summary>
    /// Initializes the directory system for standalone platforms.
    /// Loads custom directory if valid, otherwise shows the directory selection dialog.
    /// </summary>
    void InitStandaloneDirectory()
    {
        string customDir = PlayerPrefs.GetString(CUSTOM_DIR_KEY, "");
        
        // Check if we have a valid custom directory
        if(!string.IsNullOrEmpty(customDir) && Directory.Exists(customDir))
        {
            // Valid directory exists, load games from it
            GetList(customDir);
        }
        else
        {
            // No valid directory set, show the directory selection dialog automatically
            // Use a small delay to ensure UI is ready
            GenericUtils.StartCoroutine(ShowDirectoryDialogDelayed());
        }
    }
    
    /// <summary>
    /// Shows the directory dialog after a short delay to ensure UI is initialized.
    /// </summary>
    System.Collections.IEnumerator ShowDirectoryDialogDelayed()
    {
        yield return null; // Wait one frame
        ShowDirectoryDialog();
    }
    
    /// <summary>
    /// Shows the directory selection dialog.
    /// Can be called from menu items or automatically on startup.
    /// </summary>
    public void ShowDirectoryDialog()
    {
        var ow = EmueraContent.instance.option_window;
        if(ow == null)
            return;
            
        string currentDir = PlayerPrefs.GetString(CUSTOM_DIR_KEY, "");
        ow.ShowDirectoryInputBox(currentDir, OnDirectorySet);
    }
    
    /// <summary>
    /// Callback when a directory is set from the input dialog.
    /// </summary>
    /// <param name="path">The directory path entered by the user.</param>
    public void OnDirectorySet(string path)
    {
        if(string.IsNullOrEmpty(path))
            return;
            
        // Normalize the path
        path = uEmuera.Utils.NormalizePath(path);
        
        // Validate the directory exists
        if(!Directory.Exists(path))
        {
            var ow = EmueraContent.instance.option_window;
            ow.ShowMessageBoxPublic(
                MultiLanguage.GetText("[Error]"),
                MultiLanguage.GetText("[DirectoryNotFound]"));
            return;
        }
        
        // Save to PlayerPrefs
        PlayerPrefs.SetString(CUSTOM_DIR_KEY, path);
        PlayerPrefs.Save();
        
        // Refresh the game list
        RefreshGameList();
    }
    
    /// <summary>
    /// Refreshes the game list by reloading the FirstWindow.
    /// </summary>
    public void RefreshGameList()
    {
        // Use coroutine to properly handle the destroy/create sequence
        GenericUtils.StartCoroutine(RefreshGameListCoroutine());
    }
    
    /// <summary>
    /// Coroutine that handles refreshing the game list.
    /// Waits for the current frame to end before creating a new window.
    /// </summary>
    System.Collections.IEnumerator RefreshGameListCoroutine()
    {
        // Destroy current FirstWindow
        GameObject.Destroy(gameObject);
        // Wait for the end of frame to ensure destruction is processed
        yield return null;
        // Show a new FirstWindow
        Show();
    }

    void OnOptionClick()
    {
        var ow = EmueraContent.instance.option_window;
        ow.ShowMenu();
    }

    /// <summary>
    /// Adds a game item to the list.
    /// </summary>
    /// <param name="folder">The folder name.</param>
    /// <param name="workspace">The workspace path.</param>
    void AddItem(string folder, string workspace)
    {
        var rrt = item_.transform as UnityEngine.RectTransform;
        var obj = GameObject.Instantiate(item_);
        var text = GenericUtils.FindChildByName<UnityEngine.UI.Text>(obj, "name");
        text.text = folder;
        text = GenericUtils.FindChildByName<UnityEngine.UI.Text>(obj, "path");
        text.text = workspace + "/" + folder;

        GenericUtils.SetListenerOnClick(obj, () =>
        {
            scroll_rect_ = null;
            item_ = null;
            GameObject.Destroy(gameObject);
            // Start Game
            GenericUtils.StartCoroutine(Run(workspace, folder));
        });

        var rt = obj.transform as UnityEngine.RectTransform;
        var content = scroll_rect_.content;
        rt.SetParent(content);
        rt.localScale = Vector3.one;
        rt.anchorMax = rrt.anchorMax;
        rt.anchorMin = rrt.anchorMin;
        rt.offsetMax = rrt.offsetMax;
        rt.offsetMin = rrt.offsetMin;
        rt.sizeDelta = rrt.sizeDelta;
        rt.localPosition = new Vector2(0, -rt.sizeDelta.y * itemcount_);
        itemcount_ += 1;

        var ih = rt.sizeDelta.y * itemcount_;
        if(ih > content.sizeDelta.y)
        {
            content.sizeDelta = new Vector2(content.sizeDelta.x, ih);
        }
        obj.SetActive(true);
    }

    /// <summary>
    /// Gets the list of ERA games from a workspace directory.
    /// </summary>
    /// <param name="workspace">The workspace path to scan.</param>
    void GetList(string workspace)
    {
        workspace = uEmuera.Utils.NormalizePath(workspace);
        if(!Directory.Exists(workspace))
            return;
        try
        {
            var paths = Directory.GetDirectories(workspace, "*", SearchOption.TopDirectoryOnly);
            foreach(var p in paths)
            {
                var path = uEmuera.Utils.NormalizePath(p);
                if(File.Exists(path + "/emuera.config") || Directory.Exists(path + "/ERB"))
                    AddItem(path.Substring(workspace.Length + 1), workspace);
            }
        }
        catch(DirectoryNotFoundException e)
        { }
    }

    /// <summary>
    /// Title bar text component.
    /// </summary>
    [Tooltip("Title bar text component")]
    public Text titlebar = null;
    
    ScrollRect scroll_rect_ = null;
    GameObject item_ = null;
    GameObject setting_ = null;
    int itemcount_ = 0;
}
