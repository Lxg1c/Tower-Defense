using System.Collections.Generic;
using UnityEngine;

/// <summary>An arcing visual whose impact damages each enemy once within its splash radius.</summary>
[DisallowMultipleComponent]
public sealed class MortarShell : MonoBehaviour
{
    [SerializeField, Min(0f)] private float arcHeight = 3f;

    private Vector3 launchPosition;
    private Vector3 landingPosition;
    private float damage;
    private float splashRadius;
    private float flightDuration;
    private float elapsed;
    private LayerMask enemyMask;
    private bool launched;

    public void Launch(Vector3 landing, float hitDamage, float radius, float duration, LayerMask mask)
    {
        launchPosition = transform.position;
        landingPosition = landing;
        damage = Mathf.Max(0f, hitDamage);
        splashRadius = Mathf.Max(0f, radius);
        flightDuration = Mathf.Max(0.01f, duration);
        enemyMask = mask;
        elapsed = 0f;
        launched = true;
    }

    private void Update()
    {
        if (!launched)
            return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / flightDuration);
        transform.position = Vector3.Lerp(launchPosition, landingPosition, progress)
            + Vector3.up * (4f * arcHeight * progress * (1f - progress));
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
            if (target != null && target.IsTargetable && damaged.Add(target))
                target.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
