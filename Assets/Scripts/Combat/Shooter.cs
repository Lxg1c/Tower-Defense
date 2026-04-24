using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(DetectionZone))]
public class Shooter : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Events")]
    public UnityEvent onFired;

    public bool HasTarget { get; private set; }
    public Vector3 TargetDirection { get; private set; }
    public Damageable CurrentTarget { get; private set; }

    private DetectionZone detectionZone;
    private float fireCooldown;

    private void Awake()
    {
        detectionZone = GetComponent<DetectionZone>();
    }

    private void OnDisable()
    {
        // Clear state so other systems (PlayerMovement rotation, etc.) stop reacting.
        HasTarget       = false;
        CurrentTarget   = null;
        TargetDirection = Vector3.forward;
    }

    private void Update()
    {
        fireCooldown -= Time.deltaTime;

        CurrentTarget = FindBestTarget();

        if (CurrentTarget == null)
        {
            HasTarget = false;
            return;
        }

        Vector3 dir = CurrentTarget.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            TargetDirection = dir.normalized;

        HasTarget = true;

        if (firePoint != null)
        {
            Vector3 fireDir = CurrentTarget.transform.position - firePoint.position;
            if (fireDir.sqrMagnitude > 0.001f)
                firePoint.rotation = Quaternion.LookRotation(fireDir);
        }

        if (fireCooldown <= 0f)
        {
            Shoot(CurrentTarget);
            fireCooldown = 1f / fireRate;
        }
    }

    private Damageable FindBestTarget()
    {
        Damageable closest = detectionZone.GetClosest();
        if (closest == null)
            return null;

        // Raycast to check line of sight
        if (!HasLineOfSight(closest))
            return null;

        return closest;
    }

    private bool HasLineOfSight(Damageable target)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 dir = target.transform.position - origin;
        float dist = dir.magnitude;

        if (Physics.Raycast(origin, dir.normalized, dist, obstacleMask))
            return false; // Something blocking the way

        return true;
    }

    private void Shoot(Damageable target)
    {
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 dir = (target.transform.position - origin).normalized;

        if (projectilePrefab != null)
        {
            GameObject go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
            Projectile proj = go.GetComponent<Projectile>();
            if (proj != null)
                proj.Init(target, damage);
        }
        else
        {
            // Fallback: instant hit if no projectile prefab assigned
            target.TakeDamage(damage);
        }

        onFired?.Invoke();
    }
}
