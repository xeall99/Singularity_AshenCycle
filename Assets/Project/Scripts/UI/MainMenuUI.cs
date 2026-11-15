using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MainMenuUI : MonoBehaviour
{
    private static readonly Color DarkBackdrop = new Color(0.031f, 0.027f, 0.051f, 1f);
    private static readonly Color CardColor = new Color(0.065f, 0.045f, 0.115f, 0.95f);
    private static readonly Color VioletBorder = new Color(0.72f, 0.40f, 1.00f, 1f);
    private static readonly Color SubduedBorder = new Color(0.35f, 0.25f, 0.45f, 0.8f);
    private static readonly Color ButtonNormal = new Color(0.055f, 0.041f, 0.082f, 0.78f);
    private static readonly Color ButtonHighlight = new Color(0.20f, 0.11f, 0.31f, 0.96f);
    private static readonly Color ButtonPressed = new Color(0.44f, 0.22f, 0.70f, 1f);
    private static readonly Color TextWhite = new Color(0.96f, 0.94f, 1.00f, 1f);
    private static readonly Color TextMuted = new Color(0.72f, 0.65f, 0.82f, 1f);
    private static readonly Color TextAccent = new Color(0.85f, 0.60f, 1.00f, 1f);

    private Canvas canvas;
    private ActiveRunCheckpointStore checkpointStore;
    private ActiveRunCheckpointInspection inspection;
    private EchoFragmentWallet echoWallet;

    private Button continueButton;
    private TMP_Text continueSummaryText;
    private Button newRunButton;
    private Button unlockMenuButton;
    private Button settingsButton;
    private Button quitButton;
    private Button tutorialButton;
    private bool loadingInProgress;

    private GameObject confirmDialog;
    private Button confirmNewRunButton;
    private Button cancelConfirmButton;
    private GameObject unlockDialog;
    private TMP_Text unlockBalanceText;
    private TMP_Text unlockStatusText;
    private TMP_Text unlockMenuSummaryText;
    private Button buyUnlockButton;
    private Button skillArchiveButton;
    private SkillArchiveUI skillArchiveUI;

    private BattleSettingsUI settingsUI;
    private RunHistoryUI historyUI;
    private LocalLeaderboardUI leaderboardUI;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneBootstrap()
    {
        // The menu scene can be entered after the initial game launch too.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu") BootstrapMainMenu();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapMainMenu()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name != "MainMenu") return;

        MainMenuUI existing = FindFirstObjectByType<MainMenuUI>();
        if (existing != null) return;

        EnsureSceneEventSystem();

        Canvas existingCanvas = FindFirstObjectByType<Canvas>();
        if (existingCanvas == null)
        {
            GameObject canvasObject = new GameObject("MainMenuCanvas");
            existingCanvas = canvasObject.AddComponent<Canvas>();
            existingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        existingCanvas.gameObject.AddComponent<MainMenuUI>();
    }

    private static void EnsureSceneEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventObj = new GameObject("EventSystem");
            eventSystem = eventObj.AddComponent<EventSystem>();
        }
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        StandaloneInputModule legacyModule =
            eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null) legacyModule.enabled = false;
    }

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        checkpointStore = ActiveRunCheckpointStore.CreateDefault();
        echoWallet = EchoFragmentWallet.CreateDefault();
        settingsUI = GetComponent<BattleSettingsUI>();
        if (settingsUI == null)
        {
            settingsUI = gameObject.AddComponent<BattleSettingsUI>();
        }

        BuildUI();
    }

    private void OnEnable()
    {
        RefreshCheckpointStatus();
    }

    private void Start()
    {
        Button firstAvailable = continueButton != null && continueButton.interactable
            ? continueButton : newRunButton;
        if (firstAvailable != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstAvailable.gameObject);
    }

    private void RefreshCheckpointStatus()
    {
        // 100 Base HP, 10 Base Mana baseline
        inspection = checkpointStore.Inspect(100, 10);
        if (continueButton == null) return;

        if (inspection != null && inspection.HasUsableState)
        {
            continueButton.interactable = true;
            SeededRunState state = inspection.CreateStateCopy();
            if (continueSummaryText != null && state != null)
            {
                continueSummaryText.text = $"STAGE {state.Depth}/{state.NodeCount}";
                continueSummaryText.color = TextAccent;
            }
        }
        else
        {
            continueButton.interactable = false;
            if (continueSummaryText != null)
            {
                continueSummaryText.text = "NO SAVE";
                continueSummaryText.color = TextMuted;
            }
        }
        RefreshUnlockStatus();
    }

    private void BuildUI()
    {
        if (transform.Find("MenuCard") == null)
            BuildVisuals();
        else
            BindSceneVisuals();

        BuildSupportUI();
        RefreshCheckpointStatus();
    }

    private void BuildVisuals()
    {
        // 1. Full Screen Backdrop
        GameObject bgObj = CreateUiObject("Background", transform, typeof(Image));
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImage = bgObj.GetComponent<Image>();
        bgImage.color = DarkBackdrop;
        bgImage.raycastTarget = true;

        Sprite portrait = Resources.Load<Sprite>("UI/MainMenuWardenPortrait");
        if (portrait != null)
        {
            GameObject sceneArt = CreateUiObject("WardenPortraitBackdrop", transform,
                typeof(Image), typeof(AspectRatioFitter));
            RectTransform artRect = sceneArt.GetComponent<RectTransform>();
            artRect.anchorMin = artRect.anchorMax = new Vector2(0.5f, 0.5f);
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.anchoredPosition = Vector2.zero;
            Image sceneImage = sceneArt.GetComponent<Image>();
            sceneImage.sprite = portrait;
            sceneImage.raycastTarget = false;
            AspectRatioFitter fitter = sceneArt.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)portrait.rect.width / portrait.rect.height;
        }

        // 2. Transparent alignment root. The quiet area is part of the artwork,
        // not a large opaque panel placed over it.
        GameObject menuCard = CreateUiObject("MenuCard", transform, typeof(Image), typeof(Outline));
        RectTransform cardRect = menuCard.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(1920f, 1080f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImg = menuCard.GetComponent<Image>();
        cardImg.color = Color.clear;
        cardImg.raycastTarget = false;
        Outline cardOutline = menuCard.GetComponent<Outline>();
        cardOutline.enabled = false;

        // 3. The title and navigation occupy the quiet left side.
        TMP_Text titleText = CreateLabel("TitleText", menuCard.transform,
            "SYNGRAVA", 68f, TextWhite,
            MenuPosition(96f, 106f, 480f, 88f), new Vector2(480f, 88f),
            TextAlignmentOptions.Left, true);
        titleText.characterSpacing = 4f;

        TMP_Text subtitleText = CreateLabel("SubtitleText", menuCard.transform,
            "THE ASHEN CYCLE", 17f, TextAccent,
            MenuPosition(100f, 208f, 450f, 30f), new Vector2(450f, 30f),
            TextAlignmentOptions.Left, true);
        subtitleText.characterSpacing = 4f;

        // 4. Five evenly spaced actions and two quieter archive links.
        Vector2 primarySize = new Vector2(456f, 60f);
        Vector2 utilitySize = new Vector2(214f, 46f);

        continueButton = CreateMenuButton("ContinueButton", menuCard.transform,
            "CONTINUE EXPEDITION", MenuPosition(96f, 350f, 456f, 60f),
            primarySize, out continueSummaryText, emphasized: true);
        continueButton.interactable = false;
        continueSummaryText.text = "NO SAVE";
        continueButton.onClick.AddListener(OnContinueClicked);

        newRunButton = CreateMenuButton("NewRunButton", menuCard.transform,
            "NEW EXPEDITION", MenuPosition(96f, 428f, 456f, 60f),
            primarySize, out TMP_Text newRunSummary, emphasized: true);
        newRunSummary.text = string.Empty;
        newRunButton.onClick.AddListener(OnNewRunClicked);

        unlockMenuButton = CreateMenuButton("EchoArchiveButton", menuCard.transform,
            "ECHO ARCHIVE", MenuPosition(96f, 506f, 456f, 60f),
            primarySize, out unlockMenuSummaryText);
        unlockMenuButton.onClick.AddListener(OnUnlockClicked);

        settingsButton = CreateMenuButton("SettingsButton", menuCard.transform,
            "SETTINGS", MenuPosition(96f, 584f, 456f, 60f),
            primarySize, out TMP_Text settingsSummary);
        settingsSummary.text = string.Empty;
        settingsButton.onClick.AddListener(OnSettingsClicked);

        quitButton = CreateMenuButton("QuitButton", menuCard.transform,
            "QUIT GAME", MenuPosition(96f, 662f, 456f, 60f),
            primarySize, out TMP_Text quitSummary);
        quitSummary.text = string.Empty;
        quitButton.onClick.AddListener(OnQuitClicked);

        Button historyButton = CreateMenuButton("RunHistoryButton", menuCard.transform,
            "RUN HISTORY", MenuPosition(96f, 786f, 214f, 46f),
            utilitySize, out TMP_Text historySummary, compact: true);
        historySummary.text = string.Empty;
        historyButton.onClick.AddListener(OnHistoryClicked);

        Button topRunsButton = CreateMenuButton("TopRunsButton", menuCard.transform,
            "TOP RUNS", MenuPosition(336f, 786f, 214f, 46f),
            utilitySize, out TMP_Text topRunsSummary, compact: true);
        topRunsSummary.text = string.Empty;
        topRunsButton.onClick.AddListener(OnTopRunsClicked);

        tutorialButton = CreateMenuButton("FieldGuideButton", menuCard.transform,
            "FIELD GUIDE", MenuPosition(576f, 786f, 214f, 46f),
            utilitySize, out TMP_Text tutorialSummary, compact: true);
        tutorialSummary.text = string.Empty;
    }

    private static Vector2 MenuPosition(float x, float y, float width, float height)
    {
        return new Vector2(x + width * 0.5f - 960f,
            540f - y - height * 0.5f);
    }

    private void BindSceneVisuals()
    {
        Transform card = transform.Find("MenuCard");
        continueButton = SceneButton(card, "ContinueButton", OnContinueClicked,
            out continueSummaryText);
        newRunButton = SceneButton(card, "NewRunButton", OnNewRunClicked,
            out _);
        unlockMenuButton = SceneButton(card, "EchoArchiveButton", OnUnlockClicked,
            out unlockMenuSummaryText);
        SceneButton(card, "RunHistoryButton", OnHistoryClicked, out _);
        settingsButton = SceneButton(card, "SettingsButton", OnSettingsClicked,
            out _);
        quitButton = SceneButton(card, "QuitButton", OnQuitClicked, out _);
        SceneButton(card, "TopRunsButton", OnTopRunsClicked, out _);
        tutorialButton = CreateMenuButton("FieldGuideButton", card,
            "FIELD GUIDE", MenuPosition(576f, 786f, 214f, 46f),
            new Vector2(214f, 46f), out _, compact: true);
    }

    private static Button SceneButton(Transform card, string name, Action action,
        out TMP_Text summary)
    {
        Transform visual = card.Find(name);
        if (visual == null)
            throw new InvalidOperationException("Main Menu scene is missing " + name);
        Button button = visual.GetComponent<Button>();
        Transform summaryVisual = visual.Find("Summary");
        summary = summaryVisual != null ? summaryVisual.GetComponent<TMP_Text>() : null;
        if (button == null || summary == null)
            throw new InvalidOperationException("Main Menu scene button is incomplete: " + name);
        button.onClick.AddListener(() => action());
        return button;
    }

    private void BuildSupportUI()
    {
        // Dialogs are run-specific; the primary visual hierarchy lives in MainMenu.unity.
        BuildConfirmDialog(transform);
        BuildUnlockDialog(transform);
        skillArchiveUI = GetComponent<SkillArchiveUI>();
        if (skillArchiveUI == null) skillArchiveUI = gameObject.AddComponent<SkillArchiveUI>();
        skillArchiveUI.Initialize(SkillProgressStore.CreateDefault(),
            () => inspection != null && inspection.HasUsableState);
        if (skillArchiveButton != null)
            skillArchiveButton.onClick.AddListener(() =>
            {
                RefreshCheckpointStatus();
                unlockDialog.SetActive(false);
                skillArchiveUI.Open();
            });
        TutorialUI tutorialUI = GetComponent<TutorialUI>();
        if (tutorialUI == null) tutorialUI = gameObject.AddComponent<TutorialUI>();
        if (tutorialButton != null) tutorialButton.onClick.AddListener(tutorialUI.Open);
        historyUI = GetComponent<RunHistoryUI>();
        if (historyUI == null) historyUI = gameObject.AddComponent<RunHistoryUI>();
        historyUI.Initialize(RunHistoryStore.CreateDefault());
        leaderboardUI = GetComponent<LocalLeaderboardUI>();
        if (leaderboardUI == null)
            leaderboardUI = gameObject.AddComponent<LocalLeaderboardUI>();
        leaderboardUI.Initialize(RunHistoryStore.CreateDefault());

        // Hide default settings button from BattleSettingsUI because we trigger it via menu
        if (settingsUI != null && settingsUI.SettingsButton != null)
        {
            settingsUI.SettingsButton.gameObject.SetActive(false);
        }

    }

    private Button CreateMenuButton(
        string name, Transform parent, string labelText,
        Vector2 position, Vector2 size, out TMP_Text summaryText,
        bool compact = false, bool emphasized = false)
    {
        GameObject buttonObj = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(Outline));
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image img = buttonObj.GetComponent<Image>();
        img.sprite = null;
        img.color = Color.white;

        Outline outline = buttonObj.GetComponent<Outline>();
        outline.enabled = false;

        Button btn = buttonObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.08f, 0.075f, 0.11f, compact ? 0.38f : 0.53f);
        colors.highlightedColor = new Color(0.20f, 0.16f, 0.26f, 0.75f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.23f, 0.18f, 0.29f, 0.86f);
        colors.disabledColor = new Color(0.05f, 0.045f, 0.07f, 0.25f);
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        TMP_Text mainLabel = CreateLabel(
            "Label", buttonObj.transform, labelText,
            compact ? 17f : emphasized ? 21f : 19f, TextWhite,
            compact ? Vector2.zero : new Vector2(-50f, 0f),
            compact ? new Vector2(size.x - 32f, 32f) : new Vector2(300f, 36f),
            compact ? TextAlignmentOptions.Center : TextAlignmentOptions.Left, true
        );
        mainLabel.characterSpacing = compact ? 1.5f : 2f;

        summaryText = CreateLabel(
            "Summary", buttonObj.transform, "",
            11f, TextMuted,
            compact ? Vector2.zero : new Vector2(150f, 0f),
            compact ? new Vector2(1f, 1f) : new Vector2(100f, 26f),
            TextAlignmentOptions.Right, false
        );

        GameObject rail = CreateUiObject("FocusRail", buttonObj.transform, typeof(Image));
        RectTransform railRect = rail.GetComponent<RectTransform>();
        railRect.anchorMin = railRect.anchorMax = railRect.pivot = Vector2.one * 0.5f;
        railRect.anchoredPosition = new Vector2(-size.x * 0.5f + 16f, 0f);
        railRect.sizeDelta = new Vector2(3f, compact ? 20f : 28f);
        rail.GetComponent<Image>().raycastTarget = false;

        GameObject rule = CreateUiObject("Rule", buttonObj.transform, typeof(Image));
        RectTransform ruleRect = rule.GetComponent<RectTransform>();
        ruleRect.anchorMin = ruleRect.anchorMax = ruleRect.pivot = Vector2.one * 0.5f;
        ruleRect.anchoredPosition = new Vector2(0f, -size.y * 0.5f + 1f);
        ruleRect.sizeDelta = new Vector2(size.x - 2f, 1f);
        rule.GetComponent<Image>().raycastTarget = false;

        buttonObj.AddComponent<MainMenuActionVisual>();
        return btn;
    }

    private void BuildConfirmDialog(Transform parent)
    {
        confirmDialog = CreateUiObject("NewRunConfirmDialog", parent, typeof(Image));
        RectTransform rect = confirmDialog.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image bg = confirmDialog.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject card = CreateUiObject("ConfirmCard", confirmDialog.transform, typeof(Image), typeof(Outline));
        RectTransform cRect = card.GetComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0.5f);
        cRect.anchorMax = new Vector2(0.5f, 0.5f);
        cRect.pivot = new Vector2(0.5f, 0.5f);
        cRect.sizeDelta = new Vector2(580f, 320f);
        cRect.anchoredPosition = Vector2.zero;

        card.GetComponent<Image>().color = CardColor;
        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = VioletBorder;
        outline.effectDistance = new Vector2(2f, 2f);

        CreateLabel(
            "ConfirmTitle", card.transform,
            "ACTIVE EXPEDITION DETECTED", 20f, TextAccent,
            new Vector2(0f, 85f), new Vector2(520f, 40f),
            TextAlignmentOptions.Center, true
        );

        CreateLabel(
            "ConfirmBody", card.transform,
            "Starting a new expedition will abandon your active journey and overwrite saved checkpoint progress.\n\nAre you sure you wish to proceed?",
            14f, TextWhite,
            new Vector2(0f, 15f), new Vector2(500f, 75f),
            TextAlignmentOptions.Center, false
        );

        confirmNewRunButton = CreateSimpleButton(
            "ConfirmYes", card.transform,
            "START NEW RUN", new Vector2(-125f, -85f),
            new Vector2(210f, 52f), VioletBorder
        );
        confirmNewRunButton.onClick.AddListener(ExecuteNewRun);

        cancelConfirmButton = CreateSimpleButton(
            "ConfirmNo", card.transform,
            "CANCEL", new Vector2(125f, -85f),
            new Vector2(210f, 52f), SubduedBorder
        );
        cancelConfirmButton.onClick.AddListener(() => confirmDialog.SetActive(false));

        confirmDialog.SetActive(false);
    }

    private void BuildUnlockDialog(Transform parent)
    {
        unlockDialog = CreateUiObject("EchoArchiveDialog", parent, typeof(Image));
        RectTransform overlay = unlockDialog.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;
        unlockDialog.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

        GameObject card = CreateUiObject("EchoArchiveCard", unlockDialog.transform,
            typeof(Image), typeof(Outline));
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(700f, 550f);
        card.GetComponent<Image>().color = CardColor;
        card.GetComponent<Outline>().effectColor = VioletBorder;
        card.GetComponent<Outline>().effectDistance = new Vector2(2f, 2f);

        CreateLabel("EchoArchiveTitle", card.transform, "ECHO ARCHIVE", 28f,
            TextWhite, new Vector2(0f, 208f), new Vector2(640f, 44f),
            TextAlignmentOptions.Center, true);
        unlockBalanceText = CreateLabel("EchoArchiveBalance", card.transform,
            string.Empty, 17f, TextAccent, new Vector2(0f, 163f),
            new Vector2(620f, 34f), TextAlignmentOptions.Center, true);

        GameObject item = CreateUiObject("EchoPrismUnlock", card.transform,
            typeof(Image), typeof(Outline));
        RectTransform itemRect = item.GetComponent<RectTransform>();
        itemRect.anchorMin = itemRect.anchorMax = itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = new Vector2(0f, 20f);
        itemRect.sizeDelta = new Vector2(620f, 220f);
        item.GetComponent<Image>().color = ButtonNormal;
        item.GetComponent<Outline>().effectColor = SubduedBorder;
        CreateLabel("EchoPrismName", item.transform, "ECHO PRISM", 23f,
            TextWhite, new Vector2(0f, 75f), new Vector2(560f, 34f),
            TextAlignmentOptions.Center, true);
        CreateLabel("EchoPrismDescription", item.transform,
            "Unlocks Echo Prism for future Relic Shrine card choices. " +
            "Its first Basic Attack each battle deals +4 damage. " +
            "No starting stat increase.",
            16f, TextMuted, new Vector2(0f, 8f), new Vector2(540f, 84f),
            TextAlignmentOptions.Center, false);
        CreateLabel("EchoPrismCost", item.transform,
            "120 ECHO FRAGMENTS  •  ONE-TIME UNLOCK", 15f,
            TextAccent, new Vector2(0f, -74f), new Vector2(560f, 30f),
            TextAlignmentOptions.Center, true);

        buyUnlockButton = CreateSimpleButton("BuyEchoPrism", card.transform,
            "UNLOCK ECHO PRISM", new Vector2(0f, -151f),
            new Vector2(320f, 56f), VioletBorder);
        buyUnlockButton.onClick.AddListener(OnBuyUnlockClicked);
        unlockStatusText = CreateLabel("EchoArchiveStatus", card.transform,
            string.Empty, 13f, TextMuted, new Vector2(0f, -195f),
            new Vector2(620f, 28f), TextAlignmentOptions.Center, false);
        skillArchiveButton = CreateSimpleButton("OpenSkillArchive", card.transform,
            "SKILL ARCHIVE", new Vector2(-135f, -233f), new Vector2(250f, 44f),
            VioletBorder);
        Button close = CreateSimpleButton("CloseEchoArchive", card.transform,
            "CLOSE", new Vector2(150f, -233f), new Vector2(180f, 44f),
            SubduedBorder);
        close.onClick.AddListener(() => unlockDialog.SetActive(false));
        unlockDialog.SetActive(false);
    }

    private void RefreshUnlockStatus()
    {
        if (echoWallet == null || unlockMenuSummaryText == null) return;
        try
        {
            int balance = echoWallet.ReadBalance();
            bool owned = echoWallet.HasUnlock(HorizontalUnlockCatalog.EchoPrismId);
            bool hasActiveRun = inspection != null && inspection.HasUsableState;
            unlockMenuSummaryText.text = $"ECHO {balance}";
            if (unlockBalanceText == null) return;
            unlockBalanceText.text = $"ECHO FRAGMENTS  {balance}";
            unlockStatusText.text = owned ? "Already unlocked for future runs."
                : hasActiveRun ? "Finish or abandon your active run before purchasing."
                : balance < HorizontalUnlockCatalog.EchoPrismCost
                    ? "Not enough Echo Fragments yet."
                    : "Available for your next expedition.";
            buyUnlockButton.interactable = !owned && !hasActiveRun &&
                balance >= HorizontalUnlockCatalog.EchoPrismCost;
            buyUnlockButton.GetComponentInChildren<TMP_Text>().text = owned
                ? "UNLOCKED" : "UNLOCK ECHO PRISM";
        }
        catch (Exception error)
        {
            unlockMenuSummaryText.text = "UNAVAILABLE";
            if (unlockStatusText != null) unlockStatusText.text = error.Message;
            if (buyUnlockButton != null) buyUnlockButton.interactable = false;
        }
    }

    private void OnUnlockClicked()
    {
        RefreshCheckpointStatus();
        unlockDialog.SetActive(true);
        unlockDialog.transform.SetAsLastSibling();
    }

    private void OnHistoryClicked()
    {
        historyUI?.Show();
    }

    private void OnTopRunsClicked()
    {
        leaderboardUI?.Show();
    }

    private void OnBuyUnlockClicked()
    {
        if (inspection != null && inspection.HasUsableState) return;
        try
        {
            echoWallet.TryPurchase(HorizontalUnlockCatalog.EchoPrismId);
            RefreshUnlockStatus();
        }
        catch (Exception error)
        {
            unlockStatusText.text = error.Message;
            buyUnlockButton.interactable = false;
        }
    }

    private Button CreateSimpleButton(
        string name, Transform parent, string label,
        Vector2 pos, Vector2 size, Color borderColor)
    {
        GameObject obj = CreateUiObject(name, parent, typeof(Image), typeof(Button), typeof(Outline));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        obj.GetComponent<Image>().color = ButtonNormal;
        Outline ol = obj.GetComponent<Outline>();
        ol.effectColor = borderColor;
        ol.effectDistance = new Vector2(1.5f, 1.5f);

        Button btn = obj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = ButtonNormal;
        cb.highlightedColor = ButtonHighlight;
        cb.pressedColor = ButtonPressed;
        btn.colors = cb;

        CreateLabel(
            "Label", obj.transform, label,
            15f, TextWhite, Vector2.zero,
            size, TextAlignmentOptions.Center, true
        );

        return btn;
    }

    private void OnContinueClicked()
    {
        if (loadingInProgress || inspection == null || !inspection.HasUsableState) return;
        BeginBattleTransition(MainMenuIntent.ContinueRun);
    }

    private void OnNewRunClicked()
    {
        if (loadingInProgress) return;
        if (inspection != null && inspection.HasUsableState)
        {
            if (confirmDialog != null)
            {
                confirmDialog.SetActive(true);
                return;
            }
        }

        ExecuteNewRun();
    }

    private void ExecuteNewRun()
    {
        if (loadingInProgress) return;
        BeginBattleTransition(MainMenuIntent.NewRun);
    }

    private void BeginBattleTransition(MainMenuIntent intent)
    {
        if (canvas == null) return;
        loadingInProgress = true;
        MainMenuFlow.PendingIntent = intent;
        MainMenuLoadingUI loading = canvas.GetComponent<MainMenuLoadingUI>();
        if (loading == null) loading = canvas.gameObject.AddComponent<MainMenuLoadingUI>();
        loading.Begin(intent, () => loadingInProgress = false);
    }

    private void OnSettingsClicked()
    {
        if (settingsUI != null)
        {
            settingsUI.OpenSettings();
        }
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static GameObject CreateUiObject(string name, Transform parent, params Type[] components)
    {
        GameObject obj = new GameObject(name, components);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static TMP_Text CreateLabel(
        string name, Transform parent, string text,
        float fontSize, Color color, Vector2 anchoredPosition,
        Vector2 sizeDelta, TextAlignmentOptions alignment, bool bold)
    {
        GameObject obj = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;

        return tmp;
    }
}
