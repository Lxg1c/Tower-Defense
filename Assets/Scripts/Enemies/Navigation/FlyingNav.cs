using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// INavigationModifier for flying mobs. Moves the transform directly toward the
/// MobCore's CurrentTarget every frame, hovering at <see cref="hoverHeight"/>
/// above the target's collider point. No NavMesh needed.
///
/// UpdateDestination() is a no-op — flying motion happens in Update so it stays
/// smooth at frame-rate, not at MobCore's 0.5s refresh tick.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MobCore))]
public class FlyingNav : MonoBehaviour, INavigationModifier
{
    [SerializeField] private float speed         = 4f;
    [SerializeField] private float rotationSpeed = 8f;
    [Tooltip("Vertical offset above the target's hit point.")]
    [SerializeField] private float hoverHeight   = 5f;
    [Header("Separation")]
    [SerializeField] private float separationRadius = 1.25f;
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private LayerMask separationMask = ~0;
    [SerializeField] private int maxSeparationCandidates = 12;

    private MobCore mob;
    private NavMeshAgent agent;
    private Collider[] separationBuffer;

    private void Awake()
    {
        mob = GetComponent<MobCore>();
        agent = GetComponent<NavMeshAgent>();
        separationBuffer = new Collider[Mathf.Max(1, maxSeparationCandidates)];
        DisableNavMeshAgent();
    }

    private void OnEnable()
    {
        DisableNavMeshAgent();
    }

    public void UpdateDestination(MobCore _mob, Damageable _target)
    {
        // No-op: flying motion runs every frame in Update().
    }

    private void Update()
    {
        if (mob == null || mob.CurrentTarget == null) return;
        if (mob.Health != null && !mob.Health.IsAlive) return;

        Vector3 attackPoint = mob.GetTargetPoint(mob.CurrentTarget);
        Vector3 hoverPoint = attackPoint + Vector3.up * hoverHeight;
        Vector3 toHoverPoint = hoverPoint - transform.position;
        Vector3 toAttackPoint = attackPoint - transform.position;
        float   stop        = mob.CombatBehavior != null ? mob.CombatBehavior.AttackRange * 0.9f : 1f;

        // Move only while outside attack range; let the combat behavior do its thing once close.
        if (toAttackPoint.sqrMagnitude > stop * stop)
        {
            Vector3 dir = GetMoveDirection(toHoverPoint.normalized);
            transform.position += dir * speed * Time.deltaTime;

            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(flat.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private Vector3 GetMoveDirection(Vector3 targetDirection)
    {
        Vector3 separation = GetSeparationDirection();
        Vector3 combined = targetDirection + separation * Mathf.Max(0f, separationWeight);
        return combined.sqrMagnitude > 0.0001f ? combined.normalized : targetDirection;
    }

    private Vector3 GetSeparationDirection()
    {
        if (separationRadius <= 0f || separationWeight <= 0f)
            return Vector3.zero;

        if (separationBuffer == null || separationBuffer.Length != Mathf.Max(1, maxSeparationCandidates))
            separationBuffer = new Collider[Mathf.Max(1, maxSeparationCandidates)];

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            separationRadius,
            separationBuffer,
            separationMask,
            QueryTriggerInteraction.Collide);

        Vector3 separation = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Collider hit = separationBuffer[i];
            if (hit == null || hit.transform.IsChildOf(transform))
                continue;

            FlyingNav other = hit.GetComponentInParent<FlyingNav>();
            if (other == null || other == this)
                continue;

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float sqrDistance = away.sqrMagnitude;
            if (sqrDistance < 0.0001f)
                away = transform.right;
            else
                away /= sqrDistance;

            separation += away;
        }

        separation.y = 0f;
        return separation.sqrMagnitude > 0.0001f ? separation.normalized : Vector3.zero;
    }

    private void DisableNavMeshAgent()
    {
        if (agent == null)
            return;

        agent.enabled = false;
    }
}
