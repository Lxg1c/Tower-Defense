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
    private Projectile projectileTemplate;
    private Transform shotOwner;
    private System.Predicate<Damageable> canTarget;
    private float fireCooldown;
    private Damageable volleyTarget;
    private int nextFirePoint;
    private float nextShotTime;
    private bool volleyHasFired;

    private void Awake()
    {
        detectionZone = GetComponent<DetectionZone>();
        slowAmmo = GetComponent<SlowAmmoInventory>();
        if (!ValidateConfiguration(out string error))
        {
            Debug.LogError($"[Shooter] {error}", this);
            enabled = false;
            return;
        }
        projectileTemplate = projectilePrefab.GetComponent<Projectile>();
        shotOwner = GetComponentInParent<Damageable>()?.transform ?? transform;
        canTarget = HasLineOfSight;
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

        CurrentTarget = detectionZone.GetClosest(canTarget);

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
        if (volleyTarget != null && (!volleyTarget.IsTargetable || SuppressFire || CurrentTarget == null))
            volleyTarget = null;

        if (volleyTarget == null && CurrentTarget != null && fireCooldown <= 0f && !SuppressFire)
        {
            volleyTarget = CurrentTarget;
            nextFirePoint = 0;
            nextShotTime = Time.time;
            volleyHasFired = false;
        }

        while (volleyTarget != null && Time.time >= nextShotTime)
        {
            bool fired = ShootFrom(firePoints[nextFirePoint++], volleyTarget);
            if (volleyTarget == null || !isActiveAndEnabled || nextFirePoint >= firePoints.Length
                || SuppressFire || !volleyTarget.IsTargetable)
            {
                volleyTarget = null;
                break;
            }
            if (fired)
                nextShotTime = Time.time + delayBetweenFirePoints;
        }
    }

    private void AimFirePoints(Damageable target)
    {
        if (target == null)
            return;

        Vector3 aimPoint = Projectile.GetTargetPoint(target, target.GetComponentInChildren<Collider>());
        for (int i = 0; i < firePoints.Length; i++)
            AimFirePoint(firePoints[i], aimPoint);
    }

    private static void AimFirePoint(Transform point, Vector3 aimPoint)
    {
        if (point == null)
            return;

        Vector3 fireDir = aimPoint - point.position;
        if (fireDir.sqrMagnitude > 0.001f)
            point.rotation = Quaternion.LookRotation(fireDir);
    }

    private bool HasLineOfSight(Damageable target)
    {
        Collider collider = target.GetComponentInChildren<Collider>();
        Vector3 aimPoint = Projectile.GetTargetPoint(target, collider);
        foreach (Transform point in firePoints)
            if (point != null && projectileTemplate.CanReach(point.position, target, collider, aimPoint,
                    shotOwner, obstacleMask))
                return true;
        return false;
    }

    private bool ShootFrom(Transform point, Damageable target)
    {
        if (point == null)
            return false;

        Vector3 origin = point.position;
        Collider collider = target.GetComponentInChildren<Collider>();
        Vector3 aimPoint = Projectile.GetTargetPoint(target, collider);
        if (!projectileTemplate.CanReach(origin, target, collider, aimPoint, shotOwner, obstacleMask))
            return false;

        AimFirePoint(point, aimPoint);
        Projectile shot = Instantiate(projectileTemplate, origin, point.rotation);
        if (slowAmmo != null && slowAmmo.IsActive)
            shot.Init(target, damage, slowAmmo.SlowMultiplier, slowAmmo.SlowDuration, shotOwner);
        else
            shot.Init(target, damage, owner: shotOwner);

        if (!volleyHasFired)
        {
            fireCooldown = 1f / fireRate;
            volleyHasFired = true;
        }
        onFired?.Invoke();
        return true;
    }

}
