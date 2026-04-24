using UnityEngine;

/// <summary>
/// Walls-first priority selector (for Giant-like archetypes).
/// Searches the wall layer first within awareness radius; if nothing found,
/// falls back to the configured "defense" layer mask (closest among them).
/// </summary>
[DisallowMultipleComponent]
public class WallFirstSelector : MonoBehaviour, ITargetSelector
{
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private int maxCandidates = 32;

    private Collider[] buffer;

    private void Awake()
    {
        buffer = new Collider[Mathf.Max(4, maxCandidates)];
    }

    public Damageable GetTarget(MobCore mob)
    {
        return PickClosest(mob, wallMask);
    }

    private Damageable PickClosest(MobCore mob, LayerMask mask)
    {
        if (mask == 0) return null;

        int count = Physics.OverlapSphereNonAlloc(
            mob.transform.position, mob.AwarenessRadius, buffer, mask, QueryTriggerInteraction.Collide);

        Damageable best = null;
        float bestDistSqr = float.MaxValue;
        Vector3 origin = mob.transform.position;

        for (int i = 0; i < count; i++)
        {
            var col = buffer[i];
            if (col == null) continue;
            if (col.transform.IsChildOf(mob.transform)) continue;

            var dmg = col.GetComponentInParent<Damageable>();
            if (dmg == null || !dmg.IsTargetable) continue;

            float d = (dmg.transform.position - origin).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                best = dmg;
            }
        }

        return best;
    }
}
