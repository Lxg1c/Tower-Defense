using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float rotateSpeed = 200f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float hitDistance = 0.5f;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.05f;
    [SerializeField] private GameObject impactPrefab;
    [Tooltip("If homing requires a sharper turn than this angle, the projectile is destroyed.")]
    [SerializeField] private float maxHomingTurnAngle = 30f;
    [Tooltip("Extra distance after the initial target point where a projectile without a live target is destroyed.")]
    [SerializeField] private float missOvershootDistance = 1f;

    private Damageable target;
    private Collider targetCollider;
    private float damage;
    private float lifeTimer;
    private Vector3 lockedTargetPoint;
    private Vector3 fallbackDirection;
    private float maxTravelDistance;
    private float traveledDistance;
    private bool homingEnabled;
    private float slowMultiplier = 1f;
    private float slowDuration;
    private Transform owner;
    private bool impacted;

    public void Init(Damageable target, float damage, float slowMultiplier = 1f, float slowDuration = 0f,
        Transform owner = null)
    {
        this.owner = owner;
        impacted = false;
        this.target = target;
        targetCollider = target != null ? target.GetComponentInChildren<Collider>() : null;
        this.damage = damage;
        this.slowMultiplier = slowMultiplier;
        this.slowDuration = slowDuration;
        if (slowDuration > 0f && slowMultiplier < 1f)
            TintSlowProjectile();
        lifeTimer = lifeTime;

        lockedTargetPoint = target != null ? TargetPoint : transform.position + transform.forward;
        fallbackDirection = lockedTargetPoint - transform.position;
        if (fallbackDirection.sqrMagnitude < 0.001f)
            fallbackDirection = transform.forward;

        fallbackDirection.Normalize();
        maxTravelDistance = Vector3.Distance(transform.position, lockedTargetPoint) + Mathf.Max(0f, missOvershootDistance);
        traveledDistance = 0f;
        homingEnabled = target != null && target.IsAlive;

        if (fallbackDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(fallbackDirection);
    }

    private void Update()
    {
        if (impacted) return;
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (!homingEnabled || target == null || !target.IsAlive)
        {
            // Target gone — fly forward and expire
            homingEnabled = false;
            FlyStraight();
            return;
        }

        // Homing: rotate towards target
        Vector3 dir = TargetPoint - transform.position;
        if (dir.sqrMagnitude <= hitDistance * hitDistance)
        {
            if (StopAtObstacle(dir.normalized, dir.magnitude)) return;
            Hit();
            return;
        }

        float turnAngle = Vector3.Angle(transform.forward, dir);
        if (turnAngle > maxHomingTurnAngle)
        {
            Destroy(gameObject);
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        Vector3 moveDirection = transform.forward;
        float step = speed * Time.deltaTime;
        if (targetCollider != null && targetCollider.Raycast(new Ray(transform.position, moveDirection),
                out RaycastHit targetHit, step))
        {
            if (StopAtObstacle(moveDirection, targetHit.distance)) return;
            transform.position = targetHit.point;
            Hit();
            return;
        }
        Vector3 closestPoint = transform.position + moveDirection *
            Mathf.Clamp(Vector3.Dot(dir, moveDirection), 0f, step);
        if ((closestPoint - TargetPoint).sqrMagnitude <= hitDistance * hitDistance)
        {
            if (StopAtObstacle(moveDirection, Vector3.Distance(transform.position, closestPoint))) return;
            transform.position = closestPoint;
            Hit();
            return;
        }

        Move(moveDirection);
    }

    private void FlyStraight()
    {
        Move(fallbackDirection);

        if (traveledDistance >= maxTravelDistance)
            Destroy(gameObject);
    }

    private Vector3 TargetPoint => GetTargetPoint(target, targetCollider);

    internal static Vector3 GetTargetPoint(Damageable target, Collider collider) =>
        collider != null ? collider.bounds.center : target.transform.position;

    internal bool CanReach(Vector3 origin, Damageable victim, Collider collider, Vector3 aimPoint,
        Transform shotOwner, LayerMask extraObstacles)
    {
        Vector3 offset = aimPoint - origin;
        float distance = offset.magnitude;
        if (distance <= 0.0001f) return true;

        Vector3 direction = offset / distance;
        if (collider != null && collider.Raycast(new Ray(origin, direction), out RaycastHit hit, distance))
            distance = hit.distance;

        return !ProjectileObstacles.Cast(origin, collisionRadius, direction, distance,
            shotOwner, victim.transform, null, out _, out _, victim is MobHealth, extraObstacles);
    }

    private void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        float distance = speed * Time.deltaTime;
        if (StopAtObstacle(direction.normalized, distance)) return;
        transform.position += direction.normalized * distance;
        traveledDistance += distance;
    }

    private void Hit()
    {
        if (impacted) return;
        if (target != null && target.IsAlive)
        {
            DamageTarget(target);
        }

        FinishImpact(transform.position, -transform.forward);
    }

    private bool StopAtObstacle(Vector3 direction, float distance)
    {
        if (!ProjectileObstacles.Cast(transform.position, collisionRadius, direction, distance,
                owner, target != null ? target.transform : null, transform, out RaycastHit hit,
                out Collider blocker, includeEnemyCover: target is MobHealth)) return false;
        if (blocker != null && blocker.GetComponentInParent<MobCore>() is { ProvidesCover: true } cover &&
            cover.Health.IsTargetable)
            DamageTarget(cover.Health);
        FinishImpact(hit.point, hit.normal);
        return true;
    }

    private void DamageTarget(Damageable victim)
    {
        victim.TakeDamage(damage);
        if (victim is not MobHealth mob || !mob.IsAlive || slowDuration <= 0f || slowMultiplier >= 1f)
            return;
        var slow = mob.GetComponent<MobSlow>();
        if (slow == null) slow = mob.gameObject.AddComponent<MobSlow>();
        slow.Apply(slowMultiplier, slowDuration);
    }

    private void FinishImpact(Vector3 point, Vector3 normal)
    {
        impacted = true;
        transform.position = point;
        if (impactPrefab != null)
            Instantiate(impactPrefab, point, Quaternion.LookRotation(normal.sqrMagnitude > 0f ? normal : Vector3.up));
        Destroy(gameObject);
    }

    private void TintSlowProjectile()
    {
        var block = new MaterialPropertyBlock();
        block.SetColor("_BaseColor", new Color(0.25f, 0.9f, 1f));
        foreach (Renderer visual in GetComponentsInChildren<Renderer>())
            visual.SetPropertyBlock(block);
    }
}

