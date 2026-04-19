using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DetectionZone : MonoBehaviour
{
    [SerializeField] private float radius = 10f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private float updateInterval = 0.2f;

    private readonly List<Damageable> targets = new();
    private float updateTimer;

    public IReadOnlyList<Damageable> Targets => targets;
    public bool HasTargets => targets.Count > 0;
    public float Radius => radius;

    private void Update()
    {
        updateTimer -= Time.deltaTime;
        if (updateTimer > 0f)
            return;

        updateTimer = updateInterval;
        RefreshTargets();
    }

    private void RefreshTargets()
    {
        targets.Clear();

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers);
        foreach (Collider col in hits)
        {
            if (col.gameObject == gameObject)
                continue;

            // Search on the collider's object AND its parents
            var target = col.GetComponentInParent<Damageable>();
            if (target != null && target.IsAlive && !targets.Contains(target))
                targets.Add(target);
        }
    }

    public Damageable GetClosest()
    {
        Damageable closest = null;
        float closestDist = float.MaxValue;

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] == null || !targets[i].IsAlive)
            {
                targets.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(transform.position, targets[i].transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = targets[i];
            }
        }

        return closest;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
