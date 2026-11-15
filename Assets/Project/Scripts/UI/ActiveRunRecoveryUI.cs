using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Runtime startup modal for Continue, recovery and damaged-save handling.
/// It is attached to the existing Battle Canvas and needs no Inspector fields.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class ActiveRunRecoveryUI : MonoBehaviour
{
    private static readonly Color BackdropColor = new Color(0.01f, 0.008f, 0.022f, 1f);
    private static readonly Color CardColor = new Color(0.027f, 0.02f, 0.045f, 1f);
    private static readonly Color InnerColor = new Color(0.095f, 0.04f, 0.15f, 1f);
    private static readonly Color Violet = new Color(0.68f, 0.22f, 1f, 1f);
    private static readonly Color SoftViolet = new Color(0.78f, 0.56f, 0.94f, 1f);
    private static readonly Color White = new Color(0.96f, 0.94f, 1f, 1f);
    private static readonly Color Muted = new Color(0.74f, 0.69f, 0.80f, 1f);

    private Action continueAction;
    private Action recoverAction;
    private Action newRunAction;
    private Action recheckAction;
    private ActiveRunCheckpointStatus visibleStatus;
    private GameObject overlay;
    private RectTransform fitRect;
    private RectTransform cardRect;
    private CanvasGroup cardCanvasGroup;
    private TMP_Text titleText;
    private TMP_Text eyebrowText;
    private TMP_Text summaryText;
    private TMP_Text bodyText;
    private TMP_Text primaryLabel;
    private TMP_Text secondaryLabel;
    private Button primaryButton;
    private Button secondaryButton;
    private Coroutine openingAnimation;
    private readonly List<Sprite> ornamentSlices = new List<Sprite>();

    public bool IsOpen => overlay != null && overlay.activeSelf;
    public GameObject Overlay => overlay;
    public RectTransform CardRect => cardRect;
    public TMP_Text TitleText => titleText;
    public TMP_Text SummaryText => summaryText;
    public TMP_Text BodyText => bodyText;
    public Button PrimaryButton => primaryButton;
    public Button SecondaryButton => secondaryButton;
    public ActiveRunCheckpointStatus VisibleStatus => visibleStatus;

    public void Initialize(
        Action onContinue,
        Action onRecover,
        Action onNewRun,
        Action onRecheck
    )
    {
        continueAction = onContinue;
        recoverAction = onRecover;
        newRunAction = onNewRun;
        recheckAction = onRecheck;
        EnsureBuilt();
    }

    public void Show(ActiveRunCheckpointInspection inspection)
    {
        if (inspection == null)
        {
            throw new ArgumentNullException(nameof(inspection));
        }

        EnsureBuilt();
        if (inspection.Status == ActiveRunCheckpointStatus.None)
        {
            HideImmediate();
            return;
        }

        visibleStatus = inspection.Status;
        SeededRunState state = inspection.CreateStateCopy();
        ConfigureCopy(inspection.Status, state);
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        FitCardToCanvas();
        PlayOpenAnimation();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(primaryButton.gameObject);
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
        if (overlay != null) FitCardToCanvas();
    }

    private void EnsureBuilt()
    {
        if (overlay != null) return;

        overlay = CreateUiObject(
            "ActiveRunRecoveryOverlay",
            transform,
            typeof(Image)
        );
        StretchToParent(overlay.GetComponent<RectTransform>());
        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = BackdropColor;
        backdrop.raycastTarget = true;

        GameObject fit = CreateUiObject("ActiveRunRecoveryFit", overlay.transform);
        fitRect = fit.GetComponent<RectTransform>();
        SetCenteredRect(fitRect, Vector2.zero, new Vector2(900f, 600f));

        GameObject card = CreateUiObject(
            "ActiveRunRecoveryCard",
            fit.transform,
            typeof(Image),
            typeof(Outline),
            typeof(CanvasGroup)
        );
        cardRect = card.GetComponent<RectTransform>();
        StretchToParent(cardRect);
        card.GetComponent<Image>().color = CardColor;
        Outline cardOutline = card.GetComponent<Outline>();
        cardOutline.effectColor = new Color(0.28f, 0.18f, 0.38f, 1f);
        cardOutline.effectDistance = new Vector2(1f, -1f);
        cardCanvasGroup = card.GetComponent<CanvasGroup>();

        GameObject accent = CreateUiObject(
            "ActiveRunRecoveryAccent",
            card.transform,
            typeof(Image)
        );
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, 2f);
        accent.GetComponent<Image>().color = SoftViolet;
        accent.GetComponent<Image>().raycastTarget = false;
        BuildOrnaments(card.transform);

        eyebrowText = CreateLabel(
            "ActiveRunRecoveryEyebrow", card.transform,
            "ACTIVE RUN CHECKPOINT", 16f, SoftViolet,
            new Vector2(35f, 224f), new Vector2(650f, 28f),
            TextAlignmentOptions.Left, false
        );
        titleText = CreateLabel(
            "ActiveRunRecoveryTitle", card.transform,
            string.Empty, 42f, White,
            new Vector2(35f, 172f), new Vector2(650f, 64f),
            TextAlignmentOptions.Left, false
        );

        GameObject summaryPanel = CreateUiObject(
            "ActiveRunRecoverySummaryPanel",
            card.transform,
            typeof(Image),
            typeof(Outline)
        );
        SetCenteredRect(
            summaryPanel.GetComponent<RectTransform>(),
            new Vector2(0f, 64f),
            new Vector2(740f, 154f)
        );
        summaryPanel.GetComponent<Image>().color = new Color(0.065f, 0.044f, 0.09f, 1f);
        Outline summaryOutline = summaryPanel.GetComponent<Outline>();
        summaryOutline.effectColor = new Color(Violet.r, Violet.g, Violet.b, 0.18f);
        summaryOutline.effectDistance = new Vector2(1.5f, -1.5f);

        summaryText = CreateLabel(
            "ActiveRunRecoverySummary", summaryPanel.transform,
            string.Empty, 21f, White,
            Vector2.zero, new Vector2(680f, 116f),
            TextAlignmentOptions.Left, true
        );
        bodyText = CreateLabel(
            "ActiveRunRecoveryBody", card.transform,
            string.Empty, 17f, Muted,
            new Vector2(0f, -58f), new Vector2(740f, 76f),
            TextAlignmentOptions.Left, true
        );

        primaryButton = CreateButton(
            "ActiveRunPrimaryButton", card.transform,
            new Vector2(-190f, -194f), new Vector2(330f, 72f),
            out primaryLabel
        );
        secondaryButton = CreateButton(
            "ActiveRunSecondaryButton", card.transform,
            new Vector2(190f, -194f), new Vector2(330f, 72f),
            out secondaryLabel
        );
        primaryButton.onClick.AddListener(HandlePrimary);
        secondaryButton.onClick.AddListener(HandleSecondary);
        secondaryButton.GetComponent<Image>().color = CardColor;
        secondaryButton.GetComponent<Outline>().effectColor = new Color(0.36f, 0.28f, 0.44f, 1f);
        Navigation primaryNavigation = new Navigation { mode = Navigation.Mode.Explicit,
            selectOnRight = secondaryButton, selectOnDown = secondaryButton };
        Navigation secondaryNavigation = new Navigation { mode = Navigation.Mode.Explicit,
            selectOnLeft = primaryButton, selectOnUp = primaryButton };
        primaryButton.navigation = primaryNavigation;
        secondaryButton.navigation = secondaryNavigation;

        CreateLabel(
            "ActiveRunRecoveryHint", card.transform,
            "Starting a new journey replaces your saved run.",
            14f, new Color(Muted.r, Muted.g, Muted.b, 0.76f),
            new Vector2(0f, -260f), new Vector2(760f, 28f),
            TextAlignmentOptions.Center, false
        );

        FitCardToCanvas();
        overlay.SetActive(false);
    }

    private void BuildOrnaments(Transform parent)
    {
        var art = Resources.Load<StandardRunMapVisualAssets>(
            "RunMap/FracturedFate/StandardRunMapVisualAssets");
        if (art == null || art.Ornaments == null) return;
        AddOrnament(parent, art.Ornaments, "TopTornPaper", new Rect(.24f, .05f, .35f, .265f),
            new Vector2(0f, 275f), new Vector2(890f, 44f));
        AddOrnament(parent, art.Ornaments, "BottomTornPaper", new Rect(.735f, .82f, .25f, .17f),
            new Vector2(0f, -282f), new Vector2(890f, 32f));
        AddOrnament(parent, art.Ornaments, "JourneyEye", new Rect(.315f, .405f, .157f, .29f),
            new Vector2(-370f, 190f), new Vector2(105f, 110f));
        AddOrnament(parent, art.Ornaments, "CornerCrystal", new Rect(.43f, .325f, .10f, .215f),
            new Vector2(403f, 238f), new Vector2(65f, 105f));
    }

    private void AddOrnament(Transform parent, Sprite source, string name, Rect crop,
        Vector2 position, Vector2 size)
    {
        Rect r = source.rect;
        var sprite = Sprite.Create(source.texture,
            new Rect(r.x + crop.x * r.width, r.y + (1f - crop.y - crop.height) * r.height,
                crop.width * r.width, crop.height * r.height), Vector2.one * .5f,
            source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        ornamentSlices.Add(sprite);
        var decoration = CreateUiObject(name, parent, typeof(Image));
        SetCenteredRect(decoration.GetComponent<RectTransform>(), position, size);
        var image = decoration.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
    }

    private void OnDestroy()
    {
        foreach (var sprite in ornamentSlices) if (sprite != null) Destroy(sprite);
    }

    private void ConfigureCopy(
        ActiveRunCheckpointStatus status,
        SeededRunState state
    )
    {
        string summary = state == null
            ? "The saved journey cannot be read safely."
            : $"STAGE {state.Depth} / {state.NodeCount}\n\n" +
              $"HP  {state.CurrentHP}       MANA  {state.CurrentMana}       " +
              $"EMBERS  {state.TotalEmbers}";
        summaryText.text = summary;

        switch (status)
        {
            case ActiveRunCheckpointStatus.ValidPrimary:
                eyebrowText.text = "SYNGRAVA  /  JOURNEY SAVED";
                titleText.text = "CONTINUE RUN";
                bodyText.text =
                    "Your path is waiting. Return to the route map and choose " +
                    "where your journey leads next.";
                primaryLabel.text = "CONTINUE RUN";
                secondaryLabel.text = "START NEW RUN";
                break;

            case ActiveRunCheckpointStatus.RecoverableBackup:
                eyebrowText.text = "SYNGRAVA  /  A PATH REMAINS";
                titleText.text = "RECOVERY AVAILABLE";
                bodyText.text =
                    "Your latest save could not be opened. Recover the earlier " +
                    "checkpoint shown above. Progress after that point will be lost.";
                primaryLabel.text = "RECOVER RUN";
                secondaryLabel.text = "DISCARD & NEW RUN";
                break;

            default:
                eyebrowText.text = "SYNGRAVA  /  JOURNEY INTERRUPTED";
                titleText.text = "SAVE DATA DAMAGED";
                bodyText.text =
                    "Your save and its backup could not be opened. Try checking " +
                    "again, or discard this save to begin a new journey.";
                primaryLabel.text = "RECHECK SAVE";
                secondaryLabel.text = "DISCARD & NEW RUN";
                break;
        }
    }

    private void HandlePrimary()
    {
        switch (visibleStatus)
        {
            case ActiveRunCheckpointStatus.ValidPrimary:
                continueAction?.Invoke();
                break;
            case ActiveRunCheckpointStatus.RecoverableBackup:
                recoverAction?.Invoke();
                break;
            default:
                recheckAction?.Invoke();
                break;
        }
    }

    private void HandleSecondary()
    {
        newRunAction?.Invoke();
    }

    private void FitCardToCanvas()
    {
        if (fitRect == null || overlay == null) return;
        Rect bounds = overlay.GetComponent<RectTransform>().rect;
        float scale = Mathf.Min(
            1f,
            (bounds.width - 48f) / 900f,
            (bounds.height - 48f) / 600f
        );
        fitRect.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }

    private void PlayOpenAnimation()
    {
        if (openingAnimation != null) StopCoroutine(openingAnimation);
        openingAnimation = StartCoroutine(AnimateOpen());
    }

    private IEnumerator AnimateOpen()
    {
        const float duration = 0.24f;
        float elapsed = 0f;
        cardCanvasGroup.alpha = 0f;
        cardRect.localScale = Vector3.one * 0.94f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            cardCanvasGroup.alpha = eased;
            cardRect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
            yield return null;
        }
        cardCanvasGroup.alpha = 1f;
        cardRect.localScale = Vector3.one;
        openingAnimation = null;
    }

    private Button CreateButton(
        string objectName,
        Transform parent,
        Vector2 position,
        Vector2 size,
        out TMP_Text label
    )
    {
        GameObject buttonObject = CreateUiObject(
            objectName, parent, typeof(Image), typeof(Button), typeof(Outline)
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
            objectName + "Label", buttonObject.transform, string.Empty,
            18f, White, Vector2.zero, size - new Vector2(24f, 12f),
            TextAlignmentOptions.Center, false
        );
        return button;
    }

    private TMP_Text CreateLabel(
        string objectName,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        Vector2 position,
        Vector2 size,
        TextAlignmentOptions alignment,
        bool wrap
    )
    {
        GameObject labelObject = CreateUiObject(
            objectName, parent, typeof(TextMeshProUGUI)
        );
        SetCenteredRect(labelObject.GetComponent<RectTransform>(), position, size);
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
        if (font != null) label.font = font;
        return label;
    }

    private static GameObject CreateUiObject(
        string objectName,
        Transform parent,
        params Type[] components
    )
    {
        var allComponents = new Type[components.Length + 1];
        allComponents[0] = typeof(RectTransform);
        Array.Copy(components, 0, allComponents, 1, components.Length);
        var result = new GameObject(objectName, allComponents);
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
}
