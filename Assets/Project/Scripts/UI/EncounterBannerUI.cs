using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime cut-in; independent of combat state and third-party tween packages.</summary>
public sealed class EncounterBannerUI : MonoBehaviour
{
    public bool IsPlaying { get; private set; }
    private GameObject root;
    private int generation;

    public IEnumerator Play(string enemyName, bool boss, bool awakened = false)
    {
        Cancel();
        int version = generation;
        IsPlaying = true;
        root = new GameObject("EncounterCutIn", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        RectTransform cover = root.GetComponent<RectTransform>();
        cover.anchorMin = Vector2.zero; cover.anchorMax = Vector2.one;
        cover.offsetMin = cover.offsetMax = Vector2.zero;
        root.GetComponent<Image>().color = new Color(0.01f, 0, 0.025f, .78f);
        root.GetComponent<Image>().raycastTarget = true;
        var band = new GameObject("SlidingThreatBanner", typeof(RectTransform), typeof(Image), typeof(Outline));
        band.transform.SetParent(root.transform, false);
        RectTransform rect = band.GetComponent<RectTransform>();
        float width = Mathf.Min(1600f, ((RectTransform)transform).rect.width - 90f);
        rect.sizeDelta = new Vector2(width, 260f);
        rect.localRotation = Quaternion.Euler(0, 0, boss ? 2f : -2f);
        band.GetComponent<Image>().color = new Color(.075f, .015f, .12f, 1);
        Color accent = boss ? new Color(.95f, .19f, .34f) : new Color(.73f, .39f, 1);
        band.GetComponent<Outline>().effectColor = accent;
        band.GetComponent<Outline>().effectDistance = new Vector2(3, 3);
        for (int edge = -1; edge <= 1; edge += 2)
        {
            var streak = new GameObject("ThreatAccentRail", typeof(RectTransform), typeof(Image));
            streak.transform.SetParent(band.transform, false);
            var line = streak.GetComponent<Image>();
            line.color = new Color(accent.r, accent.g, accent.b, .5f); line.raycastTarget = false;
            line.rectTransform.sizeDelta = new Vector2(width * .94f, 5f);
            line.rectTransform.anchoredPosition = new Vector2(edge * 25f, edge * 146f);
        }
        var assets = Resources.Load<StandardRunMapVisualAssets>("RunMap/FracturedFate/StandardRunMapVisualAssets");
        if (assets != null)
        {
            var emblem = new GameObject("EncounterEmblem", typeof(RectTransform), typeof(Image));
            emblem.transform.SetParent(band.transform, false);
            var picture = emblem.GetComponent<Image>();
            picture.sprite = assets.GetNodeIcon(boss ? StandardRunNodeType.Boss : StandardRunNodeType.EliteBattle);
            picture.preserveAspect = true; picture.raycastTarget = false;
            picture.color = new Color(accent.r, accent.g, accent.b, .28f);
            picture.rectTransform.sizeDelta = new Vector2(230, 230);
            picture.rectTransform.anchoredPosition = new Vector2(-width / 2 + 130, 0);
        }
        Label(band.transform, awakened ? "THE ABYSS AWAKENS" : boss ? "BOSS APPROACHING" : "ELITE ENCOUNTER", 38, 72, width - 70, accent);
        Label(band.transform, enemyName.ToUpperInvariant(), 48, 0, width - 70, Color.white);
        Label(band.transform, awakened ? "PHASE II  /  THE PRESSURE RISES" : boss ? "YOUR FINAL TRIAL BEGINS" : "A DANGEROUS FOE BLOCKS YOUR PATH", 20, -76, width - 70, new Color(.83f, .75f, .9f));
        float distance = ((RectTransform)transform).rect.width + width;
        float direction = boss ? 1 : -1;
        float elapsed = 0;
        BattleSettingsUI settings = GetComponent<BattleSettingsUI>();
        bool motion = settings == null || settings.MotionEffectsEnabled;
        while (elapsed < 2.1f && generation == version)
        {
            // Scaled time pauses the cut-in with battle settings and honours speed.
            elapsed += Time.deltaTime;
            float x = elapsed < .45f ? Mathf.Lerp(direction * distance, 0, 1 - Mathf.Pow(1 - elapsed / .45f, 3)) :
                elapsed > 1.6f ? Mathf.Lerp(0, -direction * distance, Mathf.Pow((elapsed - 1.6f) / .5f, 3)) : 0;
            rect.anchoredPosition = new Vector2(motion ? x : 0, 0);
            yield return null;
        }
        if (generation == version) Cancel();
    }

    private static void Label(Transform parent, string value, float size, float y, float width, Color color)
    {
        var go = new GameObject("BannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = value; label.fontSize = size; label.color = color;
        if (size >= 38) label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = size;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta = new Vector2(width, 68);
        label.rectTransform.anchoredPosition = new Vector2(0, y);
    }

    public void Cancel()
    {
        generation++;
        if (root != null) { root.SetActive(false); Destroy(root); }
        root = null;
        IsPlaying = false;
    }

    private void OnDisable() => Cancel();
}
