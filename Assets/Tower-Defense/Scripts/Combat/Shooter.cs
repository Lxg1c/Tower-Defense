using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(DetectionZone))]
public class Shooter : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private Transform[] firePoints;
    [Tooltip("Delay between consecutive muzzle shots in the same volley.")]
    [SerializeField] private float delayBetweenFirePoints = 0.08f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Events")]
    public UnityEvent onFired = new();

    public bool HasTarget { get; private set; }
    public Vector3 TargetDirection { get; private set; }
    public Damageable CurrentTarget { get; private set; }
    /// <summary>If true, the shooter still tracks targets (rotation, HasTarget) but does not fire.</summary>
    public bool SuppressFire { get; set; }

    public float Damage   { get => damage;   set => damage   = value; }
    public float FireRate { get => fireRate; set => fireRate = Mathf.Max(0.0001f, value); }

    private DetectionZone detectionZone;
    private SlowAmmoInventory slowAmmo;
    private float fireCooldown;
    private Damageable volleyTarget;
    private int nextFirePoint;
    private float nextShotTime;

    private void Awake()
    {
        detectionZone = GetComponent<DetectionZone>();
        slowAmmo = GetComponent<SlowAmmoInventory>();
        if (!ValidateConfiguration(out string error))
        {
            Debug.LogError($"[Shooter] {error}", this);
            enabled = false;
        }
    }

    public bool ValidateConfiguration(out string error)
    {
        if (firePoints == null || firePoints.Length == 0)
            error = "Assign at least one fire point.";
        else if (System.Array.Exists(firePoints, point => point == null))
            error = "Every fire point must be assigned.";
        else if (projectilePrefab == null || projectilePrefab.GetComponent<Projectile>() == null)
            error = "Assign a projectile prefab with a Projectile component.";
        else if (fireRate <= 0f || damage < 0f || delayBetweenFirePoints < 0f)
            error = "Fire rate must be positive; damage and volley delay must be non-negative.";
        else
        {
            error = null;
            return true;
        }
        return false;
    }

    private void OnDisable()
    {
        volleyTarget = null;

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
    }

    // PlayerGunAim runs first in LateUpdate, after animation. Firing here keeps
    // every shot in a staggered volley attached to the displayed muzzle pose.
    private void LateUpdate()
    {
        AimFirePoints(CurrentTarget);
        if (volleyTarget != null && (!volleyTarget.IsTargetable || SuppressFire))
            volleyTarget = null;

        if (volleyTarget == null && CurrentTarget != null && fireCooldown <= 0f && !SuppressFire)
        {
            volleyTarget = CurrentTarget;
            nextFirePoint = 0;
            nextShotTime = Time.time;
            fireCooldown = 1f / fireRate;
        }

        while (volleyTarget != null && Time.time >= nextShotTime)
        {
            ShootFrom(firePoints[nextFirePoint++], volleyTarget);
            if (volleyTarget == null || !isActiveAndEnabled || nextFirePoint >= firePoints.Length
                || SuppressFire || !volleyTarget.IsTargetable)
            {
                volleyTarget = null;
                break;
            }
            nextShotTime = Time.time + delayBetweenFirePoints;
        }
    }

    private void AimFirePoints(Damageable target)
    {
        if (target == null)
            return;

        for (int i = 0; i < firePoints.Length; i++)
            AimFirePoint(firePoints[i], target);
    }

    private void AimFirePoint(Transform point, Damageable target)
    {
        if (point == null)
            return;

        Vector3 fireDir = target.transform.position - point.position;
        if (fireDir.sqrMagnitude > 0.001f)
            point.rotation = Quaternion.LookRotation(fireDir);
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
        Vector3 origin = firePoints[0].position;
        Vector3 dir = target.transform.position - origin;
        float dist = dir.magnitude;

        if (Physics.Raycast(origin, dir.normalized, dist, obstacleMask))
            return false; // Something blocking the way

        return true;
    }

    private void ShootFrom(Transform point, Damageable target)
    {
        if (target == null)
            return;

        Vector3 origin = point.position;
        Vector3 dir = (target.transform.position - origin).normalized;

        GameObject go = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(dir));
        if (slowAmmo != null && slowAmmo.IsActive)
            go.GetComponent<Projectile>().Init(target, damage, slowAmmo.SlowMultiplier, slowAmmo.SlowDuration);
        else
            go.GetComponent<Projectile>().Init(target, damage);

        onFired?.Invoke();
    }

}
