using System.Collections;
using UnityEngine;

public class MonsterHitReaction : MonoBehaviour
{
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float shakeDistance = 0.18f;
    [SerializeField] private float squashAmount = 0.2f;

    private Coroutine reactionCoroutine;
    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;

    public void PlayHitReaction()
    {
        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);

            transform.localPosition = startLocalPosition;
            transform.localScale = startLocalScale;
        }

        startLocalPosition = transform.localPosition;
        startLocalScale = transform.localScale;

        reactionCoroutine = StartCoroutine(HitReaction());
    }

    private IEnumerator HitReaction()
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / duration
            );

            float shake =
                Mathf.Sin(progress * Mathf.PI * 4f)
                * (1f - progress)
                * shakeDistance;

            float squash =
                Mathf.Sin(progress * Mathf.PI)
                * squashAmount;

            transform.localPosition =
                startLocalPosition + transform.right * shake;

            transform.localScale = new Vector3(
                startLocalScale.x * (1f + squash),
                startLocalScale.y * (1f - squash),
                startLocalScale.z * (1f + squash)
            );

            yield return null;
        }

        transform.localPosition = startLocalPosition;
        transform.localScale = startLocalScale;
        reactionCoroutine = null;
    }
}