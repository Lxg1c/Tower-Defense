using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DetectionZone : MonoBehaviour
{
    [Tooltip("Horizontal targeting radius, matching the ground range indicator.")]
    [Min(0f)] [SerializeField] private float radius = 10f;
    [Tooltip("Maximum target height difference above or below this object.")]
    [Min(0f)] [SerializeField] private float verticalRange = 12f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private float updateInterval = 0.2f;

    private readonly List<Damageable> targets = new();
    private float updateTimer;

    public IReadOnlyList<Damageable> Targets => targets;
    public bool HasTargets => targets.Count > 0;
    public float Radius { get => radius; set => radius = Mathf.Max(0f, value); }

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

        // The box is a broad-phase query; target aim points must also be inside
        // the cylinder. Flight height must not reduce horizontal attack reach.
        Collider[] hits = Physics.OverlapBox(transform.position,
            new Vector3(radius, verticalRange, radius), Quaternion.identity, targetLayers);
        foreach (Collider col in hits)
        {
            if (col.transform.IsChildOf(transform))
                continue;

            // Search on the collider's object AND its parents
            var target = col.GetComponentInParent<Damageable>();
            if (target != null && target.IsTargetable && IsInRange(target) && !targets.Contains(target))
                targets.Add(target);
        }
    }

    private bool IsInRange(Damageable target)
    {
        Vector3 offset = target.transform.position - transform.position;
        return Mathf.Abs(offset.y) <= verticalRange
            && offset.x * offset.x + offset.z * offset.z <= radius * radius;
    }

    public Damageable GetClosest()
    {
        Damageable closest = null;
        float closestDist = float.MaxValue;

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] == null || !targets[i].IsTargetable || !IsInRange(targets[i]))
            {
                targets.RemoveAt(i);
                continue;
            }

            Vector3 offset = targets[i].transform.position - transform.position;
            float dist = offset.x * offset.x + offset.z * offset.z;
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
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Vector3 height = Vector3.up * verticalRange;
        const int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float b = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 start = transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius;
            Vector3 end = transform.position + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * radius;
            Gizmos.DrawLine(start + height, end + height);
            Gizmos.DrawLine(start - height, end - height);
            if (i % 8 == 0) Gizmos.DrawLine(start - height, start + height);
        }
    }
}
