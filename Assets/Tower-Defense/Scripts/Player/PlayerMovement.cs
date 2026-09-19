using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed     = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Joystick  joystick;

    [Header("States")]
    [Tooltip("Speed multiplier while in ghost state (0 = can't move, 1 = normal).")]
    [SerializeField] private float ghostSpeedMultiplier = 1f;
    [Tooltip("Speed multiplier while between waves (Build phase). 1 = no bonus.")]
    [SerializeField] private float buildPhaseSpeedMultiplier = 1.5f;

    private Rigidbody     rb;
    private Vector2       moveInput;
    private Transform     cam;
    private PlayerHealth  playerHealth;
    private PlayerAnimator playerAnimator;
    private Shooter       shooter;
    private PlayerUltimate ultimate;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        playerHealth = GetComponent<PlayerHealth>();
        playerAnimator = GetComponent<PlayerAnimator>();
        shooter      = GetComponentInChildren<Shooter>();
        ultimate     = GetComponent<PlayerUltimate>();

        if (Camera.main != null)
            cam = Camera.main.transform;
    }

    private void Update()
    {
        if (joystick != null)
            moveInput = joystick.Direction;
    }

    private void FixedUpdate()
    {
        // ── Speed multiplier ─────────────────────────────────────────────────
        float speedMult = 1f;
        if (playerHealth != null && playerHealth.IsGhost)
            speedMult = ghostSpeedMultiplier;
        else if (WaveSpawner.Instance != null && WaveSpawner.Instance.IsBuildPhase)
            speedMult = buildPhaseSpeedMultiplier;

        // ── Direction relative to camera ─────────────────────────────────────
        Transform reference = cam != null ? cam : transform;
        Vector3 forward = reference.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = reference.right;   right.y   = 0f; right.Normalize();

        Vector3 move = right * moveInput.x + forward * moveInput.y;
        playerAnimator?.SetMoving(move.sqrMagnitude > 0.01f);

        // ── Rotation ─────────────────────────────────────────────────────────
        // Same path for normal attack and ultimate: Shooter tracks the nearest
        // target (it stays enabled during ult charge — only firing is suppressed).
        if (shooter != null && shooter.enabled && shooter.HasTarget)
        {
            Quaternion targetRot = Quaternion.LookRotation(shooter.TargetDirection);
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
