using UnityEngine;

/// <summary>
/// Generic melee attack. Deals a flat damage value at a configured rate.
/// Supports per-target-type damage multipliers so a Giant can deal reduced
/// damage to towers, more to walls, etc. Keep the multiplier list short —
/// first match wins.
/// </summary>
[DisallowMultipleComponent]
public class StandardAttack : MonoBehaviour, ICombatBehavior
{
    [System.Serializable]
    public class DamageMultiplier
    {
        [Tooltip("MonoBehaviour type name to match against the target's components (e.g. 'TowerHealth').")]
        public string targetComponentName;
        [Range(0f, 5f)] public float multiplier = 1f;
    }

    [Header("Attack")]
    [SerializeField] private float damage = 10f;
    [Tooltip("Attacks per second.")]
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Damage modifiers (optional)")]
    [SerializeField] private DamageMultiplier[] multipliers;

    private float cooldown;

    public float AttackRange => attackRange;

    public void OnTargetChanged(MobCore mob, Damageable newTarget)
    {
        // Small delay on acquiring a new target so mobs don't "teleport-hit"
        // the instant they arrive in range.
        cooldown = Mathf.Max(cooldown, 0.2f);
    }

    public void Tick(MobCore mob, Damageable target)
    {
        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;
        if (target == null || !target.IsAlive) return;

        float finalDamage = damage * GetMultiplierFor(target);
        target.TakeDamage(finalDamage);

        cooldown = 1f / Mathf.Max(0.01f, attackRate);
    }

    private float GetMultiplierFor(Damageable target)
    {
        if (multipliers == null || multipliers.Length == 0) return 1f;

        for (int i = 0; i < multipliers.Length; i++)
        {
            var m = multipliers[i];
            if (string.IsNullOrEmpty(m.targetComponentName)) continue;
            if (target.GetComponent(m.targetComponentName) != null)
                return m.multiplier;
        }
        return 1f;
    }
}
