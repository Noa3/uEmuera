using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages the option window UI including menus, buttons, and settings dialogs.
/// </summary>
public class OptionWindow : MonoBehaviour
{
	// Use this for initialization
	void Start ()
    {
        GenericUtils.SetListenerOnClick(quick_button.gameObject, OnQuickButtonClick);
        GenericUtils.SetListenerOnClick(input_button.gameObject, OnInputPadButtonClick);
        GenericUtils.SetListenerOnClick(magnifier_button.gameObject, OnScalePadButtonClick);
        GenericUtils.SetListenerOnClick(option_button.gameObject, OnShowMenu2);

        orientation_lock_image = orientation_lock_button.GetComponent<Image>();
        GenericUtils.SetListenerOnClick(orientation_lock_button.gameObject, OnLockOrientationClick);

        GenericUtils.SetListenerOnClick(msg_confirm, OnMsgConfirm);
        GenericUtils.SetListenerOnClick(msg_cancel, OnMsgCancel);

        GenericUtils.SetListenerOnClick(menu_pad, OnMenuPad);
        GenericUtils.SetListenerOnClick(menu_1_resolution, OnMenuResolution);
        GenericUtils.SetListenerOnClick(menu_1_language, ShowLanguageBox);
        GenericUtils.SetListenerOnClick(menu_1_settings, ShowSettingsBox);
        // Only show directory option on standalone platforms (Windows, Linux, macOS)
        // Hide on Android
        if(menu_1_directory != null)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            menu_1_directory.SetActive(true);
            GenericUtils.SetListenerOnClick(menu_1_directory, OnMenuDirectory);
#else
            // Hide directory button on Android and other non-standalone platforms
            menu_1_directory.SetActive(false);
#endif
        }
        GenericUtils.SetListenerOnClick(menu_1_github, OnGithub);
        GenericUtils.SetListenerOnClick(menu_1_exit, OnMenuExit);

        GenericUtils.SetListenerOnClick(menu_2_back, OnMenu2Back);
        GenericUtils.SetListenerOnClick(menu_2_restart, OnMenu2Restart);
        GenericUtils.SetListenerOnClick(menu_2_gototitle, OnMenuGotoTitle);
        GenericUtils.SetListenerOnClick(menu_2_savelog, OnMenuSaveLog);
        GenericUtils.SetListenerOnClick(menu_2_intent, OnIntentBoxShow);
        GenericUtils.SetListenerOnClick(menu_2_exit, OnMenuExit);

        GenericUtils.SetListenerOnClick(resolution_pad, OnResolutionOut);
        GenericUtils.SetListenerOnClick(resolution_1080p, OnResolution1080p);
        GenericUtils.SetListenerOnClick(resolution_900p, OnResolution900p);
        GenericUtils.SetListenerOnClick(resolution_720p, OnResolution720p);
        GenericUtils.SetListenerOnClick(resolution_540p, OnResolution540p);

        GenericUtils.SetListenerOnClick(language_zhcn, OnSelectLanguage);
        GenericUtils.SetListenerOnClick(language_jp, OnSelectLanguage);
        GenericUtils.SetListenerOnClick(language_enus, OnSelectLanguage);

        GenericUtils.SetListenerOnClick(intentbox_L_left, OnIntentLLeft);
        GenericUtils.SetListenerOnClick(intentbox_L_right, OnIntentLRight);
        GenericUtils.SetListenerOnClick(intentbox_R_left, OnIntentRLeft);
        GenericUtils.SetListenerOnClick(intentbox_R_right, OnIntentRRight);
        GenericUtils.SetListenerOnClick(intentbox_close, OnIntentClose);
        GenericUtils.SetListenerOnClick(intentbox_reset, OnIntentReset);

        // Initialize settings box
        InitSettingsBox();
        
        // Initialize directory input box for standalone platforms
        InitDirectoryInputBox();
        
        // Apply dark theme to menus
        ApplyDarkThemeToMenus();

