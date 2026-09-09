using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using uEmuera.Runtime;

/// <summary>
/// Modern, code-owned launcher presentation.
///
/// The launcher deliberately lives outside the large legacy Options prefab. FirstWindow
/// continues to own discovery and game startup; this component only presents the game
/// library and exposes the existing menu actions through a flatter navigation surface.
///
/// If construction fails FirstWindow keeps using the legacy prefab UI.
/// </summary>
public sealed class LauncherDashboardController : MonoBehaviour
{
    sealed class CardEntry
    {
        public GameObject Root;
        public string SearchText;
        public RuntimeKind Runtime;
        public string SortKey;
    }

    readonly List<CardEntry> cards_ = new List<CardEntry>();

    FirstWindow owner_;
    Font font_;
    GameObject modernRoot_;
    RectTransform content_;
    InputField search_;
    Text searchPlaceholder_;
    Text countText_;
    Text emptyTitle_;
    Text emptyHint_;
    Text libraryPathText_;

    Text titleText_;
    Text subtitleText_;
    Text chooseFolderText_;
    Text refreshText_;
    Text settingsText_;
    Text displayText_;
    Text languageText_;
    Text projectText_;
    Text exitText_;
    GameObject runtimeChoiceOverlay_;

    int emueraCount_;
    int eraElectronCount_;

    public bool IsBuilt => modernRoot_ != null;

    public bool Build(FirstWindow owner)
    {
        if (modernRoot_ != null)
            return true;
        if (owner == null)
            return false;

        owner_ = owner;
        font_ = owner.titlebar != null ? owner.titlebar.font : null;
        if (font_ == null)
        {
            try { font_ = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { }
        }
        if (font_ == null)
            return false;

        var legacyChildren = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
            legacyChildren.Add(transform.GetChild(i).gameObject);

        try
        {
            modernRoot_ = CreateObject("ModernLauncherRoot", transform);
            Stretch(modernRoot_.GetComponent<RectTransform>());
            var rootImage = modernRoot_.AddComponent<Image>();
            rootImage.color = UIStyleManager.ModernTheme.Background;

            BuildHeader();
            BuildLibrary();
            BuildFooter();
            RefreshLocalizedText();
            RefreshLibraryPath();
            UpdateLibraryState();

            // Only hide the old prefab after the new hierarchy is complete.
            for (int i = 0; i < legacyChildren.Count; i++)
                legacyChildren[i].SetActive(false);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[LauncherDashboard] Failed to build modern launcher: " + ex);
            if (modernRoot_ != null)
                Destroy(modernRoot_);
            modernRoot_ = null;
            for (int i = 0; i < legacyChildren.Count; i++)
                legacyChildren[i].SetActive(true);
            return false;
        }
    }

    void BuildHeader()
    {
        var header = CreatePanel("LauncherHeader", modernRoot_.transform,
            UIStyleManager.ModernTheme.Surface);
        SetAnchored(header.GetComponent<RectTransform>(),
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero,
            new Vector2(0, 92), new Vector2(0.5f, 1));

        titleText_ = CreateText("Title", header.transform, 26, FontStyle.Bold,
            UIStyleManager.ModernTheme.TextPrimary, TextAnchor.UpperLeft);
        SetAnchored(titleText_.rectTransform,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(28, -16),
            new Vector2(430, -12), new Vector2(0, 1));

        subtitleText_ = CreateText("Subtitle", header.transform, 13, FontStyle.Normal,
            UIStyleManager.ModernTheme.TextSecondary, TextAnchor.LowerLeft);
        SetAnchored(subtitleText_.rectTransform,
            new Vector2(0, 0), new Vector2(0, 1), new Vector2(28, 14),
            new Vector2(460, -52), new Vector2(0, 0));

        var actions = CreateObject("PrimaryActions", header.transform);
        var actionsRect = actions.GetComponent<RectTransform>();
        SetAnchored(actionsRect, new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-24, 0), new Vector2(440, 48), new Vector2(1, 0.5f));
        var layout = actions.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = false;

        chooseFolderText_ = CreateButton("ChooseFolder", actions.transform, 132, () =>
            owner_?.OpenGameFolderPicker());
        refreshText_ = CreateButton("Refresh", actions.transform, 108, () =>
            owner_?.RefreshGameList());
        settingsText_ = CreateButton("Settings", actions.transform, 118, () =>
            GetOptions()?.ShowSettingsBox(), true);
    }

