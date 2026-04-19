using UnityEngine;

/// <summary>
/// Идёт к ближайшей башне в DetectionZone, бьёт когда в AttackRange.
/// Если башен нет — идёт к базе.
/// DetectionZone (большой радиус) — обнаружение.
/// AttackRange (маленький радиус в EnemyBehavior) — удар.
/// </summary>
public class TowerAttacker : EnemyBehavior
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
        // Nearest tower in detection zone
        Damageable nearest     = null;
        float      nearestDist = float.MaxValue;

        foreach (var t in DetectionZone.Targets)
        {
            if (!(t is TowerHealth) || !t.IsAlive) continue;

            float d = Vector3.Distance(transform.position, t.transform.position);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest     = t;
            }
        }

        if (nearest != null) return nearest;

        // Fall back to base
        return FinalTarget;
    }
}
