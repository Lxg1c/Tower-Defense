using System.Collections.Generic;
using UnityEngine;

/// <summary>An arcing visual whose impact damages each enemy once within its splash radius.</summary>
[DisallowMultipleComponent]
public sealed class MortarShell : MonoBehaviour
{
    [SerializeField, Min(0f)] private float arcHeight = 3f;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.08f;
    [SerializeField] private GameObject impactPrefab;

    private Vector3 launchPosition;
    private Vector3 landingPosition;
    private float damage;
    private float splashRadius;
    private float flightDuration;
    private float flightArcHeight;
    private float elapsed;
    private LayerMask enemyMask;
    private bool launched;
    private Transform owner;

    public void Launch(Vector3 landing, float hitDamage, float radius, float duration, LayerMask mask,
        Transform owner = null, bool followMuzzleAngle = false)
    {
        this.owner = owner;
        launchPosition = transform.position;
        landingPosition = landing;
        damage = Mathf.Max(0f, hitDamage);
        splashRadius = Mathf.Max(0f, radius);
        flightDuration = Mathf.Max(0.01f, duration);
        flightArcHeight = arcHeight;
        if (followMuzzleAngle)
        {
            Vector3 displacement = landing - launchPosition;
            float groundDistance = Vector3.ProjectOnPlane(displacement, Vector3.up).magnitude;
            float horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).magnitude;
            float slope = transform.forward.y / Mathf.Max(.001f, horizontalForward);
            // The vertical tangent at launch is displacement.y + four times the arc height.
            flightArcHeight = Mathf.Max(0f, (groundDistance * slope - displacement.y) * .25f);
        }
        enemyMask = mask;
        elapsed = 0f;
        launched = true;
    }

    private void Update()
    {
        if (!launched)
            return;

        float previousProgress = Mathf.Clamp01(elapsed / flightDuration);
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / flightDuration);
        int steps = Mathf.Max(1, Mathf.CeilToInt((progress - previousProgress) * 32f));
        for (int step = 1; step <= steps; step++)
        {
            float t = Mathf.Lerp(previousProgress, progress, step / (float)steps);
            Vector3 next = Vector3.Lerp(launchPosition, landingPosition, t)
                + Vector3.up * (4f * flightArcHeight * t * (1f - t));
            Vector3 segment = next - transform.position;
            if (ProjectileObstacles.Cast(transform.position, collisionRadius, segment.normalized,
                    segment.magnitude, owner, null, transform, out RaycastHit hit))
            {
                landingPosition = hit.point + hit.normal * 0.02f;
                transform.position = landingPosition;
                Explode();
                return;
            }
            transform.position = next;
        }
        if (progress >= 1f)
            Explode();
    }

    private void Explode()
    {
        launched = false;
        Collider[] hits = Physics.OverlapSphere(landingPosition, splashRadius, enemyMask,
            QueryTriggerInteraction.Collide);
        var damaged = new HashSet<Damageable>();
        foreach (Collider hit in hits)
        {
            Damageable target = hit.GetComponentInParent<Damageable>();
            if (target != null && target.IsTargetable && HasLineOfSight(target, hit) && damaged.Add(target))
                target.TakeDamage(damage);
        }
        if (impactPrefab != null) Instantiate(impactPrefab, landingPosition, Quaternion.identity);
        Destroy(gameObject);
    }

    private bool HasLineOfSight(Damageable target, Collider collider)
    {
        Vector3 ray = collider.bounds.center - landingPosition;
        return !ProjectileObstacles.Cast(landingPosition, 0.001f, ray.normalized, ray.magnitude,
            owner, target.transform, transform, out _);
    }
}
