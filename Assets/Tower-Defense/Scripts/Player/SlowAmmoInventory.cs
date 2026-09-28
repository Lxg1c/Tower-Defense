using UnityEngine;

[DisallowMultipleComponent]
public sealed class SlowAmmoInventory : MonoBehaviour
{
    [Tooltip("Maximum accumulated bonus time in seconds.")]
    [SerializeField, Min(1)] private int capacity = 24;
    [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.5f;
    [SerializeField, Min(0f)] private float slowDuration = 2.5f;

    private float expiresAt;
    private float displayDuration;

    public float RemainingSeconds => Mathf.Max(0f, expiresAt - Time.time);
    public bool IsActive => RemainingSeconds > 0f;
    public float Progress => displayDuration > 0f ? Mathf.Clamp01(RemainingSeconds / displayDuration) : 0f;
    public float SlowMultiplier => slowMultiplier;
    public float SlowDuration => slowDuration;
    public float Add(float seconds)
    {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return 0f;
        float previous = RemainingSeconds;
        float next = Mathf.Min(capacity, previous + seconds);
        expiresAt = Time.time + next;
        displayDuration = next;
        return next - previous;
    }
}
