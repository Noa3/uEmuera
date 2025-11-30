using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-Tool zum automatischen Hinzufügen von LocalizedTextComponent zu Text-GameObjects
/// </summary>
public class LocalizationSetupHelper : EditorWindow
{
    [MenuItem("Tools/Localization/Setup GameObjects")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationSetupHelper>("Localization Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Localization Setup Helper", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Add LocalizedTextComponent to FirstWindow"))
        {
            SetupFirstWindow();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Add LocalizedTextComponent to Options"))
        {
            SetupOptions();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Setup All Prefabs"))
        {
            SetupFirstWindow();
            SetupOptions();
        }
    }

    private static void SetupFirstWindow()
    {
        string prefabPath = "Assets/Resources/Prefab/FirstWindow.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"FirstWindow prefab not found at {prefabPath}");
            return;
        }

        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        
        try
        {
            // Titlebar
            Transform titlebar = FindDeepChild(instance.transform, "titlebar");
            if (titlebar != null)
            {
                AddLocalizedComponent(titlebar.gameObject, "UI", "FirstWindow.Titlebar.title");
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Debug.Log("FirstWindow localization setup completed!");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(instance);
        }
    }

    private static void SetupOptions()
    {
        string prefabPath = "Assets/Resources/Prefab/Options.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogError($"Options prefab not found at {prefabPath}");
            return;
        }

        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);
        
        try
        {
            // Menu 1
            SetupMenuItem(instance, "MenuPad/Menu1/resolution", "Options.MenuPad.Menu1.resolution.Text");
            SetupMenuItem(instance, "MenuPad/Menu1/language", "Options.MenuPad.Menu1.language.Text");
            SetupMenuItem(instance, "MenuPad/Menu1/github", "Options.MenuPad.Menu1.github.Text");
            SetupMenuItem(instance, "MenuPad/Menu1/exit", "Options.MenuPad.Menu1.exit.Text");

            // Menu 2
            SetupMenuItem(instance, "MenuPad/Menu2/back", "Options.MenuPad.Menu2.back.Text");
            SetupMenuItem(instance, "MenuPad/Menu2/intent", "Options.MenuPad.Menu2.intent.Text");
            SetupMenuItem(instance, "MenuPad/Menu2/savelog", "Options.MenuPad.Menu2.savelog.Text");
            SetupMenuItem(instance, "MenuPad/Menu2/gototitle", "Options.MenuPad.Menu2.gototitle.Text");
            SetupMenuItem(instance, "MenuPad/Menu2/restart", "Options.MenuPad.Menu2.restart.Text");
            SetupMenuItem(instance, "MenuPad/Menu2/exit", "Options.MenuPad.Menu2.exit.Text");

            // Resolution Pad
            SetupMenuItem(instance, "resolution_pad/1080p", "Options.Resolution.Pad.1080p.Text");
            SetupMenuItem(instance, "resolution_pad/900p", "Options.Resolution.Pad.900p.Text");
            SetupMenuItem(instance, "resolution_pad/720p", "Options.Resolution.Pad.720p.Text");
            SetupMenuItem(instance, "resolution_pad/540p", "Options.Resolution.Pad.540p.Text");

            // Language Box
            Transform langBox = FindDeepChild(instance.transform, "language_box");
            if (langBox != null)
            {
                SetupMenuItemRelative(langBox, "border/titlebar/title", "Options.LanguageBox.border.titlebar.title");
                SetupMenuItemRelative(langBox, "border/zh_cn/Text", "Options.LanguageBox.border.zh_cn.Text");
                SetupMenuItemRelative(langBox, "border/jp/Text", "Options.LanguageBox.border.jp.Text");
                SetupMenuItemRelative(langBox, "border/en_us/Text", "Options.LanguageBox.border.en_us.Text");
            }

            // Intent Box
            Transform intentBox = FindDeepChild(instance.transform, "intentbox");
            if (intentBox != null)
            {
                SetupMenuItemRelative(intentBox, "border/titlebar/title", "Options.IntentBox.border.titlebar.title");
                SetupMenuItemRelative(intentBox, "border/L_left/Text", "Options.IntentBox.border.L_left.Text");
                SetupMenuItemRelative(intentBox, "border/L_right/Text", "Options.IntentBox.border.L_right.Text");
                SetupMenuItemRelative(intentBox, "border/R_left/Text", "Options.IntentBox.border.R_left.Text");
                SetupMenuItemRelative(intentBox, "border/R_right/Text", "Options.IntentBox.border.R_right.Text");
                SetupMenuItemRelative(intentBox, "border/close/Text", "Options.IntentBox.border.close.Text");
                SetupMenuItemRelative(intentBox, "border/reset/Text", "Options.IntentBox.border.reset.Text");
            }

            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Debug.Log("Options localization setup completed!");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(instance);
        }
    }

    private static void SetupMenuItem(GameObject root, string path, string key)
    {
        Transform transform = FindDeepChild(root.transform, path);
        if (transform != null)
        {
            AddLocalizedComponent(transform.gameObject, "UI", key);
        }
        else
        {
            Debug.LogWarning($"Could not find: {path}");
        }
    }

    private static void SetupMenuItemRelative(Transform parent, string relativePath, string key)
    {
        Transform transform = parent.Find(relativePath);
        if (transform != null)
        {
            AddLocalizedComponent(transform.gameObject, "UI", key);
        }
        else
        {
            Debug.LogWarning($"Could not find: {relativePath} under {parent.name}");
        }
    }

    private static void AddLocalizedComponent(GameObject obj, string tableName, string key)
    {
        Text textComponent = obj.GetComponent<Text>();
        if (textComponent == null)
        {
            Debug.LogWarning($"No Text component found on {obj.name}");
            return;
        }

        LocalizedTextComponent localizedComp = obj.GetComponent<LocalizedTextComponent>();
        if (localizedComp == null)
        {
            localizedComp = obj.AddComponent<LocalizedTextComponent>();
        }

        SerializedObject serializedObject = new SerializedObject(localizedComp);
        serializedObject.FindProperty("tableName").stringValue = tableName;
        serializedObject.FindProperty("key").stringValue = key;
        serializedObject.FindProperty("useLegacyForBracketKeys").boolValue = false;
        serializedObject.ApplyModifiedProperties();

        Debug.Log($"Added LocalizedTextComponent to {obj.name} with key: {key}");
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
#endif
