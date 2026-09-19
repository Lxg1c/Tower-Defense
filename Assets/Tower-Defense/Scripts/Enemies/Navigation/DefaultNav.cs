using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Default navigation: drives the NavMeshAgent toward the closest point on the
/// target's collider (via MobCore.GetTargetPoint). Uses collider-edge instead
/// of pivot so large obstacles (like the main Base) are reachable.
/// </summary>
[DisallowMultipleComponent]
public class DefaultNav : MonoBehaviour, INavigationModifier
{
    [Tooltip("Max distance for snapping the destination back onto the NavMesh.")]
    [SerializeField] private float navMeshSampleRadius = 2f;

    public void UpdateDestination(MobCore mob, Damageable target)
    {
        var agent = mob.Agent;
        if (agent == null || !agent.isOnNavMesh) return;

        if (target == null)
        {
            agent.isStopped = true;
            return;
        }

        Vector3 desired = mob.GetTargetPoint(target);

        // Snap onto NavMesh so destination isn't stuck inside an obstacle (e.g. Base).
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            desired = hit.position;

        agent.isStopped = false;
        agent.SetDestination(desired);
    }
}
