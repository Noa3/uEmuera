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
    /// Shows the scene-owned first window. The Resources prefab remains a
    /// compatibility fallback for scenes that do not contain the picker.
    /// </summary>
    public static void Show()
    {
        var existing = instance;
        if(existing == null)
            existing = Object.FindAnyObjectByType<FirstWindow>();

        if(existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.RebuildGameList();
            return;
        }

        var obj = Resources.Load<GameObject>("Prefab/FirstWindow");
        if(obj == null)
            return;
        obj = GameObject.Instantiate(obj);
        obj.name = "FirstWindow";
    }
    
    /// <summary>
    /// Runs a game directory that already contains the game files.
    /// </summary>
    /// <param name="gamePath">The directory containing emuera.config or ERB.</param>
    static System.Collections.IEnumerator Run(string gamePath)
    {
        MinorShift.Emuera.GameProc.StartupProfiler.Begin();
        MinorShift.Emuera.GameProc.StartupProfiler.Mark("SelectGame");

        var async = Resources.UnloadUnusedAssets();
        while(!async.isDone)
            yield return null;

        var ow = EmueraContent.instance.option_window;
        ow.gameObject.SetActive(true);
        ow.ShowGameButton(true);
        ow.ShowInProgress(true);
        yield return null;

        // NOTE (Phase 6): forced System.GC.Collect() removed from the critical boot
        // path. The single UnloadUnusedAssets above already reclaims the previous
        // game's assets; a synchronous full GC here only delayed time-to-title.
        // See Docs/STARTUP_REGRESSIONS.md.
        SpriteManager.Init();
        MinorShift.Emuera.GameProc.StartupProfiler.Mark("GamePathPrepared");

        var resolvedGamePath = uEmuera.Utils.NormalizeExistingDirectoryPath(gamePath);
        if (string.IsNullOrEmpty(resolvedGamePath) || !GameDiscovery.IsGameDirectory(resolvedGamePath))
        {
            ow.ShowMessageBoxPublic(MultiLanguage.GetText("[Error]"), "Game directory not found: " + gamePath);
            ow.ShowInProgress(false);
            yield break;
        }

        try
        {
            LoadedFileTracker.Reset(resolvedGamePath);
            Sys.SetGameFolder(resolvedGamePath);
            uEmuera.Utils.ResourcePrepare();
        }
        catch (System.Exception ex)
        {
            ow.ShowInProgress(false);
            ow.ShowMessageBoxPublic(
                MultiLanguage.GetText("[Error]"),
                "Failed to load game: " + gamePath + "\n\n" + ex.Message);
            FirstWindow.Show();
            yield break;
        }

        // NOTE (Phase 6): the second Resources.UnloadUnusedAssets() that used to run
        // here (right before EmueraMain.Run) was removed from the critical path — it
        // duplicated the unload at the top of Run() and stalled time-to-title.
        // See Docs/STARTUP_REGRESSIONS.md.
        try
        {
            EmueraContent.instance.SetNoReady();
            var emuera = Object.FindAnyObjectByType<EmueraMain>();
            if (emuera != null)
                emuera.Run();
        }
        catch (System.Exception ex)
        {
            ow.ShowInProgress(false);
            ow.ShowMessageBoxPublic(
                MultiLanguage.GetText("[Error]"),
                "Failed to load game: " + gamePath + "\n\n" + ex.Message);
            FirstWindow.Show();
        }
    }

    IEnumerator StartSingleGame(string gamePath)
    {
        yield return null;
        var path = gamePath;
        gameObject.SetActive(false);
        yield return Run(path);
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
        
        // Apply dark theme styling
        ApplyDarkTheme();

        setting_.SetActive(true);
        PopulateGameList(true);
    }
    
