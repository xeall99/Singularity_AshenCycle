using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Button))]
public sealed class RewardCardView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    private const float HoverDuration = 0.14f;

    private Button cardButton;
    private RectTransform cardRect;
    private CanvasGroup canvasGroup;
    private Outline border;
    private TMP_Text cardLabel;

    private Vector2 restingPosition;
    private Vector3 restingScale = Vector3.one;
    private Color accentColour;
    private Color restingBorderColour;

    private Coroutine transition;
    private bool pointerInside;
    private bool hasFocus;
    private bool selectionEnabled;
    private bool isConfigured;
    private Action<RewardCardView> highlightRequested;

    public bool HasPortrait => transform.Find("RewardPortrait") != null;
    public Vector2 RestingPosition => restingPosition;
    public TMP_Text CardLabel => cardLabel;
    public bool IsHighlighted =>
        CanHighlight() && (pointerInside || hasFocus);

    public void SetHighlightCoordinator(
        Action<RewardCardView> coordinator
    )
    {
        highlightRequested = coordinator;
    }

    public void Configure(
        Color accent,
        string glyph,
        TMP_Text label,
        TMP_FontAsset fallbackFont
    )
    {
        cardButton = GetComponent<Button>();
        cardRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        border = GetComponent<Outline>();

        if (border == null)
        {
            border = gameObject.AddComponent<Outline>();
        }

        accentColour = accent;
        cardLabel = label;
        restingBorderColour = new Color(accent.r, accent.g, accent.b, 0.45f);
        border.effectColor = restingBorderColour;
        border.effectDistance = new Vector2(2f, -2f);
        border.useGraphicAlpha = true;

        Image cardImage = GetComponent<Image>();

        if (cardImage != null)
        {
            cardImage.color = new Color(0.07f, 0.055f, 0.11f, 0.98f);
        }

        cardButton.transition = Selectable.Transition.None;
        CreateOrUpdatePortrait(
            glyph,
            cardLabel == null ? fallbackFont : cardLabel.font
        );
        isConfigured = true;
    }

    public void SetLabel(string value)
    {
        if (cardLabel != null)
        {
            cardLabel.text = value;
        }
    }

    public void SetPresentation(Color accent, string glyph)
    {
        if (!isConfigured)
        {
            return;
        }

        accentColour = accent;
        restingBorderColour = new Color(accent.r, accent.g, accent.b, 0.45f);
        border.effectColor = restingBorderColour;
        CreateOrUpdatePortrait(
            glyph,
            cardLabel == null ? null : cardLabel.font
        );
    }

    public void SetRestingLayout(Vector2 position, Vector2 size)
    {
        if (cardRect == null)
        {
            cardRect = GetComponent<RectTransform>();
        }

        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = size;
        cardRect.anchoredPosition = position;
        cardRect.localScale = Vector3.one;
        cardRect.localRotation = Quaternion.identity;

        restingPosition = position;
        restingScale = Vector3.one;
    }

    public void PrepareEntrance(float verticalOffset)
    {
        if (!isConfigured)
        {
            return;
        }

        StopTransition();
        pointerInside = false;
        hasFocus = false;
        selectionEnabled = false;
        cardButton.interactable = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        cardRect.anchoredPosition = restingPosition + Vector2.down * verticalOffset;
        cardRect.localScale = restingScale * 0.86f;
        border.effectColor = restingBorderColour;
        border.effectDistance = new Vector2(2f, -2f);
    }

    public void PlayEntrance(float delay, float duration)
    {
        if (!isConfigured)
        {
            return;
        }

        StopTransition();
        transition = StartCoroutine(AnimateEntrance(delay, duration));
    }

    public void SetSelectionEnabled(bool enabled)
    {
        selectionEnabled = enabled;
        cardButton.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;

        if (!enabled)
        {
            ClearHighlightImmediate();
        }
    }

    public void ClearHighlightImmediate()
    {
        pointerInside = false;
        hasFocus = false;

        if (!isConfigured)
        {
            return;
        }

        StopTransition();
        cardRect.anchoredPosition = restingPosition;
        cardRect.localScale = restingScale;
        cardRect.localRotation = Quaternion.identity;
        border.effectColor = restingBorderColour;
        border.effectDistance = new Vector2(2f, -2f);
    }

    public void ResetImmediate()
    {
        if (!isConfigured)
        {
            return;
        }

        StopTransition();
        pointerInside = false;
        hasFocus = false;
        selectionEnabled = false;
        cardButton.interactable = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        cardRect.anchoredPosition = restingPosition;
        cardRect.localScale = restingScale;
        cardRect.localRotation = Quaternion.identity;
        border.effectColor = restingBorderColour;
        border.effectDistance = new Vector2(2f, -2f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanHighlight())
        {
            return;
        }

        pointerInside = true;
        highlightRequested?.Invoke(this);
        RefreshHighlight();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        RefreshHighlight();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!CanHighlight())
        {
            return;
        }

        hasFocus = true;
        highlightRequested?.Invoke(this);
        RefreshHighlight();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        hasFocus = false;
        RefreshHighlight();
    }

    private bool CanHighlight()
    {
        return isConfigured && selectionEnabled && cardButton.interactable;
    }

    private void RefreshHighlight()
    {
        if (!isConfigured)
        {
            return;
        }

        bool highlighted = CanHighlight() && (pointerInside || hasFocus);
        Vector2 targetPosition = highlighted
            ? restingPosition + Vector2.up * 12f
            : restingPosition;
        Vector3 targetScale = highlighted
            ? restingScale * 1.045f
            : restingScale;
        Color targetBorder = highlighted
            ? new Color(accentColour.r, accentColour.g, accentColour.b, 1f)
            : restingBorderColour;
        Vector2 targetDistance = highlighted
            ? new Vector2(4f, -4f)
            : new Vector2(2f, -2f);

        StopTransition();
        transition = StartCoroutine(
            AnimateHighlight(
                targetPosition,
                targetScale,
                targetBorder,
                targetDistance
            )
        );
    }

    private IEnumerator AnimateEntrance(float delay, float duration)
    {
        float delayElapsed = 0f;

        while (delayElapsed < delay)
        {
            delayElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Vector2 startPosition = cardRect.anchoredPosition;
        Vector3 startScale = cardRect.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(progress);

            cardRect.anchoredPosition = Vector2.LerpUnclamped(
                startPosition,
                restingPosition,
                eased
            );
            cardRect.localScale = Vector3.LerpUnclamped(
                startScale,
                restingScale,
                eased
            );
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);
            yield return null;
        }

        cardRect.anchoredPosition = restingPosition;
        cardRect.localScale = restingScale;
        canvasGroup.alpha = 1f;
        transition = null;
    }

    private IEnumerator AnimateHighlight(
        Vector2 targetPosition,
        Vector3 targetScale,
        Color targetBorder,
        Vector2 targetDistance
    )
    {
        Vector2 startPosition = cardRect.anchoredPosition;
        Vector3 startScale = cardRect.localScale;
        Color startBorder = border.effectColor;
        Vector2 startDistance = border.effectDistance;
        float elapsed = 0f;

        while (elapsed < HoverDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / HoverDuration);
            float eased = progress * progress * (3f - 2f * progress);

            cardRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                eased
            );
            cardRect.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                eased
            );
            border.effectColor = Color.Lerp(startBorder, targetBorder, eased);
            border.effectDistance = Vector2.Lerp(startDistance, targetDistance, eased);
            yield return null;
        }

        cardRect.anchoredPosition = targetPosition;
        cardRect.localScale = targetScale;
        border.effectColor = targetBorder;
        border.effectDistance = targetDistance;
        transition = null;
    }

    private void CreateOrUpdatePortrait(string glyph, TMP_FontAsset fontAsset)
    {
        Transform portraitTransform = transform.Find("RewardPortrait");
        GameObject portraitObject;

        if (portraitTransform == null)
        {
            portraitObject = new GameObject(
                "RewardPortrait",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );
            portraitObject.transform.SetParent(transform, false);
            portraitObject.transform.SetAsFirstSibling();
        }
        else
        {
            portraitObject = portraitTransform.gameObject;
        }

        RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.07f, 0.45f);
        portraitRect.anchorMax = new Vector2(0.93f, 0.94f);
        portraitRect.offsetMin = Vector2.zero;
        portraitRect.offsetMax = Vector2.zero;

        Image portraitImage = portraitObject.GetComponent<Image>();
        portraitImage.color = new Color(
            accentColour.r * 0.22f,
            accentColour.g * 0.22f,
            accentColour.b * 0.22f,
            0.98f
        );
        portraitImage.raycastTarget = false;

        Transform ringsTransform = portraitObject.transform.Find("SingularityRings");
        SingularityOrbitRings rings;

        if (ringsTransform == null)
        {
            GameObject ringsObject = new GameObject(
                "SingularityRings",
                typeof(RectTransform)
            );
            ringsObject.transform.SetParent(portraitObject.transform, false);
            rings = ringsObject.AddComponent<SingularityOrbitRings>();
        }
        else
        {
            rings = ringsTransform.GetComponent<SingularityOrbitRings>();
        }

        RectTransform ringsRect = rings.rectTransform;
        ringsRect.anchorMin = Vector2.zero;
        ringsRect.anchorMax = Vector2.one;
        ringsRect.offsetMin = new Vector2(8f, 8f);
        ringsRect.offsetMax = new Vector2(-8f, -8f);
        rings.color = new Color(
            accentColour.r,
            accentColour.g,
            accentColour.b,
            0.82f
        );
        rings.raycastTarget = false;

        Transform glyphTransform = portraitObject.transform.Find("RewardGlyph");
        TextMeshProUGUI glyphText;

        if (glyphTransform == null)
        {
            GameObject glyphObject = new GameObject(
                "RewardGlyph",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );
            glyphObject.transform.SetParent(portraitObject.transform, false);
            glyphText = glyphObject.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            glyphText = glyphTransform.GetComponent<TextMeshProUGUI>();
        }

        RectTransform glyphRect = glyphText.rectTransform;
        glyphRect.anchorMin = Vector2.zero;
        glyphRect.anchorMax = Vector2.one;
        glyphRect.offsetMin = Vector2.zero;
        glyphRect.offsetMax = Vector2.zero;
        glyphText.text = glyph;
        glyphText.font = fontAsset;
        glyphText.fontSize = 72f;
        glyphText.fontStyle = FontStyles.Bold;
        glyphText.alignment = TextAlignmentOptions.Center;
        glyphText.color = new Color(1f, 1f, 1f, 0.94f);
        glyphText.raycastTarget = false;
    }

    private void StopTransition()
    {
        if (transition == null)
        {
            return;
        }

        StopCoroutine(transition);
        transition = null;
    }

    private static float EaseOutCubic(float value)
    {
        return 1f - Mathf.Pow(1f - value, 3f);
    }

    private void OnDisable()
    {
        StopTransition();
    }
}
