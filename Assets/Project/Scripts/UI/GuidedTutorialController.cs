using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GuidedTutorialController : MonoBehaviour
{
    public const string CompletedSettingKey = "guided_tutorial_completed";

    private enum Step
    {
        Inactive,
        SelectMapNode,
        EnterMapNode,
        BattleIntro,
        WaitingForPlayerTurn,
        BasicAttack,
        Skill,
        Guard,
        SkillAfterGuard,
        BasicAttackForOrbit,
        SkillUnavailable,
        SkillUnavailableAfterGuard,
        Analyze,
        AnalyzeUnavailable,
        Complete
    }

    private static readonly Color Backdrop = new Color(0.012f, 0.006f, 0.025f, 0.86f);
    private static readonly Color CardColor = new Color(0.035f, 0.018f, 0.070f, 0.99f);
    private static readonly Color Violet = new Color(0.78f, 0.40f, 1f, 1f);
    private static readonly Color TextColor = new Color(0.96f, 0.93f, 1f, 1f);
    private static readonly Color MutedColor = new Color(0.77f, 0.71f, 0.84f, 1f);

    private BattleManager battleManager;
    private StandardRunMapUI mapUI;
    private Canvas canvas;
    private GameObject overlay;
    private RectTransform overlayRect;
    private RectTransform focusRect;
    private RectTransform cardRect;
    private Image[] blockers;
    private TMP_Text kickerText;
    private TMP_Text titleText;
    private TMP_Text bodyText;
    private Button skipButton;
    private Button continueButton;
    private Button targetButton;
    private Step step;
    private Step stepAfterTurn;
    private bool enemyOpeningPending;
    private bool skillWasDeferred;

    public bool IsRunning => step != Step.Inactive;
    public bool IsOpen => overlay != null && overlay.activeSelf;
    public string CurrentStepName => step.ToString();
    public Button HighlightedButton => targetButton;
    public Button SkipButton => skipButton;
    public Button ContinueButton => continueButton;
    public RectTransform CardRect => cardRect;
    public RectTransform FocusRect => focusRect;
    public RectTransform OverlayRect => overlayRect;

    private void Awake()
    {
        Build();
    }

    private void Update()
    {
        if (IsRunning && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Skip();

        if (step == Step.SelectMapNode && mapUI != null &&
            !string.IsNullOrWhiteSpace(mapUI.SelectedNodeCode))
        {
            ShowMapEnterStep();
        }
        else if (step == Step.SelectMapNode &&
            (targetButton == null || !targetButton.interactable))
        {
            targetButton = FirstReachableMapButton();
        }
        else if (step == Step.WaitingForPlayerTurn)
        {
            if (battleManager == null || battleManager.GuidedTutorialBattleEnded)
            {
                HideWithoutCompleting();
            }
            else if (battleManager.GuidedTutorialCanPlayerAct)
            {
                ShowCombatStep(stepAfterTurn);
            }
        }

        if (IsOpen)
        {
            UpdateSpotlightAndCard();
            if (focusRect != null && targetButton != null)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.2f);
                float scale = Mathf.Lerp(1f, 1.035f, wave);
                focusRect.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }

    public void Initialize(BattleManager manager, StandardRunMapUI runMapUI)
    {
        battleManager = manager;
        mapUI = runMapUI;
        if (overlay == null) Build();
    }

    public void TryBeginAtMap(RunManager run, StandardRunMapUI runMapUI)
    {
        if (run == null || runMapUI == null || IsRunning || HasCompletedTutorial()) return;
        if (!run.IsActive || run.IsCompleted || run.Depth != RunManager.FirstDepth ||
            run.NodeEntered || run.VisitedNodeCodes.Length != 0)
            return;

        mapUI = runMapUI;
        if (string.IsNullOrWhiteSpace(mapUI.SelectedNodeCode))
        {
            targetButton = FirstReachableMapButton();
            if (targetButton == null) return;
            ShowStep(Step.SelectMapNode, "YOUR FIRST PATH",
                "Choose a reachable node to begin the expedition. Select the glowing node on the map.",
                targetButton, false);
        }
        else
        {
            ShowMapEnterStep();
        }
    }

    public bool BeginFirstBattle()
    {
        if (step != Step.EnterMapNode) return false;

        enemyOpeningPending = battleManager != null &&
            battleManager.GuidedTutorialEnemyActsFirst;
        string body = enemyOpeningPending
            ? "Combat is turn-based. The enemy has initiative, so its prepared opening action resolves first. Watch it; then I’ll point to your first command."
            : "Combat is turn-based. The enemy’s action is prepared before its turn. Start with BASIC ATTACK, which deals damage without spending Mana.";
        ShowStep(Step.BattleIntro, "FIRST ENCOUNTER", body, null, true,
            enemyOpeningPending ? "WATCH OPENING" : "SHOW MY FIRST MOVE");
        return enemyOpeningPending;
    }

    public void OnCombatActionResolved(PlayerActionType action)
    {
        if (!IsRunning) return;

        Step next;
        switch (step)
        {
            case Step.BasicAttack:
                if (action != PlayerActionType.Attack) return;
                next = Step.Skill;
                break;
            case Step.Skill:
                if (action != PlayerActionType.Skill) return;
                next = Step.Guard;
                break;
            case Step.Guard:
                if (action != PlayerActionType.Guard) return;
                next = skillWasDeferred ? Step.SkillAfterGuard : Step.Analyze;
                break;
            case Step.SkillAfterGuard:
                if (action != PlayerActionType.Skill) return;
                next = Step.Analyze;
                break;
            case Step.BasicAttackForOrbit:
                if (action != PlayerActionType.Attack) return;
                next = Step.Analyze;
                break;
            default:
                return;
        }

        targetButton = null;
        stepAfterTurn = next;
        step = Step.WaitingForPlayerTurn;
        HideOverlayOnly();
    }

    public void OnAnalyzeResolved()
    {
        if (step != Step.Analyze) return;
        ShowCompletionStep(
            "Analyze revealed the enemy’s prepared action without spending your turn. Choose any available combat command when you’re ready."
        );
    }

    public void OnBattleEnded(bool playerWon)
    {
        if (!IsRunning) return;
        enemyOpeningPending = false;
        targetButton = null;
        if (playerWon)
        {
            PersistCompleted();
            step = Step.Inactive;
            HideOverlayOnly();
        }
        else
        {
            // A defeated first attempt can be guided again when a fresh run begins.
            step = Step.Inactive;
            HideOverlayOnly();
        }
    }

    public void HandleEscape()
    {
        if (IsRunning) Skip();
    }

    public void Skip()
    {
        if (!IsRunning) return;
        bool releaseOpening = step == Step.BattleIntro && enemyOpeningPending;
        enemyOpeningPending = false;
        PersistCompleted();
        step = Step.Inactive;
        HideOverlayOnly();
        RestoreGameplayFocus();
        if (releaseOpening && battleManager != null)
            battleManager.BeginGuidedTutorialEnemyOpeningTurn();
    }

    public void Continue()
    {
        switch (step)
        {
            case Step.BattleIntro:
                if (enemyOpeningPending)
                {
                    enemyOpeningPending = false;
                    step = Step.WaitingForPlayerTurn;
                    stepAfterTurn = Step.BasicAttack;
                    HideOverlayOnly();
                    battleManager?.BeginGuidedTutorialEnemyOpeningTurn();
                }
                else
                {
                    ShowCombatStep(Step.BasicAttack);
                }
                break;
            case Step.SkillUnavailable:
                skillWasDeferred = true;
                ShowCombatStep(Step.Guard);
                break;
            case Step.SkillUnavailableAfterGuard:
                ShowCombatStep(Step.BasicAttackForOrbit);
                break;
            case Step.AnalyzeUnavailable:
                ShowCompletionStep(
                    "Analyze reveals the enemy’s prepared action, costs Mana, can be used once per node, and does not spend your turn or enter Orbit. Continue with any available command."
                );
                break;
            case Step.Complete:
                PersistCompleted();
                step = Step.Inactive;
                HideOverlayOnly();
                RestoreGameplayFocus();
                break;
        }
    }

    private void ShowMapEnterStep()
    {
        if (mapUI == null || mapUI.EnterNodeButton == null) return;
        targetButton = mapUI.EnterNodeButton;
        ShowStep(Step.EnterMapNode, "ENTER THE ENCOUNTER",
            "This node is selected. Use ENTER NODE to enter the first battle.",
            targetButton, false);
    }

    private void ShowCombatStep(Step next)
    {
        if (battleManager == null) return;
        Button button;
        string title;
        string body;

        switch (next)
        {
            case Step.BasicAttack:
                button = battleManager.GuidedTutorialBasicAttackButton;
                title = "BASIC ATTACK";
                body = "Use Basic Attack to damage the enemy. After your turn, the enemy responds with its prepared action.";
                break;
            case Step.Skill:
                button = battleManager.GuidedTutorialSkillButton;
                title = "SPEND MANA ON A SKILL";
                body = "SHADOW STRIKE deals skill damage and uses the Mana cost shown on its button. Click it to try the Skill.";
                break;
            case Step.Guard:
                button = battleManager.GuidedTutorialGuardButton;
                title = "GUARD AND BUILD ORBIT";
                body = skillWasDeferred
                    ? "GUARD reduces the next incoming hit and restores Mana. Your Attack filled one Orbit slot; if Skill was unavailable, Guard still protects you while you recover Mana."
                    : "GUARD reduces the next incoming hit and restores Mana. Attack and Skill have filled two Orbit slots; Guard fills the third and triggers one Convergence.";
                break;
            case Step.SkillAfterGuard:
                button = battleManager.GuidedTutorialSkillButton;
                title = "USE THE MANA YOU RESTORED";
                body = "Guard restored Mana. Use SHADOW STRIKE to fill the next Orbit slot; this may complete the three-action Convergence.";
                break;
            case Step.BasicAttackForOrbit:
                button = battleManager.GuidedTutorialBasicAttackButton;
                title = "COMPLETE SINGULARITY ORBIT";
                body = "This Attack fills the third Orbit slot and triggers a Convergence. Each successful Attack, Skill, or Guard fills one slot.";
                break;
            case Step.Analyze:
                button = battleManager.GuidedTutorialAnalyzeButton;
                title = "READ THE ENEMY WITH ANALYZE";
                body = "Analyze reveals the already-prepared enemy action. It costs 75% of effective Max Mana, rounded up, works once per node, and does not spend your turn or enter Orbit.";
                break;
            default:
                return;
        }

        if (button == null || !button.interactable)
        {
            if (next == Step.Skill)
                ShowStep(Step.SkillUnavailable, "SKILL NEEDS MANA",
                    "SHADOW STRIKE is disabled until you have enough Mana. Guard restores Mana; continue and I’ll show you Guard first.",
                    null, true, "CONTINUE");
            else if (next == Step.SkillAfterGuard)
                ShowStep(Step.SkillUnavailableAfterGuard, "SKILL NOT READY",
                    "Mana is still too low for SHADOW STRIKE. Use one more Basic Attack to fill the remaining Orbit slot, then you can continue normally.",
                    null, true, "CONTINUE");
            else if (next == Step.Analyze)
                ShowStep(Step.AnalyzeUnavailable, "ANALYZE NEEDS MANA",
                    "Analyze is available once you have enough Mana. It reveals the enemy’s prepared action, costs 75% of effective Max Mana rounded up, works once per node, and does not spend your turn or enter Orbit.",
                    null, true, "CONTINUE");
            else
                ShowCompletionStep("The encounter ended before this command was needed. Rewards and the expedition continue normally.");
            return;
        }

        ShowStep(next, title, body, button, false);
    }

    private void ShowCompletionStep(string message)
    {
        ShowStep(Step.Complete, "YOU’RE READY", message, null, true, "CONTINUE RUN");
    }

    private void ShowStep(Step newStep, string title, string body,
        Button focusButton, bool showContinue, string continueCaption = "CONTINUE")
    {
        if (!EnsureBuilt()) return;
        step = newStep;
        targetButton = focusButton;
        titleText.text = title;
        bodyText.text = body;
        continueButton.gameObject.SetActive(showContinue);
        if (showContinue)
        {
            TMP_Text label = continueButton.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = continueCaption;
        }
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        UpdateSpotlightAndCard();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(
                focusButton != null ? focusButton.gameObject :
                showContinue ? continueButton.gameObject : skipButton.gameObject);
    }

    private void UpdateSpotlightAndCard()
    {
        if (overlayRect == null || blockers == null || blockers.Length < 4) return;
        if (targetButton == null || !targetButton.gameObject.activeInHierarchy ||
            !targetButton.enabled)
        {
            SetFullBlocker();
            focusRect.gameObject.SetActive(false);
            PositionCard(Vector2.zero, false);
            return;
        }

        RectTransform targetRectTransform = targetButton.transform as RectTransform;
        if (targetRectTransform == null)
        {
            SetFullBlocker();
            focusRect.gameObject.SetActive(false);
            PositionCard(Vector2.zero, false);
            return;
        }

        Vector3[] worldCorners = new Vector3[4];
        targetRectTransform.GetWorldCorners(worldCorners);
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldCorners[i]);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect, screen, canvas.worldCamera, out Vector2 local))
            {
                SetFullBlocker();
                focusRect.gameObject.SetActive(false);
                PositionCard(Vector2.zero, false);
                return;
            }
            minX = Mathf.Min(minX, local.x);
            minY = Mathf.Min(minY, local.y);
            maxX = Mathf.Max(maxX, local.x);
            maxY = Mathf.Max(maxY, local.y);
        }

        Rect area = overlayRect.rect;
        minX = Mathf.Clamp(minX, area.xMin, area.xMax);
        maxX = Mathf.Clamp(maxX, area.xMin, area.xMax);
        minY = Mathf.Clamp(minY, area.yMin, area.yMax);
        maxY = Mathf.Clamp(maxY, area.yMin, area.yMax);
        if (maxX - minX < 2f || maxY - minY < 2f)
        {
            SetFullBlocker();
            focusRect.gameObject.SetActive(false);
            PositionCard(Vector2.zero, false);
            return;
        }

        SetSpotlightBlockers(minX, minY, maxX, maxY, area);
        focusRect.gameObject.SetActive(true);
        SetRect(focusRect, minX, minY, maxX, maxY);
        PositionCard(new Vector2((minX + maxX) * 0.5f,
            (minY + maxY) * 0.5f), true, minY, maxY);
    }

    private void SetSpotlightBlockers(float minX, float minY, float maxX,
        float maxY, Rect area)
    {
        SetBlocker(0, area.xMin, maxY, area.xMax, area.yMax);
        SetBlocker(1, area.xMin, area.yMin, area.xMax, minY);
        SetBlocker(2, area.xMin, minY, minX, maxY);
        SetBlocker(3, maxX, minY, area.xMax, maxY);
    }

    private void SetFullBlocker()
    {
        if (overlayRect == null || blockers == null) return;
        Rect area = overlayRect.rect;
        SetBlocker(0, area.xMin, area.yMin, area.xMax, area.yMax);
        for (int i = 1; i < blockers.Length; i++) blockers[i].gameObject.SetActive(false);
    }

    private void SetBlocker(int index, float minX, float minY, float maxX, float maxY)
    {
        if (index < 0 || index >= blockers.Length) return;
        Image image = blockers[index];
        bool visible = maxX - minX > 0.5f && maxY - minY > 0.5f;
        image.gameObject.SetActive(visible);
        if (visible) SetRect(image.rectTransform, minX, minY, maxX, maxY);
    }

    private void PositionCard(Vector2 targetCenter, bool hasTarget,
        float targetMinY = 0f, float targetMaxY = 0f)
    {
        if (cardRect == null || overlayRect == null) return;
        Rect bounds = overlayRect.rect;
        float margin = Mathf.Min(24f, bounds.height * 0.04f);
        float width = Mathf.Min(540f, bounds.width - margin * 2f);
        float height = Mathf.Min(260f, bounds.height - margin * 2f);
        cardRect.sizeDelta = new Vector2(width, height);
        float x = 0f;
        float y = 0f;
        if (hasTarget)
        {
            x = Mathf.Clamp(targetCenter.x,
                bounds.xMin + width * 0.5f + margin,
                bounds.xMax - width * 0.5f - margin);
            y = targetCenter.y >= 0f
                ? targetMinY - margin - height * 0.5f
                : targetMaxY + margin + height * 0.5f;
            y = Mathf.Clamp(y, bounds.yMin + height * 0.5f + margin,
                bounds.yMax - height * 0.5f - margin);
        }
        cardRect.anchoredPosition = new Vector2(x, y);
    }

    private static void SetRect(RectTransform rect, float minX, float minY,
        float maxX, float maxY)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(Mathf.Max(0f, maxX - minX), Mathf.Max(0f, maxY - minY));
        rect.anchoredPosition = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
    }

    private Button FirstReachableMapButton()
    {
        if (mapUI == null || mapUI.NodeButtons == null) return null;
        foreach (Button button in mapUI.NodeButtons)
            if (button != null && button.isActiveAndEnabled && button.interactable)
                return button;
        return null;
    }

    private void OnSkipClicked() => Skip();
    private void OnContinueClicked() => Continue();

    private bool HasCompletedTutorial()
    {
        try
        {
            string value = UserSettingsStore.CreateDefault().Read(CompletedSettingKey);
            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Guided Tutorial completion could not be read: " + exception.Message);
            return false;
        }
    }

    private void PersistCompleted()
    {
        try
        {
            UserSettingsStore.CreateDefault().Write(CompletedSettingKey, "true");
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Guided Tutorial completion could not be saved: " + exception.Message);
        }
    }

    private void HideWithoutCompleting()
    {
        step = Step.Inactive;
        targetButton = null;
        HideOverlayOnly();
    }

    private void HideOverlayOnly()
    {
        if (overlay != null) overlay.SetActive(false);
        if (EventSystem.current != null && continueButton != null &&
            EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(overlay.transform))
            EventSystem.current.SetSelectedGameObject(targetButton != null
                ? targetButton.gameObject : null);
    }

    private void RestoreGameplayFocus()
    {
        if (EventSystem.current == null) return;
        Button candidate = battleManager != null &&
            battleManager.GuidedTutorialBasicAttackButton != null &&
            battleManager.GuidedTutorialBasicAttackButton.interactable
                ? battleManager.GuidedTutorialBasicAttackButton
                : mapUI != null ? mapUI.EnterNodeButton : null;
        if (candidate != null && candidate.gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(candidate.gameObject);
    }

    private bool EnsureBuilt()
    {
        if (overlay != null && skipButton != null && continueButton != null) return true;
        Build();
        return overlay != null && skipButton != null && continueButton != null;
    }

    private void Build()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>(true);
        if (canvas == null) return;

        overlay = new GameObject("GuidedTutorialOverlay", typeof(RectTransform));
        overlay.transform.SetParent(canvas.transform, false);
        overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;

        blockers = new Image[4];
        for (int i = 0; i < blockers.Length; i++)
        {
            var blocker = new GameObject("GuidedTutorialRaycastBlocker_" + i,
                typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(overlay.transform, false);
            Image image = blocker.GetComponent<Image>();
            image.color = Backdrop;
            image.raycastTarget = true;
            blockers[i] = image;
        }

        var focus = new GameObject("GuidedTutorialFocus", typeof(RectTransform),
            typeof(Image), typeof(Outline));
        focus.transform.SetParent(overlay.transform, false);
        focusRect = focus.GetComponent<RectTransform>();
        Image focusImage = focus.GetComponent<Image>();
        focusImage.color = new Color(Violet.r, Violet.g, Violet.b, 0.04f);
        focusImage.raycastTarget = false;
        Outline focusOutline = focus.GetComponent<Outline>();
        focusOutline.effectColor = Violet;
        focusOutline.effectDistance = new Vector2(4f, -4f);

        var card = new GameObject("GuidedTutorialCard", typeof(RectTransform),
            typeof(Image), typeof(Outline));
        card.transform.SetParent(overlay.transform, false);
        cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        card.GetComponent<Image>().color = CardColor;
        Outline cardOutline = card.GetComponent<Outline>();
        cardOutline.effectColor = Violet;
        cardOutline.effectDistance = new Vector2(2f, -2f);

        kickerText = Label("GuidedTutorialKicker", card.transform,
            "SYNGRAVA  /  FIELD TRAINING", 14, Violet,
            new Vector2(0.055f, 0.80f), new Vector2(0.93f, 0.94f),
            TextAlignmentOptions.Left);
        titleText = Label("GuidedTutorialTitle", card.transform,
            "", 23, TextColor,
            new Vector2(0.055f, 0.63f), new Vector2(0.93f, 0.81f),
            TextAlignmentOptions.Left);
        bodyText = Label("GuidedTutorialBody", card.transform,
            "", 17, MutedColor,
            new Vector2(0.055f, 0.27f), new Vector2(0.93f, 0.61f),
            TextAlignmentOptions.TopLeft);

        skipButton = MakeButton("GuidedTutorialSkipButton", card.transform,
            "SKIP TUTORIAL", new Vector2(0.055f, 0.06f),
            new Vector2(0.38f, 0.21f), OnSkipClicked);
        continueButton = MakeButton("GuidedTutorialContinueButton", card.transform,
            "CONTINUE", new Vector2(0.62f, 0.06f),
            new Vector2(0.945f, 0.21f), OnContinueClicked);

        overlay.SetActive(false);
        focusRect.gameObject.SetActive(false);
    }

    private static TMP_Text Label(string objectName, Transform parent,
        string value, float size, Color color, Vector2 min, Vector2 max,
        TextAlignmentOptions alignment)
    {
        var label = new GameObject(objectName, typeof(RectTransform),
            typeof(TextMeshProUGUI));
        label.transform.SetParent(parent, false);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TMP_Text text = label.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static Button MakeButton(string objectName, Transform parent,
        string caption, Vector2 min, Vector2 max, Action action)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform),
            typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        buttonObject.GetComponent<Image>().color = new Color(0.16f, 0.08f, 0.25f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.86f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.78f, 0.62f, 0.95f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => action());
        Label(objectName + "Label", buttonObject.transform, caption,
            13, TextColor, Vector2.zero, Vector2.one,
            TextAlignmentOptions.Center);
        return button;
    }
}
