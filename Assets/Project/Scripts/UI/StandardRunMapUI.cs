using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StandardRunMapUI : MonoBehaviour
{
    private static readonly Color BackdropColor =
        new Color(0.008f, 0.006f, 0.018f, 0.92f);

    private static readonly Color CardColor =
        new Color(0.030f, 0.022f, 0.060f, 0.99f);

    private static readonly Color RoutePanelColor =
        new Color(0.020f, 0.017f, 0.040f, 1.00f);

    private static readonly Color DetailPanelColor =
        new Color(0.050f, 0.030f, 0.090f, 1.00f);

    private static readonly Color Purple =
        new Color(0.65f, 0.25f, 1.00f, 1.00f);

    private static readonly Color BrightPurple =
        new Color(0.82f, 0.55f, 1.00f, 1.00f);

    private static readonly Color White =
        new Color(0.96f, 0.94f, 1.00f, 1.00f);

    private static readonly Color Muted =
        new Color(0.42f, 0.39f, 0.50f, 1.00f);

    private static readonly Color Completed =
        new Color(0.22f, 0.13f, 0.34f, 1.00f);

    private static readonly Color Locked =
        new Color(0.045f, 0.040f, 0.060f, 1.00f);

    private GameObject overlay;
    private Transform routeRoot;
    private TMP_Text detailTitle;
    private TMP_Text detailBody;
    private TMP_Text progressText;
    private Button enterNodeButton;
    private TMP_Text enterNodeLabel;

    private readonly List<Button> nodeButtons = new List<Button>();
    private readonly List<Image> nodeImages = new List<Image>();
    private readonly List<TMP_Text> nodeLabels = new List<TMP_Text>();
    private readonly List<Image> routeLines = new List<Image>();

    private StandardRunNodeMap currentMap;
    private StandardRunNode selectedNode;
    private Action<string> onEnterNode;
    private int currentDepth;
    private bool selectionCommitted;
    private bool uiBuilt;

    public bool IsOpen => overlay != null && overlay.activeSelf;
    public GameObject Overlay => overlay;
    public Button EnterNodeButton => enterNodeButton;
    public TMP_Text DetailTitle => detailTitle;
    public TMP_Text DetailBody => detailBody;
    public IReadOnlyList<Button> NodeButtons => nodeButtons;
    public int CurrentDepth => currentDepth;
    public string SelectedNodeCode => selectedNode == null
        ? string.Empty
        : selectedNode.Code;

    private void Awake()
    {
        EnsureUiBuilt();
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
        selectedNode = map.GetNodeAtDepth(activeDepth);
        selectionCommitted = false;

        RebuildRoute();
        RefreshDetails();

        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();

        if (EventSystem.current != null && enterNodeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(
                enterNodeButton.gameObject
            );
        }
    }

    public void HideImmediate()
    {
        if (overlay != null)
        {
            overlay.SetActive(false);
        }

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
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = BackdropColor;
        backdrop.raycastTarget = true;

        GameObject card = CreateUiObject(
            "StandardRunMapCard",
            overlay.transform,
            typeof(Image),
            typeof(Outline)
        );

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1640f, 900f);

        Image cardImage = card.GetComponent<Image>();
        cardImage.color = CardColor;
        cardImage.raycastTarget = true;

        Outline cardOutline = card.GetComponent<Outline>();
        cardOutline.effectColor = Purple;
        cardOutline.effectDistance = new Vector2(4f, 4f);
        cardOutline.useGraphicAlpha = true;

        CreateLabel(
            "RunMapTitle",
            card.transform,
            "SYNGRAVA ROUTE",
            38f,
            White,
            new Vector2(-500f, 390f),
            new Vector2(520f, 56f),
            TextAlignmentOptions.Left,
            false
        );

        CreateLabel(
            "RunMapSubtitle",
            card.transform,
            "Choose the current node to continue the Standard Run.",
            17f,
            new Color(0.72f, 0.64f, 0.82f, 1f),
            new Vector2(-408f, 350f),
            new Vector2(700f, 34f),
            TextAlignmentOptions.Left,
            false
        );

        progressText = CreateLabel(
            "RunMapProgress",
            card.transform,
            string.Empty,
            18f,
            BrightPurple,
            new Vector2(610f, 386f),
            new Vector2(300f, 40f),
            TextAlignmentOptions.Right,
            false
        );

        GameObject routePanel = CreatePanel(
            "RunMapRoutePanel",
            card.transform,
            new Vector2(-230f, -30f),
            new Vector2(1120f, 700f),
            RoutePanelColor,
            new Color(0.22f, 0.13f, 0.36f, 1f)
        );

        GameObject routeContent = CreateUiObject(
            "RunMapRouteContent",
            routePanel.transform
        );
        RectTransform routeRect = routeContent.GetComponent<RectTransform>();
        routeRect.anchorMin = new Vector2(0.5f, 0.5f);
        routeRect.anchorMax = new Vector2(0.5f, 0.5f);
        routeRect.pivot = new Vector2(0.5f, 0.5f);
        routeRect.anchoredPosition = new Vector2(0f, 20f);
        routeRect.sizeDelta = new Vector2(1040f, 520f);
        routeRoot = routeContent.transform;

        CreateLegend(routePanel.transform);

        GameObject detailPanel = CreatePanel(
            "RunMapDetailPanel",
            card.transform,
            new Vector2(585f, -30f),
            new Vector2(390f, 700f),
            DetailPanelColor,
            new Color(0.38f, 0.19f, 0.62f, 1f)
        );

        CreateLabel(
            "RunMapDetailHeader",
            detailPanel.transform,
            "NODE DETAILS",
            18f,
            BrightPurple,
            new Vector2(0f, 298f),
            new Vector2(340f, 36f),
            TextAlignmentOptions.Center,
            false
        );

        detailTitle = CreateLabel(
            "RunMapDetailTitle",
            detailPanel.transform,
            string.Empty,
            27f,
            White,
            new Vector2(0f, 225f),
            new Vector2(330f, 66f),
            TextAlignmentOptions.Center,
            true
        );

        detailBody = CreateLabel(
            "RunMapDetailBody",
            detailPanel.transform,
            string.Empty,
            17f,
            new Color(0.82f, 0.78f, 0.88f, 1f),
            new Vector2(0f, 20f),
            new Vector2(320f, 310f),
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
        enterRect.anchorMin = new Vector2(0.5f, 0.5f);
        enterRect.anchorMax = new Vector2(0.5f, 0.5f);
        enterRect.pivot = new Vector2(0.5f, 0.5f);
        enterRect.anchoredPosition = new Vector2(0f, -282f);
        enterRect.sizeDelta = new Vector2(300f, 68f);

        Image enterImage = enterObject.GetComponent<Image>();
        enterImage.color = new Color(0.17f, 0.08f, 0.30f, 1f);
        enterImage.raycastTarget = true;

        Outline enterOutline = enterObject.GetComponent<Outline>();
        enterOutline.effectColor = Purple;
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
            new Vector2(280f, 58f),
            TextAlignmentOptions.Center,
            false
        );

        overlay.SetActive(false);
    }

    private void CreateLegend(Transform parent)
    {
        CreateLabel(
            "RunMapLegend",
            parent,
            "B  BATTLE     E  ELITE     ?  EVENT     R  REST     M  MERCHANT     X  BOSS",
            15f,
            new Color(0.70f, 0.65f, 0.78f, 1f),
            new Vector2(0f, -315f),
            new Vector2(1030f, 32f),
            TextAlignmentOptions.Center,
            false
        );
    }

    private void RebuildRoute()
    {
        ClearRouteObjects();

        if (currentMap == null || routeRoot == null)
        {
            return;
        }

        progressText.text =
            $"NODE {currentDepth} / {currentMap.Count}";

        Vector2[] positions = new Vector2[currentMap.Count];

        for (int index = 0; index < positions.Length; index++)
        {
            positions[index] = GetNodePosition(index);
        }

        for (int index = 0; index < positions.Length - 1; index++)
        {
            CreateRouteLine(
                positions[index],
                positions[index + 1],
                index + 1 < currentDepth
            );
        }

        for (int index = 0; index < currentMap.Nodes.Length; index++)
        {
            CreateNodeButton(currentMap.Nodes[index], positions[index]);
        }
    }

    private void ClearRouteObjects()
    {
        nodeButtons.Clear();
        nodeImages.Clear();
        nodeLabels.Clear();
        routeLines.Clear();

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

    private Vector2 GetNodePosition(int index)
    {
        const int columns = 5;
        int row = index / columns;
        int column = index % columns;

        if ((row & 1) == 1)
        {
            column = columns - 1 - column;
        }

        return new Vector2(-420f + column * 210f, row == 0 ? 135f : -135f);
    }

    private void CreateRouteLine(Vector2 start, Vector2 end, bool completed)
    {
        GameObject lineObject = CreateUiObject(
            "RunMapRouteLine",
            routeRoot,
            typeof(Image)
        );

        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        Vector2 delta = end - start;
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = (start + end) * 0.5f;
        lineRect.sizeDelta = new Vector2(delta.magnitude, completed ? 8f : 5f);
        lineRect.localRotation = Quaternion.Euler(
            0f,
            0f,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg
        );

        Image line = lineObject.GetComponent<Image>();
        line.color = completed
            ? new Color(0.65f, 0.30f, 0.95f, 0.90f)
            : new Color(0.28f, 0.25f, 0.34f, 0.70f);
        line.raycastTarget = false;
        routeLines.Add(line);
    }

    private void CreateNodeButton(StandardRunNode node, Vector2 position)
    {
        GameObject nodeObject = CreateUiObject(
            $"RunNode_{node.Depth:00}_{node.Type}",
            routeRoot,
            typeof(Image),
            typeof(Button),
            typeof(Outline)
        );

        RectTransform nodeRect = nodeObject.GetComponent<RectTransform>();
        nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
        nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
        nodeRect.pivot = new Vector2(0.5f, 0.5f);
        nodeRect.anchoredPosition = position;
        nodeRect.sizeDelta = node.IsFinal
            ? new Vector2(108f, 108f)
            : new Vector2(88f, 88f);

        Image image = nodeObject.GetComponent<Image>();
        image.color = GetNodeColorForDepth(node);
        image.raycastTarget = true;

        Outline outline = nodeObject.GetComponent<Outline>();
        outline.effectColor = node.Depth == currentDepth
            ? BrightPurple
            : new Color(0.25f, 0.20f, 0.31f, 0.90f);
        outline.effectDistance = node.Depth == currentDepth
            ? new Vector2(4f, 4f)
            : new Vector2(2f, 2f);
        outline.useGraphicAlpha = true;

        Button button = nodeObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.colors = CreateButtonColors();
        button.interactable = node.Depth == currentDepth;

        int nodeIndex = node.Depth - 1;
        button.onClick.AddListener(() => SelectNode(nodeIndex));

        TMP_Text label = CreateLabel(
            "RunNodeLabel",
            nodeObject.transform,
            GetNodeGlyph(node.Type),
            node.IsFinal ? 29f : 25f,
            node.Depth > currentDepth ? Muted : White,
            new Vector2(0f, 5f),
            new Vector2(78f, 42f),
            TextAlignmentOptions.Center,
            false
        );

        CreateLabel(
            "RunNodeDepth",
            nodeObject.transform,
            node.Depth < currentDepth
                ? "CLEARED"
                : node.Depth == currentDepth
                    ? $"NODE {node.Depth}"
                    : "LOCKED",
            10f,
            node.Depth == currentDepth ? BrightPurple : Muted,
            new Vector2(0f, -27f),
            new Vector2(82f, 22f),
            TextAlignmentOptions.Center,
            false
        );

        nodeButtons.Add(button);
        nodeImages.Add(image);
        nodeLabels.Add(label);
    }

    private void SelectNode(int index)
    {
        if (selectionCommitted ||
            currentMap == null ||
            index < 0 ||
            index >= currentMap.Count)
        {
            return;
        }

        StandardRunNode node = currentMap.Nodes[index];

        if (node == null || node.Depth != currentDepth)
        {
            return;
        }

        selectedNode = node;
        RefreshDetails();
    }

    private void RefreshDetails()
    {
        if (selectedNode == null)
        {
            detailTitle.text = "NO NODE SELECTED";
            detailBody.text = "Select the current reachable node.";
            enterNodeButton.interactable = false;
            return;
        }

        detailTitle.text = GetNodeDisplayName(selectedNode.Type);
        detailBody.text =
            $"Depth: {selectedNode.Depth}\n" +
            $"Route Code: {selectedNode.Code}\n\n" +
            GetNodeDescription(selectedNode.Type);
        enterNodeLabel.text = GetEnterButtonLabel(selectedNode.Type);
        enterNodeButton.interactable = !selectionCommitted;
    }

    private void CommitSelectedNode()
    {
        if (selectionCommitted || selectedNode == null || onEnterNode == null)
        {
            return;
        }

        selectionCommitted = true;
        enterNodeButton.interactable = false;

        for (int index = 0; index < nodeButtons.Count; index++)
        {
            nodeButtons[index].interactable = false;
        }

        onEnterNode(selectedNode.Code);
    }

    private Color GetNodeColorForDepth(StandardRunNode node)
    {
        if (node.Depth < currentDepth)
        {
            return Completed;
        }

        if (node.Depth == currentDepth)
        {
            return new Color(0.28f, 0.10f, 0.48f, 1.00f);
        }

        return Locked;
    }

    private static string GetNodeGlyph(StandardRunNodeType type)
    {
        switch (type)
        {
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
            case StandardRunNodeType.NormalBattle:
                return "NORMAL BATTLE";
            case StandardRunNodeType.EliteBattle:
                return "ELITE BATTLE";
            case StandardRunNodeType.RandomEvent:
                return "RANDOM EVENT";
            case StandardRunNodeType.Rest:
                return "REST NODE";
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
            case StandardRunNodeType.NormalBattle:
                return "Enter a standard combat encounter. Victory opens " +
                    "one three-card run reward before returning to the map.";
            case StandardRunNodeType.EliteBattle:
                return "Enter the current Elite prototype encounter. Elite " +
                    "content and its stronger reward pool will expand later.";
            case StandardRunNodeType.RandomEvent:
                return "Functional placeholder. The route advances without " +
                    "changing HP, Mana, Embers, or modifiers.";
            case StandardRunNodeType.Rest:
                return "Functional placeholder. Rest choices and healing " +
                    "values are planned; current resources remain unchanged.";
            case StandardRunNodeType.Merchant:
                return "Functional placeholder. Merchant inventory and " +
                    "currency spending are planned for a later milestone.";
            case StandardRunNodeType.Boss:
                return "Enter the final encounter. Defeating this node ends " +
                    "the Standard Run exactly once.";
            default:
                return "No node description is available.";
        }
    }

    private static string GetEnterButtonLabel(StandardRunNodeType type)
    {
        switch (type)
        {
            case StandardRunNodeType.NormalBattle:
                return "ENTER BATTLE";
            case StandardRunNodeType.EliteBattle:
                return "ENTER ELITE";
            case StandardRunNodeType.Boss:
                return "CHALLENGE BOSS";
            default:
                return "RESOLVE NODE";
        }
    }

    private GameObject CreatePanel(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Color color,
        Color outlineColor
    )
    {
        GameObject panel = CreateUiObject(
            objectName,
            parent,
            typeof(Image),
            typeof(Outline)
        );

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(2f, 2f);
        outline.useGraphicAlpha = true;
        return panel;
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
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

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

    private static ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.58f, 0.95f, 1f);
        colors.selectedColor = new Color(0.92f, 0.78f, 1f, 1f);
        colors.disabledColor = new Color(0.55f, 0.52f, 0.60f, 0.55f);
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
        if (enterNodeButton != null)
        {
            enterNodeButton.onClick.RemoveListener(CommitSelectedNode);
        }

        onEnterNode = null;
    }
}
