using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Released charge-orb projectile.
///
/// Upon first contact with any enemy, the orb explodes and damages ALL enemies
/// within the explosion radius.
/// </summary>
[DisallowMultipleComponent]
public class ChargeOrbProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.15f;
    private Vector3    direction;
    private float      speed;
    private float      damage;
    private float      lifetime;
    private float      explosionRadius;
    private LayerMask  hitMask;
    private LayerMask  obstacleMask;
    private Damageable owner;
    private GameObject explosionPrefab;
    private float      explosionLifetime;

    private float age;
    private static readonly Collider[] buffer = new Collider[32];
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[32];
    private bool hasExploded;

    public void Init(Vector3 direction,
                     float speed,
                     float fullDamage,
                     float lifetime,
                     float explosionRadius,
                     LayerMask hitMask,
                     LayerMask obstacleMask,
                     Damageable owner,
                     GameObject explosionPrefab = null,
                     float explosionLifetime = 1.5f)
    {
        this.direction        = direction.normalized;
        this.speed            = speed;
        this.damage  = fullDamage;
        this.lifetime         = lifetime;
        this.explosionRadius  = explosionRadius;
        this.hitMask          = hitMask;
        this.obstacleMask     = obstacleMask;
        this.owner            = owner;
        this.explosionPrefab  = explosionPrefab;
        this.explosionLifetime = explosionLifetime;
    }

    private void Update()
    {
        if (hasExploded) return;
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float moveDistance = speed * Time.deltaTime;
        float allowedDistance = moveDistance;
        bool blocked = HitsObstacle(moveDistance, out RaycastHit obstacleHit);
        if (blocked)
            allowedDistance = obstacleHit.distance;

        if (HitsTarget(allowedDistance, out Vector3 hitPoint))
        {
            transform.position = hitPoint;
            Explode();
            return;
        }

        if (blocked)
        {
            transform.position = obstacleHit.point + obstacleHit.normal * 0.02f;
            Explode();
            return;
        }

        transform.position += direction * moveDistance;

        // Проверяем, не коснулись ли мы какого-нибудь врага
        int count = Physics.OverlapSphereNonAlloc(transform.position, GetCollisionRadius(), buffer, hitMask, QueryTriggerInteraction.Collide);
     
        for (int i = 0; i < count; i++)
        {
            var d = buffer[i].GetComponentInParent<Damageable>();
            if (d == null || d == owner) continue;
            if (!d.IsTargetable) continue;
            if (!HasLineOfSight(d)) continue;

            if (!hasExploded)
            {
                Explode();
                return;
            }
        }
    }

    private void Explode()
    {
        hasExploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, hitMask,
            QueryTriggerInteraction.Collide);
        var damaged = new HashSet<Damageable>();
        foreach (Collider hit in hits)
        {
            var target = hit.GetComponentInParent<Damageable>();
            if (target == null || target == owner || !target.IsTargetable || damaged.Contains(target)) continue;
            if (!HasLineOfSight(target)) continue;
            damaged.Add(target);
            target.TakeDamage(damage);
        }

        // Спавним визуальный эффект взрыва
        if (explosionPrefab != null)
        {
            var vfx = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            if (explosionLifetime > 0f) Destroy(vfx, explosionLifetime);
        }

        // Уничтожаем снаряд
        Destroy(gameObject);
    }

    // Опционально: визуализация радиуса взрыва в редакторе
    private bool HitsObstacle(float distance, out RaycastHit hit)
    {
        return ProjectileObstacles.Cast(transform.position, GetCollisionRadius(), direction, distance,
            owner != null ? owner.transform : null, null, transform, out hit, obstacleMask);
    }

    private bool HitsTarget(float distance, out Vector3 hitPoint)
    {
        hitPoint = transform.position + direction * Mathf.Max(0f, distance);

        int count = Physics.SphereCastNonAlloc(
            transform.position,
            GetCollisionRadius(),
            direction,
            hitBuffer,
            distance,
            hitMask,
            QueryTriggerInteraction.Collide);

        float bestDistance = float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
            Damageable d = hit.collider != null ? hit.collider.GetComponentInParent<Damageable>() : null;
            if (d == null || d == owner) continue;
            if (!d.IsTargetable) continue;

            float hitDistance = Mathf.Max(0f, hit.distance);
            if (hitDistance >= bestDistance) continue;

            bestDistance = hitDistance;
            hitPoint = hit.point == Vector3.zero ? transform.position + direction * hitDistance : hit.point;
            found = true;
        }

        return found;
    }

    private float GetCollisionRadius()
    {
        Vector3 scale = transform.lossyScale;
        return collisionRadius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
    }

    private bool HasLineOfSight(Damageable target)
    {
        if (target == null)
            return true;

        Vector3 targetPoint = GetTargetPoint(target);
        Vector3 toTarget = targetPoint - transform.position;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f)
            return true;

        return !ProjectileObstacles.Cast(transform.position, 0.001f, toTarget / distance, distance,
            owner != null ? owner.transform : null, target.transform, transform, out _, obstacleMask);
    }

    private Vector3 GetTargetPoint(Damageable target)
    {
        Collider col = target.GetComponentInChildren<Collider>();
        return col != null ? col.bounds.center : target.transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
