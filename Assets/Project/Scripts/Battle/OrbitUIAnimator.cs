using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OrbitUIAnimator : MonoBehaviour
{
    private static readonly Vector2[] AuthoredSlotCenters =
    {
        new Vector2(10f, 101f),
        new Vector2(11f, -2f),
        new Vector2(11f, -103f)
    };

    [Serializable]
    private sealed class SkillIconEntry
    {
        [SerializeField] private string skillCode;
        [SerializeField] private Sprite icon;

        public Sprite Icon => icon;

        public bool Matches(string requestedSkillCode)
        {
            return !string.IsNullOrWhiteSpace(skillCode) &&
                   !string.IsNullOrWhiteSpace(requestedSkillCode) &&
                   string.Equals(
                       skillCode.Trim(),
                       requestedSkillCode.Trim(),
                       StringComparison.OrdinalIgnoreCase
                   );
        }
    }

    [Header("Orbit References")]
    [SerializeField] private RectTransform orbitPanel;
    [SerializeField] private TMP_Text orbitSlot1Text;
    [SerializeField] private TMP_Text orbitSlot2Text;
    [SerializeField] private TMP_Text orbitSlot3Text;
    [SerializeField] private TMP_Text convergencePreviewText;

    [Header("Orbit Action Icons")]
    [SerializeField] private Image orbitSlot1Icon;
    [SerializeField] private Image orbitSlot2Icon;
    [SerializeField] private Image orbitSlot3Icon;
    [SerializeField] private Sprite attackIcon;
    [SerializeField] private Sprite skillFallbackIcon;
    [SerializeField] private Sprite guardIcon;
    [SerializeField] private SkillIconEntry[] skillIcons =
        Array.Empty<SkillIconEntry>();

    [Header("Slot Animation")]
    [SerializeField] private float slotAnimationDuration = 0.28f;
    [SerializeField, Range(0.1f, 1f)] private float slotStartingScale = 0.55f;
    [SerializeField] private float slotStartingRotation = 12f;

    [Header("Stage Entrance")]
    [SerializeField, Min(0.1f)] private float stageEntranceDuration = 0.85f;
    [SerializeField, Min(0f)] private float stageEntranceDistance = 440f;
    [SerializeField, Min(0f)] private float stageEntranceOvershoot = 14f;
    [SerializeField, Range(0f, 1f)] private float stageEntranceStartAlpha = 0f;

    [Header("Convergence Pulse")]
    [SerializeField] private float convergencePulseDuration = 0.42f;
    [SerializeField] private float convergencePulseScale = 1.08f;

    [Header("Orbit Colours")]
    [SerializeField] private Color emptyColour = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color attackColour = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color skillColour = new Color(0.76f, 0.35f, 1f, 1f);
    [SerializeField] private Color guardColour = new Color(0.53f, 0.58f, 1f, 1f);
    [SerializeField] private Color convergenceColour = new Color(0.88f, 0.58f, 1f, 1f);

    private TMP_Text[] slotTexts;
    private Image[] slotIcons;
    private CanvasGroup[] slotCanvasGroups;
    private Coroutine[] slotCoroutines;
    private string[] previousSlotValues;
    private string[] skillCodesBySlot;
    private PlayerActionType[] slotActionTypes;
    private bool[] slotHasActions;
    private Vector3[] originalSlotScales;
    private Quaternion[] originalSlotRotations;

    private Coroutine convergenceCoroutine;
    private Coroutine stageEntranceCoroutine;
    private CanvasGroup orbitPanelCanvasGroup;
    private Vector3 originalPanelScale;
    private Vector2 stageRestingPosition;
    private Color originalPreviewColour;
    private int previousFilledSlotCount;
    private bool detectionEnabled;

    private void Awake()
    {
        if (orbitPanel == null)
        {
            orbitPanel = transform as RectTransform;
        }

        slotTexts = new[]
        {
            orbitSlot1Text,
            orbitSlot2Text,
            orbitSlot3Text
        };

        slotIcons = new[]
        {
            orbitSlot1Icon,
            orbitSlot2Icon,
            orbitSlot3Icon
        };

        int slotCount = slotTexts.Length;
        slotCanvasGroups = new CanvasGroup[slotCount];
        slotCoroutines = new Coroutine[slotCount];
        previousSlotValues = new string[slotCount];
        skillCodesBySlot = new string[slotCount];
        slotActionTypes = new PlayerActionType[slotCount];
        slotHasActions = new bool[slotCount];
        originalSlotScales = new Vector3[slotCount];
        originalSlotRotations = new Quaternion[slotCount];

        PrepareLegacyTextState();
        PrepareIconState();

        if (orbitPanel != null)
        {
            originalPanelScale = orbitPanel.localScale;
            stageRestingPosition = orbitPanel.anchoredPosition;
            orbitPanelCanvasGroup = orbitPanel.GetComponent<CanvasGroup>();

            if (orbitPanelCanvasGroup == null)
            {
                orbitPanelCanvasGroup = orbitPanel.gameObject.AddComponent<CanvasGroup>();
            }

            orbitPanelCanvasGroup.alpha = 1f;
            orbitPanelCanvasGroup.interactable = false;
            orbitPanelCanvasGroup.blocksRaycasts = false;
        }

        if (convergencePreviewText != null)
        {
            originalPreviewColour = convergencePreviewText.color;
        }
    }

    private void PrepareLegacyTextState()
    {
        for (int i = 0; i < slotTexts.Length; i++)
        {
            TMP_Text slotText = slotTexts[i];

            if (slotText == null)
            {
                continue;
            }

            // BattleManager and existing Inspector references retain these values.
            // Rendering is delegated to the authored icons.
            slotText.raycastTarget = false;
            slotText.enabled = false;
        }
    }

    private void PrepareIconState()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            Image slotIcon = slotIcons[i];

            if (slotIcon == null)
            {
                continue;
            }

            slotIcon.raycastTarget = false;
            slotIcon.preserveAspect = true;
            slotIcon.transform.SetAsLastSibling();
            ApplyAuthoredSlotCenter(i, slotIcon);
            slotIcon.sprite = null;
            slotIcon.color = emptyColour;
            slotIcon.enabled = false;

            CanvasGroup canvasGroup = slotIcon.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = slotIcon.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            slotCanvasGroups[i] = canvasGroup;
            originalSlotScales[i] = slotIcon.rectTransform.localScale;
            originalSlotRotations[i] = slotIcon.rectTransform.localRotation;
        }
    }

    private IEnumerator Start()
    {
        // BattleManager writes its initial Orbit state during Start.
        yield return null;

        SynchronizeFromLegacyText(false);
        detectionEnabled = true;
    }

    private void Update()
    {
        if (!detectionEnabled)
        {
            return;
        }

        bool anySlotChanged = false;

        for (int i = 0; i < slotTexts.Length; i++)
        {
            TMP_Text slotText = slotTexts[i];

            if (slotText == null)
            {
                continue;
            }

            string currentValue = Normalise(slotText.text);

            if (currentValue == previousSlotValues[i])
            {
                continue;
            }

            previousSlotValues[i] = currentValue;

            if (TryParseAction(currentValue, out PlayerActionType actionType))
            {
                string skillCode = actionType == PlayerActionType.Skill
                    ? skillCodesBySlot[i]
                    : null;
                SetSlotVisual(i, actionType, skillCode, true);
            }
            else
            {
                ClearSlotVisual(i);
            }

            anySlotChanged = true;
        }

        if (!anySlotChanged)
        {
            RepairVisibleIcons();
            return;
        }

        int filledSlotCount = CountFilledSlots();

        if (filledSlotCount == slotIcons.Length &&
            previousFilledSlotCount < slotIcons.Length)
        {
            PlayConvergencePulse();
        }

        previousFilledSlotCount = filledSlotCount;
        RepairVisibleIcons();
    }

    public void SetSlotAction(
        int slotIndex,
        PlayerActionType actionType,
        string skillCode = null
    )
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        string actionLabel = GetActionLabel(actionType);

        if (slotTexts[slotIndex] != null)
        {
            slotTexts[slotIndex].text = actionLabel;
        }

        previousSlotValues[slotIndex] = actionLabel;
        SetSlotVisual(slotIndex, actionType, skillCode, true);
        previousFilledSlotCount = CountFilledSlots();
    }

    public void SynchronizeSlots(
        IReadOnlyList<PlayerActionType> actions,
        bool animateMissingIcons = false
    )
    {
        int actionCount = actions == null ? 0 : actions.Count;

        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (i >= actionCount)
            {
                if (slotTexts[i] != null)
                {
                    slotTexts[i].text = "—";
                }

                previousSlotValues[i] = "—";

                if (slotHasActions[i] ||
                    (slotIcons[i] != null &&
                     (slotIcons[i].enabled || slotIcons[i].sprite != null)))
                {
                    ClearSlotVisual(i);
                }

                continue;
            }

            PlayerActionType actionType = actions[i];
            string actionLabel = GetActionLabel(actionType);
            string skillCode = actionType == PlayerActionType.Skill
                ? skillCodesBySlot[i]
                : null;

            if (slotTexts[i] != null)
            {
                slotTexts[i].text = actionLabel;
            }

            previousSlotValues[i] = actionLabel;
            Image slotIcon = slotIcons[i];
            Sprite expectedIcon = ResolveActionIcon(actionType, skillCode);
            bool needsRepair = slotIcon == null ||
                               !slotHasActions[i] ||
                               slotActionTypes[i] != actionType ||
                               slotIcon.sprite != expectedIcon ||
                               !slotIcon.enabled ||
                               !slotIcon.gameObject.activeInHierarchy;

            if (needsRepair)
            {
                SetSlotVisual(
                    i,
                    actionType,
                    skillCode,
                    animateMissingIcons
                );
            }
        }

        previousFilledSlotCount = CountFilledSlots();
        RepairVisibleIcons();
    }

    public void ClearSlotIcons()
    {
        if (slotIcons == null)
        {
            return;
        }

        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (slotTexts[i] != null)
            {
                slotTexts[i].text = "—";
            }

            previousSlotValues[i] = "—";
            ClearSlotVisual(i);
        }

        previousFilledSlotCount = 0;
    }

    public void PlaySlotAnimation(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        Image slotIcon = slotIcons[slotIndex];

        if (slotIcon == null || slotIcon.sprite == null)
        {
            return;
        }

        if (slotCoroutines[slotIndex] != null)
        {
            StopCoroutine(slotCoroutines[slotIndex]);
        }

        ResetSlotTransform(slotIndex);
        slotCoroutines[slotIndex] = StartCoroutine(AnimateSlot(slotIndex));
    }

    public void PlayConvergencePulse()
    {
        if (orbitPanel == null)
        {
            return;
        }

        if (convergenceCoroutine != null)
        {
            StopCoroutine(convergenceCoroutine);
        }

        orbitPanel.localScale = originalPanelScale;
        convergenceCoroutine = StartCoroutine(AnimateConvergence());
    }

    public void PlayStageEntrance()
    {
        if (orbitPanel == null || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (stageEntranceCoroutine != null)
        {
            StopCoroutine(stageEntranceCoroutine);
        }

        ResetStageEntrancePresentation();
        stageEntranceCoroutine = StartCoroutine(AnimateStageEntrance());
    }

    private void SynchronizeFromLegacyText(bool animate)
    {
        for (int i = 0; i < slotTexts.Length; i++)
        {
            string value = slotTexts[i] == null
                ? string.Empty
                : Normalise(slotTexts[i].text);
            previousSlotValues[i] = value;

            if (TryParseAction(value, out PlayerActionType actionType))
            {
                SetSlotVisual(
                    i,
                    actionType,
                    actionType == PlayerActionType.Skill
                        ? skillCodesBySlot[i]
                        : null,
                    animate
                );
            }
            else
            {
                ClearSlotVisual(i);
            }
        }

        previousFilledSlotCount = CountFilledSlots();
    }

    private void SetSlotVisual(
        int slotIndex,
        PlayerActionType actionType,
        string skillCode,
        bool animate
    )
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        Image slotIcon = slotIcons[slotIndex];

        if (slotIcon == null)
        {
            return;
        }

        skillCodesBySlot[slotIndex] = actionType == PlayerActionType.Skill
            ? skillCode ?? string.Empty
            : string.Empty;
        slotActionTypes[slotIndex] = actionType;
        slotHasActions[slotIndex] = true;
        slotIcon.gameObject.SetActive(true);
        slotIcon.transform.SetAsLastSibling();
        slotIcon.sprite = ResolveActionIcon(actionType, skillCode);
        slotIcon.enabled = slotIcon.sprite != null;

        CanvasGroup canvasGroup = slotCanvasGroups[slotIndex];

        if (slotIcon.sprite == null)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            return;
        }

        slotIcon.color = animate
            ? GetActionColour(actionType)
            : Color.white;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = animate ? 0.1f : 1f;
        }

        if (animate)
        {
            PlaySlotAnimation(slotIndex);
        }
        else
        {
            ResetSlotTransform(slotIndex);
        }
    }

    private void ClearSlotVisual(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        if (slotCoroutines[slotIndex] != null)
        {
            StopCoroutine(slotCoroutines[slotIndex]);
            slotCoroutines[slotIndex] = null;
        }

        slotHasActions[slotIndex] = false;
        skillCodesBySlot[slotIndex] = string.Empty;
        Image slotIcon = slotIcons[slotIndex];

        if (slotIcon != null)
        {
            slotIcon.sprite = null;
            slotIcon.color = emptyColour;
            slotIcon.enabled = false;
        }

        if (slotCanvasGroups[slotIndex] != null)
        {
            slotCanvasGroups[slotIndex].alpha = 0f;
        }

        ResetSlotTransform(slotIndex);
    }

    private Sprite ResolveActionIcon(
        PlayerActionType actionType,
        string skillCode
    )
    {
        switch (actionType)
        {
            case PlayerActionType.Attack:
                return attackIcon;

            case PlayerActionType.Skill:
                return ResolveSkillIcon(skillCode);

            case PlayerActionType.Guard:
                return guardIcon;

            default:
                return null;
        }
    }

    private Sprite ResolveSkillIcon(string skillCode)
    {
        if (skillIcons != null)
        {
            for (int i = 0; i < skillIcons.Length; i++)
            {
                SkillIconEntry entry = skillIcons[i];

                if (entry != null &&
                    entry.Icon != null &&
                    entry.Matches(skillCode))
                {
                    return entry.Icon;
                }
            }
        }

        return skillFallbackIcon;
    }

    private IEnumerator AnimateSlot(int slotIndex)
    {
        Image slotIcon = slotIcons[slotIndex];
        RectTransform slotTransform = slotIcon.rectTransform;
        CanvasGroup canvasGroup = slotCanvasGroups[slotIndex];
        float rotationDirection = slotIndex % 2 == 0 ? -1f : 1f;
        Vector3 startScale =
            originalSlotScales[slotIndex] * slotStartingScale;
        Quaternion startRotation =
            originalSlotRotations[slotIndex] *
            Quaternion.Euler(
                0f,
                0f,
                slotStartingRotation * rotationDirection
            );
        Color startColour = GetActionColour(slotActionTypes[slotIndex]);

        slotTransform.localScale = startScale;
        slotTransform.localRotation = startRotation;
        slotIcon.color = startColour;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.1f;
        }

        float elapsedTime = 0f;

        while (elapsedTime < slotAnimationDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / slotAnimationDuration
            );
            float scaleProgress = EaseOutBack(progress);
            float smoothProgress = SmoothStep(progress);

            slotTransform.localScale = Vector3.LerpUnclamped(
                startScale,
                originalSlotScales[slotIndex],
                scaleProgress
            );
            slotTransform.localRotation = Quaternion.Slerp(
                startRotation,
                originalSlotRotations[slotIndex],
                smoothProgress
            );
            slotIcon.color = Color.Lerp(
                startColour,
                Color.white,
                smoothProgress
            );

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(
                    0.1f,
                    1f,
                    smoothProgress
                );
            }

            yield return null;
        }

        ResetSlotTransform(slotIndex);
        slotIcon.color = Color.white;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        slotCoroutines[slotIndex] = null;
    }

    private IEnumerator AnimateStageEntrance()
    {
        float duration = Mathf.Max(0.1f, stageEntranceDuration);
        const float travelFraction = 0.82f;
        Vector2 startPosition = stageRestingPosition -
                                Vector2.right * stageEntranceDistance;
        Vector2 overshootPosition = stageRestingPosition +
                                    Vector2.right * stageEntranceOvershoot;
        float elapsedTime = 0f;

        orbitPanel.anchoredPosition = startPosition;

        if (orbitPanelCanvasGroup != null)
        {
            orbitPanelCanvasGroup.alpha = stageEntranceStartAlpha;
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            if (progress < travelFraction)
            {
                float travelProgress = SmoothStep(progress / travelFraction);
                orbitPanel.anchoredPosition = Vector2.LerpUnclamped(
                    startPosition,
                    overshootPosition,
                    travelProgress
                );
            }
            else
            {
                float settleProgress = SmoothStep(
                    (progress - travelFraction) / (1f - travelFraction)
                );
                orbitPanel.anchoredPosition = Vector2.LerpUnclamped(
                    overshootPosition,
                    stageRestingPosition,
                    settleProgress
                );
            }

            if (orbitPanelCanvasGroup != null)
            {
                float fadeProgress = SmoothStep(
                    Mathf.Clamp01(progress / 0.62f)
                );
                orbitPanelCanvasGroup.alpha = Mathf.Lerp(
                    stageEntranceStartAlpha,
                    1f,
                    fadeProgress
                );
            }

            yield return null;
        }

        ResetStageEntrancePresentation();
        stageEntranceCoroutine = null;
    }

    private IEnumerator AnimateConvergence()
    {
        float elapsedTime = 0f;

        if (convergencePreviewText != null)
        {
            convergencePreviewText.color = convergenceColour;
        }

        while (elapsedTime < convergencePulseDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / convergencePulseDuration
            );
            float pulse = Mathf.Sin(progress * Mathf.PI);

            orbitPanel.localScale = originalPanelScale * Mathf.Lerp(
                1f,
                convergencePulseScale,
                pulse
            );

            yield return null;
        }

        orbitPanel.localScale = originalPanelScale;

        if (convergencePreviewText != null)
        {
            convergencePreviewText.color = originalPreviewColour;
        }

        convergenceCoroutine = null;
    }

    private int CountFilledSlots()
    {
        int filledSlotCount = 0;

        for (int i = 0; i < slotHasActions.Length; i++)
        {
            if (slotHasActions[i])
            {
                filledSlotCount++;
            }
        }

        return filledSlotCount;
    }

    private void RepairVisibleIcons()
    {
        if (slotIcons == null || slotHasActions == null)
        {
            return;
        }

        for (int i = 0; i < slotIcons.Length; i++)
        {
            if (!slotHasActions[i] || slotIcons[i] == null)
            {
                continue;
            }

            Image slotIcon = slotIcons[i];
            Sprite expectedIcon = ResolveActionIcon(
                slotActionTypes[i],
                skillCodesBySlot[i]
            );
            CanvasGroup canvasGroup = slotCanvasGroups[i];
            bool animationRunning = slotCoroutines[i] != null;
            bool hiddenAfterAnimation = !animationRunning &&
                                        canvasGroup != null &&
                                        canvasGroup.alpha <= 0.001f;

            if (slotIcon.sprite == expectedIcon &&
                slotIcon.enabled &&
                slotIcon.gameObject.activeInHierarchy &&
                !hiddenAfterAnimation)
            {
                continue;
            }

            SetSlotVisual(
                i,
                slotActionTypes[i],
                skillCodesBySlot[i],
                false
            );
        }
    }

    private Color GetActionColour(PlayerActionType actionType)
    {
        switch (actionType)
        {
            case PlayerActionType.Skill:
                return skillColour;

            case PlayerActionType.Guard:
                return guardColour;

            default:
                return attackColour;
        }
    }

    private static bool TryParseAction(
        string slotValue,
        out PlayerActionType actionType
    )
    {
        string normalisedValue = Normalise(slotValue);

        if (normalisedValue == "A" || normalisedValue.Contains("ATTACK"))
        {
            actionType = PlayerActionType.Attack;
            return true;
        }

        if (normalisedValue == "S" || normalisedValue.Contains("SKILL"))
        {
            actionType = PlayerActionType.Skill;
            return true;
        }

        if (normalisedValue == "G" || normalisedValue.Contains("GUARD"))
        {
            actionType = PlayerActionType.Guard;
            return true;
        }

        actionType = default;
        return false;
    }

    private static string GetActionLabel(PlayerActionType actionType)
    {
        switch (actionType)
        {
            case PlayerActionType.Attack:
                return "A";

            case PlayerActionType.Skill:
                return "S";

            case PlayerActionType.Guard:
                return "G";

            default:
                return string.Empty;
        }
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIcons != null &&
               slotIndex >= 0 &&
               slotIndex < slotIcons.Length;
    }

    private void ResetSlotTransform(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        Image slotIcon = slotIcons[slotIndex];

        if (slotIcon == null)
        {
            return;
        }

        slotIcon.rectTransform.localScale = originalSlotScales[slotIndex];
        slotIcon.rectTransform.localRotation = originalSlotRotations[slotIndex];
        ApplyAuthoredSlotCenter(slotIndex, slotIcon);
    }

    private static void ApplyAuthoredSlotCenter(
        int slotIndex,
        Image slotIcon
    )
    {
        if (slotIcon == null ||
            slotIndex < 0 ||
            slotIndex >= AuthoredSlotCenters.Length)
        {
            return;
        }

        slotIcon.rectTransform.anchoredPosition =
            AuthoredSlotCenters[slotIndex];
    }

    private static string Normalise(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    private static float SmoothStep(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.70158f;
        float shiftedValue = value - 1f;

        return 1f +
               (overshoot + 1f) * shiftedValue * shiftedValue * shiftedValue +
               overshoot * shiftedValue * shiftedValue;
    }

    private void ResetStageEntrancePresentation()
    {
        if (orbitPanel != null)
        {
            orbitPanel.anchoredPosition = stageRestingPosition;
        }

        if (orbitPanelCanvasGroup != null)
        {
            orbitPanelCanvasGroup.alpha = 1f;
        }
    }

    private void OnDisable()
    {
        if (orbitPanel != null)
        {
            orbitPanel.localScale = originalPanelScale;
        }

        ResetStageEntrancePresentation();
        stageEntranceCoroutine = null;

        if (slotIcons != null)
        {
            for (int i = 0; i < slotIcons.Length; i++)
            {
                ResetSlotTransform(i);
            }
        }

        if (convergencePreviewText != null)
        {
            convergencePreviewText.color = originalPreviewColour;
        }
    }
}
