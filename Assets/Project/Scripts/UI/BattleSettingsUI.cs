using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleSettingsUI : MonoBehaviour
{
    private static readonly Color BackdropColor =
        new Color(0.015f, 0.010f, 0.028f, 0.88f);

    private static readonly Color CardColor =
        new Color(0.055f, 0.030f, 0.105f, 0.98f);

    private static readonly Color BorderColor =
        new Color(0.72f, 0.40f, 1.00f, 1.00f);

    private static readonly Color ButtonColor =
        new Color(0.085f, 0.050f, 0.145f, 1.00f);

    private static readonly Color ButtonHighlightColor =
        new Color(0.23f, 0.12f, 0.40f, 1.00f);

    private static readonly Color ButtonPressedColor =
        new Color(0.43f, 0.20f, 0.68f, 1.00f);

    private static readonly Color TextColor =
        new Color(0.95f, 0.92f, 1.00f, 1.00f);

    private static readonly Color PanelColor =
        new Color(0.035f, 0.020f, 0.070f, 0.98f);

    private static readonly Color RowColor =
        new Color(0.075f, 0.045f, 0.125f, 1.00f);

    private static readonly Color MutedTextColor =
        new Color(0.72f, 0.68f, 0.80f, 1.00f);

    private static readonly Color EnabledValueColor =
        new Color(0.27f, 0.14f, 0.46f, 1.00f);

    private static readonly string[] SettingCategoryNames =
    {
        "GAMEPLAY",
        "ACCESSIBILITY",
        "GRAPHICS",
        "AUDIO"
    };

    private static readonly string[][] SettingNames =
    {
        new[] { "Checkpoint", "Enemy Intent" },
        new[] { "Reduce Motion" },
        new[] { "Display Mode", "Quality Preset" },
        new[] { "Master Volume" }
    };

    private const string ReduceMotionKey = "reduce_motion";
    private const string DisplayModeKey = "display_mode";
    private const string QualityKey = "quality_level";
    private const string MasterVolumeKey = "master_volume";

    private Button settingsButton;
    private GameObject settingsPanel;
    private Button closeButton;
    private Button returnToMenuButton;
    private TMP_Text saveStatusText;
    private Transform settingsContentRoot;
    private readonly List<Image> categoryButtonImages = new List<Image>();
    private readonly Dictionary<string, bool> toggleStates =
        new Dictionary<string, bool>();

    private float previousTimeScale = 1f;
    private UserSettingsStore settingsStore;
    private float masterVolume = 1f;
    private FullScreenMode selectedDisplayMode;
    private bool returningToMenu;
    public bool MotionEffectsEnabled => !toggleStates.TryGetValue("1:0", out bool reduced) || !reduced;
    private bool hasStoredTimeScale;
    private bool uiBuilt;
    private bool IsBattleScene => gameObject.scene.name == "Battle";

    public Button SettingsButton => settingsButton;
    public GameObject SettingsPanel => settingsPanel;
    public bool IsOpen => settingsPanel != null && settingsPanel.activeSelf;

    private void Awake()
    {
        LoadPreferences();
        EnsureUiBuilt();
    }

    private void LoadPreferences()
    {
        toggleStates["1:0"] = false;
        selectedDisplayMode = Screen.fullScreenMode;
        try
        {
            settingsStore = UserSettingsStore.CreateDefault();
            toggleStates["1:0"] = settingsStore.Read(ReduceMotionKey) == "1";

            string display = settingsStore.Read(DisplayModeKey);
            if (display == "windowed")
                selectedDisplayMode = FullScreenMode.Windowed;
            else if (display == "borderless")
                selectedDisplayMode = FullScreenMode.FullScreenWindow;
            if (display == "windowed" || display == "borderless")
                Screen.fullScreenMode = selectedDisplayMode;

            string quality = settingsStore.Read(QualityKey);
            if (int.TryParse(quality, out int level) &&
                level >= 0 && level < QualitySettings.names.Length)
                QualitySettings.SetQualityLevel(level, true);

            string volume = settingsStore.Read(MasterVolumeKey);
            if (float.TryParse(volume, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out float savedVolume) && !float.IsNaN(savedVolume) &&
                !float.IsInfinity(savedVolume))
                masterVolume = Mathf.Clamp01(savedVolume);
            AudioListener.volume = masterVolume;
        }
        catch (System.Exception exception)
        {
            settingsStore = null;
            Debug.LogError("Settings gagal dimuat: " + exception.Message);
        }
    }

    private void Start()
    {
        // Scene reloads and script recompiles can leave a component alive while
        // its runtime children have not been created yet. Keep the operation
        // idempotent so this never creates duplicate buttons or listeners.
        EnsureUiBuilt();
    }

    private void EnsureUiBuilt()
    {
        if (uiBuilt && settingsButton != null && settingsPanel != null)
        {
            return;
        }

        Canvas canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError(
                "BattleSettingsUI harus ditempatkan pada GameObject Canvas."
            );
            return;
        }

        if (settingsButton == null)
        {
            BuildSettingsButton(canvas.transform);
        }

        if (settingsPanel == null)
        {
            BuildSettingsPanel(canvas.transform);
        }

        if (settingsButton != null && IsBattleScene)
        {
            settingsButton.gameObject.SetActive(true);
            settingsButton.transform.SetAsLastSibling();
        }
        else if (settingsButton != null)
        {
            settingsButton.gameObject.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        uiBuilt = settingsButton != null && settingsPanel != null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureBattleSceneComponent()
    {
        if (Object.FindAnyObjectByType<BattleSettingsUI>(
                FindObjectsInactive.Include
            ) != null)
        {
            return;
        }

        // The fallback is restricted to a real Battle scene so a Main Menu
        // Canvas never receives battle-only controls.
        BattleManager battleManager = Object.FindAnyObjectByType<BattleManager>(
            FindObjectsInactive.Include
        );

        if (battleManager == null)
        {
            return;
        }

        Scene battleScene = battleManager.gameObject.scene;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include
        );

        for (int index = 0; index < canvases.Length; index++)
        {
            Canvas canvas = canvases[index];

            if (canvas != null && canvas.gameObject.scene == battleScene)
            {
                canvas.gameObject.AddComponent<BattleSettingsUI>();
                return;
            }
        }
    }

    private void BuildSettingsButton(Transform canvasTransform)
    {
        GameObject buttonObject = CreateUiObject(
            "SettingsButton",
            canvasTransform,
            typeof(Image),
            typeof(Button),
            typeof(Outline)
        );

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.zero;
        buttonRect.pivot = Vector2.zero;
        buttonRect.anchoredPosition = new Vector2(34f, 34f);
        buttonRect.sizeDelta = new Vector2(68f, 68f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = ButtonColor;
        buttonImage.raycastTarget = true;

        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2.5f, 2.5f);
        outline.useGraphicAlpha = true;

        settingsButton = buttonObject.GetComponent<Button>();
        settingsButton.targetGraphic = buttonImage;
        settingsButton.transition = Selectable.Transition.ColorTint;
        settingsButton.colors = CreateButtonColors();
        settingsButton.onClick.AddListener(ToggleSettings);

        GameObject gearObject = CreateUiObject(
            "GearIcon",
            buttonObject.transform,
            typeof(GearIconGraphic)
        );

        RectTransform gearRect = gearObject.GetComponent<RectTransform>();
        gearRect.anchorMin = new Vector2(0.5f, 0.5f);
        gearRect.anchorMax = new Vector2(0.5f, 0.5f);
        gearRect.pivot = new Vector2(0.5f, 0.5f);
        gearRect.anchoredPosition = Vector2.zero;
        gearRect.sizeDelta = new Vector2(42f, 42f);

        GearIconGraphic gear = gearObject.GetComponent<GearIconGraphic>();
        gear.color = TextColor;
        gear.raycastTarget = false;

        CanvasRenderer gearRenderer = gear.GetComponent<CanvasRenderer>();

        if (gearRenderer != null)
        {
            gearRenderer.cull = false;
            gearRenderer.cullTransparentMesh = false;
        }

        // Force the first geometry/material rebuild after the runtime child is
        // fully sized and coloured. This avoids an empty mesh when the Canvas
        // is already active while the button hierarchy is being built.
        gear.SetAllDirty();
        gear.transform.SetAsLastSibling();

        Shadow shadow = gearObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.45f, 0.18f, 0.85f, 0.75f);
        shadow.effectDistance = new Vector2(2f, -2f);
        shadow.useGraphicAlpha = true;
    }

    private void BuildSettingsPanel(Transform canvasTransform)
    {
        settingsPanel = CreateUiObject("SettingsOverlay", canvasTransform, typeof(Image));

        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image backdrop = settingsPanel.GetComponent<Image>();
        backdrop.color = BackdropColor;
        backdrop.raycastTarget = true;

        GameObject cardObject = CreateUiObject(
            "SettingsCard",
            settingsPanel.transform,
            typeof(Image),
            typeof(Outline)
        );

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(980f, 620f);

        Image cardImage = cardObject.GetComponent<Image>();
        cardImage.color = CardColor;
        cardImage.raycastTarget = true;

        Outline cardOutline = cardObject.GetComponent<Outline>();
        cardOutline.effectColor = BorderColor;
        cardOutline.effectDistance = new Vector2(4f, 4f);
        cardOutline.useGraphicAlpha = true;

        CreateLabel(
            "SettingsTitle",
            cardObject.transform,
            "SETTINGS",
            32f,
            TextColor,
            new Vector2(0f, 266f),
            new Vector2(900f, 48f),
            TextAlignmentOptions.Center
        );

        CreateLabel(
            "SettingsSubtitle",
            cardObject.transform,
            IsBattleScene ? "BATTLE PAUSED" : "MAIN MENU",
            18f,
            new Color(0.72f, 0.58f, 0.90f, 1f),
            new Vector2(0f, 224f),
            new Vector2(760f, 30f),
            TextAlignmentOptions.Center
        );

        CreateLabel(
            "SettingsHint",
            cardObject.transform,
            "Choose a category. Changes are saved immediately.",
            15f,
            MutedTextColor,
            new Vector2(0f, 193f),
            new Vector2(760f, 28f),
            TextAlignmentOptions.Center
        );

        BuildSettingsNavigation(cardObject.transform);

        bool battleScene = IsBattleScene;
        saveStatusText = CreateLabel(
            "SettingsSaveStatus", cardObject.transform, string.Empty, 14f,
            MutedTextColor, new Vector2(0f, -209f),
            new Vector2(900f, 30f), TextAlignmentOptions.Center);

        closeButton = CreateFooterButton(
            cardObject.transform, "ResumeBattleButton",
            battleScene ? "RESUME BATTLE" : "CLOSE SETTINGS",
            battleScene ? -150f : 0f);
        closeButton.onClick.AddListener(CloseSettings);

        if (battleScene)
        {
            returnToMenuButton = CreateFooterButton(
                cardObject.transform, "ReturnToMainMenuButton",
                "SAVE & MAIN MENU", 150f);
            returnToMenuButton.onClick.AddListener(SaveAndReturnToMainMenu);
        }

        settingsPanel.SetActive(false);
    }

    private Button CreateFooterButton(Transform parent, string objectName,
        string caption, float x)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent,
            typeof(Image), typeof(Button), typeof(Outline));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, -267f);
        rect.sizeDelta = new Vector2(250f, 56f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = ButtonColor;
        image.raycastTarget = true;
        Outline outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, 2f);
        outline.useGraphicAlpha = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateButtonColors();
        CreateLabel(objectName + "Label", buttonObject.transform, caption, 16f,
            TextColor, Vector2.zero, new Vector2(230f, 48f),
            TextAlignmentOptions.Center);
        return button;
    }

    private void BuildSettingsNavigation(Transform cardTransform)
    {
        categoryButtonImages.Clear();

        GameObject categoryPanel = CreateSettingsBox(
            "SettingsCategoryPanel",
            cardTransform,
            new Vector2(-350f, -18f),
            new Vector2(230f, 340f),
            PanelColor,
            new Color(0.35f, 0.18f, 0.58f, 0.70f),
            2f
        );

        CreateLabel(
            "SettingsCategoryHeader",
            categoryPanel.transform,
            "CATEGORIES",
            15f,
            new Color(0.76f, 0.60f, 0.96f, 1f),
            new Vector2(0f, 148f),
            new Vector2(208f, 28f),
            TextAlignmentOptions.Center
        );

        for (int index = 0; index < SettingCategoryNames.Length; index++)
        {
            int categoryIndex = index;
            GameObject categoryObject = CreateUiObject(
                $"SettingsCategory_{index}",
                categoryPanel.transform,
                typeof(Image),
                typeof(Button),
                typeof(Outline)
            );

            RectTransform categoryRect = categoryObject.GetComponent<RectTransform>();
            categoryRect.anchorMin = new Vector2(0.5f, 0.5f);
            categoryRect.anchorMax = new Vector2(0.5f, 0.5f);
            categoryRect.pivot = new Vector2(0.5f, 0.5f);
            categoryRect.anchoredPosition = new Vector2(
                0f,
                106f - index * 45f
            );
            categoryRect.sizeDelta = new Vector2(204f, 38f);

            Image categoryImage = categoryObject.GetComponent<Image>();
            categoryImage.color = index == 0
                ? ButtonHighlightColor
                : ButtonColor;
            categoryImage.raycastTarget = true;

            Outline categoryOutline = categoryObject.GetComponent<Outline>();
            categoryOutline.effectColor = BorderColor;
            categoryOutline.effectDistance = new Vector2(1.5f, 1.5f);
            categoryOutline.useGraphicAlpha = true;

            Button categoryButton = categoryObject.GetComponent<Button>();
            categoryButton.targetGraphic = categoryImage;
            categoryButton.transition = Selectable.Transition.None;
            categoryButton.onClick.AddListener(
                () => ShowSettingsCategory(categoryIndex)
            );

            CreateLabel(
                $"SettingsCategoryLabel_{index}",
                categoryObject.transform,
                SettingCategoryNames[index],
                15f,
                TextColor,
                new Vector2(0f, 0f),
                new Vector2(190f, 32f),
                TextAlignmentOptions.Center
            );

            categoryButtonImages.Add(categoryImage);
        }

        GameObject contentPanel = CreateSettingsBox(
            "SettingsContentPanel",
            cardTransform,
            new Vector2(125f, -18f),
            new Vector2(680f, 340f),
            PanelColor,
            new Color(0.35f, 0.18f, 0.58f, 0.70f),
            2f
        );

        settingsContentRoot = contentPanel.transform;
        contentPanel.AddComponent<RectMask2D>();
        ShowSettingsCategory(0);
    }

    private void ShowSettingsCategory(int categoryIndex)
    {
        if (settingsContentRoot == null ||
            categoryIndex < 0 ||
            categoryIndex >= SettingCategoryNames.Length)
        {
            return;
        }

        for (int childIndex = settingsContentRoot.childCount - 1;
             childIndex >= 0;
             childIndex--)
        {
            Transform child = settingsContentRoot.GetChild(childIndex);
            Destroy(child.gameObject);
        }

        for (int index = 0; index < categoryButtonImages.Count; index++)
        {
            if (categoryButtonImages[index] != null)
            {
                categoryButtonImages[index].color = index == categoryIndex
                    ? ButtonHighlightColor
                    : ButtonColor;
            }
        }

        CreateLabel(
            "SettingsContentTitle",
            settingsContentRoot,
            SettingCategoryNames[categoryIndex],
            21f,
            TextColor,
            new Vector2(0f, 142f),
            new Vector2(640f, 36f),
            TextAlignmentOptions.Center
        );

        GameObject dividerObject = CreateUiObject(
            "SettingsContentDivider",
            settingsContentRoot,
            typeof(Image)
        );

        RectTransform dividerRect = dividerObject.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0.5f, 0.5f);
        dividerRect.anchorMax = new Vector2(0.5f, 0.5f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.anchoredPosition = new Vector2(0f, 119f);
        dividerRect.sizeDelta = new Vector2(620f, 2f);

        Image dividerImage = dividerObject.GetComponent<Image>();
        dividerImage.color = new Color(0.55f, 0.30f, 0.86f, 0.75f);
        dividerImage.raycastTarget = false;

        string[] names = SettingNames[categoryIndex];

        for (int index = 0; index < names.Length; index++)
        {
            CreateSettingsRow(
                settingsContentRoot,
                categoryIndex,
                index,
                names[index]
            );
        }
    }

    private void CreateSettingsRow(
        Transform parent,
        int categoryIndex,
        int rowIndex,
        string settingName
    )
    {
        GameObject rowObject = CreateSettingsBox(
            $"SettingsRow_{categoryIndex}_{rowIndex}",
            parent,
            new Vector2(0f, 82f - rowIndex * 54f),
            new Vector2(640f, 46f),
            RowColor,
            new Color(0.26f, 0.14f, 0.42f, 0.80f),
            1f
        );

        CreateLabel(
            $"SettingsRowLabel_{categoryIndex}_{rowIndex}",
            rowObject.transform,
            settingName,
            16f,
            TextColor,
            new Vector2(-92f, 0f),
            new Vector2(430f, 38f),
            TextAlignmentOptions.Left
        );

        string stateKey = $"{categoryIndex}:{rowIndex}";

        if (categoryIndex == 1)
        {
            bool toggleState = toggleStates.TryGetValue(stateKey, out bool saved)
                && saved;

            GameObject valueObject = CreateUiObject(
                $"SettingsToggle_{categoryIndex}_{rowIndex}",
                rowObject.transform,
                typeof(Image),
                typeof(Button),
                typeof(Outline)
            );

            RectTransform valueRect = valueObject.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.5f, 0.5f);
            valueRect.anchorMax = new Vector2(0.5f, 0.5f);
            valueRect.pivot = new Vector2(0.5f, 0.5f);
            valueRect.anchoredPosition = new Vector2(230f, 0f);
            valueRect.sizeDelta = new Vector2(112f, 32f);

            Image valueImage = valueObject.GetComponent<Image>();
            valueImage.color = toggleState
                ? EnabledValueColor
                : ButtonColor;
            valueImage.raycastTarget = true;

            Outline valueOutline = valueObject.GetComponent<Outline>();
            valueOutline.effectColor = BorderColor;
            valueOutline.effectDistance = new Vector2(1f, 1f);
            valueOutline.useGraphicAlpha = true;

            Button valueButton = valueObject.GetComponent<Button>();
            valueButton.targetGraphic = valueImage;
            valueButton.transition = Selectable.Transition.ColorTint;
            valueButton.colors = CreateButtonColors();

            TMP_Text valueLabel = CreateLabel(
                $"SettingsToggleLabel_{categoryIndex}_{rowIndex}",
                valueObject.transform,
                toggleState ? "ON" : "OFF",
                14f,
                TextColor,
                Vector2.zero,
                new Vector2(102f, 28f),
                TextAlignmentOptions.Center
            );

            valueButton.onClick.AddListener(() =>
            {
                bool next = !toggleStates[stateKey];
                if (!TryStoreSetting(ReduceMotionKey, next ? "1" : "0")) return;
                toggleStates[stateKey] = next;
                valueLabel.text = next ? "ON" : "OFF";
                valueImage.color = next ? EnabledValueColor : ButtonColor;
            });
        }
        else
        {
            GameObject valueObject = CreateSettingsBox(
                $"SettingsValue_{categoryIndex}_{rowIndex}",
                rowObject.transform,
                new Vector2(230f, 0f),
                new Vector2(130f, 32f),
                ButtonColor,
                new Color(0.32f, 0.18f, 0.50f, 0.85f),
                1f
            );

            CreateLabel(
                $"SettingsValueLabel_{categoryIndex}_{rowIndex}",
                valueObject.transform,
                GetSettingValue(categoryIndex, rowIndex),
                13f,
                MutedTextColor,
                Vector2.zero,
                new Vector2(120f, 28f),
                TextAlignmentOptions.Center
            );

            if (categoryIndex >= 2)
            {
                Button valueButton = valueObject.AddComponent<Button>();
                valueButton.targetGraphic = valueObject.GetComponent<Image>();
                valueButton.transition = Selectable.Transition.ColorTint;
                valueButton.colors = CreateButtonColors();
                TMP_Text valueLabel = valueObject.GetComponentInChildren<TMP_Text>();
                valueButton.onClick.AddListener(() =>
                {
                    if (TryCycleSetting(categoryIndex, rowIndex))
                        valueLabel.text = GetSettingValue(categoryIndex, rowIndex);
                });
            }
        }
    }

    private string GetSettingValue(int categoryIndex, int rowIndex)
    {
        if (categoryIndex == 0)
            return rowIndex == 0 ? "AFTER NODE" : "ANALYZE ONLY";
        if (categoryIndex == 2 && rowIndex == 0)
            return selectedDisplayMode == FullScreenMode.Windowed
                ? "WINDOWED" : "BORDERLESS";
        if (categoryIndex == 2)
        {
            string[] names = QualitySettings.names;
            int level = QualitySettings.GetQualityLevel();
            return level >= 0 && level < names.Length
                ? names[level].ToUpperInvariant() : "DEFAULT";
        }
        return Mathf.RoundToInt(masterVolume * 100f) + "%";
    }

    private bool TryCycleSetting(int categoryIndex, int rowIndex)
    {
        if (categoryIndex == 2 && rowIndex == 0)
        {
            bool windowed = selectedDisplayMode == FullScreenMode.Windowed;
            if (!TryStoreSetting(DisplayModeKey, windowed ? "borderless" : "windowed"))
                return false;
            selectedDisplayMode = windowed
                ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.fullScreenMode = selectedDisplayMode;
            return true;
        }
        if (categoryIndex == 2 && rowIndex == 1)
        {
            int count = QualitySettings.names.Length;
            if (count == 0) return false;
            int next = (QualitySettings.GetQualityLevel() + 1) % count;
            if (!TryStoreSetting(QualityKey, next.ToString(CultureInfo.InvariantCulture)))
                return false;
            QualitySettings.SetQualityLevel(next, true);
            return true;
        }
        if (categoryIndex == 3 && rowIndex == 0)
        {
            int nextStep = (Mathf.RoundToInt(masterVolume * 4f) + 4) % 5;
            float next = nextStep * 0.25f;
            if (!TryStoreSetting(MasterVolumeKey,
                    next.ToString("0.00", CultureInfo.InvariantCulture)))
                return false;
            masterVolume = next;
            AudioListener.volume = masterVolume;
            return true;
        }
        return false;
    }

    private bool TryStoreSetting(string key, string value)
    {
        try
        {
            if (settingsStore == null)
                settingsStore = UserSettingsStore.CreateDefault();
            settingsStore.Write(key, value);
            if (saveStatusText != null) saveStatusText.text = "SETTING SAVED";
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Settings gagal disimpan: " + exception.Message);
            if (saveStatusText != null)
                saveStatusText.text = "SETTING COULD NOT BE SAVED";
            return false;
        }
    }

    private GameObject CreateSettingsBox(
        string objectName,
        Transform parent,
        Vector2 anchoredPosition,
        Vector2 size,
        Color fillColor,
        Color outlineColor,
        float outlineWidth
    )
    {
        GameObject boxObject = CreateUiObject(
            objectName,
            parent,
            typeof(Image),
            typeof(Outline)
        );

        RectTransform boxRect = boxObject.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = anchoredPosition;
        boxRect.sizeDelta = size;

        Image boxImage = boxObject.GetComponent<Image>();
        boxImage.color = fillColor;
        boxImage.raycastTarget = true;

        Outline boxOutline = boxObject.GetComponent<Outline>();
        boxOutline.effectColor = outlineColor;
        boxOutline.effectDistance = new Vector2(outlineWidth, outlineWidth);
        boxOutline.useGraphicAlpha = true;

        return boxObject;
    }

    public void ToggleSettings()
    {
        if (IsOpen)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }

    public void OpenSettings()
    {
        if (settingsPanel == null || IsOpen)
        {
            return;
        }

        if (IsBattleScene)
        {
            previousTimeScale = Time.timeScale;
            hasStoredTimeScale = true;
            Time.timeScale = 0f;
        }
        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();

        if (saveStatusText != null)
        {
            BattleManager battle = IsBattleScene
                ? Object.FindAnyObjectByType<BattleManager>() : null;
            saveStatusText.text = battle == null ? string.Empty :
                battle.CanSaveAtMapBoundary
                    ? "Current map progress will be saved before leaving."
                    : battle.HasFinishedRunSummary
                        ? "Run results will be checked before leaving."
                        : "Finish this node and reward before saving to Main Menu.";
        }

        // Keep the toggle control readable above the dimming backdrop. The
        // Resume button remains the primary close action, while the gear also
        // stays available as a predictable toggle for mouse and gamepad users.
        if (settingsButton != null && IsBattleScene)
        {
            settingsButton.gameObject.SetActive(true);
            settingsButton.transform.SetAsLastSibling();
        }

        if (EventSystem.current != null && closeButton != null)
        {
            EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel == null || !IsOpen)
        {
            return;
        }

        settingsPanel.SetActive(false);

        if (hasStoredTimeScale)
        {
            Time.timeScale = previousTimeScale;
            hasStoredTimeScale = false;
        }

        if (EventSystem.current != null && settingsButton != null &&
            settingsButton.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(settingsButton.gameObject);
        }
        else if (EventSystem.current != null)
        {
            Transform menuSettings = transform.Find("MenuCard/SettingsButton");
            if (menuSettings != null)
                EventSystem.current.SetSelectedGameObject(menuSettings.gameObject);
        }
    }

    private void SaveAndReturnToMainMenu()
    {
        if (returningToMenu || !IsBattleScene) return;
        BattleManager battle = Object.FindAnyObjectByType<BattleManager>();
        if (battle == null)
        {
            if (saveStatusText != null)
                saveStatusText.text = "RUN IS NOT READY. STAYING IN GAME.";
            return;
        }

        if (!battle.TrySaveForMainMenu(out string feedback))
        {
            if (saveStatusText != null) saveStatusText.text = feedback;
            return;
        }

        returningToMenu = true;
        CloseSettings();
        MainMenuFlow.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    private TMP_Text CreateLabel(
        string objectName,
        Transform parent,
        string text,
        float fontSize,
        Color color,
        Vector2 anchoredPosition,
        Vector2 size,
        TextAlignmentOptions alignment
    )
    {
        GameObject labelObject = CreateUiObject(
            objectName,
            parent,
            typeof(TextMeshProUGUI)
        );

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = anchoredPosition;
        labelRect.sizeDelta = size;

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

        if (defaultFont == null)
        {
            defaultFont = Resources.Load<TMP_FontAsset>(
                "Fonts & Materials/LiberationSans SDF"
            );
        }

        if (defaultFont != null)
        {
            label.font = defaultFont;
        }

        return label;
    }

    private static ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHighlightColor;
        colors.pressedColor = ButtonPressedColor;
        colors.selectedColor = ButtonHighlightColor;
        colors.disabledColor = new Color(
            ButtonColor.r,
            ButtonColor.g,
            ButtonColor.b,
            0.45f
        );
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.14f;
        return colors;
    }

    private GameObject CreateUiObject(
        string objectName,
        Transform parent,
        params System.Type[] componentTypes
    )
    {
        System.Type[] uiComponentTypes = new System.Type[componentTypes.Length + 1];
        uiComponentTypes[0] = typeof(RectTransform);
        System.Array.Copy(
            componentTypes,
            0,
            uiComponentTypes,
            1,
            componentTypes.Length
        );

        GameObject uiObject = new GameObject(objectName, uiComponentTypes);
        uiObject.layer = gameObject.layer;
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private void OnDestroy()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(ToggleSettings);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseSettings);
        }

        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveListener(SaveAndReturnToMainMenu);
        }

        if (hasStoredTimeScale)
        {
            Time.timeScale = previousTimeScale;
            hasStoredTimeScale = false;
        }
    }
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class GearIconGraphic : MaskableGraphic
{
    private const int ToothCount = 8;
    private const int SegmentsPerTooth = 4;

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = rectTransform.rect;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;

        if (radius <= 0f)
        {
            return;
        }

        float baseRadius = radius * 0.70f;
        float tipRadius = radius * 0.94f;
        float innerRadius = radius * 0.43f;
        int segmentCount = ToothCount * SegmentsPerTooth;

        for (int index = 0; index < segmentCount; index++)
        {
            float startAngle =
                (index / (float)segmentCount) * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float endAngle =
                ((index + 1) / (float)segmentCount) * Mathf.PI * 2f -
                Mathf.PI * 0.5f;

            float startRadius = IsToothTip(index) ? tipRadius : baseRadius;
            float endRadius = IsToothTip((index + 1) % segmentCount)
                ? tipRadius
                : baseRadius;

            Vector2 outerStart = PointOnCircle(startRadius, startAngle);
            Vector2 outerEnd = PointOnCircle(endRadius, endAngle);
            Vector2 innerStart = PointOnCircle(innerRadius, startAngle);
            Vector2 innerEnd = PointOnCircle(innerRadius, endAngle);

            int vertexIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, outerStart);
            AddVertex(vertexHelper, outerEnd);
            AddVertex(vertexHelper, innerEnd);
            AddVertex(vertexHelper, innerStart);
            vertexHelper.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vertexHelper.AddTriangle(vertexIndex, vertexIndex + 2, vertexIndex + 3);
        }
    }

    private void AddVertex(VertexHelper vertexHelper, Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        vertex.uv0 = Vector2.zero;
        vertexHelper.AddVert(vertex);
    }

    private static bool IsToothTip(int segmentIndex)
    {
        int phase = segmentIndex % SegmentsPerTooth;
        return phase == 1 || phase == 2;
    }

    private static Vector2 PointOnCircle(float radius, float angle)
    {
        return new Vector2(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius
        );
    }
}
