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
    private Damageable owner;
    private GameObject explosionPrefab;
    private float      explosionLifetime;

    private float age;
    private static readonly Collider[] buffer = new Collider[32];
    private bool hasExploded;

    public void Init(Vector3 direction,
                     float speed,
                     float fullDamage,
                     float lifetime,
                     float explosionRadius,
                     LayerMask hitMask,
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

        transform.position += direction * speed * Time.deltaTime;

        // Проверяем, не коснулись ли мы какого-нибудь врага
        int count = Physics.OverlapSphereNonAlloc(transform.position, 0.5f, buffer, hitMask, QueryTriggerInteraction.Collide);
     
        for (int i = 0; i < count; i++)
        {
            var d = buffer[i].GetComponentInParent<Damageable>();
            if (d == null || d == owner) continue;
            if (!d.IsTargetable) continue;

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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}