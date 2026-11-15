using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkillArchiveUI : MonoBehaviour
{
    private static readonly Color Backdrop = new Color(0f, 0f, 0f, 0.93f);
    private static readonly Color Surface = new Color(0.025f, 0.014f, 0.05f, 1f);
    private static readonly Color Panel = new Color(0.047f, 0.027f, 0.075f, 1f);
    private static readonly Color Inner = new Color(0.075f, 0.042f, 0.11f, 1f);
    private static readonly Color Violet = new Color(0.74f, 0.36f, 1f, 1f);
    private static readonly Color White = new Color(0.97f, 0.95f, 1f, 1f);
    private static readonly Color Muted = new Color(0.72f, 0.66f, 0.80f, 1f);

    private SkillProgressStore store;
    private Func<bool> activeRunCheck;
    private GameObject overlay;
    private Transform listContent;
    private TMP_Text collectionCountText;
    private TMP_Text selectedNameText;
    private TMP_Text selectedCostText;
    private TMP_Text selectedDescriptionText;
    private TMP_Text selectedPassiveText;
    private TMP_Text statusText;
    private TMP_Text loadoutNoticeText;
    private TMP_Text[] slotLabels = new TMP_Text[SkillProgressStore.LoadoutSlotCount];
    private Button[] equipButtons = new Button[SkillProgressStore.LoadoutSlotCount];
    private Button[] clearButtons = new Button[SkillProgressStore.LoadoutSlotCount];
    private readonly List<Button> rewardButtons = new List<Button>();
    private string selectedSkillCode;
    private string[] draftLoadout;
    private bool initialized;

    public void Initialize(SkillProgressStore progressStore, Func<bool> hasActiveRun)
    {
        store = progressStore ?? throw new ArgumentNullException(nameof(progressStore));
        activeRunCheck = hasActiveRun;
    }

    public void Open()
    {
        if (store == null) store = SkillProgressStore.CreateDefault();
        EnsureBuilt();
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
        Refresh();
        if (EventSystem.current != null && listContent.childCount > 0)
        {
            Button first = listContent.GetChild(0).GetComponent<Button>();
            if (first != null) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
    }

    public void Close()
    {
        if (overlay != null) overlay.SetActive(false);
    }

    private void EnsureBuilt()
    {
        if (initialized) return;
        overlay = Create("SkillArchiveOverlay", transform, typeof(Image));
        Stretch(overlay.GetComponent<RectTransform>());
        Image backdrop = overlay.GetComponent<Image>();
        backdrop.color = Backdrop;
        backdrop.raycastTarget = true;

        GameObject card = Create("SkillArchiveCard", overlay.transform,
            typeof(Image), typeof(Outline));
        Center(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1500f, 870f));
        card.GetComponent<Image>().color = Surface;
        card.GetComponent<Outline>().effectColor = Violet;
        card.GetComponent<Outline>().effectDistance = new Vector2(3f, -3f);

        Label(card.transform, "SkillArchiveTitle", "SKILL ARCHIVE", 34f, Violet,
            new Vector2(0f, 389f), new Vector2(1240f, 54f), TextAlignmentOptions.Center, FontStyles.Bold);
        loadoutNoticeText = Label(card.transform, "SkillArchiveNotice", string.Empty, 16f, Muted,
            new Vector2(0f, 349f), new Vector2(1300f, 32f), TextAlignmentOptions.Center, FontStyles.Normal);

        BuildCollection(card.transform);
        BuildDetailsAndLoadout(card.transform);
        BuildRewardChoices(card.transform);
        statusText = Label(card.transform, "SkillArchiveStatus", string.Empty, 15f, Muted,
            new Vector2(100f, -393f), new Vector2(820f, 30f), TextAlignmentOptions.Left, FontStyles.Normal);
        Button save = Button(card.transform, "SaveLoadout", "SAVE LOADOUT",
            new Vector2(500f, -393f), new Vector2(210f, 48f), Violet);
        save.onClick.AddListener(SaveLoadout);
        Button close = Button(card.transform, "CloseSkillArchive", "CLOSE",
            new Vector2(690f, -393f), new Vector2(140f, 48f), Muted);
        close.onClick.AddListener(Close);

        initialized = true;
        overlay.SetActive(false);
    }

    private void BuildCollection(Transform parent)
    {
        GameObject collection = Create("SkillCollectionPanel", parent, typeof(Image), typeof(Outline));
        Center(collection.GetComponent<RectTransform>(), new Vector2(-505f, -8f), new Vector2(430f, 680f));
        collection.GetComponent<Image>().color = Panel;
        collection.GetComponent<Outline>().effectColor = new Color(0.32f, 0.18f, 0.46f, 1f);
        collectionCountText = Label(collection.transform, "SkillCollectionCount", string.Empty, 17f,
            Violet, new Vector2(0f, 307f), new Vector2(380f, 34f), TextAlignmentOptions.Center, FontStyles.Bold);

        GameObject viewport = Create("SkillCollectionViewport", collection.transform, typeof(Image), typeof(Mask));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Center(viewportRect, new Vector2(0f, -20f), new Vector2(404f, 615f));
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = Create("SkillCollectionContent", viewport.transform,
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 5f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        listContent = content.transform;

        ScrollRect scroll = collection.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 36f;
    }

    private void BuildDetailsAndLoadout(Transform parent)
    {
        GameObject detailPanel = Create("SelectedSkillPanel", parent, typeof(Image), typeof(Outline));
        Center(detailPanel.GetComponent<RectTransform>(), new Vector2(245f, 162f), new Vector2(1025f, 320f));
        detailPanel.GetComponent<Image>().color = Panel;
        detailPanel.GetComponent<Outline>().effectColor = new Color(0.32f, 0.18f, 0.46f, 1f);
        selectedNameText = Label(detailPanel.transform, "SelectedSkillName", "SELECT A SKILL", 27f,
            White, new Vector2(0f, 119f), new Vector2(900f, 48f), TextAlignmentOptions.Center, FontStyles.Bold);
        selectedCostText = Label(detailPanel.transform, "SelectedSkillCost", string.Empty, 17f,
            Violet, new Vector2(0f, 78f), new Vector2(900f, 32f), TextAlignmentOptions.Center, FontStyles.Bold);
        selectedDescriptionText = Label(detailPanel.transform, "SelectedSkillDescription", string.Empty,
            17f, Muted, new Vector2(0f, 8f), new Vector2(940f, 108f), TextAlignmentOptions.Center, FontStyles.Normal);
        selectedPassiveText = Label(detailPanel.transform, "SelectedSkillPassive", string.Empty,
            16f, Muted, new Vector2(0f, -90f), new Vector2(940f, 62f), TextAlignmentOptions.Center, FontStyles.Normal);

        Label(parent, "LoadoutHeading", "NEXT EXPEDITION LOADOUT  •  THREE SKILL SLOTS",
            18f, Violet, new Vector2(250f, -43f), new Vector2(930f, 36f), TextAlignmentOptions.Center, FontStyles.Bold);
        float[] xPositions = { -50f, 250f, 550f };
        for (int slot = 0; slot < SkillProgressStore.LoadoutSlotCount; slot++)
        {
            GameObject slotCard = Create("LoadoutSlot" + (slot + 1), parent, typeof(Image), typeof(Outline));
            Center(slotCard.GetComponent<RectTransform>(), new Vector2(xPositions[slot], -104f), new Vector2(270f, 86f));
            slotCard.GetComponent<Image>().color = Inner;
            slotCard.GetComponent<Outline>().effectColor = new Color(0.39f, 0.21f, 0.55f, 1f);
            slotLabels[slot] = Label(slotCard.transform, "SlotSkillName", string.Empty, 16f,
                White, new Vector2(0f, 22f), new Vector2(244f, 34f), TextAlignmentOptions.Center, FontStyles.Bold);
            equipButtons[slot] = Button(slotCard.transform, "EquipSlot" + (slot + 1),
                "EQUIP HERE", new Vector2(-36f, -22f), new Vector2(160f, 34f), Violet);
            int capturedSlot = slot;
            equipButtons[slot].onClick.AddListener(() => EquipSelected(capturedSlot));
            clearButtons[slot] = Button(slotCard.transform, "ClearSlot" + (slot + 1),
                "CLEAR", new Vector2(87f, -22f), new Vector2(76f, 34f), Muted);
            clearButtons[slot].onClick.AddListener(() => ClearSlot(capturedSlot));
        }
    }

    private void BuildRewardChoices(Transform parent)
    {
        Label(parent, "PendingSkillRewardHeading", "PENDING SKILL MEMORY  •  CHOOSE ONE TO ADD IT TO YOUR COLLECTION",
            14f, Violet, new Vector2(250f, -178f), new Vector2(940f, 28f), TextAlignmentOptions.Center, FontStyles.Bold);
        float[] xPositions = { -50f, 250f, 550f };
        for (int index = 0; index < 3; index++)
        {
            Button choice = Button(parent, "PendingSkillChoice" + index, "NO PENDING REWARD",
                new Vector2(xPositions[index], -215f), new Vector2(270f, 62f), Violet);
            rewardButtons.Add(choice);
        }
    }

    private void Refresh()
    {
        try
        {
            PlayerSkillUnlockRow[] ownedRows = store.GetCollection();
            var owned = new HashSet<string>(ownedRows.Select(row => row.SkillCode), StringComparer.Ordinal);
            collectionCountText.text = $"COLLECTION  {owned.Count} / {SkillCatalog.Count}";
            loadoutNoticeText.text = activeRunCheck != null && activeRunCheck()
                ? "CURRENT EXPEDITION KEEPS ITS SAVED LOADOUT. CHANGES APPLY TO YOUR NEXT EXPEDITION."
                : "CHOOSE UP TO THREE OWNED SKILLS FOR YOUR NEXT EXPEDITION.";
            draftLoadout = store.GetLoadout();
            RebuildSkillRows(owned);
            RefreshLoadoutSlots();
            RefreshPendingReward();
            statusText.text = string.Empty;
        }
        catch (Exception error)
        {
            statusText.text = "ARCHIVE ERROR: " + error.Message;
        }
    }

    private void RebuildSkillRows(HashSet<string> owned)
    {
        for (int index = listContent.childCount - 1; index >= 0; index--)
            Destroy(listContent.GetChild(index).gameObject);
        foreach (SkillDefinition definition in SkillCatalog.All)
        {
            bool isOwned = owned.Contains(definition.Code);
            string rowText = isOwned ? definition.Name : definition.Name + "  •  LOCKED";
            Button row = Button(listContent, "SkillRow_" + definition.Code, rowText,
                Vector2.zero, new Vector2(390f, 48f), isOwned ? Violet : Muted);
            LayoutElement layoutElement = row.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 48f;
            string code = definition.Code;
            row.onClick.AddListener(() => SelectSkill(code, isOwned));
        }
    }

    private void SelectSkill(string skillCode, bool owned)
    {
        selectedSkillCode = skillCode;
        SkillDefinition definition = SkillCatalog.Get(skillCode);
        if (definition == null) return;
        selectedNameText.text = definition.Name.ToUpperInvariant() + (owned ? string.Empty : "  •  LOCKED");
        selectedCostText.text = $"{definition.ManaCost} MANA  •  {definition.Power} BASE POWER";
        selectedDescriptionText.text = definition.Description;
        selectedPassiveText.text = "PASSIVE  •  " + definition.PassiveDescription;
        for (int slot = 0; slot < equipButtons.Length; slot++)
            equipButtons[slot].interactable = owned;
    }

    private void EquipSelected(int slot)
    {
        if (string.IsNullOrWhiteSpace(selectedSkillCode) || store == null || !store.Owns(selectedSkillCode)) return;
        for (int index = 0; index < draftLoadout.Length; index++)
            if (draftLoadout[index] == selectedSkillCode) draftLoadout[index] = null;
        draftLoadout[slot] = selectedSkillCode;
        RefreshLoadoutSlots();
        statusText.text = "UNSAVED LOADOUT CHANGE. PRESS SAVE LOADOUT TO KEEP IT.";
    }

    private void ClearSlot(int slot)
    {
        if (draftLoadout == null || slot < 0 || slot >= draftLoadout.Length) return;
        draftLoadout[slot] = null;
        RefreshLoadoutSlots();
        statusText.text = "UNSAVED LOADOUT CHANGE. PRESS SAVE LOADOUT TO KEEP IT.";
    }

    private void SaveLoadout()
    {
        try
        {
            store.SaveLoadout(draftLoadout);
            statusText.text = "LOADOUT SAVED FOR YOUR NEXT EXPEDITION.";
            RefreshLoadoutSlots();
        }
        catch (Exception error)
        {
            statusText.text = "LOADOUT NOT SAVED: " + error.Message;
        }
    }

    private void RefreshLoadoutSlots()
    {
        if (draftLoadout == null) return;
        for (int slot = 0; slot < slotLabels.Length; slot++)
        {
            SkillDefinition definition = SkillCatalog.Get(draftLoadout[slot]);
            slotLabels[slot].text = $"SLOT {slot + 1}  •  " +
                (definition == null ? "EMPTY" : definition.Name);
            clearButtons[slot].interactable = definition != null;
        }
    }

    private void RefreshPendingReward()
    {
        SkillRewardClaimRow claim = store.GetPendingOffer();
        string[] offered = claim == null ? new string[0] : claim.OfferedSkills;
        for (int index = 0; index < rewardButtons.Count; index++)
        {
            Button button = rewardButtons[index];
            if (index >= offered.Length)
            {
                button.interactable = false;
                button.GetComponentInChildren<TMP_Text>(true).text = offered.Length == 0
                    ? "NO PENDING REWARD" : "—";
                button.onClick.RemoveAllListeners();
                continue;
            }
            SkillDefinition definition = SkillCatalog.Get(offered[index]);
            button.interactable = true;
            button.GetComponentInChildren<TMP_Text>(true).text = definition.Name.ToUpperInvariant();
            button.onClick.RemoveAllListeners();
            string code = definition.Code;
            string claimId = claim.ClaimId;
            button.onClick.AddListener(() => ClaimPendingSkill(claimId, code));
        }
    }

    private void ClaimPendingSkill(string claimId, string skillCode)
    {
        try
        {
            if (!store.TryClaim(claimId, skillCode))
            {
                statusText.text = "SKILL MEMORY CLAIM FAILED. THE OFFER IS STILL SAVED.";
                return;
            }
            statusText.text = SkillCatalog.Get(skillCode).Name + " ADDED TO YOUR COLLECTION.";
            Refresh();
        }
        catch (Exception error)
        {
            statusText.text = "CLAIM FAILED: " + error.Message;
        }
    }

    private static Button Button(Transform parent, string name, string text,
        Vector2 position, Vector2 size, Color accent)
    {
        GameObject obj = Create(name, parent, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (parent.GetComponent<VerticalLayoutGroup>() == null)
            Center(rect, position, size);
        else
            rect.sizeDelta = size;
        obj.GetComponent<Image>().color = new Color(0.12f, 0.06f, 0.18f, 1f);
        obj.GetComponent<Outline>().effectColor = accent;
        obj.GetComponent<Outline>().effectDistance = new Vector2(1.2f, -1.2f);
        Label(obj.transform, "Label", text, 14f, White, Vector2.zero, size,
            TextAlignmentOptions.Center, FontStyles.Bold);
        ColorBlock colors = obj.GetComponent<Button>().colors;
        colors.normalColor = new Color(0.12f, 0.06f, 0.18f, 1f);
        colors.highlightedColor = new Color(0.24f, 0.12f, 0.36f, 1f);
        colors.pressedColor = Violet;
        obj.GetComponent<Button>().colors = colors;
        return obj.GetComponent<Button>();
    }

    private static TMP_Text Label(Transform parent, string name, string value, float size,
        Color color, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment,
        FontStyles style)
    {
        GameObject obj = Create(name, parent, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Center(obj.GetComponent<RectTransform>(), position, dimensions);
        TMP_Text text = obj.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, size - 3f);
        text.fontSizeMax = size;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject Create(string name, Transform parent, params Type[] components)
    {
        GameObject obj = new GameObject(name, components);
        obj.transform.SetParent(parent, false);
        return obj;
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

    private void OnDestroy()
    {
        if (store != null)
        {
            store.Dispose();
            store = null;
        }
    }
}