        HideResolutionIcon();
        switch(ResolutionHelper.resolution_index)
        {
        case 2:
            resolution_900p_icon.SetActive(true);
            break;
        case 3:
            resolution_720p_icon.SetActive(true);
            break;
        case 4:
            resolution_540p_icon.SetActive(true);
            break;
        case 1:
        default:
            resolution_1080p_icon.SetActive(true);
            break;
        }
    }

    void OnQuickButtonClick()
    {
        if(quick_buttons.IsShow)
        {
            quick_buttons.Hide();
            SwitchButton(-1);
        }
        else
        {
            input_pad.Hide();
            scale_pad.Hide();
            quick_buttons.Show();
            EmueraContent.instance.SetLastButtonGeneration(
                EmueraContent.instance.button_generation);

            SwitchButton(0);
        }
    }
    void OnInputPadButtonClick()
    {
        if(input_pad.IsShow)
        {
            input_pad.Hide();
            SwitchButton(-1);
        }
        else
        {
            quick_buttons.Hide();
            scale_pad.Hide();
            input_pad.Show();
            SwitchButton(1);
        }
    }
    void OnScalePadButtonClick()
    {
        if(scale_pad.IsShow)
        {
            scale_pad.Hide();
            SwitchButton(-1);
        }
        else
        {
            quick_buttons.Hide();
            input_pad.Hide();
            scale_pad.Show();
            SwitchButton(2);
        }
    }
    void OnLockOrientationClick()
    {
        if(auto_rotation)
        {
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            orientation_lock_image.sprite = lock_sprite;
        }
        else
        {
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            orientation_lock_image.sprite = unlock_sprite;
        }
    }

    public void ShowMenu()
    {
        menu_pad.SetActive(true);
        menu_1.SetActive(true);
    }

    void OnShowMenu2()
    {
        menu_pad.SetActive(true);
        menu_2.SetActive(true);
    }
    void OnMenu2Back()
    {
        if(EmueraThread.instance.Running())
        {
            ShowMessageBox(
                MultiLanguage.GetText("[Wait]"), 
                MultiLanguage.GetText("[WaitContent]"));
        }
        else
        {
            ShowMessageBox(
                MultiLanguage.GetText("[BackMenu]"),
                MultiLanguage.GetText("[BackMenuContent]"),
                () =>
                {
                    var emuera = Object.FindFirstObjectByType<EmueraMain>();
                    emuera.Clear();
                }, () => { });
        }
        HideMenu();
    }
    void OnMenu2Restart()
    {
        if(EmueraThread.instance.Running())
        {
            ShowMessageBox(
                MultiLanguage.GetText("[Wait]"),
                MultiLanguage.GetText("[WaitContent]"));
        }
        else
        {
            ShowMessageBox(
                MultiLanguage.GetText("[ReloadGame]"),
                MultiLanguage.GetText("[ReloadGameContent]"),
            () =>
            {
                var emuera = Object.FindFirstObjectByType<EmueraMain>();
                emuera.Restart();
            }, () => { });
        }
        HideMenu();
    }
    void OnMenuGotoTitle()
    {
        if(EmueraThread.instance.Running())
        {
            ShowMessageBox(
                MultiLanguage.GetText("[Wait]"),
                MultiLanguage.GetText("[WaitContent]"));
        }
        else
        {
            ShowMessageBox(
                MultiLanguage.GetText("[BackTitle]"),
                MultiLanguage.GetText("[BackTitleContent]"),
            () =>
            {
                MinorShift.Emuera.GlobalStatic.Console.GotoTitle();
            }, () => { });
        }
        HideMenu();
    }
    void OnMenuSaveLog()
    {
        var path = MinorShift.Emuera.Program.ExeDir;
        System.DateTime time = System.DateTime.Now;
        string fname = time.ToString("yyyyMMdd-HHmmss");
        path = path + fname + ".log";
        bool result = MinorShift.Emuera.GlobalStatic.Console.OutputLog(path);

        ShowMessageBox(MultiLanguage.GetText("[SaveLog]"), 
            result ? string.Format("{1}：\n{0}", path, MultiLanguage.GetText("[SavePath]")) : MultiLanguage.GetText("[Failure]"));
        HideMenu();
    }
    void OnMenuResolution()
    {
        resolution_pad.SetActive(true);
        HideMenu();
    }
    
    /// <summary>
    /// Called when the directory menu item is clicked.
    /// Shows the directory selection dialog. Only available on standalone platforms.
    /// </summary>
    void OnMenuDirectory()
    {
        HideMenu();
        if(FirstWindow.instance != null)
        {
            FirstWindow.instance.ShowDirectoryDialog();
        }
    }
    
    void OnMenuExit()
    {
        ShowMessageBox(
            MultiLanguage.GetText("[Exit]"),
            MultiLanguage.GetText("[ExitContent]"), 
            ()=> {
                Application.Quit();
            }, ()=> { });
        HideMenu();
    }

    void OnMenuPad()
    {
        HideMenu();
    }

    void OnResolutionOut()
    {
        resolution_pad.SetActive(false);
    }

    void OnResolution1080p()
    {
        ResolutionHelper.resolution_index = 1;
        ResolutionHelper.Apply();
        HideResolutionIcon();
        resolution_1080p_icon.SetActive(true);
    }

    void OnResolution900p()
    {
        ResolutionHelper.resolution_index = 2;
        ResolutionHelper.Apply();
        HideResolutionIcon();
        resolution_900p_icon.SetActive(true);
    }

    void OnResolution720p()
    {
        ResolutionHelper.resolution_index = 3;
        ResolutionHelper.Apply();
        HideResolutionIcon();
        resolution_720p_icon.SetActive(true);
    }

    void OnResolution540p()
    {
        ResolutionHelper.resolution_index = 4;
        ResolutionHelper.Apply();
        HideResolutionIcon();
        resolution_540p_icon.SetActive(true);
    }

    void HideResolutionIcon()
    {
        resolution_1080p_icon.SetActive(false);
        resolution_900p_icon.SetActive(false);
        resolution_720p_icon.SetActive(false);
        resolution_540p_icon.SetActive(false);
    }

    public void Ready()
    {
        var texts = inprogress.GetComponentsInChildren<Text>();
        var length = texts.Length;
        for(int i=0; i<length; ++i)
        {
            var text = texts[i];
            text.color = EmueraBehaviour.FontColor;
        }

        var buttoncolor = EmueraBehaviour.FontColor;
        buttoncolor.a = 0.6f;
        quick_button.GetComponent<Image>().color = buttoncolor;
        input_button.GetComponent<Image>().color = buttoncolor;
        magnifier_button.GetComponent<Image>().color = buttoncolor;
        option_button.GetComponent<Image>().color = buttoncolor;
        orientation_lock_image.color = buttoncolor;
        scale_pad.SetColor(buttoncolor);
        input_pad.SetColor(buttoncolor, 
            GenericUtils.ToUnityColor(MinorShift.Emuera.Config.BackColor));

        buttoncolor.a = 1.0f;
        button_shadows = new List<Shadow>();
        var shadow = quick_button.GetComponent<Shadow>();
        shadow.effectColor = buttoncolor;
        button_shadows.Add(shadow);
        shadow = input_button.GetComponent<Shadow>();
        shadow.effectColor = buttoncolor;
        button_shadows.Add(shadow);
        shadow = magnifier_button.GetComponent<Shadow>();
        shadow.effectColor = buttoncolor;
        button_shadows.Add(shadow);
        shadow = option_button.GetComponent<Shadow>();
        shadow.effectColor = buttoncolor;
        button_shadows.Add(shadow);
    }

    public void ShowGameButton(bool value)
    {
        game_button.SetActive(value);
        if(auto_rotation)
            orientation_lock_image.sprite = unlock_sprite;
        else
            orientation_lock_image.sprite = lock_sprite;
    }

    public void ShowInProgress(bool value)
    {
        inprogress.SetActive(value);
    }

    void SwitchButton(int index)
    {
        for(int i=0; i < button_shadows.Count; ++i)
        {
            var shadow = button_shadows[i];
            shadow.enabled = (i == index);
        }
    }

    void HideMenu()
    {
        menu_1.SetActive(false);
        menu_2.SetActive(false);
        menu_pad.SetActive(false);
    }

    void ShowMessageBox(string title, string content,
        System.Action confirm_callback = null,
        System.Action cancel_callback = null)
    {
        msg_confirm_callback = confirm_callback;
        msg_cancel_callback = cancel_callback;
        msg_cancel.SetActive(msg_cancel_callback != null);
        msg_title.text = title;
        msg_content.text = content;
        msg_box.SetActive(true);
    }
    void HideMessageBox()
    {
        msg_title.text = "";
        msg_content.text = "";
        msg_confirm_callback = null;
        msg_cancel_callback = null;
        msg_box.SetActive(false);
    }
    void OnMsgConfirm()
    {
        if(msg_confirm_callback != null)
            msg_confirm_callback();
        HideMessageBox();
    }
    void OnMsgCancel()
    {
        if(msg_cancel_callback != null)
            msg_cancel_callback();
        HideMessageBox();
    }

    public void ShowLanguageBox()
    {
        HideMenu();
        language_box.SetActive(true);
    }
    void OnSelectLanguage(PointerEventData e)
    {
        MultiLanguage.SetLanguage(e.pointerPress.name);
        language_box.SetActive(false);
    }
    
    /// <summary>
    /// Shows the settings dialog box.
    /// </summary>
    public void ShowSettingsBox()
    {
        HideMenu();
        if (settings_box != null)
        {
            settings_box.SetActive(true);
            UpdatePostProcessingToggle();
        }
    }
    
    /// <summary>
    /// Initializes the settings box by wiring up button events.
    /// </summary>
    void InitSettingsBox()
    {
        if (settings_box == null)
            return;
            
        settings_box.SetActive(false);
        
        if (settings_close != null)
        {
            GenericUtils.SetListenerOnClick(settings_close, OnSettingsClose);
        }
        
        if (settings_postprocessing_toggle != null)
        {
            GenericUtils.SetListenerOnClick(settings_postprocessing_toggle, OnPostProcessingToggle);
        }
    }
    
    /// <summary>
    /// Updates the post-processing toggle visual state.
    /// </summary>
    void UpdatePostProcessingToggle()
    {
        if (PostProcessingManager.instance == null)
            return;
            
        bool isEnabled = PostProcessingManager.instance.IsEnabled();
        
        if (settings_postprocessing_icon != null)
        {
            settings_postprocessing_icon.SetActive(isEnabled);
        }
        
        if (settings_postprocessing_text != null)
        {
            settings_postprocessing_text.text = isEnabled ? 
                MultiLanguage.GetText("[PostProcessingOn]") : 
                MultiLanguage.GetText("[PostProcessingOff]");
        }
    }
    
    /// <summary>
    /// Called when the settings close button is clicked.
    /// </summary>
    void OnSettingsClose()
    {
        if (settings_box != null)
        {
            settings_box.SetActive(false);
        }
    }
    
    /// <summary>
    /// Called when the post-processing toggle button is clicked.
    /// </summary>
    void OnPostProcessingToggle()
    {
        if (PostProcessingManager.instance != null)
        {
            PostProcessingManager.instance.TogglePostProcessing();
            UpdatePostProcessingToggle();
        }
    }
    
    /// <summary>
    /// Applies dark theme styling to all menu elements.
    /// </summary>
    void ApplyDarkThemeToMenus()
    {
        // Style menu pad (background overlay)
        if (menu_pad != null)
        {
            var image = menu_pad.GetComponent<Image>();
            if (image != null)
            {
                // Semi-transparent dark overlay
                image.color = new Color(0f, 0f, 0f, 0.85f);
            }
        }
        
        // Style Menu 1
        if (menu_1 != null)
        {
            UIStyleManager.ApplyDarkTheme(menu_1);
            StyleMenuPanel(menu_1);
        }
        
        // Style Menu 2
        if (menu_2 != null)
        {
            UIStyleManager.ApplyDarkTheme(menu_2);
            StyleMenuPanel(menu_2);
        }
        
        // Style resolution panel
        if (resolution_pad != null)
        {
            var image = resolution_pad.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0f, 0f, 0f, 0.85f);
            }
            
            var contentPanel = GenericUtils.FindChildByName(resolution_pad, "border");
            if (contentPanel != null)
            {
                UIStyleManager.ApplyDarkTheme(contentPanel);
                StyleMenuPanel(contentPanel);
            }
        }
        
        // Style language box
        if (language_box != null)
        {
            UIStyleManager.ApplyDarkTheme(language_box);
            var border = GenericUtils.FindChildByName(language_box, "border");
            if (border != null)
            {
                StyleMenuPanel(border);
            }
        }
        
        // Style intent box
        if (intentbox != null)
        {
            UIStyleManager.ApplyDarkTheme(intentbox);
        }
        
        // Style message box
        if (msg_box != null)
        {
            UIStyleManager.ApplyDarkTheme(msg_box);
            var border = GenericUtils.FindChildByName(msg_box, "border");
            if (border != null)
            {
                StyleMenuPanel(border);
            }
        }
        
        // Style directory input box
        if (directoryInputBox != null)
        {
            UIStyleManager.ApplyDarkTheme(directoryInputBox);
        }
        
        // Style settings box
        if (settings_box != null)
        {
            UIStyleManager.ApplyDarkTheme(settings_box);
            var border = GenericUtils.FindChildByName(settings_box, "border");
            if (border != null)
            {
                StyleMenuPanel(border);
            }
        }
    }
    
    /// <summary>
    /// Styles a menu panel with dark theme colors.
    /// </summary>
    /// <param name="panel">The panel GameObject to style.</param>
    void StyleMenuPanel(GameObject panel)
    {
        if (panel == null)
            return;
            
        var image = panel.GetComponent<Image>();
        if (image != null)
        {
            image.color = UIStyleManager.DarkTheme.BackgroundMedium;
        }
        
        // Add subtle outline/border effect
        var outline = panel.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panel.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = UIStyleManager.DarkTheme.Border;
        outline.effectDistance = new Vector2(2f, -2f);
    }

    void OnGithub()
    {
        Application.OpenURL("https://github.com/noa3/uEmuera/releases");
    }

    void OnIntentBoxShow()
    {
        intentbox.SetActive(true);
        HideMenu();
    }
    void OnIntentLLeft()
    {
        int value = PlayerPrefs.GetInt("IntentBox_L", 0);
        value -= 1;
        if(value < 0)
            value = 0;
        PlayerPrefs.SetInt("IntentBox_L", value);
        intentbox_L_text.text = value.ToString();

        EmueraContent.instance.SetIntentBox(PlayerPrefs.GetInt("IntentBox_L", 0),
                                            PlayerPrefs.GetInt("IntentBox_R", 0));
    }
    void OnIntentLRight()
    {
        int value = PlayerPrefs.GetInt("IntentBox_L", 0);
        value += 1;
        if(value > 99)
            value = 99;
        PlayerPrefs.SetInt("IntentBox_L", value);
        intentbox_L_text.text = value.ToString();

        EmueraContent.instance.SetIntentBox(PlayerPrefs.GetInt("IntentBox_L", 0),
                                            PlayerPrefs.GetInt("IntentBox_R", 0));
    }
    void OnIntentRLeft()
    {
        int value = PlayerPrefs.GetInt("IntentBox_R", 0);
        value += 1;
        if(value > 99)
            value = 99;
        PlayerPrefs.SetInt("IntentBox_R", value);
        intentbox_R_text.text = value.ToString();

        EmueraContent.instance.SetIntentBox(PlayerPrefs.GetInt("IntentBox_L", 0),
                                            PlayerPrefs.GetInt("IntentBox_R", 0));
    }
    void OnIntentRRight()
    {
        int value = PlayerPrefs.GetInt("IntentBox_R", 0);
        value -= 1;
        if(value < 0)
            value = 0;
        PlayerPrefs.SetInt("IntentBox_R", value);
        intentbox_R_text.text = value.ToString();

        EmueraContent.instance.SetIntentBox(PlayerPrefs.GetInt("IntentBox_L", 0),
                                            PlayerPrefs.GetInt("IntentBox_R", 0));
    }
    void OnIntentClose()
    {
        intentbox.SetActive(false);
    }
    void OnIntentReset()
    {
        PlayerPrefs.SetInt("IntentBox_L", 0);
        PlayerPrefs.SetInt("IntentBox_R", 0);
        intentbox_L_text.text = "0";
        intentbox_R_text.text = "0";
        EmueraContent.instance.SetIntentBox(0, 0);
    }
    
    #region Directory Input Box
    
    /// <summary>
    /// Callback for when directory is confirmed.
    /// </summary>
    private System.Action<string> dir_input_callback_;
    
    /// <summary>
    /// Shows a public message box (wrapper for ShowMessageBox).
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="content">The content of the message box.</param>
    public void ShowMessageBoxPublic(string title, string content)
    {
        ShowMessageBox(title, content);
    }
    
    /// <summary>
    /// Initializes the directory input box by wiring up button events.
    /// Called from Start(). Only active on standalone platforms (Windows, Linux, macOS).
    /// </summary>
    void InitDirectoryInputBox()
    {
        if(directoryInputBox == null)
            return;

        // Ensure directory box starts hidden on all platforms
        directoryInputBox.SetActive(false);
        
#if UNITY_STANDALONE || UNITY_EDITOR
        // Wire up confirm button (only on standalone platforms)
        if(directoryInputConfirm != null)
        {
            GenericUtils.SetListenerOnClick(directoryInputConfirm, OnDirInputConfirm);
        }
        
        // Wire up cancel button (only on standalone platforms)
        if(directoryInputCancel != null)
        {
            GenericUtils.SetListenerOnClick(directoryInputCancel, OnDirInputCancel);
        }
#endif
    }
    
    /// <summary>
    /// Shows the directory input dialog box.
    /// Only available on standalone platforms (Windows, Linux, macOS).
    /// </summary>
    /// <param name="currentPath">The current path to display in the input field.</param>
    /// <param name="callback">Callback when directory is confirmed.</param>
    public void ShowDirectoryInputBox(string currentPath, System.Action<string> callback)
    {
#if !UNITY_STANDALONE && !UNITY_EDITOR
        // Directory selection is not available on Android
        return;
#endif
        if(directoryInputBox == null)
        {
            Debug.LogWarning("DirectoryInputBox is not assigned in OptionWindow");
            return;
        }
        
        dir_input_callback_ = callback;
        
        // Set the current path
        if(directoryInputField != null)
        {
            directoryInputField.text = currentPath ?? "";
        }
        
        directoryInputBox.SetActive(true);
    }
    
    /// <summary>
    /// Hides the directory input dialog box.
    /// </summary>
    public void HideDirectoryInputBox()
    {
        if(directoryInputBox != null)
        {
            directoryInputBox.SetActive(false);
        }
        dir_input_callback_ = null;
    }
    
    /// <summary>
    /// Called when the directory input confirm button is clicked.
    /// </summary>
    void OnDirInputConfirm()
    {
        string path = directoryInputField != null ? directoryInputField.text : "";
        
        if(directoryInputBox != null)
        {
            directoryInputBox.SetActive(false);
        }
        
        if(dir_input_callback_ != null)
        {
            dir_input_callback_(path);
            dir_input_callback_ = null;
        }
    }
    
    /// <summary>
    /// Called when the directory input cancel button is clicked.
    /// </summary>
    void OnDirInputCancel()
    {
        if(directoryInputBox != null)
        {
            directoryInputBox.SetActive(false);
        }
        dir_input_callback_ = null;
    }
    
    #endregion
    
    #region Storage Permission Dialog
    
    /// <summary>
    /// Shows a dialog for storage permission requests on Android.
    /// The confirm button triggers the grant callback, the cancel button just closes the dialog.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="content">The content message explaining why permission is needed.</param>
    /// <param name="grantCallback">Callback when user wants to grant permission.</param>
    public void ShowStoragePermissionDialog(string title, string content, 
        System.Action grantCallback)
    {
        storage_permission_grant_callback_ = grantCallback;
        
        // Use the standard message box infrastructure
        msg_confirm_callback = () =>
        {
            storage_permission_grant_callback_?.Invoke();
            storage_permission_grant_callback_ = null;
        };
        // Cancel button just closes the dialog without any action
        msg_cancel_callback = () =>
        {
            storage_permission_grant_callback_ = null;
        };
        
        msg_cancel.SetActive(true);
        msg_title.text = title;
        msg_content.text = content;
        
        // Update button labels for permission dialog
        var confirmText = GenericUtils.FindChildByName<Text>(msg_confirm, "Text");
        var cancelText = GenericUtils.FindChildByName<Text>(msg_cancel, "Text");
        
        if (confirmText != null)
            confirmText.text = MultiLanguage.GetText("[Grant]");
        if (cancelText != null)
            cancelText.text = MultiLanguage.GetText("[Cancel]");
        
        msg_box.SetActive(true);
    }
    
    private System.Action storage_permission_grant_callback_;
    
    #endregion

    #region UI References
    
    /// <summary>
    /// Container for game-related buttons.
    /// </summary>
    [Header("Game Buttons")]
    [Tooltip("Container for game-related buttons")]
    public GameObject game_button;
    
    /// <summary>
    /// Button to toggle quick buttons panel.
    /// </summary>
    [Tooltip("Button to toggle quick buttons panel")]
    public Button quick_button;
    
    /// <summary>
    /// Button to toggle input pad.
    /// </summary>
    [Tooltip("Button to toggle input pad")]
    public Button input_button;
    
    /// <summary>
    /// Button to toggle scale/magnifier pad.
    /// </summary>
    [Tooltip("Button to toggle scale/magnifier pad")]
    public Button magnifier_button;
    
    /// <summary>
    /// Button to show options menu.
    /// </summary>
    [Tooltip("Button to show options menu")]
    public Button option_button;

    /// <summary>
    /// Button to toggle screen orientation lock.
    /// </summary>
    [Header("Orientation")]
    [Tooltip("Button to toggle screen orientation lock")]
    public Button orientation_lock_button;
    Image orientation_lock_image;
    
    /// <summary>
    /// Sprite shown when orientation is locked.
    /// </summary>
    [Tooltip("Sprite shown when orientation is locked")]
    public Sprite lock_sprite;
    
    /// <summary>
    /// Sprite shown when orientation is unlocked.
    /// </summary>
    [Tooltip("Sprite shown when orientation is unlocked")]
    public Sprite unlock_sprite;

    /// <summary>
    /// Loading/progress indicator.
    /// </summary>
    [Header("Progress")]
    [Tooltip("Loading/progress indicator")]
    public GameObject inprogress;
    List<Shadow> button_shadows;

    /// <summary>
    /// Quick buttons panel component.
    /// </summary>
    [Header("Panels")]
    [Tooltip("Quick buttons panel component")]
    public QuickButtons quick_buttons;
    
    /// <summary>
    /// Input pad panel component.
    /// </summary>
    [Tooltip("Input pad panel component")]
    public Inputpad input_pad;
    
    /// <summary>
    /// Scale pad panel component.
    /// </summary>
    [Tooltip("Scale pad panel component")]
    public Scalepad scale_pad;

    /// <summary>
    /// Message box container.
    /// </summary>
    [Header("Message Box")]
    [Tooltip("Message box container")]
    public GameObject msg_box;
    public Text msg_title;
    public Text msg_content;
    public GameObject msg_confirm;
    public GameObject msg_cancel;
    System.Action msg_confirm_callback;
    System.Action msg_cancel_callback;

    [Header("Menu 1")]
    public GameObject menu_pad;
    public GameObject menu_1;
    public GameObject menu_1_resolution;
    public GameObject menu_1_language;
    public GameObject menu_1_settings;
    public GameObject menu_1_directory;
    public GameObject menu_1_github;
    public GameObject menu_1_exit;

    [Header("Menu 2")]
    public GameObject menu_2;
    public GameObject menu_2_back;
    public GameObject menu_2_restart;
    public GameObject menu_2_gototitle;
    public GameObject menu_2_savelog;
    public GameObject menu_2_intent;
    public GameObject menu_2_exit;

    [Header("Resolution Settings")]
    public GameObject resolution_pad;
    public GameObject resolution_1080p;
    public GameObject resolution_1080p_icon;
    public GameObject resolution_900p;
    public GameObject resolution_900p_icon;
    public GameObject resolution_720p;
    public GameObject resolution_720p_icon;
    public GameObject resolution_540p;
    public GameObject resolution_540p_icon;

    [Header("Language Settings")]
    public GameObject language_box;
    public GameObject language_zhcn;
    public GameObject language_jp;
    public GameObject language_enus;

    [Header("Intent Box")]
    public GameObject intentbox;
    public GameObject intentbox_L_left;
    public GameObject intentbox_L_right;
    public GameObject intentbox_R_left;
    public GameObject intentbox_R_right;
    public GameObject intentbox_close;
    public GameObject intentbox_reset;
    public Text intentbox_L_text;
    public Text intentbox_R_text;

    [Header("Directory Input Box")]
    [Tooltip("Directory input dialog container (for standalone platforms)")]
    public GameObject directoryInputBox;
    [Tooltip("Input field for directory path")]
    public InputField directoryInputField;
    [Tooltip("Confirm button for directory input")]
    public GameObject directoryInputConfirm;
    [Tooltip("Cancel button for directory input")]
    public GameObject directoryInputCancel;
    
    [Header("Settings Box")]
    [Tooltip("Settings dialog container")]
    public GameObject settings_box;
    [Tooltip("Close button for settings dialog")]
    public GameObject settings_close;
    [Tooltip("Post-processing toggle button")]
    public GameObject settings_postprocessing_toggle;
    [Tooltip("Post-processing enabled icon")]
    public GameObject settings_postprocessing_icon;
    [Tooltip("Post-processing toggle text")]
    public Text settings_postprocessing_text;
    
    #endregion

    /// <summary>
    /// Gets whether auto-rotation is enabled.
    /// </summary>
    bool auto_rotation
    {
        get
        {
            return Screen.autorotateToLandscapeLeft &&
                    Screen.autorotateToLandscapeRight &&
                    Screen.autorotateToPortrait &&
                    Screen.autorotateToPortraitUpsideDown;
        }
    }
}
