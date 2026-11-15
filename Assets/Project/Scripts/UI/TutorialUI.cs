using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class TutorialUI : MonoBehaviour
{
    private sealed class Page
    {
        public readonly string Title;
        public readonly string Body;
        public Page(string title, string body) { Title = title; Body = body; }
    }

    private static readonly Page[] Pages =
    {
        new Page("01  /  THE EXPEDITION", "You guide one Warden through a branching Standard Run of 8–10 nodes. Choose a connected node on the map. The final path ends at the Abyss Herald.\n\nEach node advances the expedition and can change your resources or temporary build."),
        new Page("02  /  YOUR TURN", "BASIC ATTACK deals damage without spending Mana. SHADOW STRIKE is a Skill and costs Mana. GUARD reduces incoming damage.\n\nAfter your action, the enemy takes its prepared turn. Watch your HP and Mana before choosing."),
        new Page("03  /  READ THE ENEMY", "Enemy intent is hidden by default. ANALYZE reveals the enemy action already prepared for this turn.\n\nAnalyze costs 75% of effective maximum Mana, rounded up. It can be used once per node, does not consume your turn, and does not enter Singularity Orbit."),
        new Page("04  /  SINGULARITY ORBIT", "Successful ATTACK, SKILL, and GUARD actions are recorded in Orbit. When three valid actions are filled, one Convergence triggers and the slots reset.\n\nAnalyze is informational only: it never fills an Orbit slot."),
        new Page("05  /  NODES & REWARDS", "Battle, Elite, Event, Rest, Merchant, Relic, and Boss nodes offer different risks and benefits. After a non-final victory, choose one reward when the reward cards appear.\n\nStat upgrades are temporary and belong to the current run."),
        new Page("06  /  KEEP YOUR RECORD", "Continue restores a saved run from its last safe map boundary. Clear, Defeat, and Abandon end that run and reset its temporary build.\n\nFinished runs are recorded in Run History. Echo is awarded separately and can unlock options; Top Runs ranks eligible local Standard Run records.")
    };

    private static readonly Color BackdropColor = new Color(0.008f, 0.004f, 0.018f, 0.91f);
    private static readonly Color CardColor = new Color(0.035f, 0.018f, 0.065f, 0.99f);
    private static readonly Color Violet = new Color(0.72f, 0.38f, 1f, 1f);
    private static readonly Color Text = new Color(0.96f, 0.93f, 1f, 1f);
    private static readonly Color Muted = new Color(0.72f, 0.66f, 0.80f, 1f);

    private GameObject overlay;
    private TMP_Text chapterText;
    private TMP_Text titleText;
    private TMP_Text bodyText;
    private TMP_Text counterText;
    private Button previousButton;
    private Button nextButton;
    private GameObject priorSelection;
    private int pageIndex;

    public bool IsOpen => overlay != null && overlay.activeSelf;
    public int PageIndex => pageIndex;
    public int PageCount => Pages.Length;
    public Button PreviousButton => previousButton;
    public Button NextButton => nextButton;

    private void Awake() => Build();

    private void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            HandleEscape();
    }

    public void HandleEscape()
    {
        if (IsOpen) Close();
    }

    public void Open()
    {
        if (overlay == null) Build();
        priorSelection = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject : null;
        pageIndex = 0;
        RefreshPage();
        overlay.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nextButton.gameObject);
    }

    public void Close()
    {
        if (overlay == null) return;
        overlay.SetActive(false);
        if (EventSystem.current != null && priorSelection != null && priorSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(priorSelection);
        priorSelection = null;
    }

    public void NextPage()
    {
        if (pageIndex >= Pages.Length - 1) return;
        pageIndex++;
        RefreshPage();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(nextButton.interactable ? nextButton.gameObject : previousButton.gameObject);
    }

    public void PreviousPage()
    {
        if (pageIndex <= 0) return;
        pageIndex--;
        RefreshPage();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(previousButton.gameObject);
    }

    private void Build()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        overlay = new GameObject("FieldGuideOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        Image shade = overlay.GetComponent<Image>();
        shade.color = BackdropColor;
        shade.raycastTarget = true;

        GameObject card = new GameObject("FieldGuideCard", typeof(RectTransform), typeof(Image), typeof(Outline));
        card.transform.SetParent(overlay.transform, false);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.07f, 0.08f);
        rect.anchorMax = new Vector2(0.93f, 0.92f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        card.GetComponent<Image>().color = CardColor;
        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = Violet;
        outline.effectDistance = new Vector2(2f, -2f);

        chapterText = Label("FieldGuideKicker", card.transform, "SYNGRAVA  /  FIELD GUIDE", 16, Violet,
            new Vector2(0.07f, 0.86f), new Vector2(0.93f, 0.96f), TextAlignmentOptions.Left);
        titleText = Label("FieldGuideTitle", card.transform, "", 32, Text,
            new Vector2(0.07f, 0.70f), new Vector2(0.93f, 0.85f), TextAlignmentOptions.Left);
        bodyText = Label("FieldGuideBody", card.transform, "", 21, Muted,
            new Vector2(0.09f, 0.30f), new Vector2(0.91f, 0.68f), TextAlignmentOptions.TopLeft);
        counterText = Label("FieldGuidePageCounter", card.transform, "", 14, Violet,
            new Vector2(0.40f, 0.19f), new Vector2(0.60f, 0.25f), TextAlignmentOptions.Center);

        previousButton = MakeButton("PreviousPageButton", card.transform, "PREVIOUS",
            new Vector2(0.07f, 0.08f), new Vector2(0.28f, 0.17f), PreviousPage);
        nextButton = MakeButton("NextPageButton", card.transform, "NEXT",
            new Vector2(0.72f, 0.08f), new Vector2(0.93f, 0.17f), NextPage);
        MakeButton("CloseFieldGuideButton", card.transform, "CLOSE",
            new Vector2(0.40f, 0.07f), new Vector2(0.60f, 0.15f), Close);

        RefreshPage();
        overlay.SetActive(false);
    }

    private void RefreshPage()
    {
        if (chapterText == null) return;
        Page page = Pages[pageIndex];
        titleText.text = page.Title;
        bodyText.text = page.Body;
        counterText.text = $"{pageIndex + 1}  /  {Pages.Length}";
        previousButton.interactable = pageIndex > 0;
        nextButton.interactable = pageIndex < Pages.Length - 1;
    }

    private static Button MakeButton(string name, Transform parent, string caption,
        Vector2 min, Vector2 max, Action action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>(); image.color = new Color(0.12f, 0.07f, 0.20f, 1f);
        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.85f, 1f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.78f, 0.62f, 0.95f, 1f);
        button.colors = colors;
        button.onClick.AddListener(() => action());
        Label(name + "Label", go.transform, caption, 15, Text,
            Vector2.zero, Vector2.one, TextAlignmentOptions.Center);
        return button;
    }

    private static TMP_Text Label(string name, Transform parent, string value,
        float size, Color color, Vector2 min, Vector2 max, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value; text.fontSize = size; text.color = color;
        text.alignment = alignment; text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
