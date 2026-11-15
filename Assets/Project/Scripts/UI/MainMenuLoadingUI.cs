using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One-shot visual transition from the Main Menu to the real Battle scene.</summary>
[DisallowMultipleComponent]
public sealed class MainMenuLoadingUI : MonoBehaviour
{
    private static readonly Color Void = new Color(0.031f, 0.027f, 0.051f, 1f);
    private static readonly Color Ash = new Color(0.95f, 0.93f, 0.97f, 1f);
    private static readonly Color Violet = new Color(0.66f, 0.42f, 0.94f, 1f);
    private static readonly Color Muted = new Color(0.72f, 0.67f, 0.78f, 1f);

    private GameObject overlay;
    private CanvasGroup overlayGroup;
    private Image progressFill;
    private TMP_Text statusLabel;
    private TMP_Text hintLabel;
    private TMP_Text errorLabel;
    private GameObject errorActions;
    private Coroutine loadRoutine;
    private MainMenuIntent intent;
    private Action onCanceled;

    public void Begin(MainMenuIntent requestedIntent, Action canceled)
    {
        if (loadRoutine != null) return;
        intent = requestedIntent;
        onCanceled = canceled;
        if (overlay == null) Build();
        overlay.transform.SetAsLastSibling();
        overlay.SetActive(true);
        errorActions.SetActive(false);
        errorLabel.gameObject.SetActive(false);
        hintLabel.gameObject.SetActive(true);
        progressFill.fillAmount = 0f;
        statusLabel.text = "OPENING THE NEXT PATH";
        overlayGroup.alpha = 0f;
        loadRoutine = StartCoroutine(LoadBattle());
    }

    private void Build()
    {
        overlay = Object("MainMenuLoadingOverlay", transform, typeof(Image),
            typeof(CanvasGroup));
        Stretch(overlay.GetComponent<RectTransform>());
        Image baseImage = overlay.GetComponent<Image>();
        baseImage.color = Void;
        baseImage.raycastTarget = true;
        overlayGroup = overlay.GetComponent<CanvasGroup>();
        overlayGroup.blocksRaycasts = true;

        Sprite background = Resources.Load<Sprite>("UI/LoadingVoid");
        if (background != null)
        {
            GameObject plate = Object("LoadingBackgroundPlate", overlay.transform,
                typeof(Image), typeof(AspectRatioFitter));
            RectTransform rect = plate.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = plate.GetComponent<Image>();
            image.sprite = background;
            image.raycastTarget = false;
            AspectRatioFitter fitter = plate.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)background.rect.width / background.rect.height;
        }

        StandardRunMapVisualAssets mapArt = Resources.Load<StandardRunMapVisualAssets>(
            "RunMap/FracturedFate/StandardRunMapVisualAssets");
        if (mapArt != null && mapArt.CurrentMarker != null)
        {
            GameObject seal = Object("LoadingEyeSeal", overlay.transform, typeof(Image));
            Place(seal.GetComponent<RectTransform>(), Vector2.up * 176f,
                new Vector2(224f, 224f));
            Image image = seal.GetComponent<Image>();
            image.sprite = mapArt.CurrentMarker;
            image.preserveAspect = true;
            image.color = Ash;
            image.raycastTarget = false;
        }

        Label("LoadingTitle", "SYNGRAVA", new Vector2(0f, -10f),
            new Vector2(920f, 110f), 73f, Ash, true);
        Label("LoadingSubtitle", "THE ASHEN CYCLE", new Vector2(0f, -85f),
            new Vector2(740f, 40f), 21f, Violet, true);

        GameObject rail = Object("LoadingProgressRail", overlay.transform, typeof(Image));
        Place(rail.GetComponent<RectTransform>(), new Vector2(0f, -188f),
            new Vector2(580f, 8f));
        Image railImage = rail.GetComponent<Image>();
        railImage.color = new Color(0.22f, 0.16f, 0.30f, 1f);
        railImage.raycastTarget = false;

        GameObject fill = Object("LoadingProgressFill", rail.transform, typeof(Image));
        Stretch(fill.GetComponent<RectTransform>());
        progressFill = fill.GetComponent<Image>();
        progressFill.color = Violet;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
        progressFill.raycastTarget = false;

        statusLabel = Label("LoadingStatus", "OPENING THE NEXT PATH",
            new Vector2(0f, -231f), new Vector2(720f, 35f), 18f, Ash, false);
        hintLabel = Label("LoadingHint", "A FRACTURED ROUTE CAN STILL LEAD FORWARD",
            new Vector2(0f, -367f), new Vector2(840f, 32f), 16f, Muted, false);
        errorLabel = Label("LoadingError", string.Empty,
            new Vector2(0f, -282f), new Vector2(900f, 72f), 17f, Ash, false);