#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Initializes Android storage access with proper permission handling.
    /// Checks for storage permissions and requests them if not granted.
    /// </summary>
    IEnumerator InitAndroidStorage()
    {
        // Check if we already have permissions
        if (AndroidPermissionManager.HasStoragePermissions())
        {
            // Permissions already granted, load game list
            LoadAndroidGameList();
            yield break;
        }
        
        // Show a dialog explaining why we need permissions
        bool shouldShowRationale = AndroidPermissionManager.ShouldShowPermissionRationale();
        
        if (shouldShowRationale)
        {
            // User previously denied permission, show explanation
            ShowStoragePermissionRationale();
        }
        else
        {
            // First time asking or user didn't select "Don't ask again"
            yield return RequestStoragePermissionsWithUI();
        }
    }
    
    /// <summary>
    /// Shows a dialog explaining why storage permissions are needed.
    /// Provides options to grant permissions or cancel.
    /// </summary>
    void ShowStoragePermissionRationale()
    {
        var ow = EmueraContent.instance.option_window;
        if (ow == null)
            return;
            
        ow.ShowStoragePermissionDialog(
            MultiLanguage.GetText("[StoragePermissionTitle]"),
            MultiLanguage.GetText("[StoragePermissionRationale]"),
            () =>
            {
                // User wants to grant permissions
                GenericUtils.StartCoroutine(RequestStoragePermissionsWithUI());
            }
        );
    }
    
    /// <summary>
    /// Requests storage permissions and handles the result.
    /// </summary>
    IEnumerator RequestStoragePermissionsWithUI()
    {
        bool? permissionResult = null;
        
        yield return AndroidPermissionManager.RequestStoragePermissionsCoroutine((granted) =>
        {
            permissionResult = granted;
        });
        
        if (permissionResult == true)
        {
            // Permissions granted, load game list
            LoadAndroidGameList();
        }
        else
        {
            // Permissions denied, show message with option to open settings
            ShowPermissionDeniedMessage();
        }
    }
    
    /// <summary>
    /// Shows a message when storage permissions are denied.
    /// Provides an option to try again.
    /// </summary>
    void ShowPermissionDeniedMessage()
    {
        var ow = EmueraContent.instance.option_window;
        if (ow == null)
            return;
            
        ow.ShowStoragePermissionDialog(
            MultiLanguage.GetText("[StoragePermissionDeniedTitle]"),
            MultiLanguage.GetText("[StoragePermissionDenied]"),
            () =>
            {
                // Try requesting permissions again
                GenericUtils.StartCoroutine(RequestStoragePermissionsWithUI());
            }
        );
    }
    
    /// <summary>
    /// Predefined Android external storage paths where emuera games may be located.
    /// </summary>
    static readonly string[] AndroidStoragePaths = new string[]
    {
        "/storage/emulated/0/emuera",
        "/storage/emulated/1/emuera",
        "/storage/emulated/2/emuera",
        "/storage/sdcard0/emuera",
        "/storage/sdcard1/emuera",
        "/storage/sdcard2/emuera"
    };
    
    /// <summary>
    /// Loads the game list from Android external storage paths.
    /// </summary>
    void LoadAndroidGameList()
    {
        foreach (var path in AndroidStoragePaths)
        {
            GetList(path);
        }
    }
