using UnityEngine;

/// <summary>
/// Drives the regular tower's firing animation from Shooter events.
/// TowerHealth owns the death bool; Shooter owns only combat logic.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Shooter))]
public class TowerAnimator : MonoBehaviour
{
    [SerializeField] private string shootTrigger = "Shoot";
    [SerializeField] private string fireRateMultiplierParam = "ShootSpeedMultiplier";

    private Animator animator;
    private Shooter shooter;
    private int shootHash;
    private int fireRateHash;
    private bool hasShootTrigger;
    private bool hasFireRateMultiplier;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        shooter = GetComponent<Shooter>();

        shootHash = Animator.StringToHash(shootTrigger);
        fireRateHash = Animator.StringToHash(fireRateMultiplierParam);

        CacheParameters();
    }

    private void OnEnable()
    {
        if (shooter != null)
            shooter.onFired.AddListener(OnFired);
    }

    private void OnDisable()
    {
        if (shooter != null)
            shooter.onFired.RemoveListener(OnFired);
    }

    private void OnFired()
    {
        if (animator == null || shooter == null)
            return;

        if (hasFireRateMultiplier)
            animator.SetFloat(fireRateHash, shooter.FireRate);

        if (hasShootTrigger)
            animator.SetTrigger(shootHash);
    }

    private void CacheParameters()
    {
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == shootTrigger)
                hasShootTrigger = true;
            else if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == fireRateMultiplierParam)
                hasFireRateMultiplier = true;
        }
    }
}
