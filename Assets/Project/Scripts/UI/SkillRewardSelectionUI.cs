using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class SkillRewardSelectionUI : MonoBehaviour
{
    private static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.94f);
    private static readonly Color Surface = new Color(0.025f, 0.014f, 0.05f, 1f);
    private static readonly Color Card = new Color(0.065f, 0.035f, 0.10f, 1f);
    private static readonly Color Violet = new Color(0.74f, 0.36f, 1f, 1f);
    private static readonly Color White = new Color(0.97f, 0.95f, 1f, 1f);
    private static readonly Color Muted = new Color(0.72f, 0.66f, 0.80f, 1f);

    private GameObject overlay;
    private TMP_Text messageText;
    private Button laterButton;
    private readonly List<Button> choiceButtons = new List<Button>();
    private Action<string> claimAction;

    public bool IsOpen => overlay != null && overlay.activeSelf;

    public void Show(string[] offerCodes, Action<string> onClaim, Action onDefer = null)
    {
        if (offerCodes == null || offerCodes.Length == 0) return;
        EnsureBuilt();
        claimAction = onClaim;
        ConfigureChoices(offerCodes);
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        if (EventSystem.current != null && choiceButtons.Count > 0)
            EventSystem.current.SetSelectedGameObject(choiceButtons[0].gameObject);
        laterButton.onClick.RemoveAllListeners();
        laterButton.onClick.AddListener(() =>
        {
            HideImmediate();
            onDefer?.Invoke();
        });
    }

    public void HideImmediate()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    public void ShowMessage(string message)
    {
        if (messageText != null) messageText.text = message;
    }

    private void EnsureBuilt()
    {
        if (overlay != null) return;
        overlay = Create("SkillMemoryOverlay", transform, typeof(Image));
        Stretch(overlay.GetComponent<RectTransform>());
        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = Backdrop;
        backdrop.raycastTarget = true;

        GameObject panel = Create("SkillMemoryPanel", overlay.transform,
            typeof(Image), typeof(Outline));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        Center(panelRect, Vector2.zero, new Vector2(1280f, 650f));
        panel.GetComponent<Image>().color = Surface;
        panel.GetComponent<Outline>().effectColor = Violet;
        panel.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);

        Label(panel.transform, "SkillMemoryTitle", "SKILL MEMORY", 34f, Violet,
            new Vector2(0f, 268f), new Vector2(1120f, 54f), FontStyles.Bold);
        Label(panel.transform, "SkillMemorySubtitle",
            "Choose one new skill to keep in your Skill Archive.", 19f, Muted,
            new Vector2(0f, 222f), new Vector2(1120f, 36f), FontStyles.Normal);
        messageText = Label(panel.transform, "SkillMemoryMessage", string.Empty,
            16f, Muted, new Vector2(0f, -247f), new Vector2(950f, 30f), FontStyles.Normal);

        laterButton = ButtonView(panel.transform, "DeferSkillMemory", "DECIDE LATER",
            new Vector2(0f, -286f), new Vector2(250f, 48f), Violet);
        overlay.SetActive(false);
    }

    private void ConfigureChoices(string[] offerCodes)
    {
        foreach (Button button in choiceButtons)
            if (button != null) Destroy(button.gameObject);
        choiceButtons.Clear();

        Transform panel = overlay.transform.Find("SkillMemoryPanel");
        int count = Mathf.Min(3, offerCodes.Length);
        float spacing = 365f;
        for (int index = 0; index < count; index++)
        {
            SkillDefinition definition = SkillCatalog.Get(offerCodes[index]);
            if (definition == null) continue;
            float x = (index - (count - 1) * 0.5f) * spacing;
            GameObject card = Create("SkillMemoryChoice" + index, panel,
                typeof(Image), typeof(Outline));
            Center(card.GetComponent<RectTransform>(), new Vector2(x, -11f),
                new Vector2(330f, 414f));
            card.GetComponent<Image>().color = Card;
            card.GetComponent<Outline>().effectColor = new Color(0.34f, 0.18f, 0.49f, 1f);
            card.GetComponent<Outline>().effectDistance = new Vector2(1.5f, -1.5f);

            Label(card.transform, "SkillName", definition.Name.ToUpperInvariant(),
                22f, White, new Vector2(0f, 169f), new Vector2(294f, 58f), FontStyles.Bold);
            Label(card.transform, "SkillCost", $"{definition.ManaCost} MANA", 16f,
                Violet, new Vector2(0f, 119f), new Vector2(290f, 30f), FontStyles.Bold);
            Label(card.transform, "SkillDescription", definition.Description, 15f,
                Muted, new Vector2(0f, 26f), new Vector2(288f, 142f), FontStyles.Normal);
            Label(card.transform, "SkillPassive", "PASSIVE\n" + definition.PassiveDescription,
                14f, Muted, new Vector2(0f, -94f), new Vector2(288f, 82f), FontStyles.Normal);

            Button choose = ButtonView(card.transform, "ChooseSkill", "ADD TO ARCHIVE",
                new Vector2(0f, -164f), new Vector2(250f, 46f), Violet);
            string capturedCode = definition.Code;
            choose.onClick.AddListener(() => claimAction?.Invoke(capturedCode));
            choiceButtons.Add(choose);
        }
        messageText.text = string.Empty;
    }

    private static Button ButtonView(Transform parent, string name, string text,
        Vector2 position, Vector2 size, Color accent)
    {
        GameObject obj = Create(name, parent, typeof(Image), typeof(Button), typeof(Outline));
        Center(obj.GetComponent<RectTransform>(), position, size);
        obj.GetComponent<Image>().color = new Color(0.12f, 0.06f, 0.18f, 1f);
        obj.GetComponent<Outline>().effectColor = accent;
        obj.GetComponent<Outline>().effectDistance = new Vector2(1.5f, -1.5f);
        Label(obj.transform, "Label", text, 15f, White, Vector2.zero, size, FontStyles.Bold);
        return obj.GetComponent<Button>();
    }

    private static TMP_Text Label(Transform parent, string name, string value, float size,
        Color color, Vector2 position, Vector2 dimensions, FontStyles style)
    {
        GameObject obj = Create(name, parent, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        RectTransform rect = obj.GetComponent<RectTransform>();
        Center(rect, position, dimensions);
        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, size - 3f);
        text.fontSizeMax = size;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject Create(string name, Transform parent, params Type[] components)
    {
        GameObject result = new GameObject(name, components);
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Center(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
