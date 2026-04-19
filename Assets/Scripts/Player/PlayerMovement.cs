using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed     = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Joystick  joystick;
    [SerializeField] private Transform cam;

    [Header("Combat")]
    [SerializeField] private AutoShooter autoShooter;

    [Header("States")]
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("Speed multiplier applied during the Prep (between-waves) phase.")]
    [SerializeField] private float prepSpeedMultiplier  = 1.5f;
    [Tooltip("Speed multiplier while in ghost state (0 = can't move, 1 = normal).")]
    [SerializeField] private float ghostSpeedMultiplier = 1f;

    private Rigidbody rb;
    private Vector2   moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (joystick != null)
            moveInput = joystick.Direction;
    }

    private void FixedUpdate()
    {
        // ── Speed multipliers ────────────────────────────────────────────────
        float speedMult = 1f;

        if (playerHealth != null && playerHealth.IsGhost)
            speedMult = ghostSpeedMultiplier;
        else if (WaveManager.Instance != null &&
                 WaveManager.Instance.CurrentPhase == WaveManager.Phase.Prep)
            speedMult = prepSpeedMultiplier;

        // ── Direction relative to camera ─────────────────────────────────────
        Transform reference = cam != null ? cam : transform;
        Vector3 forward = reference.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = reference.right;   right.y   = 0f; right.Normalize();

        Vector3 move = right * moveInput.x + forward * moveInput.y;

        // ── Rotation ─────────────────────────────────────────────────────────
        if (autoShooter != null && autoShooter.HasTarget)
        {
            Quaternion targetRot = Quaternion.LookRotation(autoShooter.TargetDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
        else if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }

        // ── Apply velocity ────────────────────────────────────────────────────
        move   *= walkSpeed * speedMult;
        move.y  = rb.linearVelocity.y;
        rb.linearVelocity = move;
    }
}