    void BuildLibrary()
    {
        var library = CreateObject("LibraryArea", modernRoot_.transform);
        var libraryRect = library.GetComponent<RectTransform>();
        libraryRect.anchorMin = new Vector2(0, 0);
        libraryRect.anchorMax = new Vector2(1, 1);
        libraryRect.offsetMin = new Vector2(24, 72);
        libraryRect.offsetMax = new Vector2(-24, -108);

        var searchPanel = CreatePanel("SearchPanel", library.transform,
            UIStyleManager.ModernTheme.SurfaceElevated);
        SetAnchored(searchPanel.GetComponent<RectTransform>(),
            new Vector2(0, 1), new Vector2(1, 1), Vector2.zero,
            new Vector2(0, 54), new Vector2(0.5f, 1));

        search_ = CreateSearchField(searchPanel.transform);
        SetAnchored(search_.GetComponent<RectTransform>(),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(14, 0),
            new Vector2(430, 38), new Vector2(0, 0.5f));
        search_.onValueChanged.AddListener(FilterCards);

        countText_ = CreateText("LibraryCount", searchPanel.transform, 13, FontStyle.Normal,
            UIStyleManager.ModernTheme.TextSecondary, TextAnchor.MiddleRight);
        SetAnchored(countText_.rectTransform,
            new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(-14, 0),
            new Vector2(-470, 0), new Vector2(1, 0.5f));

        var scroll = CreatePanel("GameScroll", library.transform,
            UIStyleManager.ModernTheme.Background);
        var scrollRectTransform = scroll.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = new Vector2(0, -66);

        var scrollRect = scroll.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 34f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateObject("Viewport", scroll.transform);
        var viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect);
        viewportRect.offsetMin = new Vector2(0, 0);
        viewportRect.offsetMax = new Vector2(-4, 0);
        viewport.AddComponent<RectMask2D>();

        var content = CreateObject("GameCards", viewport.transform);
        content_ = content.GetComponent<RectTransform>();
        content_.anchorMin = new Vector2(0, 1);
        content_.anchorMax = new Vector2(1, 1);
        content_.pivot = new Vector2(0.5f, 1);
        content_.anchoredPosition = Vector2.zero;
        content_.sizeDelta = Vector2.zero;

        var vertical = content.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(0, 8, 8, 12);
        vertical.spacing = 10;
        vertical.childAlignment = TextAnchor.UpperCenter;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewportRect;
        scrollRect.content = content_;

