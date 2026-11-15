using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Runtime-built final run presentation. It replaces the small result card
/// without adding scene or Inspector dependencies.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class RunSummaryUI : MonoBehaviour
{
    private static readonly Color Backdrop = new Color(0.004f, 0.003f, 0.012f, 0.985f);
    private static readonly Color Surface = new Color(0.025f, 0.015f, 0.05f, 1f);
    private static readonly Color Panel = new Color(0.045f, 0.025f, 0.072f, 1f);
    private static readonly Color Inner = new Color(0.065f, 0.035f, 0.102f, 1f);
    private static readonly Color Violet = new Color(0.73f, 0.29f, 1f, 1f);
    private static readonly Color SoftViolet = new Color(0.79f, 0.53f, 1f, 1f);
    private static readonly Color DefeatAccent = new Color(0.93f, 0.20f, 0.34f, 1f);
    private static readonly Color White = new Color(0.97f, 0.95f, 1f, 1f);
    private static readonly Color Muted = new Color(0.70f, 0.65f, 0.77f, 1f);

    private Action restartAction;
    private GameObject overlay;
    private RectTransform fitRect;
    private RectTransform cardRect;
    private CanvasGroup cardGroup;
    private Image topRail;
    private TMP_Text eyebrowText;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private TMP_Text statusText;
    private TMP_Text depthText;
    private TMP_Text scoreText;
    private TMP_Text hpValueText;
    private TMP_Text manaValueText;
    private TMP_Text embersValueText;
    private TMP_Text echoValueText;
    private TMP_Text echoBalanceText;
    private TMP_Text echoBreakdownText;
    private TMP_Text battlesValueText;
    private TMP_Text damageDealtValueText;
    private TMP_Text damageTakenValueText;
    private TMP_Text convergenceValueText;
    private TMP_Text criticalValueText;
    private TMP_Text actionsValueText;
    private TMP_Text healingValueText;
    private TMP_Text durationValueText;
    private TMP_Text favoriteValueText;
    private TMP_Text relicsText;
    private TMP_Text anomaliesText;
    private TMP_Text modifiersText;
    private TMP_Text routeText;
    private TMP_Text seedText;
    private TMP_Text historyWarningText;
    private TMP_Text actionLabel;
    private Button actionButton;
    private Button mainMenuButton;
    private Button quitButton;
    private RectTransform routePanel;
    private Image routeLine;
    private readonly List<Image> routeMarkerBacks = new List<Image>();
    private readonly List<Image> routeMarkers = new List<Image>();
    private readonly List<Sprite> ornamentSlices = new List<Sprite>();
    private Coroutine openingAnimation;
    private StandardRunMapVisualAssets art;

    public bool IsOpen => overlay != null && overlay.activeSelf;
    public GameObject Overlay => overlay;
    public RectTransform CardRect => cardRect;
    public TMP_Text TitleText => titleText;
    public TMP_Text StatusText => statusText;
    public TMP_Text RouteText => routeText;
    public TMP_Text RelicsText => relicsText;
    public TMP_Text AnomaliesText => anomaliesText;
    public TMP_Text EchoValueText => echoValueText;
    public Button ActionButton => actionButton;

    public void Initialize(Action onRestart)
    {
        restartAction = onRestart;
        EnsureBuilt();
    }

    public void Show(RunSummarySnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        EnsureBuilt();
        Configure(snapshot);
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        FitToCanvas();
        PlayOpenAnimation();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(actionButton.gameObject);
        }
    }

    public void HideImmediate()
    {
        if (openingAnimation != null)
        {
            StopCoroutine(openingAnimation);
            openingAnimation = null;
        }
        if (overlay != null) overlay.SetActive(false);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (overlay != null) FitToCanvas();
    }

    private void EnsureBuilt()
    {
        if (overlay != null) return;

        art = Resources.Load<StandardRunMapVisualAssets>(
            "RunMap/FracturedFate/StandardRunMapVisualAssets"
        );
        overlay = CreateUiObject("RunSummaryOverlay", transform, typeof(Image));
        StretchToParent(overlay.GetComponent<RectTransform>());
        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = Backdrop;
        backdrop.raycastTarget = true;

        GameObject fit = CreateUiObject("RunSummaryFit", overlay.transform);
        fitRect = fit.GetComponent<RectTransform>();
        SetCenteredRect(fitRect, Vector2.zero, new Vector2(1660f, 930f));

        GameObject card = CreateUiObject(
            "RunSummaryCard",
            fit.transform,
            typeof(Image),
            typeof(Outline),
            typeof(CanvasGroup)
        );
        cardRect = card.GetComponent<RectTransform>();
        StretchToParent(cardRect);
        card.GetComponent<Image>().color = Surface;
        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = Violet;
        outline.effectDistance = new Vector2(3f, -3f);
        cardGroup = card.GetComponent<CanvasGroup>();

        topRail = CreateUiObject(
            "RunSummaryOutcomeRail",
            card.transform,
            typeof(Image)
        ).GetComponent<Image>();
        SetTopRect(topRail.rectTransform, 0f, 5f, 30f);
        topRail.color = Violet;
        topRail.raycastTarget = false;

        BuildOrnaments(card.transform);
        BuildHeader(card.transform);
        BuildRoute(card.transform);
        BuildResources(card.transform);
        BuildCombatRecord(card.transform);
        BuildArchive(card.transform);
        BuildFooter(card.transform);

        overlay.SetActive(false);
    }

    private void BuildHeader(Transform parent)
    {
        eyebrowText = CreateLabel(
            "RunSummaryEyebrow", parent, string.Empty, 18f, SoftViolet,
            new Vector2(-200f, 371f), new Vector2(1000f, 34f),
            TextAlignmentOptions.Left, false
        );
        titleText = CreateLabel(
            "RunSummaryTitle", parent, string.Empty, 50f, White,
            new Vector2(-200f, 314f), new Vector2(1000f, 78f),
            TextAlignmentOptions.Left, false
        );
        titleText.fontStyle = FontStyles.Bold;
        subtitleText = CreateLabel(
            "RunSummarySubtitle", parent, string.Empty, 20f, Muted,
            new Vector2(-200f, 267f), new Vector2(1000f, 38f),
            TextAlignmentOptions.Left, false
        );
        statusText = CreateLabel(
            "RunSummaryStatus", parent, string.Empty, 22f, SoftViolet,
            new Vector2(607f, 362f), new Vector2(245f, 48f),
            TextAlignmentOptions.Center, false
        );
        statusText.fontStyle = FontStyles.Bold;
        depthText = CreateLabel(
            "RunSummaryDepth", parent, string.Empty, 17f, Muted,
            new Vector2(607f, 316f), new Vector2(245f, 36f),
            TextAlignmentOptions.Center, false
        );
        scoreText = CreateLabel(
            "RunSummaryScore", parent, string.Empty, 19f, SoftViolet,
            new Vector2(607f, 269f), new Vector2(245f, 38f),
            TextAlignmentOptions.Center, false
        );
        scoreText.fontStyle = FontStyles.Bold;
    }

    private void BuildRoute(Transform parent)
    {
        GameObject routeObject = CreatePanel(
            "RunSummaryRoute", parent,
            new Vector2(0f, 194f), new Vector2(1470f, 78f)
        );
        routePanel = routeObject.GetComponent<RectTransform>();
        routeLine = CreateUiObject(
            "RunSummaryTravelledPath", routeObject.transform, typeof(Image)
        ).GetComponent<Image>();
        SetCenteredRect(routeLine.rectTransform, Vector2.zero, new Vector2(1305f, 3f));
        routeLine.color = new Color(0.40f, 0.27f, 0.51f, 0.9f);
        routeLine.raycastTarget = false;

        for (int index = 0; index < StandardRunNodeMap.MaximumNodeCount; index++)
        {
            Image backing = CreateUiObject(
                $"RunSummaryNodeBacking{index + 1}",
                routeObject.transform,
                typeof(Image), typeof(Outline)
            ).GetComponent<Image>();
            backing.color = new Color(0.12f, 0.065f, 0.19f, 1f);
            backing.raycastTarget = false;
            backing.GetComponent<Outline>().effectColor = SoftViolet;
            backing.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            routeMarkerBacks.Add(backing);
            Image marker = CreateUiObject(
                $"RunSummaryNode{index + 1}",
                routeObject.transform,
                typeof(Image)
            ).GetComponent<Image>();
            marker.preserveAspect = true;
            marker.raycastTarget = false;
            routeMarkers.Add(marker);
        }
    }

    private void BuildResources(Transform parent)
    {
        Transform panel = CreatePanel(
            "RunSummaryResources", parent,
            new Vector2(-388f, 28f), new Vector2(700f, 174f)
        ).transform;
        CreateLabel(
            "RunSummaryResourcesTitle", panel, "FINAL RESOURCES", 17f,
            SoftViolet, new Vector2(-186f, 61f), new Vector2(300f, 32f),
            TextAlignmentOptions.Left, false
        ).fontStyle = FontStyles.Bold;

        echoBalanceText = CreateLabel(
            "RunSummaryEchoBalance", panel, string.Empty, 14f, SoftViolet,
            new Vector2(204f, 61f), new Vector2(272f, 32f),
            TextAlignmentOptions.Right, false
        );
        hpValueText = BuildMetricCard(panel, "HP", -255f);
        manaValueText = BuildMetricCard(panel, "MANA", -85f);
        embersValueText = BuildMetricCard(panel, "EMBERS", 85f);
        echoValueText = BuildMetricCard(panel, "ECHO", 255f);
        Image echoIcon = CreateUiObject(
            "EchoFragmentIcon", echoValueText.transform.parent, typeof(Image)
        ).GetComponent<Image>();
        SetCenteredRect(echoIcon.rectTransform, new Vector2(-52f, 18f),
            new Vector2(38f, 38f));
        echoIcon.sprite = Resources.Load<Sprite>("UI/EchoFragment");
        echoIcon.preserveAspect = true;
        echoIcon.raycastTarget = false;
        echoBreakdownText = CreateLabel(
            "RunSummaryEchoBreakdown", panel, string.Empty, 12f, Muted,
            new Vector2(0f, -69f), new Vector2(650f, 20f),
            TextAlignmentOptions.Center, false
        );
    }

    private TMP_Text BuildMetricCard(Transform parent, string label, float x)
    {
        GameObject card = CreatePanel(
            "RunSummary" + label,
            parent,
            new Vector2(x, -22f),
            new Vector2(154f, 78f),
            Inner
        );
        CreateLabel(
            label + "Label", card.transform, label, 13f, Muted,
            new Vector2(label == "ECHO" ? 20f : -22f, 20f),
            new Vector2(label == "ECHO" ? 90f : 100f, 22f),
            TextAlignmentOptions.Left, false
        ).fontStyle = FontStyles.Bold;
        TMP_Text value = CreateLabel(
            label + "Value", card.transform, string.Empty, 24f, White,
            new Vector2(label == "ECHO" ? 20f : -2f, -12f),
            new Vector2(label == "ECHO" ? 100f : 136f, 34f),
            TextAlignmentOptions.Left, false
        );
        value.fontStyle = FontStyles.Bold;
        return value;
    }

    private void BuildCombatRecord(Transform parent)
    {
        Transform panel = CreatePanel(
            "RunSummaryCombat", parent,
            new Vector2(-388f, -199f), new Vector2(700f, 256f)
        ).transform;
        CreateLabel(
            "RunSummaryCombatTitle", panel, "COMBAT RECORD", 17f,
            SoftViolet, new Vector2(-186f, 102f), new Vector2(300f, 32f),
            TextAlignmentOptions.Left, false
        ).fontStyle = FontStyles.Bold;

        battlesValueText = BuildCombatMetric(panel, "BATTLES WON", -242f, 47f);
        damageDealtValueText = BuildCombatMetric(panel, "DAMAGE DEALT", -28f, 47f);
        damageTakenValueText = BuildCombatMetric(panel, "DAMAGE TAKEN", 186f, 47f);
        convergenceValueText = BuildCombatMetric(panel, "CONVERGENCES", -242f, -27f);
        criticalValueText = BuildCombatMetric(panel, "CRITICAL HITS", -28f, -27f);
        actionsValueText = BuildCombatMetric(panel, "ACTIONS", 186f, -27f);
        healingValueText = BuildCombatMetric(panel, "HEALING", -242f, -91f);
        durationValueText = BuildCombatMetric(panel, "DURATION", -28f, -91f);
        favoriteValueText = BuildCombatMetric(panel, "FAVORITE PATTERN", 186f, -91f, 17f);
    }

    private TMP_Text BuildCombatMetric(
        Transform parent,
        string label,
        float x,
        float y,
        float valueSize = 24f
    )
    {
        CreateLabel(
            label + "Label", parent, label, 12f, Muted,
            new Vector2(x, y + 13f), new Vector2(188f, 20f),
            TextAlignmentOptions.Left, false
        ).fontStyle = FontStyles.Bold;
        TMP_Text value = CreateLabel(
            label + "Value", parent, string.Empty, valueSize, White,
            new Vector2(x, y - 16f), new Vector2(188f, 34f),
            TextAlignmentOptions.Left, true
        );
        value.fontStyle = FontStyles.Bold;
        return value;
    }

    private void BuildArchive(Transform parent)
    {
        Transform panel = CreatePanel(
            "RunSummaryArchive", parent,
            new Vector2(388f, -100f), new Vector2(742f, 430f)
        ).transform;
        CreateLabel(
            "RunSummaryArchiveTitle", panel, "BUILD ARCHIVE", 17f,
            SoftViolet, new Vector2(-191f, 187f), new Vector2(300f, 32f),
            TextAlignmentOptions.Left, false
        ).fontStyle = FontStyles.Bold;
        CreateArchiveSection(panel, "RELICS", 120f, out relicsText);
        CreateArchiveSection(panel, "ANOMALIES", 28f, out anomaliesText);
        CreateArchiveSection(panel, "RUN MODIFIERS", -64f, out modifiersText);
        CreateLabel(
            "RunSummaryArchiveHint", panel,
            "Temporary build from this journey. A New Run restores locked Base Stats.",
            15f, Muted, new Vector2(0f, -178f), new Vector2(670f, 46f),
            TextAlignmentOptions.Left, true
        );
    }

    private void CreateArchiveSection(
        Transform parent,
        string title,
        float y,
        out TMP_Text value
    )
    {
        CreateLabel(
            title + "Label", parent, title, 14f, Muted,
            new Vector2(-261f, y + 22f), new Vector2(150f, 28f),
            TextAlignmentOptions.Left, false
        ).fontStyle = FontStyles.Bold;
        value = CreateLabel(
            title + "Value", parent, string.Empty, 19f, White,
            new Vector2(0f, y - 10f), new Vector2(670f, 42f),
            TextAlignmentOptions.Left, true
        );
    }

    private void BuildFooter(Transform parent)
    {
        Image line = CreateUiObject(
            "RunSummaryFooterRail", parent, typeof(Image)
        ).GetComponent<Image>();
        SetCenteredRect(line.rectTransform, new Vector2(0f, -338f), new Vector2(1470f, 2f));
        line.color = new Color(0.28f, 0.13f, 0.43f, 1f);
        line.raycastTarget = false;

        routeText = CreateLabel(
            "RunSummaryRouteText", parent, string.Empty, 14f, Muted,
            new Vector2(-278f, -377f), new Vector2(900f, 52f),
            TextAlignmentOptions.Left, true
        );
        seedText = CreateLabel(
            "RunSummarySeed", parent, string.Empty, 16f, Muted,
            new Vector2(-500f, -389f), new Vector2(480f, 54f),
            TextAlignmentOptions.Left, false
        );
        // The illustrated route above already communicates node history. Keep
        // the run identity visible so a player can report a reproducible seed
        // and match this result to its saved history entry.
        routeText.gameObject.SetActive(false);

        historyWarningText = CreateLabel(
            "RunSummaryHistoryWarning", parent, string.Empty, 16f, DefeatAccent,
            new Vector2(500f, -389f), new Vector2(430f, 54f),
            TextAlignmentOptions.Center, true
        );
        historyWarningText.gameObject.SetActive(false);

        actionButton = CreateButton(
            "RunSummaryRestartButton", parent,
            new Vector2(260f, -389f), new Vector2(310f, 64f),
            out actionLabel
        );
        actionButton.onClick.AddListener(HandleRestart);

        TMP_Text mainMenuLabel;
        mainMenuButton = CreateButton(
            "RunSummaryMainMenuButton", parent,
            new Vector2(535f, -389f), new Vector2(210f, 58f),
            out mainMenuLabel
        );
        mainMenuLabel.text = "MAIN MENU";
        mainMenuLabel.fontSize = 16f;
        mainMenuButton.onClick.AddListener(HandleMainMenu);

        TMP_Text quitLabel;
        quitButton = CreateButton(
            "RunSummaryQuitButton", parent,
            new Vector2(740f, -389f), new Vector2(150f, 58f),
            out quitLabel
        );
        quitLabel.text = "EXIT GAME";
        quitLabel.fontSize = 15f;
        quitButton.onClick.AddListener(HandleQuit);
        quitButton.GetComponent<Image>().color = new Color(0.09f, 0.045f, 0.13f, 1f);
        quitButton.GetComponent<Outline>().effectColor = Muted;

        historyWarningText.rectTransform.anchoredPosition = new Vector2(500f, -440f);
        historyWarningText.rectTransform.sizeDelta = new Vector2(430f, 36f);
    }

    private void Configure(RunSummarySnapshot snapshot)
    {
        bool cleared = snapshot.Outcome == RunSummaryOutcome.Cleared;
        Color accent = cleared ? Violet : DefeatAccent;
        topRail.color = accent;
        cardRect.GetComponent<Outline>().effectColor = accent;
        actionButton.GetComponent<Outline>().effectColor = accent;
        mainMenuButton.GetComponent<Outline>().effectColor = accent;
        mainMenuButton.interactable = !snapshot.EchoSaveFailed &&
            !snapshot.HistorySaveFailed;

        eyebrowText.text = "SYNGRAVA  /  STANDARD RUN ARCHIVE";
        titleText.text = cleared ? "THREAD OF FATE SECURED" : "THREAD SEVERED";
        subtitleText.text = snapshot.EndingReason;
        statusText.text = cleared ? "RUN CLEARED" : "RUN DEFEATED";
        statusText.color = cleared ? SoftViolet : DefeatAccent;
        depthText.text = $"{snapshot.NodesResolved} / {snapshot.NodeCount} NODES";
        scoreText.text = $"SCORE  {snapshot.Score.Total:N0}";

        hpValueText.text = $"{snapshot.CurrentHP} / {snapshot.EffectiveMaxHP}";
        manaValueText.text = $"{snapshot.CurrentMana} / {snapshot.EffectiveMaxMana}";
        embersValueText.text = snapshot.TotalEmbers.ToString();
        echoValueText.text = snapshot.EchoSaveFailed
            ? "PENDING"
            : snapshot.EchoPreviouslyClaimed ? "CLAIMED" : "+" + snapshot.EchoEarned;
        echoValueText.fontSize = snapshot.EchoSaveFailed || snapshot.EchoPreviouslyClaimed
            ? 16f : 24f;
        echoBalanceText.text = snapshot.EchoSaveFailed
            ? "ECHO SAVE FAILED" : "ECHO TOTAL  " + snapshot.EchoBalance;
        echoBreakdownText.text = snapshot.EchoSaveFailed
            ? "Your last checkpoint is safe. Retry the Echo save before starting a new run."
            : snapshot.EchoPreviouslyClaimed
                ? "This run's Echo reward was already saved. Your balance is unchanged."
            : snapshot.EchoDefeatPenalty
                ? $"FOUND {snapshot.EchoFound}   •   60% KEPT"
                : $"BATTLE +{snapshot.EchoBattleReward}   ELITE +{snapshot.EchoEliteReward}   BOSS +{snapshot.EchoBossReward}   CLEAR +{snapshot.EchoClearBonus}";
        battlesValueText.text = snapshot.BattlesWon.ToString();
        damageDealtValueText.text = snapshot.DamageDealt.ToString();
        damageTakenValueText.text = snapshot.DamageTaken.ToString();
        convergenceValueText.text = snapshot.ConvergencesTriggered.ToString();
        criticalValueText.text = snapshot.CriticalHits.ToString();
        actionsValueText.text = snapshot.ActionsTaken.ToString();
        healingValueText.text = snapshot.HealingReceived.ToString();
        durationValueText.text = snapshot.DurationText;
        favoriteValueText.text = snapshot.FavoriteConvergence;
        relicsText.text = snapshot.RelicsText;
        anomaliesText.text = snapshot.AnomaliesText;
        modifiersText.text = snapshot.ModifiersText;
        routeText.text = snapshot.RouteText;
        seedText.text = $"SEED  {snapshot.RunSeed}\nRUN ID  {snapshot.RunId}";
        historyWarningText.gameObject.SetActive(snapshot.HistorySaveFailed);
        historyWarningText.text = snapshot.HistorySaveFailed
            ? "HISTORY SAVE FAILED\nCHECKPOINT SAFE" : string.Empty;
        actionLabel.text = snapshot.EchoSaveFailed
            ? "RETRY ECHO SAVE" : snapshot.HistorySaveFailed
                ? "RETRY HISTORY SAVE" : "BEGIN NEW JOURNEY";

        ConfigureRouteMarkers(snapshot.VisitedNodeTypes, accent);
    }

    private void ConfigureRouteMarkers(
        StandardRunNodeType[] visitedTypes,
        Color accent
    )
    {
        int count = visitedTypes == null ? 0 : visitedTypes.Length;
        float width = 1305f;
        routeLine.gameObject.SetActive(count > 1);
        routeLine.color = new Color(0.40f, 0.27f, 0.51f, 0.9f);

        for (int index = 0; index < routeMarkers.Count; index++)
        {
            Image marker = routeMarkers[index];
            Image backing = routeMarkerBacks[index];
            bool visible = index < count;
            marker.gameObject.SetActive(visible);
            backing.gameObject.SetActive(visible);
            if (!visible) continue;

            float normalized = count <= 1 ? 0.5f : index / (float)(count - 1);
            Vector2 position = new Vector2(-width * 0.5f + normalized * width, 0f);
            SetCenteredRect(
                backing.rectTransform,
                position,
                new Vector2(index == count - 1 ? 56f : 50f,
                    index == count - 1 ? 56f : 50f)
            );
            backing.GetComponent<Outline>().effectColor = index == count - 1
                ? accent : SoftViolet;
            SetCenteredRect(
                marker.rectTransform,
                position,
                new Vector2(index == count - 1 ? 42f : 38f,
                    index == count - 1 ? 42f : 38f)
            );
            marker.sprite = art == null ? null : art.GetNodeIcon(visitedTypes[index]);
            marker.color = White;
        }
    }

    private void HandleRestart()
    {
        restartAction?.Invoke();
    }

    private void HandleMainMenu()
    {
        if (!mainMenuButton.interactable) return;
        MainMenuFlow.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    private void HandleQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private GameObject CreatePanel(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size,
        Color? fill = null
    )
    {
        GameObject panel = CreateUiObject(name, parent, typeof(Image), typeof(Outline));
        SetCenteredRect(panel.GetComponent<RectTransform>(), position, size);
        panel.GetComponent<Image>().color = fill ?? Panel;
        Outline outline = panel.GetComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.12f, 0.39f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);
        return panel;
    }

    private Button CreateButton(
        string name,
        Transform parent,
        Vector2 position,
        Vector2 size,
        out TMP_Text label
    )
    {
        GameObject buttonObject = CreateUiObject(
            name, parent, typeof(Image), typeof(Button), typeof(Outline)
        );
        SetCenteredRect(buttonObject.GetComponent<RectTransform>(), position, size);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.055f, 0.27f, 1f);
        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = Violet;
        outline.effectDistance = new Vector2(2f, -2f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.18f, 1.08f, 1.28f, 1f);
        colors.pressedColor = new Color(0.78f, 0.66f, 0.86f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        label = CreateLabel(
            name + "Label", buttonObject.transform, string.Empty,
            18f, White, Vector2.zero, size - new Vector2(24f, 12f),
            TextAlignmentOptions.Center, false
        );
        label.fontStyle = FontStyles.Bold;
        return button;
    }

    private TMP_Text CreateLabel(
        string name,
        Transform parent,
        string value,
        float fontSize,
        Color color,
        Vector2 position,
        Vector2 size,
        TextAlignmentOptions alignment,
        bool wrap
    )
    {
        GameObject labelObject = CreateUiObject(name, parent, typeof(TextMeshProUGUI));
        SetCenteredRect(labelObject.GetComponent<RectTransform>(), position, size);
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = value;
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
        if (font != null) label.font = font;
        return label;
    }

    private void BuildOrnaments(Transform parent)
    {
        if (art == null || art.Ornaments == null) return;
        AddOrnament(parent, art.Ornaments, "SummaryTopTear",
            new Rect(0.24f, 0.05f, 0.35f, 0.265f),
            new Vector2(0f, 441f), new Vector2(1540f, 48f));
        AddOrnament(parent, art.Ornaments, "SummaryBottomTear",
            new Rect(0.735f, 0.82f, 0.25f, 0.17f),
            new Vector2(0f, -447f), new Vector2(1540f, 34f));
        AddOrnament(parent, art.Ornaments, "SummaryEye",
            new Rect(0.315f, 0.405f, 0.157f, 0.29f),
            new Vector2(-768f, 336f), new Vector2(100f, 105f));
        AddOrnament(parent, art.Ornaments, "SummaryCrystal",
            new Rect(0.43f, 0.325f, 0.10f, 0.215f),
            new Vector2(770f, 365f), new Vector2(70f, 115f));
    }

    private void AddOrnament(
        Transform parent,
        Sprite source,
        string name,
        Rect crop,
        Vector2 position,
        Vector2 size
    )
    {
        Rect sourceRect = source.rect;
        var slice = Sprite.Create(
            source.texture,
            new Rect(
                sourceRect.x + crop.x * sourceRect.width,
                sourceRect.y + (1f - crop.y - crop.height) * sourceRect.height,
                crop.width * sourceRect.width,
                crop.height * sourceRect.height
            ),
            Vector2.one * 0.5f,
            source.pixelsPerUnit,
            0,
            SpriteMeshType.FullRect
        );
        ornamentSlices.Add(slice);
        Image image = CreateUiObject(name, parent, typeof(Image)).GetComponent<Image>();
        SetCenteredRect(image.rectTransform, position, size);
        image.sprite = slice;
        image.raycastTarget = false;
    }

    private void PlayOpenAnimation()
    {
        if (openingAnimation != null) StopCoroutine(openingAnimation);
        openingAnimation = StartCoroutine(AnimateOpen());
    }

    private IEnumerator AnimateOpen()
    {
        const float duration = 0.30f;
        float elapsed = 0f;
        cardGroup.alpha = 0f;
        cardRect.localScale = Vector3.one * 0.965f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            cardGroup.alpha = eased;
            cardRect.localScale = Vector3.one * Mathf.Lerp(0.965f, 1f, eased);
            yield return null;
        }
        cardGroup.alpha = 1f;
        cardRect.localScale = Vector3.one;
        openingAnimation = null;
    }

    private void FitToCanvas()
    {
        if (fitRect == null || overlay == null) return;
        Rect bounds = overlay.GetComponent<RectTransform>().rect;
        float scale = Mathf.Min(
            1f,
            (bounds.width - 28f) / 1660f,
            (bounds.height - 28f) / 930f
        );
        fitRect.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }

    private void OnDestroy()
    {
        if (actionButton != null) actionButton.onClick.RemoveListener(HandleRestart);
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(HandleMainMenu);
        if (quitButton != null) quitButton.onClick.RemoveListener(HandleQuit);
        foreach (Sprite slice in ornamentSlices)
        {
            if (slice != null) Destroy(slice);
        }
    }

    private static GameObject CreateUiObject(
        string name,
        Transform parent,
        params Type[] components
    )
    {
        var allComponents = new Type[components.Length + 1];
        allComponents[0] = typeof(RectTransform);
        Array.Copy(components, 0, allComponents, 1, components.Length);
        var result = new GameObject(name, allComponents);
        result.transform.SetParent(parent, false);
        return result;
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
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void SetTopRect(
        RectTransform rect,
        float y,
        float height,
        float horizontalMargin
    )
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -y);
        rect.sizeDelta = new Vector2(-horizontalMargin * 2f, height);
    }
}
