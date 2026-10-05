using UnityEngine;

[DisallowMultipleComponent]
public sealed class PickupHighlight : MonoBehaviour
{
    [SerializeField] private Transform sparkleRoot;
    [SerializeField] private Transform haloRoot;
    [SerializeField, Min(0f)] private float bobHeight = 0.12f;
    [SerializeField, Min(0f)] private float bobFrequency = 2.5f;
    [SerializeField, Min(0f)] private float sparkleSpeed = 100f;

    private Vector3 startPosition;
    private Vector3 startScale;
    private Vector3 haloScale;

    private void Awake()
    {
        if (sparkleRoot == null)
        {
            Debug.LogError("[PickupHighlight] Assign the sparkle visual root.", this);
            enabled = false;
            return;
        }
        startPosition = sparkleRoot.localPosition;
        startScale = sparkleRoot.localScale;
        if (haloRoot != null) haloScale = haloRoot.localScale;
    }

    private void Update()
    {
        float phase = Time.time * bobFrequency;
        sparkleRoot.localPosition = startPosition + Vector3.up * (Mathf.Sin(phase) * bobHeight);
        sparkleRoot.Rotate(Vector3.up, sparkleSpeed * Time.deltaTime, Space.Self);
        sparkleRoot.localScale = startScale * (1f + 0.12f * Mathf.Sin(phase * 2f));
        if (haloRoot != null)
            haloRoot.localScale = haloScale * (1f + 0.1f * Mathf.Sin(phase));
    }
}
