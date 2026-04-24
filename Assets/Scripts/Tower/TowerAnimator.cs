using UnityEngine;

/// <summary>
/// Drives the tower's Animator shoot state.
/// Animator bool parameter "IsShooting": true while the Shooter has a valid target, false otherwise.
/// </summary>
[DisallowMultipleComponent]
public class TowerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Shooter shooter;

    [Header("Animator")]
    [SerializeField] private string isShootingParam = "IsShooting";

    private int isShootingHash;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (shooter  == null) shooter  = GetComponentInChildren<Shooter>();
        isShootingHash = Animator.StringToHash(isShootingParam);
    }

    private void Update()
    {
        if (animator == null) return;
        bool shooting = shooter != null && shooter.HasTarget;
        animator.SetBool(isShootingHash, shooting);
    }

    private void OnDisable()
    {
        if (animator != null)
            animator.SetBool(isShootingHash, false);
    }
}
