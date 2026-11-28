using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public sealed class GroundTrap : MonoBehaviour
{
    [SerializeField] private GameObject impactPrefab;
    [SerializeField, Min(1f)] private float damage = 85f;
    [SerializeField, Min(0.1f)] private float blastRadius = 2.5f;

    private bool detonated;

    private void OnTriggerEnter(Collider other)
    {
        if (detonated)
            return;

        var victim = other.GetComponentInParent<MobHealth>();
        Vector3 origin = transform.position + Vector3.up * 0.25f;
        if (!CanDamage(victim) || IsBlocked(origin, other, victim))
            return;

        detonated = true;
        GetComponent<SphereCollider>().enabled = false;
        if (impactPrefab != null)
            Instantiate(impactPrefab, origin, Quaternion.identity);

        int enemyMask = LayerMask.GetMask("Enemy");
        var damaged = new HashSet<MobHealth> { victim };
        victim.TakeDamage(damage);
        foreach (Collider hit in Physics.OverlapSphere(origin, blastRadius, enemyMask,
                     QueryTriggerInteraction.Collide))
        {
            var nearby = hit.GetComponentInParent<MobHealth>();
            if (!CanDamage(nearby) || damaged.Contains(nearby) || IsBlocked(origin, hit, nearby))
                continue;

            damaged.Add(nearby);
            nearby.TakeDamage(damage);
        }
        Destroy(gameObject);
    }

    private bool IsBlocked(Vector3 origin, Collider collider, MobHealth victim)
    {
        Vector3 offset = collider.bounds.center - origin;
        return ProjectileObstacles.Cast(origin, 0.001f, offset.normalized, offset.magnitude,
            transform, victim.transform, null, out _);
    }

    private static bool CanDamage(MobHealth victim) =>
        victim != null && victim.IsTargetable && !victim.TryGetComponent<FlyingNav>(out _);
}