#endif
    
    /// <summary>
    /// Initializes the directory system for standalone platforms.
    /// Shows the directory selection dialog for manual selection.
    /// (Auto-initialization now handled in Start method)
    /// </summary>
    void InitStandaloneDirectory()
    {
        // Show the directory selection dialog
        GenericUtils.StartCoroutine(ShowDirectoryDialogDelayed());
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
            
        // Normalize and resolve the path across platforms
        var normalized = uEmuera.Utils.NormalizePath(path);
        var resolved = uEmuera.Utils.NormalizeExistingDirectoryPath(normalized);
        var finalPath = string.IsNullOrEmpty(resolved) ? normalized : resolved;
        
        // Validate the directory exists (case-insensitive check)
        if(!uEmuera.Utils.DirectoryExistsInsensitive(finalPath))
        {
            var ow = EmueraContent.instance.option_window;
            ow.ShowMessageBoxPublic(
                MultiLanguage.GetText("[Error]"),
                MultiLanguage.GetText("[DirectoryNotFound]"));
            return;
        }
        
        // Save to PlayerPrefs
        PlayerPrefs.SetString(CUSTOM_DIR_KEY, finalPath);
        PlayerPrefs.Save();
        
        // Refresh the game list
        RefreshGameList();
    }
    
    /// <summary>
    /// Rebuilds the dynamic game rows while retaining the scene-owned picker.
    /// </summary>
    void RebuildGameList()
    {
        EnsureListReferences();
        ClearGameItems();
        PopulateGameList(false);
    }

    void EnsureListReferences()
    {
        if(scroll_rect_ == null)
            scroll_rect_ = GenericUtils.FindChildByName<ScrollRect>(gameObject, "ScrollRect");
        if(item_ == null)
            item_ = GenericUtils.FindChildByName(gameObject, "Item", true);
        if(setting_ == null)
        {
            setting_ = GenericUtils.FindChildByName(gameObject, "optionbtn", true);
            GenericUtils.SetListenerOnClick(setting_, OnOptionClick);
        }
    }

    void ClearGameItems()
    {
        for(var i = 0; i < game_items_.Count; i++)
        {
            if(game_items_[i] != null)
                GameObject.Destroy(game_items_[i]);
        }
        game_items_.Clear();
        listed_game_paths_.Clear();
        itemcount_ = 0;

        if(scroll_rect_ != null && scroll_rect_.content != null)
        {
            var size = scroll_rect_.content.sizeDelta;
            size.y = 0f;
            scroll_rect_.content.sizeDelta = size;
        }
    }

    void PopulateGameList(bool openDirectoryDialog)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        HideWebGLDownloadButton();
#endif
        GetList(Application.persistentDataPath);

#if UNITY_EDITOR
        var main_entry = Object.FindAnyObjectByType<MainEntry>();
        if(main_entry != null && !string.IsNullOrEmpty(main_entry.era_path))
            GetList(main_entry.era_path);

        // Keep editor game data in a project-local, non-Assets directory.
        var editorGamesRoot = GetEditorGamesRoot();
        Directory.CreateDirectory(editorGamesRoot);
        GetList(editorGamesRoot);

        string customDir = PlayerPrefs.GetString(CUSTOM_DIR_KEY, "");
        if(!string.IsNullOrEmpty(customDir) && uEmuera.Utils.DirectoryExistsInsensitive(customDir))
        {
            var normalizedCustomDir = uEmuera.Utils.NormalizeExistingDirectoryPath(customDir);
            if(!string.Equals(normalizedCustomDir, editorGamesRoot, System.StringComparison.OrdinalIgnoreCase))
                GetList(normalizedCustomDir);
        }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        GenericUtils.StartCoroutine(InitAndroidStorage());
#endif
#if UNITY_STANDALONE && !UNITY_EDITOR
        string customDir = PlayerPrefs.GetString(CUSTOM_DIR_KEY, "");
        if(!string.IsNullOrEmpty(customDir) && uEmuera.Utils.DirectoryExistsInsensitive(customDir))
        {
            var normalized = uEmuera.Utils.NormalizeExistingDirectoryPath(customDir);
            if(!openDirectoryDialog || !TryAutoStart(normalized))
                GetList(normalized);
        }
        else
        {
            var executableRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if(openDirectoryDialog && TryAutoStart(executableRoot))
                return;

            GetList(executableRoot);
            if(openDirectoryDialog)
                InitStandaloneDirectory();
        }
