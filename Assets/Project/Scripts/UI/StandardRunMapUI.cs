using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StandardRunMapUI : MonoBehaviour
{
    private const string VisualAssetsResourcePath =
        "RunMap/FracturedFate/StandardRunMapVisualAssets";

    private static readonly Color BackdropColor =
        new Color(0.006f, 0.004f, 0.014f, 1f);

    private static readonly Color CardColor =
        new Color(0.018f, 0.012f, 0.035f, 0.99f);

    private static readonly Color RoutePanelColor =
        new Color(0.010f, 0.008f, 0.024f, 1f);

    private static readonly Color DetailPanelColor =
        new Color(0.022f, 0.014f, 0.042f, 0.98f);

    private static readonly Color Purple =
        new Color(0.61f, 0.20f, 0.98f, 1.00f);

    private static readonly Color BrightPurple =
        new Color(0.84f, 0.58f, 1.00f, 1.00f);

    private static readonly Color White =
        new Color(0.97f, 0.95f, 1.00f, 1.00f);

    private static readonly Color Muted =
        new Color(0.45f, 0.42f, 0.52f, 1.00f);

    private static readonly Color Completed =
        new Color(0.80f, 0.74f, 0.88f, 1f);

    private static readonly Color Locked =
        new Color(0.66f, 0.60f, 0.74f, 1f);

    private StandardRunMapVisualAssets visualAssets;
    private GameObject overlay;
    private RectTransform cardRect;
    private RectTransform fitRect;
    private Sprite trimmedConnector;
    private Sprite cleanConnector;
    private readonly List<Sprite> presentationSlices = new List<Sprite>();
    private CanvasGroup cardCanvasGroup;
    private Vector2 cardRestPosition;
    private Transform routeRoot;
    private ScrollRect routeScroll;
    private RunManager displayedRun;
    private StandardRunNode[] displayedNodes = new StandardRunNode[0];
    private TMP_Text detailTitle;
    private TMP_Text detailBody;
    private TMP_Text progressText;
    private Button enterNodeButton;
    private Button alternativeButton;
    private Button leaveMerchantButton;
    private GameObject checkpointWarning;
    private Button retryCheckpointButton;
    private Action retryCheckpoint;
    private bool checkpointRetryInProgress;
    private int resourceMaxHP = 100;
    private int resourceMaxMana = 10;
    public int MerchantChoiceIndex { get; private set; }
    public Button LeaveMerchantButton => leaveMerchantButton;
    public Button RestManaButton => alternativeButton;
    public bool RestoresMana { get; private set; }
    public int EventChoiceIndex { get; private set; }
    public Button EventAlternativeButton => alternativeButton;
    private TMP_Text enterNodeLabel;
    private Image detailIcon;
    private RectTransform currentPulseRect;
    private Image currentPulseImage;
    private Coroutine openingAnimation;

    private readonly List<Button> nodeButtons = new List<Button>();
    private readonly List<Image> nodeImages = new List<Image>();
    private readonly List<Image> nodeIcons = new List<Image>();
    private readonly List<TMP_Text> nodeLabels = new List<TMP_Text>();
    private readonly List<Image> routeLines = new List<Image>();
    private readonly List<Image> reachablePulses = new List<Image>();

    private StandardRunNodeMap currentMap;
    private StandardRunNode selectedNode;
    private Action<string> onEnterNode;
    private int currentDepth;
    private bool selectionCommitted;
    private bool uiBuilt;

    public bool IsOpen => overlay != null && overlay.activeSelf;
    public bool UsesAuthoredVisuals =>
        visualAssets != null && visualAssets.HasCompleteCoreSet;
    public GameObject Overlay => overlay;
    public Button EnterNodeButton => enterNodeButton;
    public GameObject CheckpointWarning => checkpointWarning;
    public Button RetryCheckpointButton => retryCheckpointButton;
    public TMP_Text DetailTitle => detailTitle;
    public TMP_Text DetailBody => detailBody;
    public Image DetailIcon => detailIcon;
    public IReadOnlyList<Button> NodeButtons => nodeButtons;
    public IReadOnlyList<Image> NodeImages => nodeImages;
    public IReadOnlyList<Image> NodeIcons => nodeIcons;
    public IReadOnlyList<Image> RouteLines => routeLines;
    public int CurrentDepth => currentDepth;
    public string SelectedNodeCode => selectedNode == null
        ? string.Empty
        : selectedNode.Code;

    private void Awake()
    {
        EnsureUiBuilt();
    }

    private void Update()
    {
        FitCardToCanvas();
        if (!IsOpen || currentPulseRect == null || currentPulseImage == null)
        {
            return;
        }

        float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.2f);
        float scale = Mathf.Lerp(1f, 1.075f, wave);
        foreach (Image pulse in reachablePulses)
        {
            pulse.rectTransform.localScale = new Vector3(scale, scale, 1f);
            Color pulseColor = pulse.color;
            pulseColor.a = Mathf.Lerp(0.52f, 0.88f, wave);
            pulse.color = pulseColor;
        }
    }

    public void Initialize(Action<string> enterNodeCallback)
    {
        EnsureUiBuilt();
        onEnterNode = enterNodeCallback;
    }

    public void Show(StandardRunNodeMap map, int activeDepth)
    {
        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        map.Validate();

        if (activeDepth < RunManager.FirstDepth || activeDepth > map.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(activeDepth));
        }

        EnsureUiBuilt();

        if (!uiBuilt)
        {
            return;
        }

        currentMap = map;
        currentDepth = activeDepth;
        StandardRunNode[] choices = displayedRun == null
            ? new[] { map.GetNodeAtDepth(activeDepth) } : displayedRun.ReachableNodes;
        selectedNode = choices.Length == 1 ? choices[0] : null;
        selectionCommitted = false;
        RestoresMana = false;
        EventChoiceIndex = 0;

        RebuildRoute();
        RefreshDetails();

        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        // The map fills the Canvas. Keep the battle Settings gear above it so
        // the player can save and return to Main Menu at this safe boundary.
        BattleSettingsUI settings = GetComponent<BattleSettingsUI>();
        if (settings != null && settings.SettingsButton != null)
            settings.SettingsButton.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        float contentHeight = ((RectTransform)routeRoot).rect.height;
        float viewHeight = routeScroll.viewport.rect.height;
        float offset = Mathf.Clamp((currentDepth - 2) * 190f - 20f, 0f, Mathf.Max(0f, contentHeight - viewHeight));
        routeScroll.verticalNormalizedPosition = offset / Mathf.Max(1f, contentHeight - viewHeight);
        PlayOpenAnimation();

        if (EventSystem.current != null && enterNodeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(
                enterNodeButton.gameObject
            );
        }
    }

    public void ShowRun(RunManager run)
    {
        displayedRun = run ?? throw new ArgumentNullException(nameof(run));
        Show(run.NodeMap, run.Depth);
    }

    public void SetResourceCaps(int maxHP, int maxMana)
    {
        resourceMaxHP = Mathf.Max(1, maxHP);
        resourceMaxMana = Mathf.Max(0, maxMana);
    }

    public void SetCheckpointSaveStatus(bool failed, Action onRetry)
    {
        EnsureUiBuilt();
        retryCheckpoint = onRetry;
        if (checkpointWarning == null || retryCheckpointButton == null) return;
        checkpointWarning.SetActive(failed);
        retryCheckpointButton.interactable = failed && onRetry != null &&
            !checkpointRetryInProgress;
    }

    private bool IsReachable(StandardRunNode node) => displayedRun == null
        ? node.Depth == currentDepth : displayedRun.CanEnterNode(node.Code);

    private bool WasVisited(StandardRunNode node) => displayedRun == null
        ? node.Depth < currentDepth : displayedRun.HasVisited(node.Code);

    public void HideImmediate()
    {
        if (openingAnimation != null)
        {
            StopCoroutine(openingAnimation);
            openingAnimation = null;
        }

        if (overlay != null)
        {
            overlay.SetActive(false);
        }

        ResetCardTransform();
        selectionCommitted = false;
    }

    private void EnsureUiBuilt()
    {
        if (uiBuilt && overlay != null && enterNodeButton != null)
        {
            return;
        }

        Canvas canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError(
                "StandardRunMapUI harus ditempatkan pada GameObject Canvas."
            );
            return;
        }

        visualAssets = Resources.Load<StandardRunMapVisualAssets>(
            VisualAssetsResourcePath
        );

        BuildOverlay(canvas.transform);
        uiBuilt = overlay != null && enterNodeButton != null;
    }

    private void BuildOverlay(Transform canvasTransform)
    {
        overlay = CreateUiObject(
            "StandardRunMapOverlay",
            canvasTransform,
            typeof(Image)
        );

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        StretchToParent(overlayRect);

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = BackdropColor;
        overlayImage.raycastTarget = true;

        GameObject fit = CreateUiObject("RunMapFit", overlay.transform);
        fitRect = fit.GetComponent<RectTransform>();
        SetCenteredRect(fitRect, Vector2.zero, new Vector2(1660f, 930f));

        GameObject card = CreateUiObject(
            "StandardRunMapCard",
            fit.transform,
            typeof(Image),
            typeof(CanvasGroup)
        );

        cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1660f, 930f);
        cardRestPosition = cardRect.anchoredPosition;

        Image cardImage = card.GetComponent<Image>();
        cardImage.color = CardColor;
        cardImage.type = Image.Type.Simple;
        cardImage.preserveAspect = false;
        cardImage.raycastTarget = true;

        cardCanvasGroup = card.GetComponent<CanvasGroup>();
        cardCanvasGroup.alpha = 1f;
        cardCanvasGroup.interactable = true;
        cardCanvasGroup.blocksRaycasts = true;

        // All decoration is below the content, never an atlas stretched over it.
        BuildSeparatedFrame(card.transform);

        CreateLabel(
            "RunMapTitle",
            card.transform,
            "SYNGRAVA ROUTE",
            39f,
            White,
            new Vector2(-435f, 391f),
            new Vector2(550f, 58f),
            TextAlignmentOptions.Left,
            false
        );

        CreateLabel(
            "RunMapSubtitle",
            card.transform,
            "Choose a connected path. Scroll or drag to inspect the route.",
            17f,
            new Color(0.76f, 0.68f, 0.86f, 1f),
            new Vector2(-320f, 350f),
            new Vector2(780f, 34f),
            TextAlignmentOptions.Left,
            false
        );

        progressText = CreateLabel(
            "RunMapProgress",
            card.transform,
            string.Empty,
            18f,
            BrightPurple,
            new Vector2(620f, 390f),
            new Vector2(300f, 40f),
            TextAlignmentOptions.Right,
            false
        );

        GameObject routePanel = CreatePanel(
            "RunMapRoutePanel",
            card.transform,
            new Vector2(-156.5f, -30f),
            new Vector2(1087f, 700f),
            RoutePanelColor,
            new Color(0.31f, 0.14f, 0.47f, 0.72f),
            null
        );

        if (visualAssets != null && visualAssets.Backdrop != null)
        {
            Image backing = routePanel.GetComponent<Image>();
            backing.sprite = SliceSprite("QuietPaper", visualAssets.Backdrop,
                new Rect(0.22f, 0.22f, 0.56f, 0.56f));
            backing.color = new Color(0.58f, 0.58f, 0.64f, 1f);
        }

        GameObject viewport = CreateUiObject("RunMapViewport", routePanel.transform,
            typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        SetCenteredRect(viewportRect, new Vector2(-28.5f, 36f), new Vector2(1000f, 566f));
        viewport.GetComponent<Image>().color = Color.clear;
        routeScroll = viewport.GetComponent<ScrollRect>();
        routeScroll.viewport = viewportRect;
        routeScroll.horizontal = false;
        routeScroll.vertical = true;
        routeScroll.movementType = ScrollRect.MovementType.Clamped;
        routeScroll.scrollSensitivity = 42f;

        GameObject routeContent = CreateUiObject(
            "RunMapRouteContent",
            viewport.transform
        );
        RectTransform routeRect = routeContent.GetComponent<RectTransform>();
        routeRect.anchorMin = new Vector2(0.5f, 0f);
        routeRect.anchorMax = new Vector2(0.5f, 0f);
        routeRect.pivot = new Vector2(0.5f, 0f);
        routeRect.anchoredPosition = Vector2.zero;
        routeRect.sizeDelta = new Vector2(1000f, 1380f);
        routeScroll.content = routeRect;
        routeRoot = routeContent.transform;

        CreateLegend(routePanel.transform);

        GameObject detailPanel = CreatePanel(
            "RunMapDetailPanel",
            card.transform,
            new Vector2(592f, -30f),
            new Vector2(410f, 700f),
            DetailPanelColor,
            new Color(0.50f, 0.24f, 0.74f, 0.85f),
            null
        );

        CreateLabel(
            "RunMapDetailHeader",
            detailPanel.transform,
            "NODE DETAILS",
            19f,
            BrightPurple,
            new Vector2(0f, 298f),
            new Vector2(342f, 38f),
            TextAlignmentOptions.Center,
            false
        );

        detailIcon = CreateImage(
            "RunMapDetailIcon",
            detailPanel.transform,
            null,
            new Vector2(0f, 230f),
            new Vector2(82f, 82f),
            White
        );
        detailIcon.preserveAspect = true;

        detailTitle = CreateLabel(
            "RunMapDetailTitle",
            detailPanel.transform,
            string.Empty,
            26f,
            White,
            new Vector2(0f, 166f),
            new Vector2(340f, 62f),
            TextAlignmentOptions.Center,
            true
        );

        detailBody = CreateLabel(
            "RunMapDetailBody",
            detailPanel.transform,
            string.Empty,
            16f,
            new Color(0.84f, 0.80f, 0.89f, 1f),
            new Vector2(0f, -18f),
            new Vector2(330f, 290f),
            TextAlignmentOptions.TopLeft,
            true
        );

        GameObject enterObject = CreateUiObject(
            "EnterRunNodeButton",
            detailPanel.transform,
            typeof(Image),
            typeof(Button),
            typeof(Outline)
        );

        RectTransform enterRect = enterObject.GetComponent<RectTransform>();
        SetCenteredRect(
            enterRect,
            new Vector2(0f, -282f),
            new Vector2(310f, 68f)
        );

        Image enterImage = enterObject.GetComponent<Image>();
        enterImage.color = new Color(0.18f, 0.06f, 0.31f, 1f);
        enterImage.raycastTarget = true;

        Outline enterOutline = enterObject.GetComponent<Outline>();
        enterOutline.effectColor = BrightPurple;
        enterOutline.effectDistance = new Vector2(2f, 2f);
        enterOutline.useGraphicAlpha = true;

        enterNodeButton = enterObject.GetComponent<Button>();
        enterNodeButton.targetGraphic = enterImage;
        enterNodeButton.colors = CreateButtonColors();
        enterNodeButton.onClick.AddListener(CommitSelectedNode);

        enterNodeLabel = CreateLabel(
            "EnterRunNodeLabel",
            enterObject.transform,
            "ENTER NODE",
            18f,
            White,
            Vector2.zero,
            new Vector2(290f, 58f),
            TextAlignmentOptions.Center,
            false
        );

        GameObject alternativeObject = Instantiate(enterObject, detailPanel.transform);
        alternativeObject.name = "RunNodeAlternativeButton";
        alternativeButton = alternativeObject.GetComponent<Button>();
        alternativeButton.onClick = new Button.ButtonClickedEvent();
        alternativeButton.onClick.AddListener(() => {
            if (selectionCommitted || selectedNode == null || !alternativeButton.interactable) return;
            if (selectedNode.Type == StandardRunNodeType.Rest) RestoresMana = true;
            else if (selectedNode.Type == StandardRunNodeType.RandomEvent) EventChoiceIndex = 1;
            else if (selectedNode.Type == StandardRunNodeType.Merchant) MerchantChoiceIndex = 1;
            else return;
            CommitSelectedNode();
        });
        alternativeObject.GetComponentInChildren<TMP_Text>().text = "RESTORE 50% MANA";
        alternativeObject.SetActive(false);

        GameObject leaveObject = Instantiate(enterObject, detailPanel.transform);
        leaveObject.name = "LeaveMerchantButton";
        leaveMerchantButton = leaveObject.GetComponent<Button>();
        leaveMerchantButton.onClick = new Button.ButtonClickedEvent();
        leaveMerchantButton.onClick.AddListener(() => {
            if (selectionCommitted || selectedNode?.Type != StandardRunNodeType.Merchant ||
                !leaveMerchantButton.interactable) return;
            MerchantChoiceIndex = RunMerchantService.LeaveChoice;
            CommitSelectedNode();
        });
        leaveObject.GetComponentInChildren<TMP_Text>().text = "LEAVE MERCHANT";
        SetCenteredRect(leaveObject.GetComponent<RectTransform>(), new Vector2(0f, -296f), new Vector2(310f, 56f));
        leaveObject.SetActive(false);

        BuildCheckpointWarning(card.transform, enterObject);
        FitCardToCanvas();
        overlay.SetActive(false);
    }

    private void BuildCheckpointWarning(Transform parent, GameObject buttonTemplate)
    {
        checkpointWarning = CreatePanel(
            "RunMapCheckpointWarning", parent,
            new Vector2(0f, -412f), new Vector2(1470f, 60f),
            CardColor, BrightPurple, null
        );
        CreateLabel(
            "RunMapCheckpointWarningText", checkpointWarning.transform,
            "PROGRESS NOT SAVED. Retry before closing the game.",
            18f, White, new Vector2(-145f, 0f),
            new Vector2(1150f, 44f), TextAlignmentOptions.Left, false
        );
        GameObject retryObject = Instantiate(buttonTemplate, checkpointWarning.transform);
        retryObject.name = "RetryCheckpointButton";
        SetCenteredRect(retryObject.GetComponent<RectTransform>(),
            new Vector2(600f, 0f), new Vector2(240f, 44f));
        retryCheckpointButton = retryObject.GetComponent<Button>();
        retryCheckpointButton.onClick = new Button.ButtonClickedEvent();
        retryCheckpointButton.onClick.AddListener(RetryCheckpointSave);
        TMP_Text label = retryObject.GetComponentInChildren<TMP_Text>();
        label.name = "RetryCheckpointLabel";
        label.text = "RETRY SAVE";
        label.rectTransform.sizeDelta = new Vector2(220f, 38f);
        checkpointWarning.SetActive(false);
    }

    private void RetryCheckpointSave()
    {
        if (!IsOpen || !checkpointWarning.activeSelf ||
            !retryCheckpointButton.interactable || checkpointRetryInProgress ||
            retryCheckpoint == null) return;

        checkpointRetryInProgress = true;
        retryCheckpointButton.interactable = false;
        try
        {
            retryCheckpoint();
        }
        finally
        {
            checkpointRetryInProgress = false;
            retryCheckpointButton.interactable = checkpointWarning.activeSelf &&
                retryCheckpoint != null;
        }
    }

    private void FitCardToCanvas()
    {
        if (fitRect == null || overlay == null) return;
        Rect bounds = overlay.GetComponent<RectTransform>().rect;
        float scale = Mathf.Min(1f, (bounds.width - 64f) / 1660f,
            (bounds.height - 64f) / 930f);
        fitRect.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }

    // Normalized rectangles use the atlas's top-left origin for art authoring.
    // Slicing only creates Sprite views; the source PNG and its alpha stay intact.
    private Sprite SliceSprite(string name, Sprite source, Rect region)
    {
        if (source == null) return null;
        Rect atlas = source.rect;
        Rect pixels = new Rect(atlas.x + region.x * atlas.width,
            atlas.y + (1f - region.yMax) * atlas.height,
            region.width * atlas.width, region.height * atlas.height);
        Sprite slice = Sprite.Create(source.texture, pixels,
            new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        slice.name = name;
        presentationSlices.Add(slice);
        return slice;
    }

    private void BuildSeparatedFrame(Transform parent)
    {
        GameObject frame = CreateUiObject("RunMapOuterFrame", parent);
        StretchToParent(frame.GetComponent<RectTransform>());

        if (visualAssets == null) return;
        Sprite sheet = visualAssets.Ornaments;
        Sprite paper = SliceSprite("TornPaperStrip", sheet,
            new Rect(0.24f, 0.05f, 0.35f, 0.265f));
        Sprite shard = SliceSprite("SingleCrystal", sheet,
            new Rect(0.43f, 0.325f, 0.10f, 0.215f));
        Sprite eye = SliceSprite("EyeAccent", sheet,
            new Rect(0.315f, 0.405f, 0.157f, 0.29f));
        Sprite lowerPaper = SliceSprite("LowerPaperStrip", sheet,
            new Rect(0.735f, 0.82f, 0.25f, 0.17f));

        // Header, left spine, footer and detail perimeter are independent Images.
        PlaceDecoration(frame.transform, "HeaderTear", paper,
            new Vector2(48.5f, 328f), new Vector2(1497f, 55f), 0f);
        PlaceDecoration(frame.transform, "LeftPaperSpine", paper,
            new Vector2(-764f, 75f), new Vector2(545f, 86f), 87f);
        PlaceDecoration(frame.transform, "FooterTear", lowerPaper,
            new Vector2(48.5f, -408f), new Vector2(1497f, 64f), 0f);
        PlaceDecoration(frame.transform, "TopLeftCrystal", shard,
            new Vector2(-757f, 387f), new Vector2(102f, 132f), -18f);
        PlaceDecoration(frame.transform, "LowerLeftCrystal", shard,
            new Vector2(-763f, -376f), new Vector2(100f, 136f), 142f);
        PlaceDecoration(frame.transform, "LeftEyeAccent", eye,
            new Vector2(-767f, 133f), new Vector2(91f, 92f), 0f);
        PlaceDecoration(frame.transform, "DetailCornerCrystal", shard,
            new Vector2(794f, 353f), new Vector2(70f, 114f), -24f);
        PlaceDecoration(frame.transform, "DetailLowerCrystal", shard,
            new Vector2(791f, -354f), new Vector2(68f, 100f), 160f);

        trimmedConnector = SliceSprite("TrimmedRouteStroke",
            visualAssets.ConnectorStroke, new Rect(0.015f, 0.43f, 0.97f, 0.13f));
    }

    private void PlaceDecoration(Transform parent, string name, Sprite sprite,
        Vector2 position, Vector2 size, float rotation)
    {
        Image decoration = CreateImage(name, parent, sprite, position, size, Color.white);
        decoration.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private void CreateLegend(Transform parent)
    {
        StandardRunNodeType[] types =
        {
            StandardRunNodeType.NormalBattle,
            StandardRunNodeType.EliteBattle,
            StandardRunNodeType.RandomEvent,
            StandardRunNodeType.Rest,
            StandardRunNodeType.Merchant,
            StandardRunNodeType.Boss,
            StandardRunNodeType.Relic
        };

        string[] names =
        {
            "BATTLE",
            "ELITE",
            "EVENT",
            "REST",
            "MERCHANT",
            "BOSS",
            "RELIC"
        };

        GameObject legendRoot = CreateUiObject(
            "RunMapLegend",
            parent
        );
        RectTransform legendRect = legendRoot.GetComponent<RectTransform>();
        SetCenteredRect(
            legendRect,
            new Vector2(0f, -310f),
            new Vector2(1040f, 62f)
        );

        const float itemWidth = 130f;
        float left = -(types.Length - 1) * itemWidth * 0.5f;

        for (int index = 0; index < types.Length; index++)
        {
            GameObject item = CreateUiObject(
                $"RunMapLegend_{types[index]}",
                legendRoot.transform
            );
            RectTransform itemRect = item.GetComponent<RectTransform>();
            SetCenteredRect(
                itemRect,
                new Vector2(left + index * itemWidth, 0f),
                new Vector2(itemWidth, 56f)
            );

            Sprite icon = visualAssets == null
                ? null
                : visualAssets.GetNodeIcon(types[index]);

            if (icon != null)
            {
                Image iconImage = CreateImage(
                    "LegendIcon",
                    item.transform,
                    icon,
                    new Vector2(-48f, 0f),
                    new Vector2(36f, 36f),
                    new Color(0.88f, 0.80f, 1f, 0.92f)
                );
                iconImage.preserveAspect = true;
            }

            CreateLabel(
                "LegendLabel",
                item.transform,
                names[index],
                12f,
                new Color(0.74f, 0.69f, 0.80f, 1f),
                new Vector2(18f, 0f),
                new Vector2(112f, 32f),
                TextAlignmentOptions.Left,
                false
            );
        }
    }

    private void RebuildRoute()
    {
        ClearRouteObjects();

        if (currentMap == null || routeRoot == null)
        {
            return;
        }

        progressText.text = $"NODE {currentDepth} / {currentMap.Count}";

        displayedNodes = currentMap.AllNodes;
        ((RectTransform)routeRoot).sizeDelta = new Vector2(1000f, Mathf.Max(routeScroll.viewport.rect.height, 260f + (currentDepth - 1) * 190f));
        foreach (StandardRunNode from in displayedNodes)
            foreach (string code in from.NextNodeCodes)
            {
                StandardRunNode to = currentMap.FindNode(code);
                bool travelled = WasVisited(from) && WasVisited(to);
                bool reachable = displayedRun != null && displayedRun.LastVisitedNodeCode == from.Code && IsReachable(to);
                CreateRouteLine(GetGraphPosition(from), GetGraphPosition(to), travelled, reachable);
                routeLines[routeLines.Count - 1].gameObject.SetActive(travelled || reachable);
            }
        foreach (StandardRunNode node in displayedNodes)
        {
            CreateNodeButton(node, GetGraphPosition(node));
            nodeButtons[nodeButtons.Count - 1].gameObject.SetActive(WasVisited(node) || IsReachable(node));
        }
    }

    private void ClearRouteObjects()
    {
        nodeButtons.Clear();
        nodeImages.Clear();
        nodeIcons.Clear();
        nodeLabels.Clear();
        routeLines.Clear();
        reachablePulses.Clear();
        currentPulseRect = null;
        currentPulseImage = null;

        if (routeRoot == null)
        {
            return;
        }

        for (int index = routeRoot.childCount - 1; index >= 0; index--)
        {
            GameObject child = routeRoot.GetChild(index).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private Vector2 GetGraphPosition(StandardRunNode node) =>
        new Vector2(node.Depth == 3 && currentMap.RouteVersion > 0
            ? (node.Lane - 1.5f) * 240f : (node.Lane - 1) * 310f,
            130f + (node.Depth - 1) * 190f);

    private void CreateRouteLine(Vector2 start, Vector2 end, bool completed, bool reachable)
    {
        GameObject lineObject = CreateUiObject(
            "RunMapRouteLine",
            routeRoot,
            typeof(Image)
        );

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        Vector2 delta = end - start;
        // Stop at the square plaque edge, keeping the icon and label clear.
        float edgeInset = 44f * delta.magnitude /
            Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
        float length = Mathf.Max(1f, delta.magnitude - edgeInset * 2f);
        lineRect.anchorMin = new Vector2(0.5f, 0f);
        lineRect.anchorMax = new Vector2(0.5f, 0f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = (start + end) * 0.5f;
        lineRect.sizeDelta = new Vector2(
            length,
            completed || reachable ? 4f : 2f
        );
        lineRect.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg
        );

        Image line = lineObject.GetComponent<Image>();
        if (cleanConnector == null)
        {
            cleanConnector = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f));
            presentationSlices.Add(cleanConnector);
        }
        line.sprite = cleanConnector;
        // Use a continuous UI quad. The authored stroke's ragged pixels are
        // appropriate for frame art but broke long diagonal path readability.
        line.type = Image.Type.Simple;
        line.preserveAspect = false;
        line.color = completed
            ? new Color(0.94f, 0.86f, 1f, 0.96f)
            : reachable ? BrightPurple : new Color(0.46f, 0.40f, 0.56f, 0.55f);
        line.raycastTarget = false;
        routeLines.Add(line);
    }

    private void CreateNodeButton(StandardRunNode node, Vector2 position)
    {
        bool isCurrent = IsReachable(node);

        if (isCurrent &&
            visualAssets != null &&
            visualAssets.StateRing != null)
        {
            currentPulseImage = CreateImage(
                $"RunNodePulse_{node.Code}",
                routeRoot,
                visualAssets.StateRing,
                position,
                node.IsFinal
                    ? new Vector2(154f, 154f)
                    : new Vector2(142f, 142f),
                new Color(0.85f, 0.55f, 1f, 0.72f)
            );
            currentPulseImage.preserveAspect = true;
            reachablePulses.Add(currentPulseImage);
            currentPulseRect = currentPulseImage.rectTransform;
            currentPulseRect.anchorMin = currentPulseRect.anchorMax = new Vector2(0.5f, 0f);
        }

        GameObject nodeObject = CreateUiObject(
            $"RunNode_{node.Code}",
            routeRoot,
            typeof(Image),
            typeof(Button)
        );

        RectTransform nodeRect = nodeObject.GetComponent<RectTransform>();
        SetCenteredRect(
            nodeRect,
            position,
            node.IsFinal
                ? new Vector2(124f, 124f)
                : new Vector2(104f, 104f)
        );
        nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0.5f, 0f);

        Image image = nodeObject.GetComponent<Image>();
        image.sprite = visualAssets == null
            ? null
            : visualAssets.NodePlaque;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = GetNodeColorForDepth(node);
        image.raycastTarget = true;

        Button button = nodeObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.colors = CreateButtonColors();
        ColorBlock nodeColors = button.colors;
        // State tint is applied once; Selectable must not darken it a second time.
        nodeColors.disabledColor = Color.white;
        button.colors = nodeColors;
        button.interactable = isCurrent;

        int nodeIndex = Array.IndexOf(displayedNodes, node);
        button.onClick.AddListener(() => SelectNode(nodeIndex));

        if ((displayedRun == null ? isCurrent :
                displayedRun.LastVisitedNodeCode == node.Code || (currentDepth == 1 && isCurrent)) &&
            visualAssets != null &&
            visualAssets.CurrentMarker != null)
        {
            Image currentMarker = CreateImage(
                "CurrentNodeMarker",
                nodeObject.transform,
                visualAssets.CurrentMarker,
                new Vector2(-65f, 0f),
                new Vector2(48f, 48f),
                Color.white
            );
            currentMarker.preserveAspect = true;
        }

        Sprite nodeIconSprite = visualAssets == null
            ? null
            : visualAssets.GetNodeIcon(node.Type);
        Image nodeIcon = CreateImage(
            "RunNodeIcon",
            nodeObject.transform,
            nodeIconSprite,
            Vector2.zero,
            node.IsFinal
                ? new Vector2(70f, 70f)
                : new Vector2(60f, 60f),
            GetNodeIconColor(node)
        );
        nodeIcon.preserveAspect = true;
        nodeIcons.Add(nodeIcon);

        if (nodeIconSprite == null)
        {
            CreateLabel(
                "RunNodeFallbackGlyph",
                nodeObject.transform,
                GetNodeGlyph(node.Type),
                node.IsFinal ? 29f : 25f,
                node.Depth > currentDepth ? Muted : White,
                Vector2.zero,
                new Vector2(78f, 42f),
                TextAlignmentOptions.Center,
                false
            );
        }

        TMP_Text stateLabel = CreateLabel(
            "RunNodeDepth",
            nodeObject.transform,
            WasVisited(node)
                ? $"{node.Depth:00}  CLEARED"
                : isCurrent
                    ? $"{node.Depth:00}  AVAILABLE"
                    : node.Depth <= currentDepth ? $"{node.Depth:00}  SKIPPED" : $"{node.Depth:00}  LOCKED",
            10f,
            isCurrent ? BrightPurple : Muted,
            new Vector2(0f, node.IsFinal ? -73f : -63f),
            new Vector2(128f, 24f),
            TextAlignmentOptions.Center,
            false
        );
        // State is communicated by the ring/path tint and the detail panel.
        // Tiny captions at the connector endpoints make the web harder to read.
        stateLabel.gameObject.SetActive(false);

        nodeButtons.Add(button);
        nodeImages.Add(image);
        nodeLabels.Add(stateLabel);
    }

    private void SelectNode(int index)
    {
        if (selectionCommitted ||
            currentMap == null ||
            index < 0 ||
            index >= displayedNodes.Length)
        {
            return;
        }

        StandardRunNode node = displayedNodes[index];

        if (node == null || !IsReachable(node))
        {
            return;
        }

        selectedNode = node;
        for (int i = 0; i < nodeImages.Count; i++)
        {
            nodeImages[i].color = GetNodeColorForDepth(displayedNodes[i]);
            if (IsReachable(displayedNodes[i]))
                nodeLabels[i].text = $"{displayedNodes[i].Depth:00}  " +
                    (displayedNodes[i] == selectedNode ? "SELECTED" : "AVAILABLE");
        }
        nodeImages[index].color = new Color(1f, 0.78f, 1f, 1f);
        RefreshDetails();
    }

    private void RefreshDetails()
    {
        bool isRest = selectedNode != null && selectedNode.Type == StandardRunNodeType.Rest;
        bool isEvent = selectedNode != null && selectedNode.Type == StandardRunNodeType.RandomEvent;
        bool isMerchant = selectedNode != null && selectedNode.Type == StandardRunNodeType.Merchant;
        alternativeButton.gameObject.SetActive(isRest || isEvent || isMerchant);
        leaveMerchantButton.gameObject.SetActive(isMerchant);
        leaveMerchantButton.interactable = !selectionCommitted;
        alternativeButton.interactable = !selectionCommitted;
        SetCenteredRect(enterNodeButton.GetComponent<RectTransform>(),
            new Vector2(0f, isMerchant ? -156f : isRest || isEvent ? -202f : -282f),
            new Vector2(310f, isMerchant ? 56f : 68f));
        SetCenteredRect(alternativeButton.GetComponent<RectTransform>(),
            new Vector2(0f, isMerchant ? -226f : -282f), new Vector2(310f, isMerchant ? 56f : 68f));
        detailBody.rectTransform.sizeDelta = new Vector2(330f, isMerchant ? 240f : 290f);
        detailBody.rectTransform.anchoredPosition = new Vector2(0f, isMerchant ? 7f : -18f);
        RestoresMana = false;
        EventChoiceIndex = 0;
        MerchantChoiceIndex = 0;
        if (selectedNode == null)
        {
            detailTitle.text = "CHOOSE YOUR PATH";
            detailBody.text = "Select one of the highlighted connected nodes.\n\n" +
                "Violet: available path\nWhite: travelled path\n\n" +
                "Complete a node to reveal the next choices. Select a node, then confirm below.";
            detailIcon.sprite = null;
            detailIcon.enabled = false;
            enterNodeButton.interactable = false;
            enterNodeLabel.text = "SELECT A NODE";
            return;
        }

        detailTitle.text = GetNodeDisplayName(selectedNode.Type);
        detailBody.text =
            $"STAGE {selectedNode.Depth} / {currentMap.Count}\n\n" +
            GetNodeDescription(selectedNode.Type);
        enterNodeLabel.text = GetEnterButtonLabel(selectedNode.Type);
        if (isRest) enterNodeLabel.text = "RESTORE 50% HP";
        alternativeButton.GetComponentInChildren<TMP_Text>().text = "RESTORE 50% MANA";
        enterNodeButton.interactable = !selectionCommitted;
        if (isEvent)
        {
            RunEventService.Encounter encounter = RunEventService.GetEncounter(currentMap.RunSeed, selectedNode.Code);
            detailTitle.text = encounter.Title;
            detailBody.text = encounter.Story + "\n\n" + encounter.First.Description + "\n\n" +
                encounter.Second.Description + "\n\nChoose one. Recovery cannot exceed your maximum.";
            enterNodeLabel.text = encounter.First.Label;
            alternativeButton.GetComponentInChildren<TMP_Text>().text = encounter.Second.Label;
            enterNodeButton.interactable = !selectionCommitted && RunEventService.CanChoose(displayedRun, selectedNode.Code, 0);
            alternativeButton.interactable = !selectionCommitted && RunEventService.CanChoose(displayedRun, selectedNode.Code, 1);
        }
        if (isMerchant)
        {
            detailTitle.text = "THE ASHEN TRADER";
            detailBody.text = $"EMBERS  {displayedRun?.TotalEmbers ?? 0}\n\n" +
                "Choose one supply, or leave.\n\n" +
                $"Balm: {RunMerchantService.HealthPrice} Embers. Restore 50% of maximum HP.\n\n" +
                $"Tonic: {RunMerchantService.ManaPrice} Embers. Restore 50% of maximum Mana.\n\n" +
                "Full resources cannot be purchased.";
            enterNodeLabel.text = $"HEAL HP - {RunMerchantService.HealthPrice} EMBERS";
            alternativeButton.GetComponentInChildren<TMP_Text>().text = $"HEAL MANA - {RunMerchantService.ManaPrice} EMBERS";
            enterNodeButton.interactable = !selectionCommitted && CanChooseMerchant(0);
            alternativeButton.interactable = !selectionCommitted && CanChooseMerchant(1);
        }

        Sprite selectedIcon = visualAssets == null
            ? null
            : visualAssets.GetNodeIcon(selectedNode.Type);
        detailIcon.sprite = selectedIcon;
        detailIcon.enabled = selectedIcon != null;
        detailIcon.color = White;
    }

    private void CommitSelectedNode()
    {
        if (selectionCommitted || selectedNode == null || onEnterNode == null)
        {
            return;
        }
        if (selectedNode.Type == StandardRunNodeType.RandomEvent &&
            !RunEventService.CanChoose(displayedRun, selectedNode.Code, EventChoiceIndex)) return;
        if (selectedNode.Type == StandardRunNodeType.Merchant && !CanChooseMerchant(MerchantChoiceIndex)) return;

        selectionCommitted = true;
        enterNodeButton.interactable = false;
        alternativeButton.interactable = false;
        leaveMerchantButton.interactable = false;

        for (int index = 0; index < nodeButtons.Count; index++)
        {
            nodeButtons[index].interactable = false;
        }

        onEnterNode(selectedNode.Code);
    }

    private bool CanChooseMerchant(int choice) => selectedNode != null &&
        RunMerchantService.CanChoose(displayedRun, selectedNode.Code, choice, resourceMaxHP, resourceMaxMana);

    private Color GetNodeColorForDepth(StandardRunNode node)
    {
        if (WasVisited(node))
        {
            return Completed;
        }

        if (IsReachable(node))
        {
            return visualAssets == null
                ? new Color(0.28f, 0.10f, 0.48f, 1f)
                : Color.white;
        }

        return Locked;
    }

    private Color GetNodeIconColor(StandardRunNode node)
    {
        if (WasVisited(node))
        {
            return new Color(0.88f, 0.82f, 0.94f, 1f);
        }

        if (!IsReachable(node))
        {
            return new Color(0.70f, 0.64f, 0.80f, 0.85f);
        }

        return White;
    }

    private static string GetNodeGlyph(StandardRunNodeType type)
    {
        switch (type)
        {
            case StandardRunNodeType.Relic:
                return "O";
            case StandardRunNodeType.NormalBattle:
                return "B";
            case StandardRunNodeType.EliteBattle:
                return "E";
            case StandardRunNodeType.RandomEvent:
                return "?";
            case StandardRunNodeType.Rest:
                return "R";
            case StandardRunNodeType.Merchant:
                return "M";
            case StandardRunNodeType.Boss:
                return "X";
            default:
                return "?";
        }
    }

    private static string GetNodeDisplayName(StandardRunNodeType type)
    {
        switch (type)
        {
            case StandardRunNodeType.Relic:
                return "RELIC SHRINE";
            case StandardRunNodeType.NormalBattle:
                return "BATTLE";
            case StandardRunNodeType.EliteBattle:
                return "ELITE";
            case StandardRunNodeType.RandomEvent:
                return "UNKNOWN ENCOUNTER";
            case StandardRunNodeType.Rest:
                return "REST";
            case StandardRunNodeType.Merchant:
                return "MERCHANT";
            case StandardRunNodeType.Boss:
                return "FINAL BOSS";
            default:
                return "UNKNOWN NODE";
        }
    }

    private static string GetNodeDescription(StandardRunNodeType type)
    {
        switch (type)
        {
            case StandardRunNodeType.Relic:
                return "Choose one of three relics.\n\nEach changes how you fight for this journey only. Relics are unique and cannot stack.\n\nChoosing this path replaces another encounter.";
            case StandardRunNodeType.NormalBattle:
                return "Defeat the enemy and choose one reward.\n\n" +
                    "Your remaining health and mana carry into the next stage.";
            case StandardRunNodeType.EliteBattle:
                return "Face an elite encounter.\n\nWin to choose a run reward.";
            case StandardRunNodeType.RandomEvent:
                return "Discover an encounter and choose one outcome.";
            case StandardRunNodeType.Rest:
                return "Choose one recovery:\n\nRestore 50% of your maximum HP, or 50% of your maximum Mana.\n\nCannot exceed your maximum. Choosing completes this stop.";
            case StandardRunNodeType.Merchant:
                return "A merchant's stopping place.\n\n" +
                    "Spend Embers on one healing supply, or leave without buying.";
            case StandardRunNodeType.Boss:
                return "The final confrontation.\n\nDefeat the boss to complete your journey.";
            default:
                return "No node description is available.";
        }
    }

    private static string GetEnterButtonLabel(StandardRunNodeType type)
    {
        switch (type)
        {
            case StandardRunNodeType.Relic:
                return "DISCOVER RELICS";
            case StandardRunNodeType.NormalBattle:
                return "ENTER BATTLE";
            case StandardRunNodeType.EliteBattle:
                return "ENTER ELITE";
            case StandardRunNodeType.Boss:
                return "CHALLENGE BOSS";
            default:
                return "CONTINUE";
        }
    }

    private GameObject CreatePanel(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Color color,
        Color outlineColor,
        Sprite sprite
    )
    {
        GameObject panel = CreateUiObject(
            objectName,
            parent,
            typeof(Image),
            typeof(Outline)
        );

        RectTransform rect = panel.GetComponent<RectTransform>();
        SetCenteredRect(rect, position, size);

        Image image = panel.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = sprite == null ? color : Color.white;
        image.raycastTarget = true;

        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(2f, 2f);
        outline.useGraphicAlpha = true;
        outline.enabled = sprite == null;
        return panel;
    }

    private Image CreateImage(
        string objectName,
        Transform parent,
        Sprite sprite,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color
    )
    {
        GameObject imageObject = CreateUiObject(
            objectName,
            parent,
            typeof(Image)
        );
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, anchoredPosition, size);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.color = color;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text CreateLabel(
        string objectName,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        Vector2 anchoredPosition,
        Vector2 size,
        TextAlignmentOptions alignment,
        bool wrap
    )
    {
        GameObject labelObject = CreateUiObject(
            objectName,
            parent,
            typeof(TextMeshProUGUI)
        );

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        SetCenteredRect(rect, anchoredPosition, size);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = wrap
            ? TextWrappingModes.Normal
            : TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        if (font == null)
        {
            font = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF"
            );
        }

        if (font != null)
        {
            label.font = font;
        }

        return label;
    }

    private void PlayOpenAnimation()
    {
        if (cardRect == null || cardCanvasGroup == null)
        {
            return;
        }

        if (openingAnimation != null)
        {
            StopCoroutine(openingAnimation);
        }

        openingAnimation = StartCoroutine(AnimateOpen());
    }

    private IEnumerator AnimateOpen()
    {
        const float duration = 0.36f;
        float elapsed = 0f;

        cardCanvasGroup.alpha = 0f;
        cardRect.localScale = new Vector3(0.965f, 0.965f, 1f);
        cardRect.anchoredPosition = cardRestPosition + new Vector2(0f, -22f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = normalized * normalized * (3f - 2f * normalized);
            cardCanvasGroup.alpha = eased;
            float scale = Mathf.Lerp(0.965f, 1f, eased);
            cardRect.localScale = new Vector3(scale, scale, 1f);
            cardRect.anchoredPosition = Vector2.Lerp(
                cardRestPosition + new Vector2(0f, -22f),
                cardRestPosition,
                eased
            );
            yield return null;
        }

        ResetCardTransform();
        openingAnimation = null;
    }

    private void ResetCardTransform()
    {
        if (cardCanvasGroup != null)
        {
            cardCanvasGroup.alpha = 1f;
        }

        if (cardRect != null)
        {
            cardRect.localScale = Vector3.one;
            cardRect.anchoredPosition = cardRestPosition;
        }
    }

    private static void SetCenteredRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size
    )
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.58f, 0.95f, 1f);
        colors.selectedColor = new Color(0.92f, 0.78f, 1f, 1f);
        colors.disabledColor = new Color(0.56f, 0.53f, 0.62f, 0.54f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.12f;
        return colors;
    }

    private GameObject CreateUiObject(
        string objectName,
        Transform parent,
        params Type[] componentTypes
    )
    {
        Type[] types = new Type[componentTypes.Length + 1];
        types[0] = typeof(RectTransform);
        Array.Copy(componentTypes, 0, types, 1, componentTypes.Length);

        GameObject uiObject = new GameObject(objectName, types);
        uiObject.layer = gameObject.layer;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private void OnDestroy()
    {
        foreach (Sprite slice in presentationSlices)
        {
            if (slice != null) Destroy(slice);
        }
        presentationSlices.Clear();
        if (enterNodeButton != null)
        {
            enterNodeButton.onClick.RemoveListener(CommitSelectedNode);
        }

        if (retryCheckpointButton != null)
            retryCheckpointButton.onClick.RemoveListener(RetryCheckpointSave);
        retryCheckpoint = null;

        onEnterNode = null;
    }
}
