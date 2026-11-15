using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Read-only local archive. Six entries per page keep every saved run reachable.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class RunHistoryUI : MonoBehaviour
{
    private const int PageSize = 6;
    private static readonly Color Surface = new Color(0.025f, 0.015f, 0.050f, 1f);
    private static readonly Color Panel = new Color(0.065f, 0.040f, 0.100f, 1f);
    private static readonly Color Violet = new Color(0.72f, 0.40f, 1f, 1f);
    private static readonly Color White = new Color(0.97f, 0.95f, 1f, 1f);
    private static readonly Color Muted = new Color(0.73f, 0.67f, 0.81f, 1f);
    private static readonly Color Crimson = new Color(0.94f, 0.34f, 0.47f, 1f);

    private RunHistoryStore store;
    private RunHistoryRecord[] records = new RunHistoryRecord[0];
    private GameObject overlay;
    private RectTransform cardRect;
    private readonly Button[] rowButtons = new Button[PageSize];
    private readonly TMP_Text[] rowLabels = new TMP_Text[PageSize];
    private Button previousButton;
    private Button nextButton;
    private TMP_Text pageLabel;
    private TMP_Text countLabel;
    private TMP_Text detailTitle;
    private TMP_Text detailBody;
    private int pageIndex;
    private int selectedIndex = -1;
    private string readError;

    public bool IsOpen => overlay != null && overlay.activeSelf;
    public RectTransform CardRect => cardRect;
    public GameObject Overlay => overlay;

    public void Initialize(RunHistoryStore historyStore)
    {
        store = historyStore ?? throw new ArgumentNullException(nameof(historyStore));
        if (overlay == null) Build();
    }

    public void Show()
    {
        if (store == null) Initialize(RunHistoryStore.CreateDefault());
        try
        {
            records = store.ReadRecent();
            readError = null;
        }
        catch (Exception error)
        {
            records = new RunHistoryRecord[0];
            readError = error.Message;
        }
        pageIndex = 0;
        selectedIndex = records.Length == 0 ? -1 : 0;
        Refresh();
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    public void Hide() { if (overlay != null) overlay.SetActive(false); }

    private void Build()
    {
        overlay = CreateObject("RunHistoryOverlay", transform, typeof(Image));
        RectTransform backdrop = overlay.GetComponent<RectTransform>();
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.97f);

        GameObject card = CreateObject("RunHistoryCard", overlay.transform,
            typeof(Image), typeof(Outline));
        cardRect = SetRect(card, Vector2.zero, new Vector2(1120f, 760f));
        card.GetComponent<Image>().color = Surface;
        card.GetComponent<Outline>().effectColor = Violet;
        card.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);

        Label("RunHistoryTitle", card.transform, "THREAD ARCHIVE", 31f, White,
            new Vector2(-315f, 315f), new Vector2(430f, 50f), true);
        Label("RunHistorySubtitle", card.transform,
            "Every finished path leaves a trace.", 16f, Muted,
            new Vector2(-315f, 274f), new Vector2(430f, 34f));
        countLabel = Label("RunHistoryCount", card.transform, string.Empty, 16f,
            Violet, new Vector2(405f, 305f), new Vector2(200f, 35f));
        Button close = Button("RunHistoryClose", card.transform, "CLOSE",
            new Vector2(449f, -323f), new Vector2(150f, 49f));
        close.onClick.AddListener(Hide);

        GameObject list = PanelObject("RunHistoryList", card.transform,
            new Vector2(-325f, -29f), new Vector2(420f, 510f));
        GameObject detail = PanelObject("RunHistoryDetail", card.transform,
            new Vector2(230f, -29f), new Vector2(640f, 510f));

        for (int row = 0; row < PageSize; row++)
        {
            int capturedRow = row;
            Button button = Button("RunHistoryRow" + row, list.transform,
                string.Empty, new Vector2(0f, 205f - row * 82f),
                new Vector2(380f, 76f));
            rowButtons[row] = button;
            rowLabels[row] = button.GetComponentInChildren<TMP_Text>();
            rowLabels[row].fontSize = 15f;
            rowLabels[row].textWrappingMode = TextWrappingModes.Normal;
            button.onClick.AddListener(() => Select(pageIndex * PageSize + capturedRow));
        }

        detailTitle = Label("RunHistoryDetailTitle", detail.transform,
            string.Empty, 25f, White, new Vector2(0f, 200f),
            new Vector2(585f, 48f), true);
        detailBody = Label("RunHistoryDetailBody", detail.transform,
            string.Empty, 16f, Muted, new Vector2(0f, -27f),
            new Vector2(570f, 400f));
        detailBody.alignment = TextAlignmentOptions.TopLeft;
        detailBody.overflowMode = TextOverflowModes.Ellipsis;

        previousButton = Button("RunHistoryPrevious", card.transform,
            "PREVIOUS", new Vector2(-462f, -324f), new Vector2(160f, 48f));
        previousButton.onClick.AddListener(() => ChangePage(-1));
        pageLabel = Label("RunHistoryPage", card.transform, string.Empty, 15f,
            Muted, new Vector2(-323f, -324f), new Vector2(120f, 38f));
        nextButton = Button("RunHistoryNext", card.transform,
            "NEXT", new Vector2(-183f, -324f), new Vector2(160f, 48f));
        nextButton.onClick.AddListener(() => ChangePage(1));
        overlay.SetActive(false);
    }

    private void ChangePage(int offset)
    {
        int pages = Math.Max(1, (records.Length + PageSize - 1) / PageSize);
        pageIndex = Mathf.Clamp(pageIndex + offset, 0, pages - 1);
        selectedIndex = records.Length == 0 ? -1 : pageIndex * PageSize;
        Refresh();
    }

    private void Select(int index)
    {
        if (index < 0 || index >= records.Length) return;
        selectedIndex = index;
        Refresh();
    }

    private void Refresh()
    {
        int pages = Math.Max(1, (records.Length + PageSize - 1) / PageSize);
        pageLabel.text = $"{pageIndex + 1} / {pages}";
        countLabel.text = $"{records.Length} FINISHED RUNS";
        previousButton.interactable = pageIndex > 0;
        nextButton.interactable = pageIndex + 1 < pages;
        for (int row = 0; row < PageSize; row++)
        {
            int index = pageIndex * PageSize + row;
            bool visible = index < records.Length;
            rowButtons[row].gameObject.SetActive(visible);
            if (!visible) continue;
            RunHistoryRecord record = records[index];
            string outcome = record.Outcome == RunSummaryOutcome.Cleared
                ? "CLEARED" : "DEFEATED";
            string score = record.ScoreModelVersion == 0
                ? "UNSCORED" : $"SCORE {record.Score:N0}";
            rowLabels[row].text = $"{outcome}  •  {record.NodesResolved}/{record.NodeCount} NODES  •  {score}\n" +
                record.FinishedUtc.ToLocalTime().ToString("dd MMM yyyy  HH:mm");
            rowLabels[row].color = record.Outcome == RunSummaryOutcome.Cleared
                ? White : Crimson;
            rowButtons[row].GetComponent<Outline>().effectColor =
                index == selectedIndex ? Violet : new Color(0.31f, 0.21f, 0.42f, 1f);
        }

        if (selectedIndex < 0)
        {
            detailTitle.text = readError == null ? "NO RUNS RECORDED" : "ARCHIVE UNAVAILABLE";
            detailBody.text = readError == null
                ? "Finish a Standard Run to add its path to this archive."
                : "The archive could not be read. Your active run and Echo wallet were not changed.\n\n" + readError;
            return;
        }

        RunHistoryRecord selected = records[selectedIndex];
        detailTitle.text = selected.Outcome == RunSummaryOutcome.Cleared
            ? "THREAD SECURED" : "THREAD SEVERED";
        detailTitle.color = selected.Outcome == RunSummaryOutcome.Cleared
            ? Violet : Crimson;
        detailBody.text =
            $"{selected.FinishedUtc.ToLocalTime():dd MMM yyyy  •  HH:mm}\n" +
            $"STAGE  {selected.NodesResolved}/{selected.NodeCount}     SEED  {selected.RunSeed}\n" +
            $"DURATION  {FormatDuration(selected.ElapsedSeconds)}\n" +
            FormatScore(selected) + "\n\n" +
            $"HP  {selected.CurrentHP}/{selected.EffectiveMaxHP}     " +
            $"MANA  {selected.CurrentMana}/{selected.EffectiveMaxMana}\n" +
            $"EMBERS  {selected.TotalEmbers}     ECHO  +{selected.EchoEarned}\n" +
            $"BATTLES  {selected.BattlesWon}     DAMAGE  {selected.DamageDealt}\n\n" +
            $"RELICS  {selected.RelicsText}\n" +
            $"ANOMALIES  {selected.AnomaliesText}\n" +
            $"RUN MODIFIERS  {selected.ModifiersText}\n\n" +
            $"ROUTE\n{FormatRoute(selected.RouteText)}";
    }

    private static string FormatScore(RunHistoryRecord record)
    {
        if (record.ScoreModelVersion == 0)
            return "SCORE  UNAVAILABLE FOR THIS OLDER RUN";
        return $"SCORE  {record.Score:N0}  •  MODEL {record.ScoreModelVersion}\n" +
            $"BATTLE +{record.BattlePoints}   EVENT +{record.EventPoints}   " +
            $"ELITE +{record.ElitePoints}\n" +
            $"BOSS +{record.BossPoints}   CONVERGENCE +{record.ConvergencePoints}";
    }

    private static string FormatRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route)) return "NONE";
        string[] nodes = route.Split(new[] { "  ›  " }, StringSplitOptions.None);
        for (int index = 0; index < nodes.Length; index++)
            nodes[index] = nodes[index].Trim();
        if (nodes.Length <= 5) return string.Join(" · ", nodes);
        int midpoint = (nodes.Length + 1) / 2;
        return string.Join(" · ", nodes, 0, midpoint) + "\n" +
            string.Join(" · ", nodes, midpoint, nodes.Length - midpoint);
    }

    private static string FormatDuration(float seconds)
    {
        int total = Math.Max(0, (int)Math.Floor(seconds));
        return total >= 3600
            ? $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}"
            : $"{total / 60:00}:{total % 60:00}";
    }

    private static GameObject PanelObject(string name, Transform parent,
        Vector2 position, Vector2 size)
    {
        GameObject panel = CreateObject(name, parent, typeof(Image), typeof(Outline));
        SetRect(panel, position, size);
        panel.GetComponent<Image>().color = Panel;
        panel.GetComponent<Outline>().effectColor = new Color(0.38f, 0.23f, 0.54f, 1f);
        return panel;
    }

    private static Button Button(string name, Transform parent, string text,
        Vector2 position, Vector2 size)
    {
        GameObject obj = CreateObject(name, parent,
            typeof(Image), typeof(Button), typeof(Outline));
        SetRect(obj, position, size);
        obj.GetComponent<Image>().color = new Color(0.12f, 0.075f, 0.20f, 1f);
        obj.GetComponent<Outline>().effectColor = Violet;
        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.35f, 0.30f, 0.40f, 1f);
        button.colors = colors;
        Label("Label", obj.transform, text, 16f, White,
            Vector2.zero, size - new Vector2(16f, 8f), true);
        return button;
    }

    private static TMP_Text Label(string name, Transform parent, string value,
        float size, Color color, Vector2 position, Vector2 bounds, bool bold = false)
    {
        GameObject obj = CreateObject(name, parent, typeof(TextMeshProUGUI));
        SetRect(obj, position, bounds);
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.richText = false;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform SetRect(GameObject obj, Vector2 position, Vector2 size)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static GameObject CreateObject(string name, Transform parent, params Type[] components)
    {
        var obj = new GameObject(name, components);
        obj.transform.SetParent(parent, false);
        return obj;
    }
}
