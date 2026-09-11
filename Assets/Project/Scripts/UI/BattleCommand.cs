using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleCommandMenu : MonoBehaviour
{
    [Header("Command Buttons")]
    [SerializeField] private Button basicAttackButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button guardButton;

    [Header("Entrance Animation")]
    [SerializeField] private float entranceOffset = 140f;
    [SerializeField] private float entranceDuration = 0.25f;
    [SerializeField] private float staggerDelay = 0.08f;

    [Header("Exit Animation")]
    [SerializeField] private float exitOffset = 80f;
    [SerializeField] private float exitDuration = 0.18f;

    [Header("Hover Animation")]
    [SerializeField] private float hoverScale = 1.12f;
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float hoverLift = 12f;
    [SerializeField] private float hoverTilt = 4f;
    [SerializeField] private float hoverDuration = 0.12f;

    private Button[] buttons;
    private RectTransform[] buttonRects;
    private CanvasGroup[] canvasGroups;

    private Vector2[] restingPositions;
    private Vector3[] restingScales;
    private Quaternion[] restingRotations;

    private Coroutine[] buttonTweens;
    private bool[] pointerInside;

    private Coroutine menuSequence;

    private bool wasInteractable;
    private bool menuTransitioning;
    private bool isInitialized;

    private void Awake()
    {
        buttons = new Button[]
        {
            basicAttackButton,
            skillButton,
            guardButton
        };

        int buttonCount = buttons.Length;

        buttonRects = new RectTransform[buttonCount];
        canvasGroups = new CanvasGroup[buttonCount];

        restingPositions = new Vector2[buttonCount];
        restingScales = new Vector3[buttonCount];
        restingRotations = new Quaternion[buttonCount];

        buttonTweens = new Coroutine[buttonCount];
        pointerInside = new bool[buttonCount];

        for (int index = 0; index < buttonCount; index++)
        {
            Button currentButton = buttons[index];

            if (currentButton == null)
            {
                Debug.LogError(
                    $"Command Button index {index} belum diisi."
                );

                continue;
            }

            buttonRects[index] =
                currentButton.GetComponent<RectTransform>();

            canvasGroups[index] =
                currentButton.GetComponent<CanvasGroup>();

            if (canvasGroups[index] == null)
            {
                canvasGroups[index] =
                    currentButton.gameObject.AddComponent<CanvasGroup>();
            }

            restingPositions[index] =
                buttonRects[index].anchoredPosition;

            restingScales[index] =
                buttonRects[index].localScale;

            restingRotations[index] =
                buttonRects[index].localRotation;

            int savedIndex = index;

            AddEventTrigger(
                currentButton.gameObject,
                EventTriggerType.PointerEnter,
                () => HandlePointerEnter(savedIndex)
            );

            AddEventTrigger(
                currentButton.gameObject,
                EventTriggerType.PointerExit,
                () => HandlePointerExit(savedIndex)
            );

            AddEventTrigger(
                currentButton.gameObject,
                EventTriggerType.PointerDown,
                () => HandlePointerDown(savedIndex)
            );

            AddEventTrigger(
                currentButton.gameObject,
                EventTriggerType.PointerUp,
                () => HandlePointerUp(savedIndex)
            );

            AddEventTrigger(
                currentButton.gameObject,
                EventTriggerType.Select,
                () => HandlePointerEnter(savedIndex)
            );

            AddEventTrigger(
                currentButton.gameObject,
                EventTriggerType.Deselect,
                () => HandlePointerExit(savedIndex)
            );
        }

        isInitialized = true;
    }

    private void Start()
    {
        wasInteractable = IsMenuInteractable();

        if (wasInteractable)
        {
            ShowMenuAnimated();
        }
        else
        {
            HideMenuImmediately();
        }
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        bool menuInteractable = IsMenuInteractable();

        if (menuInteractable && !wasInteractable)
        {
            ShowMenuAnimated();
        }
        else if (!menuInteractable && wasInteractable)
        {
            HideMenuAnimated();
        }

        wasInteractable = menuInteractable;
    }

    public void ShowMenuAnimated()
    {
        if (!isInitialized)
        {
            return;
        }

        StopMenuSequence();

        menuSequence = StartCoroutine(
            ShowMenuSequence()
        );
    }

    public void HideMenuAnimated()
    {
        if (!isInitialized)
        {
            return;
        }

        StopMenuSequence();

        menuSequence = StartCoroutine(
            HideMenuSequence()
        );
    }

    private IEnumerator ShowMenuSequence()
    {
        menuTransitioning = true;

        for (int index = 0; index < buttons.Length; index++)
        {
            if (buttonRects[index] == null)
            {
                continue;
            }

            pointerInside[index] = false;

            buttonRects[index].anchoredPosition =
                restingPositions[index] +
                Vector2.down * entranceOffset;

            buttonRects[index].localScale =
                restingScales[index] * 0.75f;

            buttonRects[index].localRotation =
                restingRotations[index];

            canvasGroups[index].alpha = 0f;
            canvasGroups[index].blocksRaycasts = false;
        }

        for (int index = 0; index < buttons.Length; index++)
        {
            if (buttonRects[index] == null)
            {
                continue;
            }

            canvasGroups[index].blocksRaycasts =
                buttons[index].interactable;

            StartButtonTween(
                index,
                restingPositions[index],
                restingScales[index],
                restingRotations[index],
                1f,
                entranceDuration
            );

            yield return new WaitForSecondsRealtime(
                staggerDelay
            );
        }

        yield return new WaitForSecondsRealtime(
            entranceDuration
        );

        menuTransitioning = false;
        menuSequence = null;
    }

    private IEnumerator HideMenuSequence()
    {
        menuTransitioning = true;

        for (int index = 0; index < buttons.Length; index++)
        {
            if (buttonRects[index] == null)
            {
                continue;
            }

            pointerInside[index] = false;
            canvasGroups[index].blocksRaycasts = false;

            Vector2 exitPosition =
                restingPositions[index] +
                Vector2.down * exitOffset;

            StartButtonTween(
                index,
                exitPosition,
                restingScales[index] * 0.8f,
                restingRotations[index],
                0f,
                exitDuration
            );
        }

        yield return new WaitForSecondsRealtime(
            exitDuration
        );

        menuTransitioning = false;
        menuSequence = null;
    }

    private void HideMenuImmediately()
    {
        for (int index = 0; index < buttons.Length; index++)
        {
            if (buttonRects[index] == null)
            {
                continue;
            }

            buttonRects[index].anchoredPosition =
                restingPositions[index] +
                Vector2.down * exitOffset;

            buttonRects[index].localScale =
                restingScales[index] * 0.8f;

            buttonRects[index].localRotation =
                restingRotations[index];

            canvasGroups[index].alpha = 0f;
            canvasGroups[index].blocksRaycasts = false;
        }
    }

    private void HandlePointerEnter(int index)
    {
        if (!CanAnimateButton(index))
        {
            return;
        }

        pointerInside[index] = true;

        Vector2 hoverPosition =
            restingPositions[index] +
            Vector2.up * hoverLift;

        float tiltDirection = index - 1f;

        Quaternion hoverRotation =
            restingRotations[index] *
            Quaternion.Euler(
                0f,
                0f,
                tiltDirection * hoverTilt
            );

        StartButtonTween(
            index,
            hoverPosition,
            restingScales[index] * hoverScale,
            hoverRotation,
            1f,
            hoverDuration
        );
    }

    private void HandlePointerExit(int index)
    {
        if (!IsValidButton(index))
        {
            return;
        }

        pointerInside[index] = false;

        if (menuTransitioning)
        {
            return;
        }

        StartButtonTween(
            index,
            restingPositions[index],
            restingScales[index],
            restingRotations[index],
            1f,
            hoverDuration
        );
    }

    private void HandlePointerDown(int index)
    {
        if (!CanAnimateButton(index))
        {
            return;
        }

        float tiltDirection = index - 1f;

        Quaternion pressedRotation =
            restingRotations[index] *
            Quaternion.Euler(
                0f,
                0f,
                tiltDirection * hoverTilt * 1.5f
            );

        StartButtonTween(
            index,
            restingPositions[index],
            restingScales[index] * pressedScale,
            pressedRotation,
            1f,
            hoverDuration * 0.5f
        );
    }

    private void HandlePointerUp(int index)
    {
        if (!CanAnimateButton(index))
        {
            return;
        }

        if (pointerInside[index])
        {
            HandlePointerEnter(index);
        }
        else
        {
            HandlePointerExit(index);
        }
    }

    private bool IsMenuInteractable()
    {
        for (int index = 0; index < buttons.Length; index++)
        {
            if (buttons[index] != null &&
                buttons[index].gameObject.activeInHierarchy &&
                buttons[index].interactable)
            {
                return true;
            }
        }

        return false;
    }

    private bool CanAnimateButton(int index)
    {
        if (menuTransitioning)
        {
            return false;
        }

        if (!IsValidButton(index))
        {
            return false;
        }

        return buttons[index].interactable;
    }

    private bool IsValidButton(int index)
    {
        return index >= 0 &&
               index < buttons.Length &&
               buttons[index] != null &&
               buttonRects[index] != null;
    }

    private void StartButtonTween(
        int index,
        Vector2 targetPosition,
        Vector3 targetScale,
        Quaternion targetRotation,
        float targetAlpha,
        float duration
    )
    {
        if (!IsValidButton(index))
        {
            return;
        }

        if (buttonTweens[index] != null)
        {
            StopCoroutine(buttonTweens[index]);
        }

        buttonTweens[index] = StartCoroutine(
            TweenButton(
                index,
                targetPosition,
                targetScale,
                targetRotation,
                targetAlpha,
                duration
            )
        );
    }

    private IEnumerator TweenButton(
        int index,
        Vector2 targetPosition,
        Vector3 targetScale,
        Quaternion targetRotation,
        float targetAlpha,
        float duration
    )
    {
        RectTransform targetRect = buttonRects[index];
        CanvasGroup targetCanvasGroup = canvasGroups[index];

        Vector2 startingPosition =
            targetRect.anchoredPosition;

        Vector3 startingScale =
            targetRect.localScale;

        Quaternion startingRotation =
            targetRect.localRotation;

        float startingAlpha =
            targetCanvasGroup.alpha;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            float easedProgress =
                1f - Mathf.Pow(1f - progress, 3f);

            targetRect.anchoredPosition =
                Vector2.LerpUnclamped(
                    startingPosition,
                    targetPosition,
                    easedProgress
                );

            targetRect.localScale =
                Vector3.LerpUnclamped(
                    startingScale,
                    targetScale,
                    easedProgress
                );

            targetRect.localRotation =
                Quaternion.LerpUnclamped(
                    startingRotation,
                    targetRotation,
                    easedProgress
                );

            targetCanvasGroup.alpha =
                Mathf.Lerp(
                    startingAlpha,
                    targetAlpha,
                    easedProgress
                );

            yield return null;
        }

        targetRect.anchoredPosition = targetPosition;
        targetRect.localScale = targetScale;
        targetRect.localRotation = targetRotation;
        targetCanvasGroup.alpha = targetAlpha;

        buttonTweens[index] = null;
    }

    private void AddEventTrigger(
        GameObject targetObject,
        EventTriggerType eventType,
        Action action
    )
    {
        EventTrigger eventTrigger =
            targetObject.GetComponent<EventTrigger>();

        if (eventTrigger == null)
        {
            eventTrigger =
                targetObject.AddComponent<EventTrigger>();
        }

        EventTrigger.Entry triggerEntry =
            new EventTrigger.Entry
            {
                eventID = eventType
            };

        triggerEntry.callback.AddListener(
            eventData => action.Invoke()
        );

        eventTrigger.triggers.Add(triggerEntry);
    }

    private void StopMenuSequence()
    {
        if (menuSequence == null)
        {
            return;
        }

        StopCoroutine(menuSequence);
        menuSequence = null;
    }
}