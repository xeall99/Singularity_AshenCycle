using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Read-only top-ten projection of the finished Standard Run archive.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class LocalLeaderboardUI : MonoBehaviour
{
    private const int RowCount = 10;
    private static readonly Color Surface = new Color(0.025f, 0.015f, 0.050f, 1f);
    private static readonly Color Row = new Color(0.075f, 0.044f, 0.120f, 1f);
    private static readonly Color Violet = new Color(0.72f, 0.40f, 1f, 1f);
    private static readonly Color White = new Color(0.97f, 0.95f, 1f, 1f);
    private static readonly Color Muted = new Color(0.73f, 0.67f, 0.81f, 1f);
    private static readonly Color Crimson = new Color(0.94f, 0.34f, 0.47f, 1f);

    private LocalLeaderboardService service;
    private LocalLeaderboardEntry[] entries = new LocalLeaderboardEntry[0];
    private GameObject overlay;
    private RectTransform cardRect;
    private readonly Button[] rowButtons = new Button[RowCount];
    private readonly TMP_Text[,] rowCells = new TMP_Text[RowCount, 6];
    private static readonly float[] ColumnX = { -520f, -380f, -145f, 88f, 287f, 481f };
    private static readonly float[] ColumnWidth = { 70f, 170f, 160f, 120f, 120f, 120f };
    private TMP_Text countLabel;
    private TMP_Text emptyLabel;
    private TMP_Text detailTitle;
    private TMP_Text detailBreakdown;
    private Button closeButton;
    private GameObject returnSelection;
    private int selectedIndex;
    private bool readFailed;

    public bool IsOpen => overlay != null && overlay.activeSelf;
    public RectTransform CardRect => cardRect;
    public GameObject Overlay => overlay;

    public void Initialize(RunHistoryStore history)
    {
        service = new LocalLeaderboardService(history);
        if (overlay == null) Build();
    }

    public void Show()
    {
        if (service == null) Initialize(RunHistoryStore.CreateDefault());
        if (!IsOpen && EventSystem.current != null)
            returnSelection = EventSystem.current.currentSelectedGameObject;
        try
        {
            entries = service.ReadTopTen();
            readFailed = false;
        }
        catch (Exception)
        {
            entries = new LocalLeaderboardEntry[0];
            readFailed = true;
        }
        selectedIndex = 0;
        Refresh();
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(entries.Length > 0
                ? rowButtons[0].gameObject : closeButton.gameObject);
    }

    public void Hide()
    {
        if (overlay != null) overlay.SetActive(false);
        if (EventSystem.current != null && returnSelection != null &&
            returnSelection.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(returnSelection);
    }

    private void Build()
    {
        overlay = CreateObject("LocalLeaderboardOverlay", transform, typeof(Image));
        RectTransform dim = overlay.GetComponent<RectTransform>();
        dim.anchorMin = Vector2.zero;
        dim.anchorMax = Vector2.one;
        dim.offsetMin = dim.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.97f);

        GameObject card = CreateObject("LocalLeaderboardCard", overlay.transform,
            typeof(Image), typeof(Outline));
        cardRect = SetRect(card, Vector2.zero, new Vector2(1240f, 800f));
        card.GetComponent<Image>().color = Surface;
        card.GetComponent<Outline>().effectColor = Violet;
        card.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);

        Label("LeaderboardTitle", card.transform, "FATE LEDGER", 34f, White,
            new Vector2(-383f, 329f), new Vector2(430f, 52f), true);
        Label("LeaderboardSubtitle", card.transform,
            "The strongest finished Standard Run threads.", 16f, Muted,
            new Vector2(-321f, 285f), new Vector2(555f, 31f));
        countLabel = Label("LeaderboardCount", card.transform, string.Empty,
            16f, Violet, new Vector2(365f, 326f), new Vector2(260f, 34f));
        closeButton = CreateButton("LeaderboardClose", card.transform,
            new Vector2(544f, 327f), new Vector2(100f, 43f));
        Label("LeaderboardCloseLabel", closeButton.transform, "CLOSE",
            15f, White, Vector2.zero, new Vector2(88f, 30f), true);
        closeButton.onClick.AddListener(Hide);
        AddUiEvent(closeButton, EventTriggerType.Cancel, Hide);

        GameObject header = CreateObject("LeaderboardColumns", card.transform,
            typeof(Image));
        SetRect(header, new Vector2(0f, 234f), new Vector2(1160f, 44f));
        header.GetComponent<Image>().color = new Color(0.13f, 0.07f, 0.20f, 1f);
        Column(header.transform, "RANK", -520f, 70f);
        Column(header.transform, "RESULT", -380f, 170f);
        Column(header.transform, "SCORE", -145f, 160f);
        Column(header.transform, "DEPTH", 88f, 120f);
        Column(header.transform, "BOSSES", 287f, 120f);
        Column(header.transform, "TIME", 481f, 120f);

        for (int row = 0; row < RowCount; row++)
        {
            int captured = row;
            Button button = CreateButton("LeaderboardRow" + row,
                card.transform, new Vector2(0f, 177f - row * 49f),
                new Vector2(1160f, 46f));
            rowButtons[row] = button;
            for (int column = 0; column < 6; column++)
                rowCells[row, column] = Label(
                    "LeaderboardRow" + row + "Cell" + column,
                    button.transform, string.Empty, 17f, White,
                    new Vector2(ColumnX[column], 0f),
                    new Vector2(ColumnWidth[column], 39f));
            button.onClick.AddListener(() => Select(captured));
            AddUiEvent(button, EventTriggerType.Select, () => Select(captured));
            AddUiEvent(button, EventTriggerType.Cancel, Hide);
        }

        emptyLabel = Label("LeaderboardEmpty", card.transform, string.Empty,
            23f, Muted, new Vector2(0f, -13f), new Vector2(1000f, 65f), true);

        GameObject detail = CreateObject("LeaderboardDetail", card.transform,
            typeof(Image), typeof(Outline));
        SetRect(detail, new Vector2(0f, -339f), new Vector2(1160f, 76f));
        detail.GetComponent<Image>().color = Row;
        detail.GetComponent<Outline>().effectColor = new Color(0.36f, 0.21f, 0.50f, 1f);
        detailTitle = Label("LeaderboardDetailTitle", detail.transform,
            string.Empty, 15f, White, new Vector2(0f, 17f),
            new Vector2(1100f, 27f));
        detailBreakdown = Label("LeaderboardDetailBreakdown", detail.transform,
            string.Empty, 14f, Muted, new Vector2(0f, -16f),
            new Vector2(1100f, 27f));
        overlay.SetActive(false);
    }

    private void Select(int index)
    {
        if (index < 0 || index >= entries.Length) return;
        selectedIndex = index;
        Refresh();
    }

    private void Refresh()
    {
        countLabel.text = entries.Length + " RANKED RUNS";
        emptyLabel.gameObject.SetActive(entries.Length == 0);
        emptyLabel.text = readFailed ? "ARCHIVE UNAVAILABLE" : "NO SCORED RUNS YET";
        for (int row = 0; row < RowCount; row++)
        {
            bool visible = row < entries.Length;
            rowButtons[row].gameObject.SetActive(visible);
            if (!visible) continue;
            LocalLeaderboardEntry entry = entries[row];
            string result = entry.Outcome == RunSummaryOutcome.Cleared
                ? "CLEARED" : "DEFEATED";
            rowCells[row, 0].text = "#" + entry.Rank;
            rowCells[row, 1].text = result;
            rowCells[row, 2].text = entry.Score.ToString("N0");
            rowCells[row, 3].text = $"{entry.NodesResolved}/{entry.NodeCount}";
            rowCells[row, 4].text = entry.BossCount.ToString();
            rowCells[row, 5].text = FormatDuration(entry.ElapsedSeconds);
            for (int column = 0; column < 6; column++)
                rowCells[row, column].color = entry.Outcome == RunSummaryOutcome.Cleared
                    ? White : Crimson;
            rowButtons[row].GetComponent<Outline>().effectColor =
                row == selectedIndex ? Violet : new Color(0.30f, 0.19f, 0.40f, 1f);
        }
        ConfigureNavigation();
        if (entries.Length == 0)
        {
            detailTitle.text = readFailed
                ? "Run History could not be read. Active saves were not changed."
                : "Finish a Standard Run to enter the ledger.";
            detailBreakdown.text = "Older unscored history remains in Run History.";
            return;
        }
        LocalLeaderboardEntry selected = entries[selectedIndex];
        string ruleset = string.IsNullOrEmpty(selected.RulesetVersion)
            ? "UNKNOWN" : "v" + selected.RulesetVersion;
        detailTitle.text =
            $"RANK #{selected.Rank}   •   SEED {selected.RunSeed}   •   " +
            $"{selected.FinishedUtc.ToLocalTime():dd MMM yyyy  HH:mm}   •   RULESET {ruleset}";
        detailBreakdown.text =
            $"BATTLE +{selected.BattlePoints}   EVENT +{selected.EventPoints}   " +
            $"ELITE +{selected.ElitePoints}   BOSS +{selected.BossPoints}   " +
            $"CONVERGENCE +{selected.ConvergencePoints}";
    }

    private void ConfigureNavigation()
    {
        closeButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnDown = entries.Length > 0 ? rowButtons[0] : closeButton,
            selectOnUp = entries.Length > 0 ? rowButtons[entries.Length - 1] : closeButton
        };
        for (int row = 0; row < entries.Length; row++)
            rowButtons[row].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = row == 0 ? closeButton : rowButtons[row - 1],
                selectOnDown = row == entries.Length - 1 ? closeButton : rowButtons[row + 1],
                selectOnRight = closeButton
            };
    }

    private static void AddUiEvent(Button button, EventTriggerType type,
        UnityEngine.Events.UnityAction callback)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }

    private static void Column(Transform parent, string title, float x, float width)
    {
        Label("Column" + title, parent, title, 15f, Violet,
            new Vector2(x, 0f), new Vector2(width, 30f), true);
    }

    private static string FormatDuration(float seconds)
    {
        int total = Math.Max(0, (int)Math.Floor(seconds));
        return total >= 3600
            ? $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}"
            : $"{total / 60:00}:{total % 60:00}";
    }

    private static Button CreateButton(string name, Transform parent,
        Vector2 position, Vector2 size)
    {
        GameObject obj = CreateObject(name, parent,
            typeof(Image), typeof(Button), typeof(Outline));
        SetRect(obj, position, size);
        obj.GetComponent<Image>().color = Color.white;
        obj.GetComponent<Outline>().effectColor = Violet;
        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Row;
        colors.highlightedColor = new Color(0.20f, 0.12f, 0.31f, 1f);
        colors.pressedColor = new Color(0.31f, 0.18f, 0.46f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        return button;
    }

    private static TMP_Text Label(string name, Transform parent, string value,
        float size, Color color, Vector2 position, Vector2 bounds, bool bold = false)
    {
        GameObject obj = CreateObject(name, parent, typeof(TextMeshProUGUI));
        SetRect(obj, position, bounds);
        var label = obj.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.richText = false;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform SetRect(GameObject obj, Vector2 position,
        Vector2 size)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static GameObject CreateObject(string name, Transform parent,
        params Type[] components)
    {
        var obj = new GameObject(name, components);
        obj.transform.SetParent(parent, false);
        return obj;
    }
}
