using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Shooter))]
public sealed class CompanionTurret : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float followSpeed = 10f;
    [SerializeField, Min(0.1f)] private float sideOffset = 1.3f;
    [SerializeField, Min(0f)] private float forwardOffset = 0.25f;
    [SerializeField, Min(0f)] private float hoverHeight = 1.1f;
    [SerializeField] private Transform visualRoot;

    private PlayerHealth owner;
    private Shooter shooter;
    private int slot;
    private float expiresAt;
    private float displayDuration;

    public float RemainingSeconds => Mathf.Max(0f, expiresAt - Time.time);
    public float Progress => displayDuration > 0f
        ? Mathf.Clamp01(RemainingSeconds / displayDuration) : 0f;

    public void Initialize(PlayerHealth player, int slotIndex, float durationSeconds)
    {
        owner = player;
        shooter = GetComponent<Shooter>();
        slot = slotIndex;
        Refresh(durationSeconds);
        transform.position = FollowPosition();
    }

    public void Refresh(float durationSeconds)
    {
        float duration = Mathf.Max(0f, durationSeconds);
        if (Time.time + duration <= expiresAt) return;
        expiresAt = Time.time + duration;
        displayDuration = duration;
    }

    private void Update()
    {
        if (owner == null || !owner.IsAlive || Time.time >= expiresAt)
        {
            GetComponent<Shooter>().enabled = false;
            Destroy(gameObject);
            return;
        }

        float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, FollowPosition(), blend);
        if (visualRoot != null && shooter.CurrentTarget != null)
        {
            Vector3 direction = shooter.CurrentTarget.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation,
                    Quaternion.LookRotation(direction), 360f * Time.deltaTime);
        }
    }

    private Vector3 FollowPosition()
    {
        Vector3 right = owner.transform.right;
        right.y = 0f;
        right.Normalize();
        Vector3 forward = owner.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        return owner.transform.position + right * (slot == 0 ? -sideOffset : sideOffset)
            + forward * forwardOffset + Vector3.up * hoverHeight;
    }
}
