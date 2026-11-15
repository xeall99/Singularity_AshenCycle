using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class RewardSelectionUI : MonoBehaviour
{
    private const float EntranceOffset = 760f;
    private const float EntranceDuration = 0.34f;
    private const float StaggerDelay = 0.09f;

    private readonly Vector2 cardSize = new Vector2(280f, 410f);
    private readonly Vector2[] cardPositions =
    {
        new Vector2(-320f, -25f),
        new Vector2(0f, -25f),
        new Vector2(320f, -25f)
    };

    private Button[] rewardButtons;
    private RewardCardView[] cardViews;
    private Coroutine entranceSequence;
    private bool initialized;
    private int visibleCardCount = 3;

    public bool IsInitialized => initialized;
    public bool IsAnimating { get; private set; }

    public void Initialize(
        TMP_Text titleText,
        Button firstButton,
        Button secondButton,
        Button thirdButton
    )
    {
        rewardButtons = new[]
        {
            firstButton,
            secondButton,
            thirdButton
        };

        if (titleText == null ||
            firstButton == null ||
            secondButton == null ||
            thirdButton == null)
        {
            Debug.LogError("RewardSelectionUI memerlukan title dan tiga reward button.");
            initialized = false;
            return;
        }

        Image dimLayer = GetComponent<Image>();

        if (dimLayer != null)
        {
            dimLayer.color = new Color(0f, 0f, 0f, 0.72f);
            dimLayer.raycastTarget = true;
        }

        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.anchoredPosition = new Vector2(0f, 335f);
        titleRect.sizeDelta = new Vector2(900f, 80f);
        titleText.fontSize = 38f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;

        cardViews = new RewardCardView[rewardButtons.Length];
        for (int index = 0; index < rewardButtons.Length; index++)
        {
            Button rewardButton = rewardButtons[index];
            TMP_Text label = FindCardLabel(rewardButton);

            if (label != null)
            {
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0.07f, 0.05f);
                labelRect.anchorMax = new Vector2(0.93f, 0.42f);
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                label.enableAutoSizing = true;
                label.fontSizeMin = 13f;
                label.fontSizeMax = 23f;
                label.alignment = TextAlignmentOptions.Center;
                label.richText = true;
                label.color = new Color(0.94f, 0.91f, 1f, 1f);
                label.raycastTarget = false;
                label.transform.SetAsLastSibling();
            }

            RewardCardView cardView = rewardButton.GetComponent<RewardCardView>();

            if (cardView == null)
            {
                cardView = rewardButton.gameObject.AddComponent<RewardCardView>();
            }

            cardView.Configure(
                GetTierAccent(1),
                "?",
                label,
                titleText.font
            );
            cardView.SetHighlightCoordinator(HandleHighlightRequested);
            cardView.SetRestingLayout(cardPositions[index], cardSize);
            cardView.ResetImmediate();
            cardViews[index] = cardView;
        }

        initialized = true;
    }

    public void SetCardText(int cardIndex, string value)
    {
        if (!initialized ||
            cardIndex < 0 ||
            cardIndex >= cardViews.Length)
        {
            return;
        }

        cardViews[cardIndex].SetLabel(value);
    }

    public void SetCardDefinition(
        int cardIndex,
        RewardDefinition reward,
        int currentStack
    )
    {
        if (!initialized ||
            reward == null ||
            cardIndex < 0 ||
            cardIndex >= cardViews.Length)
        {
            return;
        }

        Color accent = GetTierAccent(reward.Tier);
        string accentHex = ColorUtility.ToHtmlStringRGB(accent);
        bool relic = RunRelicCatalog.IsRelic(reward.EffectType);
        bool anomaly = RunAnomalyCatalog.IsAnomaly(reward.EffectType);
        string heading = relic
            ? "RELIC"
            : anomaly ? "ANOMALY" : $"TIER {ToRoman(reward.Tier)}";
        string footer = relic || anomaly
            ? "UNIQUE • ACTIVE RUN"
            : $"STACK {currentStack} → {currentStack + 1}";
        string cardText =
            $"<size=14><color=#{accentHex}>{heading}</color></size>\n" +
            $"<b>{reward.DisplayName}</b>\n" +
            $"<size=17>{reward.ExactEffectText}</size>\n" +
            $"<size=12>{reward.Description}</size>\n" +
            $"<size=14>{footer}</size>";
        if (relic)
            cardText = $"<size=12><color=#{accentHex}>RELIC</color></size>\n" +
                $"<size=18><b>{reward.DisplayName}</b></size>\n" +
                $"<size=14>{reward.ExactEffectText}</size>\n" +
                "<size=11>UNIQUE • ACTIVE RUN</size>";
        else if (anomaly)
            cardText = $"<size=12><color=#{accentHex}>ANOMALY</color></size>\n" +
                $"<size=18><b>{reward.DisplayName}</b></size>\n" +
                $"<size=14>{reward.ExactEffectText}</size>\n" +
                "<size=11>UNIQUE • ACTIVE RUN</size>";

        cardViews[cardIndex].SetPresentation(accent, reward.Glyph);
        cardViews[cardIndex].SetLabel(cardText);
    }

    public void SetVisibleCardCount(int count)
    {
        if (!initialized)
        {
            return;
        }

        visibleCardCount = Mathf.Clamp(count, 1, rewardButtons.Length);
        const float spacing = 320f;
        float start = -(visibleCardCount - 1) * spacing * 0.5f;

        for (int index = 0; index < rewardButtons.Length; index++)
        {
            bool visible = index < visibleCardCount;
            rewardButtons[index].gameObject.SetActive(visible);

            if (visible)
            {
                cardViews[index].SetRestingLayout(
                    new Vector2(start + index * spacing, -25f),
                    cardSize
                );
            }
        }
    }

    public void ShowAnimated()
    {
        if (!initialized)
        {
            return;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (entranceSequence != null)
        {
            StopCoroutine(entranceSequence);
        }

        for (int index = 0; index < visibleCardCount; index++)
        {
            cardViews[index].PrepareEntrance(EntranceOffset);
            cardViews[index].PlayEntrance(
                index * StaggerDelay,
                EntranceDuration
            );
        }

        entranceSequence = StartCoroutine(EnableCardsAfterEntrance());
    }

    public void LockSelection()
    {
        if (!initialized)
        {
            return;
        }

        for (int index = 0; index < visibleCardCount; index++)
        {
            cardViews[index].SetSelectionEnabled(false);
        }
    }

    public void HideImmediate()
    {
        if (entranceSequence != null)
        {
            StopCoroutine(entranceSequence);
            entranceSequence = null;
        }

        IsAnimating = false;

        if (cardViews != null)
        {
            for (int index = 0; index < cardViews.Length; index++)
            {
                cardViews[index]?.ResetImmediate();
            }
        }

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        gameObject.SetActive(false);
    }

    private IEnumerator EnableCardsAfterEntrance()
    {
        IsAnimating = true;

        yield return new WaitForSecondsRealtime(
            EntranceDuration + StaggerDelay * (visibleCardCount - 1)
        );

        for (int index = 0; index < visibleCardCount; index++)
        {
            cardViews[index].SetSelectionEnabled(true);
        }

        IsAnimating = false;
        entranceSequence = null;
    }

    private void HandleHighlightRequested(RewardCardView activeCard)
    {
        if (cardViews == null)
        {
            return;
        }

        for (int index = 0; index < cardViews.Length; index++)
        {
            RewardCardView cardView = cardViews[index];

            if (cardView != null && cardView != activeCard)
            {
                cardView.ClearHighlightImmediate();
            }
        }
    }

    private static TMP_Text FindCardLabel(Button rewardButton)
    {
        Transform portrait = rewardButton.transform.Find("RewardPortrait");
        TMP_Text[] candidates =
            rewardButton.GetComponentsInChildren<TMP_Text>(true);

        for (int index = 0; index < candidates.Length; index++)
        {
            TMP_Text candidate = candidates[index];

            if (portrait == null || !candidate.transform.IsChildOf(portrait))
            {
                return candidate;
            }
        }

        return null;
    }

    private static Color GetTierAccent(int tier)
    {
        float progress = Mathf.InverseLerp(1f, 10f, Mathf.Clamp(tier, 1, 10));
        Color foundation = new Color(0.55f, 0.37f, 0.94f, 1f);
        Color singularity = new Color(1f, 0.38f, 0.56f, 1f);
        return Color.Lerp(foundation, singularity, progress);
    }

    private static string ToRoman(int value)
    {
        switch (Mathf.Clamp(value, 1, 10))
        {
            case 1: return "I";
            case 2: return "II";
            case 3: return "III";
            case 4: return "IV";
            case 5: return "V";
            case 6: return "VI";
            case 7: return "VII";
            case 8: return "VIII";
            case 9: return "IX";
            default: return "X";
        }
    }

    private void OnDisable()
    {
        if (entranceSequence != null)
        {
            StopCoroutine(entranceSequence);
            entranceSequence = null;
        }

        IsAnimating = false;
    }
}
