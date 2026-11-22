using UnityEngine;

/// <summary>Flying raider: mines first, then nearby structures, then the base.</summary>
[DisallowMultipleComponent]
public sealed class MineFirstSelector : MonoBehaviour, ITargetSelector
{
    private int structureMask;
    private Collider[] candidates = new Collider[32];

    private void Awake() => structureMask = LayerMask.GetMask("Tower", "Mine");

    public Damageable GetTarget(MobCore mob)
    {
        Damageable mine = MineIncome.FindClosestTarget(mob.transform.position);
        if (mine != null) return mine;

        Vector3 origin = mob.transform.position;
        int count;
        while ((count = Physics.OverlapSphereNonAlloc(origin, mob.AwarenessRadius,
                   candidates, structureMask, QueryTriggerInteraction.Collide)) == candidates.Length)
            System.Array.Resize(ref candidates, candidates.Length * 2);

        TowerHealth closest = null;
        float best = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var tower = candidates[i].GetComponentInParent<TowerHealth>();
            if (tower == null || !tower.IsTargetable) continue;
            float distance = (candidates[i].ClosestPoint(origin) - origin).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            closest = tower;
        }
        if (closest != null) return closest;

        Base target = Base.Instance;
        return target != null && target.IsTargetable ? target : null;
    }
}
