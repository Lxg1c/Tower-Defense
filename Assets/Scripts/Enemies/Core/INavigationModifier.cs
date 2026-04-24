using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Modifies how a mob navigates toward its target.
/// Default implementation simply sets NavMeshAgent.destination.
/// Specialized implementations can swap between "path through walls" vs "go around", etc.
/// </summary>
public interface INavigationModifier
{
    /// <summary>
    /// Called by MobCore on a throttled cadence (target-refresh interval).
    /// Implementation should set the NavMeshAgent destination as appropriate.
    /// </summary>
    void UpdateDestination(MobCore mob, Damageable target);
}