#endif
    }

    /// <summary>
    /// Refreshes the game list by reusing the scene-owned picker shell.
    /// </summary>
    public void RefreshGameList()
    {
        GenericUtils.StartCoroutine(RefreshGameListCoroutine());
    }

    System.Collections.IEnumerator RefreshGameListCoroutine()
    {
        EnsureListReferences();
        ClearGameItems();
        yield return null;
        PopulateGameList(false);
    }

    void OnOptionClick()
    {
        var ow = EmueraContent.instance.option_window;
        ow.ShowMenu();
    }
    
    /// <summary>
    /// Applies dark theme styling to the FirstWindow UI.
    /// </summary>
    void ApplyDarkTheme()
    {
        // Apply dark theme to the entire window
        UIStyleManager.ApplyDarkTheme(gameObject);
        
        // Specifically style the title bar
        if (titlebar != null)
        {
            titlebar.color = UIStyleManager.DarkTheme.TextPrimary;
            UIStyleManager.AddTextShadow(titlebar);
        }
        
        // Style the version text
        var versionText = GenericUtils.FindChildByName<Text>(gameObject, "version");
        if (versionText != null)
        {
            versionText.color = UIStyleManager.DarkTheme.TextSecondary;
        }
        
        // Style the scroll view background
        var scrollBg = GenericUtils.FindChildByName<UnityEngine.UI.Image>(gameObject, "ScrollRect");
        if (scrollBg != null)
        {
            scrollBg.color = UIStyleManager.DarkTheme.BackgroundDark;
        }
        
        // Style the main background
        var mainBg = GetComponent<UnityEngine.UI.Image>();
        if (mainBg != null)
        {
            mainBg.color = UIStyleManager.DarkTheme.BackgroundMedium;
        }
    }

    /// <summary>
    /// Adds a game item to the list.
    /// </summary>
    /// <param name="gamePath">The complete path to the game directory.</param>
    void AddItem(string gamePath)
    {
        if(!listed_game_paths_.Add(gamePath))
            return;

        var rrt = item_.transform as UnityEngine.RectTransform;
        var obj = GameObject.Instantiate(item_);
        var text = GenericUtils.FindChildByName<UnityEngine.UI.Text>(obj, "name");
        text.text = Path.GetFileName(gamePath);
        text = GenericUtils.FindChildByName<UnityEngine.UI.Text>(obj, "path");
        text.text = gamePath;

        GenericUtils.SetListenerOnClick(obj, () =>
        {
            gameObject.SetActive(false);
            GenericUtils.StartCoroutine(Run(gamePath));
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
        game_items_.Add(obj);
        obj.SetActive(true);
    }

    /// <summary>
    /// Adds a game item from a multi-runtime <see cref="uEmuera.Runtime.GameDescriptor"/>.
    /// Shows a runtime badge and routes Emuera / EraElectron games to the correct runtime.
    /// </summary>
    void AddItemDescriptor(uEmuera.Runtime.GameDescriptor descriptor)
    {
        if (descriptor == null) return;
        string root = descriptor.GameRoot ?? "";
        if (!listed_game_paths_.Add(root)) return;

        string badge = descriptor.RuntimeKind == uEmuera.Runtime.RuntimeKind.EraElectron
            ? "[EraElectron] "
            : "";
        string title = string.IsNullOrEmpty(descriptor.Title)
            ? Path.GetFileName(root.TrimEnd('/', '\\'))
            : descriptor.Title;

        var rrt = item_.transform as UnityEngine.RectTransform;
        var obj = GameObject.Instantiate(item_);
        var nameText = GenericUtils.FindChildByName<UnityEngine.UI.Text>(obj, "name");
        nameText.text = badge + title;
        var pathText = GenericUtils.FindChildByName<UnityEngine.UI.Text>(obj, "path");
        pathText.text = root;

        // Capture for closure
        var desc = descriptor;
        GenericUtils.SetListenerOnClick(obj, () =>
        {
            gameObject.SetActive(false);
            if (desc.RuntimeKind == uEmuera.Runtime.RuntimeKind.EraElectron)
                GenericUtils.StartCoroutine(LaunchEreGameCoroutine(desc));
            else
                GenericUtils.StartCoroutine(Run(desc.GameRoot));
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
        if (ih > content.sizeDelta.y)
            content.sizeDelta = new Vector2(content.sizeDelta.x, ih);

        game_items_.Add(obj);
        obj.SetActive(true);
    }

    /// <summary>
    /// Launches an EraElectron game via <see cref="uEmuera.Runtime.GameRuntimeManager"/>.
    /// Currently routes to the EraElectronRuntime stub (logs STUB warning).
    /// Full implementation requires WebView host spike (see Docs/ADR/WEB_RUNTIME_HOST.md).
    /// </summary>
    System.Collections.IEnumerator LaunchEreGameCoroutine(uEmuera.Runtime.GameDescriptor descriptor)
    {
        var ctx = new uEmuera.Runtime.RuntimeContext
        {
            SessionId = System.Guid.NewGuid().ToString("N"),
            Logger    = new uEmuera.Runtime.UnityRuntimeLogger("[EraElectron]"),
        };

        var task = uEmuera.Runtime.GameRuntimeManager.Instance
            .LaunchAsync(descriptor, ctx);

        // Poll until the async task completes, yielding each frame.
        while (!task.IsCompleted)
            yield return null;

        if (task.IsFaulted)
        {
            var ex = task.Exception?.GetBaseException();
            string userMsg = ex is System.NotSupportedException
                ? ex.Message
                : "EraElectron launch failed.\n\n" + ex?.Message;
            uEmuera.Logger.Error("[FirstWindow] EraElectron launch failed: " + ex?.Message);
            var ow = EmueraContent.instance?.option_window;
            if (ow != null)
                ow.ShowMessageBoxPublic(
                    "EraElectron",
                    userMsg);
            gameObject.SetActive(true);
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    void OnWebGLImportClick()
    {
        WebGLGameImporter.RequestFolder(gameObject.name);
    }

    void HideWebGLDownloadButton()
    {
        var names = new[] { "download", "Download", "downloadbtn", "DownloadButton" };
        for(var i = 0; i < names.Length; i++)
        {
            var download = GenericUtils.FindChildByName(gameObject, names[i], true);
            if(download == null)
                continue;

            var button = download.GetComponent<Button>();
            if(button != null)
                button.interactable = false;
            download.SetActive(false);
        }
    }

    /// <summary>
    /// Called by the JavaScript folder importer after files have been copied into
    /// Unity's browser-persistent virtual filesystem.
    /// </summary>
    public void OnWebGLFolderImportFinished(string status)
    {
        if (string.Equals(status, "ok", System.StringComparison.OrdinalIgnoreCase))
        {
            RefreshGameList();
            return;
        }

        if (!string.Equals(status, "cancelled", System.StringComparison.OrdinalIgnoreCase))
        {
            var ow = EmueraContent.instance != null ? EmueraContent.instance.option_window : null;
            if (ow != null)
                ow.ShowMessageBoxPublic(MultiLanguage.GetText("[Error]"), "WebGL game import failed: " + status);
        }
    }
#endif

#if UNITY_EDITOR
    static string GetEditorGamesRoot()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".games"));
    }
#endif

    /// <summary>
    /// Tries the packaged-build convention: exactly one game in the executable
    /// directory or in its one-level game/ directory is started automatically.
    /// Ambiguous or empty roots remain in the picker.
    /// </summary>
    bool TryAutoStart(string root)
    {
#if UNITY_WEBGL
        return false;
#else
        var gamePath = GameDiscovery.FindSingle(root);
        if (string.IsNullOrEmpty(gamePath))
            return false;

        GenericUtils.StartCoroutine(StartSingleGame(gamePath));
        return true;
#endif
    }

    /// <summary>
    /// Gets the list of ERA games from a workspace directory.
    /// </summary>
    /// <param name="workspace">The workspace path to scan.</param>
    void GetList(string workspace)
    {
        // Multi-runtime detection: GameDetector recognises both Emuera and
        // EraElectron packages. GameDiscovery (Emuera-only) is kept for
        // backward-compatible callers that bypass GetList directly.
        var games = uEmuera.Runtime.Detection.GameDetector.CreateDefault()
            .DiscoverAll(workspace);
        for (var i = 0; i < games.Count; i++)
            AddItemDescriptor(games[i]);
    }

    /// <summary>
    /// Title bar text component.
    /// </summary>
    [Tooltip("Title bar text component")]
    public Text titlebar = null;
    
    ScrollRect scroll_rect_ = null;
    GameObject item_ = null;
    GameObject setting_ = null;
    readonly List<GameObject> game_items_ = new List<GameObject>();
    readonly HashSet<string> listed_game_paths_ = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    int itemcount_ = 0;
}
