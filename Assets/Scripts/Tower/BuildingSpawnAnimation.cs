using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildingSpawnAnimation : MonoBehaviour
{
    [SerializeField] private float startHeight = 2.5f;
    [SerializeField] private float scaleStepDuration = 0.5f;
    [SerializeField] private float scaleStepDelay = 0.08f;
    [SerializeField] private float dropDuration = 0.5f;
    [SerializeField] private Vector3[] scaleSteps =
    {
        new(0.75f, 0.75f, 0.75f),
        new(1f, 1f, 1f),
        Vector3.one
    };

    private Coroutine routine;
    private Vector3 initialLocalScale;
    private bool hasInitialLocalScale;

    private void Awake()
    {
        CaptureInitialScale();
    }

    public void Play()
    {
        Play(transform.position);
    }

    public void Play(Vector3 finalPosition)
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(Animate(finalPosition));
    }

    private IEnumerator Animate(Vector3 finalPosition)
    {
        CaptureInitialScale();

        Vector3 finalScale = GetFinalScale();
        Vector3 startPosition = finalPosition + Vector3.up * Mathf.Max(0f, startHeight);
        int steps = Mathf.Max(1, scaleSteps != null ? scaleSteps.Length : 0);

        transform.position = startPosition;
        transform.localScale = GetStepScale(0);

        if (scaleStepDelay > 0f)
            yield return new WaitForSeconds(scaleStepDelay);

        for (int i = 1; i < steps; i++)
        {
            yield return ScaleTo(GetStepScale(i));

            if (scaleStepDelay > 0f)
                yield return new WaitForSeconds(scaleStepDelay);
        }

        float safeDropDuration = Mathf.Max(0.01f, dropDuration);
        float elapsed = 0f;
        while (elapsed < safeDropDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / safeDropDuration);

            transform.position = Vector3.Lerp(startPosition, finalPosition, EaseOutCubic(progress));

            yield return null;
        }

        transform.position = finalPosition;
        transform.localScale = finalScale;
        routine = null;
    }

    private void CaptureInitialScale()
    {
        if (hasInitialLocalScale)
            return;

        initialLocalScale = transform.localScale;
        hasInitialLocalScale = true;
    }

    private Vector3 GetFinalScale()
    {
        if (scaleSteps == null || scaleSteps.Length == 0)
            return initialLocalScale;

        return scaleSteps[scaleSteps.Length - 1];
    }

    private Vector3 GetStepScale(int index)
    {
        if (scaleSteps == null || scaleSteps.Length == 0)
            return initialLocalScale;

        return scaleSteps[Mathf.Clamp(index, 0, scaleSteps.Length - 1)];
    }

    private IEnumerator ScaleTo(Vector3 targetScale)
    {
        Vector3 from = transform.localScale;
        float duration = Mathf.Max(0.01f, scaleStepDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(from, targetScale, EaseOutCubic(progress));
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
