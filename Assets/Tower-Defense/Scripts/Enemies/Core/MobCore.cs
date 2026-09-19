using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Composition root for a mob. Owns references to the agent, health and the
/// three strategy modules (target selector / combat behavior / navigation).
///
/// MobCore itself holds no gameplay rules — it just wires modules together and
/// runs a single throttled update loop for target re-evaluation + destination
/// updates. Combat tick runs every frame so individual behaviors can manage
/// their own cooldowns.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MobHealth))]
public class MobCore : MonoBehaviour
{
    [Header("Awareness")]
    [Tooltip("Radius in meters used by target selectors to find candidates.")]
    [SerializeField] private float awarenessRadius = 30f;
    [Tooltip("Target / destination refresh interval in seconds. Keep >= 0.5 to stay within perf budget.")]
    [SerializeField] private float refreshInterval = 0.5f;

    [Header("Modules (any MonoBehaviour implementing the matching interface)")]
    [SerializeField] private MonoBehaviour targetSelectorRef;
    [SerializeField] private MonoBehaviour combatBehaviorRef;
    [SerializeField] private MonoBehaviour navigationModifierRef;

    // Runtime
    public NavMeshAgent Agent   { get; private set; }
    public MobHealth    Health  { get; private set; }

    public ITargetSelector     TargetSelector     { get; private set; }
    public ICombatBehavior     CombatBehavior     { get; private set; }
    public INavigationModifier NavigationModifier { get; private set; }

    public float AwarenessRadius => awarenessRadius;

    public Damageable CurrentTarget { get; private set; }

    private float refreshTimer;
    private Collider currentTargetCollider;

    /// <summary>
    /// Returns the closest point on the target's collider, or its pivot if there is no collider.
    /// Use this for distance / destination / facing so we correctly handle large targets
    /// (like the Base) whose pivot may be far inside the geometry.
    /// </summary>
    public Vector3 GetTargetPoint(Damageable target)
    {
        if (target == null) return transform.position;
        if (currentTargetCollider != null && target == CurrentTarget)
            return currentTargetCollider.ClosestPoint(transform.position);

        var col = target.GetComponentInChildren<Collider>();
        if (col != null)
            return col.ClosestPoint(transform.position);
        return target.transform.position;
    }

    private void Awake()
    {
        Agent  = GetComponent<NavMeshAgent>();
        Health = GetComponent<MobHealth>();

        TargetSelector     = targetSelectorRef     as ITargetSelector;
        CombatBehavior     = combatBehaviorRef     as ICombatBehavior;
        NavigationModifier = navigationModifierRef as INavigationModifier;

        // Fallback: try to find modules on the same GameObject if fields were left empty.
        if (TargetSelector == null)     TargetSelector     = GetComponent<ITargetSelector>();
        if (CombatBehavior == null)     CombatBehavior     = GetComponent<ICombatBehavior>();
        if (NavigationModifier == null) NavigationModifier = GetComponent<INavigationModifier>();

        if (TargetSelector == null)     Debug.LogError($"[MobCore] No ITargetSelector on {name}");
        if (CombatBehavior == null)     Debug.LogError($"[MobCore] No ICombatBehavior on {name}");
        if (NavigationModifier == null) Debug.LogError($"[MobCore] No INavigationModifier on {name}");
    }

    private void OnEnable()
    {
        if (Health != null)
            Health.OnDeathHandled += HandleDeath;

        // Sync stopping distance with the current combat behavior.
        if (CombatBehavior != null && Agent != null)
            Agent.stoppingDistance = CombatBehavior.AttackRange * 0.9f;

        // Force an immediate refresh on spawn.
        refreshTimer = 0f;
        CurrentTarget = null;
    }

    private void OnDisable()
    {
        if (Health != null)
            Health.OnDeathHandled -= HandleDeath;
    }

    private void Update()
    {
        // Throttled target + destination refresh
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            RefreshTarget();
        }

        // Per-frame combat tick (behavior handles cooldowns internally)
        if (CurrentTarget != null && CurrentTarget.IsAlive && CombatBehavior != null)
        {
            Vector3 targetPoint = GetTargetPoint(CurrentTarget);
            float dist = Vector3.Distance(transform.position, targetPoint);
            if (dist <= CombatBehavior.AttackRange)
            {
                FaceTarget(targetPoint);
                CombatBehavior.Tick(this, CurrentTarget);
            }
        }
    }

    private void RefreshTarget()
    {
        if (TargetSelector == null) return;

        Damageable newTarget = TargetSelector.GetTarget(this);

        // Drop dead targets
        if (newTarget != null && !newTarget.IsAlive)
            newTarget = null;

        if (newTarget != CurrentTarget)
        {
            CurrentTarget = newTarget;
            currentTargetCollider = newTarget != null ? newTarget.GetComponentInChildren<Collider>() : null;
            CombatBehavior?.OnTargetChanged(this, newTarget);
        }

        NavigationModifier?.UpdateDestination(this, CurrentTarget);
    }

    private void FaceTarget(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
    }

    private void HandleDeath(MobHealth _)
    {
        // Spawner/pool will observe this via MobHealth.OnDeathHandled too.
        // Core just stops acting; actual despawn is handled externally so pooling
        // stays decoupled from gameplay.
        if (Agent != null && Agent.isOnNavMesh)
            Agent.isStopped = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, awarenessRadius);

        if (CombatBehavior != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, CombatBehavior.AttackRange);
        }
    }
}
