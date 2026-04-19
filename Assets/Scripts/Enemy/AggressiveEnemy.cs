using UnityEngine;

/// <summary>
/// Приоритет целей: игрок → башня → база.
/// DetectionZone (большой радиус) — обнаружение.
/// AttackRange (маленький радиус в EnemyBehavior) — удар.
/// </summary>
public class AggressiveEnemy : EnemyBehavior
{
    private void Update()
    {
        Damageable target = PickTarget();
        if (target != null)
            TryAttack(target);
    }

    public override Transform GetOverrideTarget()
    {
        Damageable target = PickTarget();
        return target != null ? target.transform : null;
    }

    private Damageable PickTarget()
    {
        // 1. Player in detection zone
        foreach (var t in DetectionZone.Targets)
            if (t is PlayerHealth p && p.IsAlive) return p;

        // 2. Tower or base in detection zone
        foreach (var t in DetectionZone.Targets)
            if ((t is TowerHealth || t is Base) && t.IsAlive) return t;

        // 3. Final base (always valid fallback)
        return FinalTarget;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || DetectionZone == null) return;
        foreach (var t in DetectionZone.Targets)
        {
            if (t == null) continue;
            Gizmos.color = t is PlayerHealth ? Color.red : Color.yellow;
            Gizmos.DrawLine(transform.position + Vector3.up, t.transform.position + Vector3.up);
        }
    }
#endif
}
