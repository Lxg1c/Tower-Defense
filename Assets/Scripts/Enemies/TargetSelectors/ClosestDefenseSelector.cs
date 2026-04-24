using UnityEngine;

/// <summary>
/// Picks the nearest Damageable whose layer is in the configured mask.
/// Use this for barbarians: put Player, Tower and Base layers in the mask
/// and they will always go for the closest defender.
/// </summary>
[DisallowMultipleComponent]
public class ClosestDefenseSelector : MonoBehaviour, ITargetSelector
{
    [SerializeField] private LayerMask targetMask = ~0;
    [Tooltip("Reusable buffer size for Physics.OverlapSphereNonAlloc.")]
    [SerializeField] private int maxCandidates = 32;

    private Collider[] buffer;

    private void Awake()
    {
        buffer = new Collider[Mathf.Max(4, maxCandidates)];
    }

    public Damageable GetTarget(MobCore mob)
    {
        int count = Physics.OverlapSphereNonAlloc(
            mob.transform.position, mob.AwarenessRadius, buffer, targetMask, QueryTriggerInteraction.Collide);

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
