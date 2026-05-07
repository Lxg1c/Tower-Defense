using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

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

    [HideInInspector]
    [FormerlySerializedAs("firePoint")]
    [SerializeField] private Transform legacyFirePoint;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Events")]
    public UnityEvent onFired;

    public bool HasTarget { get; private set; }
    public Vector3 TargetDirection { get; private set; }
    public Damageable CurrentTarget { get; private set; }
    /// <summary>If true, the shooter still tracks targets (rotation, HasTarget) but does not fire.</summary>
    public bool SuppressFire { get; set; }

    public float Damage   { get => damage;   set => damage   = value; }
    public float FireRate { get => fireRate; set => fireRate = Mathf.Max(0.0001f, value); }

    private DetectionZone detectionZone;
    private float fireCooldown;
    private Coroutine fireRoutine;

    private void Awake()
    {
        detectionZone = GetComponent<DetectionZone>();
    }

    private void OnDisable()
    {
        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
            fireRoutine = null;
        }

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

        AimFirePoints(CurrentTarget);

        if (fireCooldown <= 0f && !SuppressFire && fireRoutine == null)
        {
            fireRoutine = StartCoroutine(ShootVolley(CurrentTarget));
            fireCooldown = 1f / fireRate;
        }
    }

    private void AimFirePoints(Damageable target)
    {
        if (target == null)
            return;

        if (firePoints != null && firePoints.Length > 0)
        {
            for (int i = 0; i < firePoints.Length; i++)
                AimFirePoint(firePoints[i], target);

            return;
        }

        AimFirePoint(GetLegacyFirePoint(), target);
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
        Transform originPoint = GetPrimaryFirePoint();
        Vector3 origin = originPoint != null ? originPoint.position : transform.position;
        Vector3 dir = target.transform.position - origin;
        float dist = dir.magnitude;

        if (Physics.Raycast(origin, dir.normalized, dist, obstacleMask))
            return false; // Something blocking the way

        return true;
    }

    private IEnumerator ShootVolley(Damageable target)
    {
        if (target == null)
        {
            fireRoutine = null;
            yield break;
        }

        onFired?.Invoke();

        bool firedAny = false;

        if (firePoints != null && firePoints.Length > 0)
        {
            for (int i = 0; i < firePoints.Length; i++)
            {
                if (firePoints[i] == null)
                    continue;

                ShootFrom(firePoints[i], target);
                firedAny = true;

                if (delayBetweenFirePoints > 0f && i < firePoints.Length - 1)
                    yield return new WaitForSeconds(delayBetweenFirePoints);
            }
        }

        if (!firedAny)
            ShootFrom(GetLegacyFirePoint(), target);

        fireRoutine = null;
    }

    private void ShootFrom(Transform point, Damageable target)
    {
        if (target == null)
            return;

        Vector3 origin = point != null ? point.position : transform.position;
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
    }

    private Transform GetPrimaryFirePoint()
    {
        if (firePoints != null)
        {
            for (int i = 0; i < firePoints.Length; i++)
            {
                if (firePoints[i] != null)
                    return firePoints[i];
            }
        }

        return GetLegacyFirePoint();
    }

    private Transform GetLegacyFirePoint()
    {
        return legacyFirePoint != null ? legacyFirePoint : transform;
    }
}
