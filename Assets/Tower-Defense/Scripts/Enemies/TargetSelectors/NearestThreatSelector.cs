using UnityEngine;

/// <summary>
/// Default mob behavior: always head for the Base, but switch to the nearest
/// threat (tower / player / etc.) the moment one enters awareness range.
/// Selection is purely distance-based — no priorities.
///
/// Set <see cref="logSelections"/> if you want a step-by-step trace of what's
/// in range and why a particular target was picked.
/// </summary>
[DisallowMultipleComponent]
public class NearestThreatSelector : MonoBehaviour, ITargetSelector
{
    [Tooltip("Layers scanned for closer threats (towers, player, etc.).")]
    [SerializeField] private LayerMask threatMask;

    [SerializeField] private int maxCandidates = 32;

    [Header("Debug")]
    [Tooltip("Print every target decision with the list of detected colliders. Turn off for shipping.")]
    [SerializeField] private bool logSelections = false;

    private Collider[] buffer;
    private Base cachedBase;

    private void Awake()
    {
        buffer = new Collider[Mathf.Max(4, maxCandidates)];
    }

    public Damageable GetTarget(MobCore mob)
    {
        if (cachedBase == null)
            cachedBase = FindFirstObjectByType<Base>();

        Damageable nearestThreat = FindNearestThreat(mob, out int detected, out int targetable);

        Damageable chosen;
        string reason;
        if (nearestThreat != null)
        {
            chosen = nearestThreat;
            reason = "nearest threat in awareness";
        }
        else
        {
            chosen = cachedBase != null && cachedBase.IsTargetable ? (Damageable)cachedBase : null;
            reason = chosen != null ? "fallback to Base" : "no Base found";
        }

        if (logSelections)
        {
            Debug.Log(
                $"[NearestThreatSelector] {mob.name}: detected={detected}, targetable={targetable}, " +
                $"picked={(chosen != null ? chosen.name : "null")} ({reason}). " +
                $"Mask={(int)threatMask}, awareness={mob.AwarenessRadius}",
                mob);
        }

        return chosen;
    }

    private Damageable FindNearestThreat(MobCore mob, out int detected, out int targetable)
    {
        detected = 0;
        targetable = 0;

        if (threatMask == 0) return null;

        int count = Physics.OverlapSphereNonAlloc(
            mob.transform.position, mob.AwarenessRadius, buffer, threatMask, QueryTriggerInteraction.Collide);
        detected = count;

        Damageable best = null;
        float bestDistSqr = float.MaxValue;
        Vector3 origin = mob.transform.position;

        for (int i = 0; i < count; i++)
        {
            var col = buffer[i];
            if (col == null) continue;
            if (col.transform.IsChildOf(mob.transform)) continue;

            var dmg = col.GetComponentInParent<Damageable>();
            if (dmg == null)
            {
                if (logSelections)
                    Debug.Log($"[NearestThreatSelector] {mob.name}: collider '{col.name}' (layer {LayerMask.LayerToName(col.gameObject.layer)}) has no Damageable in parents — skipped.", mob);
                continue;
            }
            if (!dmg.IsTargetable)
            {
                if (logSelections)
                    Debug.Log($"[NearestThreatSelector] {mob.name}: '{dmg.name}' is not Targetable (alive={dmg.IsAlive}) — skipped.", mob);
                continue;
            }

            targetable++;
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
