using UnityEngine;

/// <summary>Flying raider: attack a surviving mine, then fly to the base.</summary>
[DisallowMultipleComponent]
public sealed class MineFirstSelector : MonoBehaviour, ITargetSelector
{
    public Damageable GetTarget(MobCore mob)
    {
        Damageable mine = MineIncome.FindClosestTarget(mob.transform.position);
        if (mine != null) return mine;
        Base target = Base.Instance;
        return target != null && target.IsTargetable ? target : null;
    }
}
