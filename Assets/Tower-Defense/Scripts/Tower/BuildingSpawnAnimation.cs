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

    [Header("Shadow")]
    [Tooltip("Optional. If empty, the script searches children whose name contains 'shadow'.")]
    [SerializeField] private Transform shadowTransform;
    [Tooltip("Direct local scale values for the shadow during spawn. Leave empty to keep the shadow scale unchanged.")]
    [SerializeField] private Vector3[] shadowScaleSteps;
    [Tooltip("Keeps shadow at its original world position while the model rises and drops.")]
    [SerializeField] private bool keepShadowWorldPosition = true;

    private Coroutine routine;
    private Vector3 initialLocalScale;
    private Vector3 initialShadowLocalScale;
    private Vector3 initialShadowWorldPosition;
    private bool hasInitialLocalScale;
    private bool hasInitialShadowState;

    private void Awake()
    {
        CaptureInitialScale();
        ResolveShadow();
        CaptureInitialShadowState();
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
        ResolveShadow();
        CaptureInitialShadowState();

        Vector3 finalScale = GetFinalScale();
        Vector3 finalShadowScale = GetFinalShadowScale();
        Vector3 startPosition = finalPosition + Vector3.up * Mathf.Max(0f, startHeight);
        int steps = Mathf.Max(1, scaleSteps != null ? scaleSteps.Length : 0);

        transform.position = startPosition;
        transform.localScale = GetStepScale(0);
        SetShadowScale(GetShadowStepScale(0));
        KeepShadowInPlace();

        if (scaleStepDelay > 0f)
            yield return new WaitForSeconds(scaleStepDelay);

        for (int i = 1; i < steps; i++)
        {
            yield return ScaleTo(GetStepScale(i), GetShadowStepScale(i));

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
            KeepShadowInPlace();

            yield return null;
        }

        transform.position = finalPosition;
        transform.localScale = finalScale;
        SetShadowScale(finalShadowScale);
        KeepShadowInPlace();
        routine = null;
    }

    private void CaptureInitialScale()
    {
        if (hasInitialLocalScale)
            return;

        initialLocalScale = transform.localScale;
        hasInitialLocalScale = true;
    }

    private void ResolveShadow()
    {
        if (shadowTransform != null)
            return;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == transform)
                continue;

            if (child.name.ToLowerInvariant().Contains("shadow"))
            {
                shadowTransform = child;
                return;
            }
        }
    }

    private void CaptureInitialShadowState()
    {
        if (hasInitialShadowState || shadowTransform == null)
            return;

        initialShadowLocalScale = shadowTransform.localScale;
        initialShadowWorldPosition = shadowTransform.position;
        hasInitialShadowState = true;
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

    private Vector3 GetFinalShadowScale()
    {
        if (shadowScaleSteps == null || shadowScaleSteps.Length == 0)
            return initialShadowLocalScale;

        return shadowScaleSteps[shadowScaleSteps.Length - 1];
    }

    private Vector3 GetShadowStepScale(int index)
    {
        if (shadowScaleSteps == null || shadowScaleSteps.Length == 0)
            return initialShadowLocalScale;

        return shadowScaleSteps[Mathf.Clamp(index, 0, shadowScaleSteps.Length - 1)];
    }

    private IEnumerator ScaleTo(Vector3 targetScale, Vector3 targetShadowScale)
    {
        Vector3 from = transform.localScale;
        Vector3 shadowFrom = shadowTransform != null ? shadowTransform.localScale : Vector3.one;
        float duration = Mathf.Max(0.01f, scaleStepDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutCubic(progress);
            transform.localScale = Vector3.Lerp(from, targetScale, eased);
            SetShadowScale(Vector3.Lerp(shadowFrom, targetShadowScale, eased));
            KeepShadowInPlace();
            yield return null;
        }

        transform.localScale = targetScale;
        SetShadowScale(targetShadowScale);
        KeepShadowInPlace();
    }

    private void SetShadowScale(Vector3 scale)
    {
        if (shadowTransform != null && hasInitialShadowState)
            shadowTransform.localScale = scale;
    }

    private void KeepShadowInPlace()
    {
        if (shadowTransform != null && keepShadowWorldPosition && hasInitialShadowState)
            shadowTransform.position = initialShadowWorldPosition;
    }

    private static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