        var empty = CreateObject("EmptyState", library.transform);
        var emptyRect = empty.GetComponent<RectTransform>();
        SetAnchored(emptyRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -16), new Vector2(640, 130), new Vector2(0.5f, 0.5f));

        emptyTitle_ = CreateText("EmptyTitle", empty.transform, 22, FontStyle.Bold,
            UIStyleManager.ModernTheme.TextPrimary, TextAnchor.MiddleCenter);
        SetAnchored(emptyTitle_.rectTransform, new Vector2(0, 0.45f), Vector2.one,
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        emptyHint_ = CreateText("EmptyHint", empty.transform, 14, FontStyle.Normal,
            UIStyleManager.ModernTheme.TextSecondary, TextAnchor.UpperCenter);
        SetAnchored(emptyHint_.rectTransform, Vector2.zero, new Vector2(1, 0.48f),
            Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
    }

    void BuildFooter()
    {
        var footer = CreatePanel("LauncherFooter", modernRoot_.transform,
            UIStyleManager.ModernTheme.Surface);
        SetAnchored(footer.GetComponent<RectTransform>(),
            Vector2.zero, new Vector2(1, 0), Vector2.zero,
            new Vector2(0, 58), new Vector2(0.5f, 0));

        libraryPathText_ = CreateText("LibraryPath", footer.transform, 12, FontStyle.Normal,
            UIStyleManager.ModernTheme.TextSecondary, TextAnchor.MiddleLeft);
        SetAnchored(libraryPathText_.rectTransform,
            new Vector2(0, 0), new Vector2(0.47f, 1), new Vector2(24, 0),
            new Vector2(-24, 0), new Vector2(0, 0.5f));

        var actions = CreateObject("SecondaryActions", footer.transform);
        var actionsRect = actions.GetComponent<RectTransform>();
        SetAnchored(actionsRect, new Vector2(0.47f, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-18, 0), new Vector2(-18, 38), new Vector2(1, 0.5f));
        var layout = actions.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 7;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = false;

        displayText_ = CreateButton("Display", actions.transform, 92,
            () => GetOptions()?.ShowResolutionBox(), false, true);
        languageText_ = CreateButton("Language", actions.transform, 100,
            () => GetOptions()?.ShowLanguageBox(), false, true);
        projectText_ = CreateButton("Project", actions.transform, 86,
            () => GetOptions()?.OpenProjectPage(), false, true);
        exitText_ = CreateButton("Exit", actions.transform, 72,
            () => GetOptions()?.ShowExitConfirmation(), false, true);
    }

    OptionWindow GetOptions()
    {
        return EmueraContent.instance != null ? EmueraContent.instance.option_window : null;
    }

    public void ShowRuntimeChoice(
        GameDescriptor primary,
        GameDescriptor alternative,
        Action<GameDescriptor> launch)
    {
        if (!IsBuilt || primary == null || alternative == null)
            return;

        if (runtimeChoiceOverlay_ != null)
            Destroy(runtimeChoiceOverlay_);

        runtimeChoiceOverlay_ = CreatePanel("RuntimeChoiceOverlay", modernRoot_.transform,
            new Color(0f, 0f, 0f, 0.78f));
        Stretch(runtimeChoiceOverlay_.GetComponent<RectTransform>());

        var panel = CreatePanel("RuntimeChoicePanel", runtimeChoiceOverlay_.transform,
            UIStyleManager.ModernTheme.Surface);
        SetAnchored(panel.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(590, 286), new Vector2(0.5f, 0.5f));

        var title = CreateText("Title", panel.transform, 22, FontStyle.Bold,
            UIStyleManager.ModernTheme.TextPrimary, TextAnchor.MiddleLeft);
        SetAnchored(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(24, -22), new Vector2(-48, 42), new Vector2(0, 1));
        title.text = T("[LauncherRuntimeChoiceTitle]", "Choose runtime");

        var body = CreateText("Body", panel.transform, 14, FontStyle.Normal,
            UIStyleManager.ModernTheme.TextSecondary, TextAnchor.UpperLeft);
        SetAnchored(body.rectTransform, new Vector2(0, 0), new Vector2(1, 1),
            new Vector2(24, -72), new Vector2(-48, -124), new Vector2(0, 1));
        body.text = string.Format(
            T("[LauncherRuntimeChoiceBody]",
              "This folder matches more than one runtime. Choose how uEmuera should start it.\n\nFolder: {0}"),
            primary.GameRoot ?? "");

        var buttons = CreateObject("Buttons", panel.transform);
        SetAnchored(buttons.GetComponent<RectTransform>(),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 22),
            new Vector2(-48, 46), new Vector2(0, 0));
        var layout = buttons.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = false;

        CreateButton("PrimaryRuntime", buttons.transform, 150,
            () => ChooseRuntime(primary, launch), true).text =
            primary.RuntimeKind.ToString();

        CreateButton("AlternativeRuntime", buttons.transform, 150,
            () => ChooseRuntime(alternative, launch), true).text =
            alternative.RuntimeKind.ToString();

        CreateButton("Cancel", buttons.transform, 100,
            CloseRuntimeChoice, false).text =
            T("[LauncherCancel]", "Cancel");
    }

    void ChooseRuntime(GameDescriptor descriptor, Action<GameDescriptor> launch)
    {
        CloseRuntimeChoice();
        launch?.Invoke(descriptor);
    }

    void CloseRuntimeChoice()
    {
        if (runtimeChoiceOverlay_ != null)
            Destroy(runtimeChoiceOverlay_);
        runtimeChoiceOverlay_ = null;
    }

    public GameObject AddGame(GameDescriptor descriptor, Action launch)
    {
        if (!IsBuilt || descriptor == null || content_ == null)
            return null;

        string title = string.IsNullOrWhiteSpace(descriptor.Title)
            ? Path.GetFileName((descriptor.GameRoot ?? "").TrimEnd('/', '\\'))
            : descriptor.Title;
        string runtime = descriptor.RuntimeKind == RuntimeKind.EraElectron
            ? "EraElectron"
            : "Emuera";

        var card = CreatePanel("Game_" + (descriptor.GameId ?? cards_.Count.ToString()),
            content_, UIStyleManager.ModernTheme.SurfaceElevated);
        var layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 86;
        layoutElement.minHeight = 78;

        var button = card.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        UIStyleManager.ConfigureButton(button, false);
        button.onClick.AddListener(() => launch?.Invoke());

        var badge = CreatePanel("RuntimeBadge", card.transform,
            descriptor.RuntimeKind == RuntimeKind.EraElectron
                ? UIStyleManager.ModernTheme.AccentSecondary
                : UIStyleManager.ModernTheme.AccentPrimary);
        SetAnchored(badge.GetComponent<RectTransform>(),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(16, 14),
            new Vector2(112, 28), new Vector2(0, 0.5f));
        var badgeText = CreateText("Runtime", badge.transform, 12, FontStyle.Bold,
            Color.white, TextAnchor.MiddleCenter);
        Stretch(badgeText.rectTransform);
        badgeText.text = runtime;

        var titleText = CreateText("GameTitle", card.transform, 19, FontStyle.Bold,
            UIStyleManager.ModernTheme.TextPrimary, TextAnchor.LowerLeft);
        SetAnchored(titleText.rectTransform,
            new Vector2(0, 0.5f), new Vector2(0.72f, 1), new Vector2(144, -10),
            new Vector2(-8, -10), new Vector2(0, 1));
        titleText.text = title;

        var pathText = CreateText("GamePath", card.transform, 12, FontStyle.Normal,
            UIStyleManager.ModernTheme.TextSecondary, TextAnchor.UpperLeft);
        SetAnchored(pathText.rectTransform,
            new Vector2(0, 0), new Vector2(0.72f, 0.5f), new Vector2(144, 8),
            new Vector2(-8, -2), new Vector2(0, 0));
        pathText.text = ShortenPath(descriptor.GameRoot, 78);

        var statusText = CreateText("Status", card.transform, 12, FontStyle.Bold,
            GetStatusColor(descriptor), TextAnchor.MiddleRight);
        SetAnchored(statusText.rectTransform,
            new Vector2(0.72f, 0), Vector2.one, new Vector2(-18, 0),
            new Vector2(-18, 0), new Vector2(1, 0.5f));
        statusText.text = DescribeStatus(descriptor);

        var entry = new CardEntry
        {
            Root = card,
            Runtime = descriptor.RuntimeKind,
            SearchText = BuildSearchText(descriptor, title, runtime),
            SortKey = (title ?? "") + "\n" + runtime,
        };
        cards_.Add(entry);
        SortCards();
        if (descriptor.RuntimeKind == RuntimeKind.EraElectron)
            eraElectronCount_++;
        else
            emueraCount_++;

        FilterCards(search_ != null ? search_.text : "");
        return card;
    }

    public void ClearGames()
    {
        for (int i = 0; i < cards_.Count; i++)
        {
            if (cards_[i].Root != null)
                Destroy(cards_[i].Root);
        }
        cards_.Clear();
        emueraCount_ = 0;
        eraElectronCount_ = 0;
        if (search_ != null)
            search_.text = "";
        UpdateLibraryState();
        RefreshLibraryPath();
    }

    void SortCards()
    {
        cards_.Sort((a, b) =>
            string.Compare(a.SortKey, b.SortKey, StringComparison.OrdinalIgnoreCase));
        for (int i = 0; i < cards_.Count; i++)
        {
            if (cards_[i].Root != null)
                cards_[i].Root.transform.SetSiblingIndex(i);
        }
    }

    void FilterCards(string query)
    {
        string normalized = (query ?? "").Trim();
        int visible = 0;
        int visibleEmuera = 0;
        int visibleEraElectron = 0;
        for (int i = 0; i < cards_.Count; i++)
        {
            bool match = MatchesSearch(cards_[i].SearchText, normalized);
            if (cards_[i].Root != null)
                cards_[i].Root.SetActive(match);
            if (!match)
                continue;

            visible++;
            if (cards_[i].Runtime == RuntimeKind.EraElectron)
                visibleEraElectron++;
            else
                visibleEmuera++;
        }
        UpdateLibraryState(
            visible,
            visibleEmuera,
            visibleEraElectron,
            !string.IsNullOrEmpty(normalized));
    }

    void UpdateLibraryState()
    {
        UpdateLibraryState(cards_.Count, emueraCount_, eraElectronCount_, false);
    }

    void UpdateLibraryState(
        int visible,
        int visibleEmuera,
        int visibleEraElectron,
        bool hasQuery)
    {
        if (countText_ != null)
        {
            countText_.text = string.Format("{0} {1}   •   {2} Emuera   •   {3} EraElectron",
                visible, T("[LauncherGames]", "games"), visibleEmuera, visibleEraElectron);
        }

        bool empty = visible == 0;
        if (emptyTitle_ != null && emptyTitle_.transform.parent != null)
            emptyTitle_.transform.parent.gameObject.SetActive(empty);

        if (emptyTitle_ != null)
            emptyTitle_.text = hasQuery
                ? T("[LauncherNoSearchResults]", "No games match your search")
                : T("[LauncherNoGames]", "No games found");
        if (emptyHint_ != null)
            emptyHint_.text = hasQuery
                ? T("[LauncherSearchHint]", "Try another title, path, or runtime.")
                : T("[LauncherNoGamesHint]", "Choose a game folder or refresh the library.");
    }

    public void RefreshLocalizedText()
    {
        if (!IsBuilt)
            return;

        if (titleText_ != null)
            titleText_.text = T("[LauncherTitle]", "Game Library");
        if (subtitleText_ != null)
            subtitleText_.text = T("[LauncherSubtitle]", "Choose a game to start");
        if (chooseFolderText_ != null)
            chooseFolderText_.text = T("[LauncherChooseFolder]", "Game Folder");
        if (refreshText_ != null)
            refreshText_.text = T("[LauncherRefresh]", "Refresh");
        if (settingsText_ != null)
            settingsText_.text = T("[LauncherSettings]", "Settings");
        if (displayText_ != null)
            displayText_.text = T("[LauncherDisplay]", "Display");
        if (languageText_ != null)
            languageText_.text = T("[LauncherLanguage]", "Language");
        if (projectText_ != null)
            projectText_.text = T("[LauncherProject]", "GitHub");
        if (exitText_ != null)
            exitText_.text = T("[LauncherExit]", "Exit");
        if (searchPlaceholder_ != null)
            searchPlaceholder_.text = T("[LauncherSearch]", "Search games, paths, or runtimes...");

        FilterCards(search_ != null ? search_.text : "");
        RefreshLibraryPath();
    }

    public void RefreshLibraryPath()
    {
        if (libraryPathText_ == null)
            return;

        string path = PlayerPrefs.GetString(FirstWindow.CUSTOM_DIR_KEY, "");
        string source = string.IsNullOrEmpty(path)
            ? T("[LauncherAutomaticLibrary]", "Automatic game discovery")
            : path;
        libraryPathText_.text = T("[LauncherLibrary]", "Library") + ": " + source +
            "   •   v" + Application.version;
    }

    public static bool MatchesSearch(string searchText, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;
        if (string.IsNullOrEmpty(searchText))
            return false;
        return searchText.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string DescribeStatus(GameDescriptor descriptor)
    {
        if (descriptor == null)
            return "";

        var detection = descriptor.DetectionResult;
        if (detection != null && detection.AmbiguousAlternative.HasValue)
            return "Ambiguous runtime";

        string confidence = detection != null
            ? detection.Confidence.ToString()
            : "Detected";

        int warnings = detection?.Warnings != null ? detection.Warnings.Count : 0;
        if (warnings > 0)
            return confidence + " • " + warnings + (warnings == 1 ? " warning" : " warnings");

        if (descriptor.RuntimeKind == RuntimeKind.EraElectron)
            return confidence + " • Experimental";

        return confidence + " • Ready";
    }

    static string ShortenPath(string path, int maxLength)
    {
        if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
            return path ?? "";
        if (maxLength < 12)
            return path.Substring(0, maxLength);
        int tail = Math.Max(8, maxLength / 2);
        int head = maxLength - tail - 3;
        return path.Substring(0, head) + "..." + path.Substring(path.Length - tail);
    }

    static string BuildSearchText(GameDescriptor descriptor, string title, string runtime)
    {
        return string.Join("\n", new[]
        {
            title ?? "",
            runtime ?? "",
            descriptor?.GameRoot ?? "",
            descriptor?.Version ?? "",
            descriptor?.Language ?? "",
        });
    }

    static Color GetStatusColor(GameDescriptor descriptor)
    {
        if (descriptor?.DetectionResult?.AmbiguousAlternative.HasValue == true)
            return UIStyleManager.ModernTheme.Danger;
        if (descriptor?.DetectionResult?.Warnings != null &&
            descriptor.DetectionResult.Warnings.Count > 0)
            return UIStyleManager.ModernTheme.Warning;
        if (descriptor?.RuntimeKind == RuntimeKind.EraElectron)
            return UIStyleManager.ModernTheme.Warning;
        return UIStyleManager.ModernTheme.Success;
    }

    static string T(string key, string fallback)
    {
        string value = MultiLanguage.GetText(key);
        return string.IsNullOrEmpty(value) || value == key ? fallback : value;
    }

    Text CreateButton(string name, Transform parent, float width, Action action,
        bool primary = false, bool compact = false)
    {
        var root = CreateObject(name, parent);
        var layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        layout.preferredHeight = compact ? 36 : 42;

        var image = root.AddComponent<Image>();
        image.color = primary
            ? UIStyleManager.ModernTheme.AccentPrimary
            : UIStyleManager.ModernTheme.SurfaceElevated;

        var button = root.AddComponent<Button>();
        button.targetGraphic = image;
        UIStyleManager.ConfigureButton(button, primary);
        button.onClick.AddListener(() => action?.Invoke());

        var text = CreateText("Text", root.transform, compact ? 12 : 13, FontStyle.Bold,
            UIStyleManager.ModernTheme.TextPrimary, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = compact ? 9 : 10;
        text.resizeTextMaxSize = compact ? 12 : 13;
        return text;
    }

    InputField CreateSearchField(Transform parent)
    {
        var root = CreatePanel("SearchField", parent,
            UIStyleManager.ModernTheme.InputBackground);
        var input = root.AddComponent<InputField>();
        input.targetGraphic = root.GetComponent<Image>();
        input.lineType = InputField.LineType.SingleLine;
        input.transition = Selectable.Transition.ColorTint;

        var text = CreateText("Text", root.transform, 14, FontStyle.Normal,
            UIStyleManager.ModernTheme.TextPrimary, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(14, 2);
        text.rectTransform.offsetMax = new Vector2(-12, -2);

        searchPlaceholder_ = CreateText("Placeholder", root.transform, 14, FontStyle.Italic,
            UIStyleManager.ModernTheme.TextMuted, TextAnchor.MiddleLeft);
        Stretch(searchPlaceholder_.rectTransform);
        searchPlaceholder_.rectTransform.offsetMin = new Vector2(14, 2);
        searchPlaceholder_.rectTransform.offsetMax = new Vector2(-12, -2);

        input.textComponent = text;
        input.placeholder = searchPlaceholder_;
        return input;
    }

    GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var go = CreateObject(name, parent);
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return go;
    }

    GameObject CreateObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = gameObject.layer;
        go.transform.SetParent(parent, false);
        return go;
    }

    Text CreateText(string name, Transform parent, int size, FontStyle style,
        Color color, TextAnchor anchor)
    {
        var go = CreateObject(name, parent);
        var text = go.AddComponent<Text>();
        text.font = font_;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.raycastTarget = false;
        text.resizeTextForBestFit = false;
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Vector2 pivot)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }
}
