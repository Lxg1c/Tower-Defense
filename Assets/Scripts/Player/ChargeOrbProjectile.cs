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
        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float moveDistance = speed * Time.deltaTime;
        float allowedDistance = moveDistance;
        if (HitsObstacle(moveDistance, out RaycastHit obstacleHit))
            allowedDistance = obstacleHit.distance;

        if (HitsTarget(allowedDistance, out Vector3 hitPoint))
        {
            transform.position = hitPoint;
            Explode();
            return;
        }

        if (allowedDistance < moveDistance)
        {
            Destroy(gameObject);
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

        // Находим ВСЕХ врагов в радиусе взрыва
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, buffer, hitMask, QueryTriggerInteraction.Collide);
        
        // Наносим урон каждому врагу в радиусе
        for (int i = 0; i < hitCount; i++)
        {
            var d = buffer[i].GetComponentInParent<Damageable>();
            if (d == null || d == owner) continue;
            if (!d.IsTargetable) continue;
            if (!HasLineOfSight(d)) continue;
            
            d.TakeDamage(damage);
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
        if (obstacleMask.value == 0)
        {
            hit = default;
            return false;
        }

        return Physics.SphereCast(
            transform.position,
            GetCollisionRadius(),
            direction,
            out hit,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
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
        return Mathf.Max(0.05f, explosionRadius);
    }

    private bool HasLineOfSight(Damageable target)
    {
        if (obstacleMask.value == 0 || target == null)
            return true;

        Vector3 targetPoint = GetTargetPoint(target);
        Vector3 toTarget = targetPoint - transform.position;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f)
            return true;

        return !Physics.Raycast(
            transform.position,
            toTarget / distance,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore);
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
