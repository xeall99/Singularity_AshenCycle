using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Restrained focus feedback for the text-led Main Menu actions.
/// The Button remains responsible for input and invoking its existing handler.
/// </summary>
[RequireComponent(typeof(Button), typeof(Image))]
public sealed class MainMenuActionVisual : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
    IPointerDownHandler, IPointerUpHandler
{
    private static readonly Color MutedText = new Color(0.68f, 0.65f, 0.74f, 1f);
    private static readonly Color FocusText = new Color(0.93f, 0.91f, 0.96f, 1f);
    private static readonly Color PressText = new Color(0.83f, 0.69f, 0.22f, 1f);
    private static readonly Color FocusViolet = new Color(0.64f, 0.44f, 0.89f, 1f);

    private Button button;
    private TMP_Text label;
    private RectTransform labelRect;
    private Image rail;
    private Image rule;
    private Vector2 labelRestPosition;
    private bool hovered;
    private bool selected;
    private bool pressed;
    private float focusAmount;

    private void Awake()
    {
        button = GetComponent<Button>();
        Transform labelObject = transform.Find("Label");
        Transform railObject = transform.Find("FocusRail");
        Transform ruleObject = transform.Find("Rule");
        if (labelObject != null)
        {
            label = labelObject.GetComponent<TMP_Text>();
            labelRect = labelObject as RectTransform;
            labelRestPosition = labelRect.anchoredPosition;
        }
        if (railObject != null) rail = railObject.GetComponent<Image>();
        if (ruleObject != null) rule = ruleObject.GetComponent<Image>();
        ApplyVisuals();
    }

    private void OnDisable()
    {
        hovered = false;
        selected = false;
        pressed = false;
        focusAmount = 0f;
        ApplyVisuals();
    }

    private void Update()
    {
        bool focused = button != null && button.interactable && (hovered || selected);
        float target = focused ? 1f : 0f;
        focusAmount = Mathf.MoveTowards(focusAmount, target,
            Time.unscaledDeltaTime / 0.15f);
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        bool available = button == null || button.interactable;
        if (label != null)
        {
            label.color = !available ? new Color(MutedText.r, MutedText.g, MutedText.b, 0.48f)
                : pressed ? PressText : Color.Lerp(MutedText, FocusText, focusAmount);
            labelRect.anchoredPosition = labelRestPosition + Vector2.right * (8f * focusAmount);
        }
        if (rail != null)
        {
            Color color = FocusViolet;
            color.a = available ? focusAmount : 0f;
            rail.color = color;
        }
        if (rule != null)
        {
            Color quiet = new Color(0.78f, 0.74f, 0.83f, available ? 0.15f : 0.07f);
            rule.color = Color.Lerp(quiet,
                new Color(FocusViolet.r, FocusViolet.g, FocusViolet.b, 0.62f),
                available ? focusAmount : 0f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; pressed = false; }
    public void OnSelect(BaseEventData eventData) { selected = true; }
    public void OnDeselect(BaseEventData eventData) { selected = false; pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { pressed = false; }
}
