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

    private MobCore mob;
    private NavMeshAgent agent;

    private void Awake()
    {
        mob = GetComponent<MobCore>();
        agent = GetComponent<NavMeshAgent>();
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

        Vector3 targetPoint = mob.GetTargetPoint(mob.CurrentTarget) + Vector3.up * hoverHeight;
        Vector3 toTarget    = targetPoint - transform.position;
        float   stop        = mob.CombatBehavior != null ? mob.CombatBehavior.AttackRange * 0.9f : 1f;

        // Move only while outside attack range; let the combat behavior do its thing once close.
        if (toTarget.sqrMagnitude > stop * stop)
        {
            Vector3 dir = toTarget.normalized;
            transform.position += dir * speed * Time.deltaTime;

            Vector3 flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(flat.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void DisableNavMeshAgent()
    {
        if (agent == null)
            return;

        agent.enabled = false;
    }
}
