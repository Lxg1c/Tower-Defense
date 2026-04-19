using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(DetectionZone))]
public abstract class EnemyBehavior : MonoBehaviour
{
    [Header("Melee Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRate   = 1f;
    [SerializeField] private float attackRange  = 1.5f;

    /// <summary>EnemyMover reads this to set NavMeshAgent.stoppingDistance.</summary>
    public float AttackRange => attackRange;

    protected DetectionZone DetectionZone { get; private set; }
    protected Base          FinalTarget   { get; private set; }

    private float _attackCooldown;

    protected virtual void Awake()
    {
        DetectionZone = GetComponent<DetectionZone>();
    }

    public void SetFinalTarget(Base baseTarget)
    {
        FinalTarget = baseTarget;
    }

    /// <summary>
    /// Attack a target if it is within attackRange.
    /// DetectionZone is used only for FINDING targets, not for gating attacks.
    /// </summary>
    protected bool TryAttack(Damageable target)
    {
        if (target == null || !target.IsAlive)
            return false;

        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist > attackRange)
            return false;   // too far — keep walking

        _attackCooldown -= Time.deltaTime;
        if (_attackCooldown <= 0f)
        {
            target.TakeDamage(attackDamage);
            _attackCooldown = 1f / attackRate;
        }

        return true;
    }

    /// <summary>
    /// Returns the transform the enemy should navigate toward right now.
    /// Null = go to the final base.
    /// </summary>
    public abstract Transform GetOverrideTarget();
}
