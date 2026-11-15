using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Local skill CRUD demonstration on the persistent SQLite content copy.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class ContentManagerUI : MonoBehaviour
{
    private const int PageSize = 5;
    private static readonly Color Surface = new Color(0.025f, 0.015f, 0.050f, 1f);
    private static readonly Color Panel = new Color(0.065f, 0.040f, 0.100f, 1f);
    private static readonly Color Violet = new Color(0.72f, 0.40f, 1f, 1f);
    private static readonly Color White = new Color(0.97f, 0.95f, 1f, 1f);
    private static readonly Color Muted = new Color(0.73f, 0.67f, 0.81f, 1f);
    private static readonly Color Error = new Color(0.96f, 0.44f, 0.51f, 1f);

    private SkillContentRepository repository;
    private SkillData[] all = Array.Empty<SkillData>();
    private SkillData[] visible = Array.Empty<SkillData>();
    private GameObject overlay;
    private GameObject confirmOverlay;
    private readonly Button[] rowButtons = new Button[PageSize];
    private readonly TMP_Text[] rowLabels = new TMP_Text[PageSize];
    private TMP_InputField searchInput;
    private TMP_InputField nameInput;
    private TMP_InputField manaInput;
    private TMP_InputField powerInput;
    private TMP_Text countText;
    private TMP_Text pageText;
    private TMP_Text editTitle;
    private TMP_Text editHint;
    private TMP_Text statusText;
    private TMP_Text confirmText;
    private Button previousButton;
    private Button nextButton;
    private Button sortButton;
    private Button createButton;
    private Button updateButton;
    private Button deleteButton;
    private Action confirmedAction;
    private int pageIndex;
    private int selectedId = -1;
    private bool creating;
    private bool descending;
    private bool readFailed;
    private string status = string.Empty;

    public bool IsOpen => overlay != null && overlay.activeSelf;
    public GameObject Overlay => overlay;

    public void Show()
    {
        if (overlay == null) Build();
        try
        {
            if (repository == null) repository = SkillContentRepository.CreateDefault();
            readFailed = false;
            status = string.Empty;
            Reload();
        }
        catch (Exception error)
        {
            Debug.LogError("Content Manager database read failed: " + error);
            readFailed = true;
            all = Array.Empty<SkillData>();
            status = "DATABASE UNAVAILABLE: " + error.Message;
            Filter();
        }
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (confirmOverlay != null) confirmOverlay.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
    }

    private void Build()
    {
        overlay = Create("ContentManagerOverlay", transform, typeof(Image));
        RectTransform backdrop = overlay.GetComponent<RectTransform>();
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.97f);

        GameObject card = PanelObject("ContentManagerCard", overlay.transform,
            Vector2.zero, new Vector2(1180f, 800f));
        card.GetComponent<Image>().color = Surface;
        card.GetComponent<Outline>().effectColor = Violet;
        Label("Title", card.transform, "CONTENT MANAGER", 31f, White,
            new Vector2(-340f, 342f), new Vector2(450f, 50f), true);
        Label("Subtitle", card.transform, "LOCAL SQLITE  /  SKILL RECORDS", 15f, Violet,
            new Vector2(-340f, 302f), new Vector2(450f, 32f));
        Button close = Button("ContentManagerClose", card.transform, "CLOSE",
            new Vector2(495f, -346f), new Vector2(150f, 48f));
        close.onClick.AddListener(Hide);

        GameObject list = PanelObject("SkillList", card.transform,
            new Vector2(-350f, -9f), new Vector2(430f, 560f));
        GameObject edit = PanelObject("SkillEditor", card.transform,
            new Vector2(225f, -9f), new Vector2(670f, 560f));

        searchInput = Input("SearchSkills", list.transform, "Search name or code",
            new Vector2(-40f, 218f), new Vector2(325f, 46f), 40, false);
        searchInput.onValueChanged.AddListener(_ => Filter());
        sortButton = Button("SortSkills", list.transform, "A–Z",
            new Vector2(147f, 218f), new Vector2(64f, 46f));
        sortButton.onClick.AddListener(() =>
        {
            descending = !descending;
            Filter();
        });
        countText = Label("SkillCount", list.transform, string.Empty, 14f, Muted,
            new Vector2(0f, 167f), new Vector2(385f, 32f));
        for (int row = 0; row < PageSize; row++)
        {
            int captured = row;
            rowButtons[row] = Button("SkillRow" + row, list.transform,
                string.Empty, new Vector2(0f, 111f - row * 73f),
                new Vector2(385f, 65f));
            rowLabels[row] = rowButtons[row].GetComponentInChildren<TMP_Text>();
            rowLabels[row].fontSize = 15f;
            rowLabels[row].textWrappingMode = TextWrappingModes.Normal;
            rowButtons[row].onClick.AddListener(() => Select(pageIndex * PageSize + captured));
        }
        previousButton = Button("SkillPrevious", list.transform, "PREV",
            new Vector2(-137f, -238f), new Vector2(105f, 43f));
        previousButton.onClick.AddListener(() => ChangePage(-1));
        pageText = Label("SkillPage", list.transform, string.Empty, 14f, Muted,
            new Vector2(0f, -238f), new Vector2(100f, 36f));
        nextButton = Button("SkillNext", list.transform, "NEXT",
            new Vector2(137f, -238f), new Vector2(105f, 43f));
        nextButton.onClick.AddListener(() => ChangePage(1));

        editTitle = Label("EditorTitle", edit.transform, string.Empty, 24f, White,
            new Vector2(0f, 220f), new Vector2(610f, 42f), true);
        editHint = Label("EditorHint", edit.transform, string.Empty, 14f, Muted,
            new Vector2(0f, 176f), new Vector2(610f, 47f));
        Label("NameLabel", edit.transform, "SKILL NAME  /  1–40 CHARACTERS", 14f,
            Violet, new Vector2(0f, 125f), new Vector2(560f, 28f));
        nameInput = Input("SkillName", edit.transform, "Enter a unique skill name",
            new Vector2(0f, 88f), new Vector2(560f, 47f), 40, false);
        Label("ManaLabel", edit.transform, "MANA COST  /  0–10", 14f,
            Violet, new Vector2(0f, 42f), new Vector2(560f, 28f));
        manaInput = Input("SkillMana", edit.transform, "0",
            new Vector2(0f, 5f), new Vector2(560f, 47f), 3, true);
        Label("PowerLabel", edit.transform, "POWER  /  0–999", 14f,
            Violet, new Vector2(0f, -41f), new Vector2(560f, 28f));
        powerInput = Input("SkillPower", edit.transform, "0",
            new Vector2(0f, -78f), new Vector2(560f, 47f), 4, true);
        Button newButton = Button("NewSkill", edit.transform, "NEW",
            new Vector2(-221f, -155f), new Vector2(120f, 48f));
        newButton.onClick.AddListener(BeginCreate);
        createButton = Button("CreateSkill", edit.transform, "CREATE",
            new Vector2(-65f, -155f), new Vector2(175f, 48f));
        createButton.onClick.AddListener(CreateSkill);
        updateButton = Button("UpdateSkill", edit.transform, "SAVE CHANGES",
            new Vector2(160f, -155f), new Vector2(245f, 48f));
        updateButton.onClick.AddListener(UpdateSkill);
        deleteButton = Button("DeleteSkill", edit.transform, "DELETE SELECTED",
            new Vector2(0f, -213f), new Vector2(260f, 43f));
        deleteButton.onClick.AddListener(DeleteSkill);
        statusText = Label("SkillStatus", edit.transform, string.Empty, 13f,
            Muted, new Vector2(0f, -255f), new Vector2(620f, 38f));

        BuildConfirmation();
        overlay.SetActive(false);
    }

    private void BuildConfirmation()
    {
        confirmOverlay = Create("ContentManagerConfirmation", overlay.transform,
            typeof(Image));
        RectTransform backdrop = confirmOverlay.GetComponent<RectTransform>();
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        confirmOverlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        GameObject card = PanelObject("ConfirmationCard", confirmOverlay.transform,
            Vector2.zero, new Vector2(600f, 280f));
        card.GetComponent<Outline>().effectColor = Violet;
        confirmText = Label("ConfirmationText", card.transform, string.Empty,
            19f, White, new Vector2(0f, 43f), new Vector2(530f, 120f), true);
        Button yes = Button("ConfirmContentChange", card.transform, "CONFIRM",
            new Vector2(-125f, -80f), new Vector2(210f, 50f));
        yes.onClick.AddListener(() =>
        {
            Action action = confirmedAction;
            confirmedAction = null;
            confirmOverlay.SetActive(false);
            action?.Invoke();
        });
        Button cancel = Button("CancelContentChange", card.transform, "CANCEL",
            new Vector2(125f, -80f), new Vector2(210f, 50f));
        cancel.onClick.AddListener(() =>
        {
            confirmedAction = null;
            confirmOverlay.SetActive(false);
        });
        confirmOverlay.SetActive(false);
    }

    private void Reload()
    {
        all = repository.ReadAll();
        readFailed = false;
        Filter();
    }

    private void Filter()
    {
        string query = searchInput.text.Trim();
        visible = all.Where(skill => string.IsNullOrEmpty(query) ||
            skill.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
            skill.Code.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        if (descending) Array.Reverse(visible);
        pageIndex = Mathf.Clamp(pageIndex, 0,
            Math.Max(0, (visible.Length - 1) / PageSize));
        if (!creating && !visible.Any(skill => skill.Id == selectedId))
        {
            selectedId = visible.Length > 0 ? visible[0].Id : -1;
            LoadSelected();
        }
        Refresh();
    }

    private void ChangePage(int offset)
    {
        pageIndex = Mathf.Clamp(pageIndex + offset, 0,
            Math.Max(0, (visible.Length - 1) / PageSize));
        Refresh();
    }

    private void Select(int index)
    {
        if (index < 0 || index >= visible.Length) return;
        creating = false;
        selectedId = visible[index].Id;
        status = string.Empty;
        LoadSelected();
        Refresh();
    }

    private void BeginCreate()
    {
        creating = true;
        selectedId = -1;
        nameInput.SetTextWithoutNotify(string.Empty);
        manaInput.SetTextWithoutNotify("0");
        powerInput.SetTextWithoutNotify("0");
        status = string.Empty;
        Refresh();
        nameInput.Select();
        nameInput.ActivateInputField();
    }

    private void LoadSelected()
    {
        SkillData selected = all.FirstOrDefault(skill => skill.Id == selectedId);
        nameInput.SetTextWithoutNotify(selected?.Name ?? string.Empty);
        manaInput.SetTextWithoutNotify(selected == null ? "0" :
            selected.ManaCost.ToString(CultureInfo.InvariantCulture));
        powerInput.SetTextWithoutNotify(selected == null ? "0" :
            selected.Power.ToString(CultureInfo.InvariantCulture));
    }

    private void Refresh()
    {
        int pages = Math.Max(1, (visible.Length + PageSize - 1) / PageSize);
        pageText.text = $"{pageIndex + 1} / {pages}";
        countText.text = visible.Length == 0 ? "NO MATCHING SKILLS" :
            $"{visible.Length} SKILL RECORDS";
        sortButton.GetComponentInChildren<TMP_Text>().text = descending ? "Z–A" : "A–Z";
        SetAvailable(previousButton, pageIndex > 0);
        SetAvailable(nextButton, pageIndex + 1 < pages);
        for (int row = 0; row < PageSize; row++)
        {
            int index = pageIndex * PageSize + row;
            bool exists = index < visible.Length;
            rowButtons[row].gameObject.SetActive(exists);
            if (!exists) continue;
            SkillData skill = visible[index];
            rowLabels[row].text = $"{skill.Name}\n" +
                $"{(SkillContentRepository.IsCustom(skill) ? "CUSTOM" : "CORE")}  •  " +
                $"MANA {skill.ManaCost}  •  POWER {skill.Power}";
            rowLabels[row].color = SkillContentRepository.IsCustom(skill) ? White : Muted;
            rowButtons[row].GetComponent<Outline>().effectColor =
                skill.Id == selectedId && !creating ? Violet : Muted;
        }

        SkillData current = all.FirstOrDefault(skill => skill.Id == selectedId);
        bool editable = SkillContentRepository.IsCustom(current);
        bool blocked;
        try
        {
            blocked = ActiveRunCheckpointStore.CreateDefault()
                .Inspect(100, 10).HasUsableState;
        }
        catch (Exception error)
        {
            Debug.LogError("Content Manager checkpoint inspection failed: " + error);
            blocked = true;
        }
        editTitle.text = creating ? "NEW SKILL" : current == null
            ? "SELECT A SKILL" : current.Name;
        editHint.text = blocked
            ? "READ ONLY WHILE AN EXPEDITION IS SAVED"
            : creating ? "Create a local demonstration record."
            : editable ? "Custom record. This skill is not equipped in combat."
            : "Core combat skill. Protected from edits.";
        nameInput.interactable = !readFailed && !blocked && (creating || editable);
        manaInput.interactable = nameInput.interactable;
        powerInput.interactable = nameInput.interactable;
        SetAvailable(createButton, !readFailed && !blocked && creating);
        SetAvailable(updateButton, !readFailed && !blocked && !creating && editable);
        SetAvailable(deleteButton, updateButton.interactable);
        statusText.text = status;
        statusText.color = readFailed || status.StartsWith("ERROR:", StringComparison.Ordinal)
            ? Error : Muted;
    }

    private bool ReadFields(out string name, out int mana, out int power)
    {
        name = nameInput.text;
        bool validMana = int.TryParse(manaInput.text, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out mana);
        bool validPower = int.TryParse(powerInput.text, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out power);
        if (validMana && validPower) return true;
        status = "ERROR: Mana and power must be whole numbers.";
        Refresh();
        return false;
    }

    private static void SetAvailable(Button button, bool available)
    {
        button.interactable = available;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = available ? White : Muted;
    }

    private void CreateSkill()
    {
        if (!creating || !ReadFields(out string name, out int mana, out int power)) return;
        RunWrite(() =>
        {
            SkillData created = repository.Create(name, mana, power);
            selectedId = created.Id;
            creating = false;
            status = "Skill created in the local SQLite database.";
        });
    }

    private void UpdateSkill()
    {
        SkillData selected = all.FirstOrDefault(skill => skill.Id == selectedId);
        if (!SkillContentRepository.IsCustom(selected) ||
            !ReadFields(out string name, out int mana, out int power)) return;
        int id = selected.Id;
        Confirm("Save changes to " + selected.Name + "?", () => RunWrite(() =>
        {
            repository.Update(id, name, mana, power);
            status = "Skill updated in the local SQLite database.";
        }));
    }

    private void DeleteSkill()
    {
        SkillData selected = all.FirstOrDefault(skill => skill.Id == selectedId);
        if (!SkillContentRepository.IsCustom(selected)) return;
        int id = selected.Id;
        Confirm("Permanently delete " + selected.Name + "?", () => RunWrite(() =>
        {
            repository.Delete(id);
            selectedId = -1;
            status = "Skill deleted from the local SQLite database.";
        }));
    }

    private void Confirm(string message, Action action)
    {
        confirmText.text = message;
        confirmedAction = action;
        confirmOverlay.SetActive(true);
        confirmOverlay.transform.SetAsLastSibling();
    }

    private void RunWrite(Action action)
    {
        try
        {
            action();
            Reload();
        }
        catch (Exception error)
        {
            Debug.LogError("Content Manager write failed: " + error);
            status = "ERROR: " + error.Message;
            Refresh();
        }
    }

    private static TMP_InputField Input(string name, Transform parent,
        string placeholder, Vector2 position, Vector2 size, int maxLength, bool integer)
    {
        GameObject holder = Create(name, parent, typeof(Image), typeof(TMP_InputField));
        SetRect(holder, position, size);
        holder.GetComponent<Image>().color = new Color(0.13f, 0.09f, 0.19f, 1f);
        TMP_Text text = Label("Text", holder.transform, string.Empty, 18f, White,
            Vector2.zero, size - new Vector2(22f, 8f));
        text.alignment = TextAlignmentOptions.MidlineLeft;
        TMP_Text hint = Label("Placeholder", holder.transform, placeholder,
            16f, Muted, Vector2.zero, size - new Vector2(22f, 8f));
        hint.alignment = TextAlignmentOptions.MidlineLeft;
        TMP_InputField input = holder.GetComponent<TMP_InputField>();
        input.textComponent = (TextMeshProUGUI)text;
        input.placeholder = (TextMeshProUGUI)hint;
        input.characterLimit = maxLength;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = integer ? TMP_InputField.ContentType.IntegerNumber
            : TMP_InputField.ContentType.Standard;
        return input;
    }

    private static GameObject PanelObject(string name, Transform parent,
        Vector2 position, Vector2 size)
    {
        GameObject panel = Create(name, parent, typeof(Image), typeof(Outline));
        SetRect(panel, position, size);
        panel.GetComponent<Image>().color = Panel;
        panel.GetComponent<Outline>().effectColor = new Color(0.38f, 0.23f, 0.54f, 1f);
        return panel;
    }

    private static Button Button(string name, Transform parent, string label,
        Vector2 position, Vector2 size)
    {
        GameObject obj = Create(name, parent,
            typeof(Image), typeof(Button), typeof(Outline));
        SetRect(obj, position, size);
        obj.GetComponent<Image>().color = new Color(0.12f, 0.075f, 0.20f, 1f);
        obj.GetComponent<Outline>().effectColor = Violet;
        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.12f, 0.075f, 0.20f, 1f);
        colors.highlightedColor = new Color(0.25f, 0.14f, 0.38f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.40f, 0.22f, 0.60f, 1f);
        colors.disabledColor = new Color(0.065f, 0.052f, 0.085f, 1f);
        button.colors = colors;
        TMP_Text text = Label("Label", obj.transform, label, 16f, White,
            Vector2.zero, size - new Vector2(12f, 8f), true);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static TMP_Text Label(string name, Transform parent, string value,
        float size, Color color, Vector2 position, Vector2 bounds, bool bold = false)
    {
        GameObject obj = Create(name, parent, typeof(TextMeshProUGUI));
        SetRect(obj, position, bounds);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
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

    private static GameObject Create(string name, Transform parent, params Type[] components)
    {
        GameObject obj = new GameObject(name, components);
        obj.transform.SetParent(parent, false);
        return obj;
    }
}
