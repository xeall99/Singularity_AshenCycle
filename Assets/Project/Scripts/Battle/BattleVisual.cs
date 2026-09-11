using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BattleVisuals : MonoBehaviour
{
    [Header("Battle Objects")]
    [SerializeField] private RectTransform playerVisual;
    [SerializeField] private RectTransform enemyVisual;
    [SerializeField] private Image playerImage;
    [SerializeField] private Image enemyImage;

    [Header("Animation Settings")]
    [SerializeField] private float lungeDistance = 80f;
    [SerializeField] private float lungeDuration = 0.12f;
    [SerializeField] private float flashDuration = 0.1f;

    public void PlayPlayerAttack()
    {
        StartCoroutine(
            LungeAnimation(playerVisual, Vector2.right)
        );
    }

    public void PlayEnemyAttack()
    {
        StartCoroutine(
            LungeAnimation(enemyVisual, Vector2.left)
        );
    }

    public void PlayEnemyHit()
    {
        StartCoroutine(FlashAnimation(enemyImage));
    }

    public void PlayPlayerHit()
    {
        StartCoroutine(FlashAnimation(playerImage));
    }

    public void PlayGuard()
    {
        StartCoroutine(GuardAnimation());
    }

    private IEnumerator LungeAnimation(
        RectTransform visual,
        Vector2 direction
    )
    {
        if (visual == null)
        {
            yield break;
        }

        Vector2 startingPosition = visual.anchoredPosition;

        Vector2 attackPosition =
            startingPosition + direction * lungeDistance;

        yield return MoveVisual(
            visual,
            startingPosition,
            attackPosition,
            lungeDuration
        );

        yield return MoveVisual(
            visual,
            attackPosition,
            startingPosition,
            lungeDuration
        );

        visual.anchoredPosition = startingPosition;
    }

    private IEnumerator MoveVisual(
        RectTransform visual,
        Vector2 startingPosition,
        Vector2 destination,
        float duration
    )
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            visual.anchoredPosition = Vector2.Lerp(
                startingPosition,
                destination,
                progress
            );

            yield return null;
        }

        visual.anchoredPosition = destination;
    }

    private IEnumerator FlashAnimation(Image targetImage)
    {
        if (targetImage == null)
        {
            yield break;
        }

        Color originalColor = targetImage.color;

        targetImage.color = Color.white;

        yield return new WaitForSeconds(flashDuration);

        targetImage.color = originalColor;
    }

    private IEnumerator GuardAnimation()
    {
        if (playerVisual == null || playerImage == null)
        {
            yield break;
        }

        Vector3 originalScale = playerVisual.localScale;
        Color originalColor = playerImage.color;

        playerVisual.localScale = originalScale * 1.12f;
        playerImage.color = new Color(0.25f, 1f, 0.7f);

        yield return new WaitForSeconds(0.25f);

        playerVisual.localScale = originalScale;
        playerImage.color = originalColor;
    }
}