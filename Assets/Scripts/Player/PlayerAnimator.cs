using System.Collections;
using UnityEngine;

/// <summary>
/// Drives player-only animator states. Normal shooting and movement are states,
/// ultimate is a one-shot trigger.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private string movingParam = "isMoving";
    [SerializeField] private string attackingParam = "IsAttacking";
    [SerializeField] private string attackSpeedMultiplierParam = "ShootSpeedMultiplier";
    [SerializeField] private string ultimateTrigger = "UltimateShoot";
    [SerializeField] private string ultimateStateName = "robot_shoot_001";
    [SerializeField] private string ultimateLayerName = "Base Layer";
    [SerializeField] private float ultimateCrossFadeDuration = 0.05f;
    [SerializeField] private string legacyShootTypeParam = "ShootType";
    [SerializeField] private float legacyShootTypeResetDelay = 0.25f;

    private Animator  animator;
    private Shooter   shooter;
    private int       movingHash;
    private int       attackingHash;
    private int       attackSpeedMultiplierHash;
    private int       ultimateHash;
    private int       ultimateStateHash;
    private int       legacyShootTypeHash;
    private bool      isMoving;
    private bool      hasMovingParam;
    private bool      hasAttackingParam;
    private bool      hasAttackSpeedMultiplier;
    private bool      hasUltimateTrigger;
    private bool      hasLegacyShootType;
    private Coroutine legacyResetRoutine;

    private void Awake()
    {
        animator      = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        shooter       = GetComponentInChildren<Shooter>();
        movingHash    = Animator.StringToHash(movingParam);
        attackingHash = Animator.StringToHash(attackingParam);
        attackSpeedMultiplierHash = Animator.StringToHash(attackSpeedMultiplierParam);
        ultimateHash  = Animator.StringToHash(ultimateTrigger);
        ultimateStateHash = Animator.StringToHash($"{ultimateLayerName}.{ultimateStateName}");
        legacyShootTypeHash = Animator.StringToHash(legacyShootTypeParam);

        CacheParameters();
    }

    private void Update()
    {
        if (animator == null)
            return;

        bool isAttacking = shooter != null
            && shooter.enabled
            && shooter.HasTarget
            && !shooter.SuppressFire;

        if (hasMovingParam)
            animator.SetBool(movingHash, isMoving);
        if (hasAttackingParam)
            animator.SetBool(attackingHash, isAttacking);

        if (hasAttackSpeedMultiplier)
            animator.SetFloat(attackSpeedMultiplierHash, isAttacking ? shooter.FireRate : 1f);
    }

    public void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    /// <summary>Call this from your ultimate ability trigger.</summary>
    public void TriggerUltimate()
    {
        if (animator == null)
            return;

        if (hasUltimateTrigger)
        {
            animator.SetTrigger(ultimateHash);

            if (!string.IsNullOrEmpty(ultimateStateName) && animator.HasState(0, ultimateStateHash))
                animator.CrossFadeInFixedTime(ultimateStateHash, ultimateCrossFadeDuration);
        }
        else if (hasLegacyShootType)
        {
            animator.SetInteger(legacyShootTypeHash, 2);

            if (legacyResetRoutine != null)
                StopCoroutine(legacyResetRoutine);

            legacyResetRoutine = StartCoroutine(ResetLegacyShootType());
        }
    }

    private IEnumerator ResetLegacyShootType()
    {
        yield return new WaitForSeconds(legacyShootTypeResetDelay);

        if (animator != null)
            animator.SetInteger(legacyShootTypeHash, 0);

        legacyResetRoutine = null;
    }

    private void CacheParameters()
    {
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == movingParam)
                hasMovingParam = true;
            else if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == attackingParam)
                hasAttackingParam = true;
            else if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == attackSpeedMultiplierParam)
                hasAttackSpeedMultiplier = true;
            else if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == ultimateTrigger)
                hasUltimateTrigger = true;
            else if (parameter.type == AnimatorControllerParameterType.Int && parameter.name == legacyShootTypeParam)
                hasLegacyShootType = true;
        }
    }
}