/// <summary>Shared swept collision against solid level geometry and buildings.</summary>
internal static class ProjectileObstacles
{
    private static readonly int worldMask = LayerMask.GetMask("Default", "Obstacle", "Tower", "Base", "Mine");
    private static readonly int enemyMask = LayerMask.GetMask("Enemy");
    private static RaycastHit[] hits = new RaycastHit[32];
    private static Collider[] overlaps = new Collider[16];

    internal static bool Cast(Vector3 origin, float radius, Vector3 direction, float distance,
        Transform owner, Transform target, Transform projectile, out RaycastHit nearest, int extraMask = 0) =>
        Cast(origin, radius, direction, distance, owner, target, projectile, out nearest, out _, false, extraMask);

    internal static bool Cast(Vector3 origin, float radius, Vector3 direction, float distance,
        Transform owner, Transform target, Transform projectile, out RaycastHit nearest,
        out Collider blocker, bool includeEnemyCover, int extraMask = 0)
    {
        nearest = default;
        blocker = null;
        if (distance <= 0f || direction.sqrMagnitude < 0.000001f) return false;
        int mask = worldMask | extraMask | (includeEnemyCover ? enemyMask : 0);
        int count;
        while ((count = Physics.OverlapSphereNonAlloc(origin, radius, overlaps, mask,
                   QueryTriggerInteraction.Ignore)) == overlaps.Length)
            System.Array.Resize(ref overlaps, overlaps.Length * 2);
        for (int i = 0; i < count; i++)
        {
            if (Ignored(overlaps[i], owner, target, projectile)) continue;
            if (includeEnemyCover && SkipNonCoverEnemy(overlaps[i])) continue;
            blocker = overlaps[i];
            Vector3 point = overlaps[i].ClosestPoint(origin);
            nearest = new RaycastHit { point = point, normal = (origin - point).sqrMagnitude > 0.000001f
                ? (origin - point).normalized : -direction.normalized, distance = 0f };
            return true;
        }
        while ((count = Physics.SphereCastNonAlloc(origin, radius, direction, hits, distance, mask,
                   QueryTriggerInteraction.Ignore)) == hits.Length)
            System.Array.Resize(ref hits, hits.Length * 2);
        float best = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            if (Ignored(hits[i].collider, owner, target, projectile) || hits[i].distance >= best) continue;
            if (includeEnemyCover && SkipNonCoverEnemy(hits[i].collider)) continue;
            nearest = hits[i];
            blocker = nearest.collider;
            best = nearest.distance;
        }
        return best < float.PositiveInfinity;
    }

    private static bool SkipNonCoverEnemy(Collider collider) =>
        (enemyMask & (1 << collider.gameObject.layer)) != 0 &&
        collider.GetComponentInParent<MobCore>() is not { ProvidesCover: true, Health: { IsTargetable: true } };

    private static bool Ignored(Collider collider, Transform owner, Transform target, Transform projectile) =>
        collider == null || owner != null && collider.transform.IsChildOf(owner) ||
        target != null && collider.transform.IsChildOf(target) ||
        projectile != null && collider.transform.IsChildOf(projectile);
}
