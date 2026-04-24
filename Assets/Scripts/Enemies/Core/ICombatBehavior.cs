using UnityEngine;

/// <summary>
/// Handles how a mob deals damage to its current target.
/// Implementations control timing (cooldowns), damage values, damage type modifiers,
/// projectile spawning, melee swings, etc.
/// </summary>
public interface ICombatBehavior
{
    /// <summary>Attack range in meters. Used by MobCore for NavMeshAgent stopping distance.</summary>
    float AttackRange { get; }

    /// <summary>
    /// Called every frame while a target exists and the mob is within AttackRange.
    /// Implementation is responsible for its own cooldown.
    /// </summary>
    void Tick(MobCore mob, Damageable target);

    /// <summary>Called when the target changes (null means target lost).</summary>
    void OnTargetChanged(MobCore mob, Damageable newTarget);
}
