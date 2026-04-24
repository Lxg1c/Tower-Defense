using UnityEngine;

/// <summary>
/// Ranged attack combat behavior: spawns a homing Projectile at the target.
/// MobCore already rotates the mob toward the target every frame; this behavior
/// just times the shots and emits projectiles from a fire point.
///
/// Supports optional line-of-sight check against obstacleMask (walls/terrain)
/// and per-target-type damage multipliers (same shape as StandardAttack).
/// </summary>
[DisallowMultipleComponent]
public class RangedAttack : MonoBehaviour, ICombatBehavior
{
    [System.Serializable]
    public class DamageMultiplier
    {
        [Tooltip("Component type name to match on the target (e.g. 'TowerHealth').")]
        public string targetComponentName;
        [Range(0f, 5f)] public float multiplier = 1f;
    }

    [Header("Attack")]
    [SerializeField] private float damage = 5f;
    [Tooltip("Shots per second.")]
    [SerializeField] private float fireRate = 1f;
    [Tooltip("Max distance at which the mob will start firing.")]
    [SerializeField] private float attackRange = 8f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("Origin point for spawned projectiles. If null, uses the mob's transform.")]
    [SerializeField] private Transform firePoint;

    [Header("Line of Sight (optional)")]
    [Tooltip("If set (non-zero), a raycast against this mask must NOT hit anything between firePoint and target for the shot to happen.")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Damage modifiers (optional)")]
    [SerializeField] private DamageMultiplier[] multipliers;

    private float cooldown;

    public float AttackRange => attackRange;

    public void OnTargetChanged(MobCore mob, Damageable newTarget)
    {
        // Small wind-up on target acquisition so they don't insta-fire.
        cooldown = Mathf.Max(cooldown, 0.3f);
    }

    public void Tick(MobCore mob, Damageable target)
    {
        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;
        if (target == null || !target.IsAlive) return;
        if (projectilePrefab == null) return;

        Vector3 origin = firePoint != null ? firePoint.position : mob.transform.position;

        // Optional LoS check
        if (obstacleMask != 0)
        {
            Vector3 to = mob.GetTargetPoint(target) - origin;
            float dist = to.magnitude;
            if (dist > 0.01f &&
                Physics.Raycast(origin, to.normalized, dist, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // Blocked — wait a bit before retrying so we don't raycast every frame for nothing.
                cooldown = 0.2f;
                return;
            }
        }

        // Aim the projectile at the target's collider edge, same point MobCore uses for distance.
        Vector3 aimDir = mob.GetTargetPoint(target) - origin;
        Quaternion rot = aimDir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(aimDir)
            : (firePoint != null ? firePoint.rotation : mob.transform.rotation);

        GameObject go = Object.Instantiate(projectilePrefab, origin, rot);
        var proj = go.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(target, damage * GetMultiplierFor(target));

        cooldown = 1f / Mathf.Max(0.01f, fireRate);
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
