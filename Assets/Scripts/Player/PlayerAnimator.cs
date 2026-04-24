using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the player's Animator shoot states.
///
/// Animator int parameter "shootType":
///   0 = idle
///   1 = normal shot (hooked to Shooter.onFired)
///   2 = ultimate shot (call TriggerUltimate())
///
/// After each shot we hold the value for <see cref="shootHoldTime"/> seconds,
/// then reset to 0 so the animator returns to idle.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Shooter shooter;

    [Header("Animator")]
    private string shootTypeParam = "ShootType";
    [Tooltip("How long we keep shootType != 0 after a shot (seconds). Should roughly match the shoot animation length.")]
    [SerializeField] private float shootHoldTime = 0.25f;

    private int shootTypeHash;
    private Coroutine resetRoutine;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (shooter  == null) shooter  = GetComponentInChildren<Shooter>();
        shootTypeHash = Animator.StringToHash(shootTypeParam);
    }

    private void OnEnable()
    {
        if (shooter != null)
            shooter.onFired.AddListener(OnNormalFired);
    }

    private void OnDisable()
    {
        if (shooter != null)
            shooter.onFired.RemoveListener(OnNormalFired);
    }

    private void OnNormalFired() => PlayShoot(1);

    /// <summary>Call this from your ultimate ability trigger.</summary>
    public void TriggerUltimate() => PlayShoot(2);

    private void PlayShoot(int type)
    {
        if (animator == null) return;
        animator.SetInteger(shootTypeHash, type);

        if (resetRoutine != null) StopCoroutine(resetRoutine);
        resetRoutine = StartCoroutine(ResetAfter(shootHoldTime));
    }

    private IEnumerator ResetAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (animator != null)
            animator.SetInteger(shootTypeHash, 0);
        resetRoutine = null;
    }
}