        errorActions = Object("LoadingErrorActions", overlay.transform,
            typeof(RectTransform));
        Place(errorActions.GetComponent<RectTransform>(), new Vector2(0f, -349f),
            new Vector2(470f, 58f));
        Button retry = CreateButton("LoadingRetryButton", "RETRY",
            errorActions.transform, new Vector2(-121f, 0f));
        retry.onClick.AddListener(Retry);
        Button back = CreateButton("LoadingBackButton", "BACK TO MENU",
            errorActions.transform, new Vector2(121f, 0f));
        back.onClick.AddListener(BackToMenu);
    }

    private IEnumerator LoadBattle()
    {
        float visibleTime = 0f;
        while (overlayGroup.alpha < 1f)
        {
            float delta = Time.unscaledDeltaTime;
            visibleTime += delta;
            overlayGroup.alpha = Mathf.Min(1f, overlayGroup.alpha + delta * 4f);
            yield return null;
        }

        if (!Application.CanStreamedLevelBeLoaded("Battle"))
        {
            Fail("BATTLE SCENE IS UNAVAILABLE. CHECK BUILD SETTINGS.");
            yield break;
        }

        AsyncOperation operation;
        try
        {
            operation = SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
        }
        catch (Exception error)
        {
            Fail("COULD NOT OPEN THE PATH: " + error.Message);
            yield break;
        }

        if (operation == null)
        {
            Fail("COULD NOT START BATTLE LOADING.");
            yield break;
        }

        operation.allowSceneActivation = false;
        while (operation.progress < 0.9f)
        {
            visibleTime += Time.unscaledDeltaTime;
            progressFill.fillAmount = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }

        while (visibleTime < 0.65f)
        {
            visibleTime += Time.unscaledDeltaTime;
            yield return null;
        }

        progressFill.fillAmount = 1f;
        statusLabel.text = "THE PATH IS OPEN";
        yield return new WaitForSecondsRealtime(0.15f);
        operation.allowSceneActivation = true;
        loadRoutine = null;
    }

    private void Fail(string message)
    {
        loadRoutine = null;
        MainMenuFlow.Clear();
        statusLabel.text = "THE PATH COULD NOT OPEN";
        errorLabel.text = message;
        errorLabel.gameObject.SetActive(true);
        hintLabel.gameObject.SetActive(false);
        errorActions.SetActive(true);
    }

    private void Retry()
    {
        if (loadRoutine != null) return;
        MainMenuFlow.PendingIntent = intent;
        Begin(intent, onCanceled);
    }

    private void BackToMenu()
    {
        if (loadRoutine != null) return;
        MainMenuFlow.Clear();
        overlay.SetActive(false);
        onCanceled?.Invoke();
    }

    private static Button CreateButton(string name, string text,
        Transform parent, Vector2 position)
    {
        GameObject obj = Object(name, parent, typeof(Image), typeof(Button));
        Place(obj.GetComponent<RectTransform>(), position, new Vector2(222f, 58f));
        Image image = obj.GetComponent<Image>();
        image.color = Color.white;
        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.15f, 0.09f, 0.22f, 1f);
        colors.highlightedColor = new Color(0.32f, 0.17f, 0.47f, 1f);
        colors.pressedColor = new Color(0.47f, 0.24f, 0.63f, 1f);
        button.colors = colors;
        Label(name + "Label", text, Vector2.zero, new Vector2(210f, 52f),
            17f, Ash, true, obj.transform);
        return button;
    }

    private TMP_Text Label(string name, string value, Vector2 position,
        Vector2 size, float fontSize, Color color, bool bold)
    {
        return Label(name, value, position, size, fontSize, color, bold,
            overlay.transform);
    }

    private static TMP_Text Label(string name, string value, Vector2 position,
        Vector2 size, float fontSize, Color color, bool bold, Transform parent)
    {
        GameObject obj = Object(name, parent, typeof(TextMeshProUGUI));
        Place(obj.GetComponent<RectTransform>(), position, size);
        TextMeshProUGUI label = obj.GetComponent<TextMeshProUGUI>();
        label.text = value;
        label.fontSize = fontSize;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }

    private static GameObject Object(string name, Transform parent, params Type[] components)
    {
        GameObject result = new GameObject(name, components);
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
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
