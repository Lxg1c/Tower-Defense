using UnityEngine;

/// <summary>
/// Drives the PulseTower's attack animation.
/// Listens to <see cref="PulseTower.onAttack"/> and fires the trigger
/// once per pulse. (IsDead is owned by TowerHealth, not this script.)
/// </summary>
[DisallowMultipleComponent]
public class PulseTowerAnimator : MonoBehaviour
{
    [SerializeField] private string attackTrigger = "Attack";

    private Animator   animator;
    private PulseTower tower;
    private int        attackHash;
    private bool       hasAttack;

    private void Awake()
    {
        animator   = GetComponentInChildren<Animator>();
        tower      = GetComponent<PulseTower>();
        attackHash = Animator.StringToHash(attackTrigger);

        if (animator != null && !string.IsNullOrEmpty(attackTrigger))
        {
            foreach (var p in animator.parameters)
                if (p.type == AnimatorControllerParameterType.Trigger && p.name == attackTrigger)
                { hasAttack = true; break; }
        }
    }

    private void OnEnable()
    {
        if (tower != null) tower.onAttack.AddListener(OnAttack);
    }

    private void OnDisable()
    {
        if (tower != null) tower.onAttack.RemoveListener(OnAttack);
    }

    private void OnAttack()
    {
        if (animator != null && hasAttack)
            animator.SetTrigger(attackHash);
    }
}
