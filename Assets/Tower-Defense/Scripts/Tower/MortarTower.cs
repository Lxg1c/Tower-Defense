using UnityEngine;

/// <summary>Fires a slow, arcing shell at the nearest enemy's position.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(DetectionZone))]
public sealed class MortarTower : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 35f;
    [SerializeField, Min(0.01f)] private float attackInterval = 2.5f;
    [SerializeField, Min(0.1f)] private float splashRadius = 2.5f;
    [SerializeField, Min(0.01f)] private float flightDuration = 0.7f;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private Transform turretPivot;
    [SerializeField, Min(1f)] private float turnSpeed = 360f;
    [SerializeField] private Transform muzzle;
    [SerializeField] private MortarShell shellPrefab;

    public float Damage { get => damage; set => damage = Mathf.Max(0f, value); }
    public float AttackInterval { get => attackInterval; set => attackInterval = Mathf.Max(0.01f, value); }
    public float SplashRadius => splashRadius;

    private DetectionZone detection;
    private float nextShotTime;

    private void Awake()
    {
        detection = GetComponent<DetectionZone>();
        if (turretPivot == null || muzzle == null || shellPrefab == null || enemyMask.value == 0)
        {
            Debug.LogError("[MortarTower] Assign a turret pivot, muzzle, MortarShell prefab, and enemy mask.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Damageable target = detection.GetClosest();
        if (target == null)
            return;

        Vector3 landing = target.transform.position;
        Vector3 direction = Vector3.ProjectOnPlane(landing - turretPivot.position, Vector3.up);
        if (direction.sqrMagnitude > .0001f)
        {
            Quaternion aim = Quaternion.LookRotation(direction);
            turretPivot.rotation = Quaternion.RotateTowards(turretPivot.rotation, aim, turnSpeed * Time.deltaTime);
            if (Quaternion.Angle(turretPivot.rotation, aim) > .1f)
                return;
        }

        if (Time.time < nextShotTime)
            return;

        Vector3 barrelForward = Vector3.ProjectOnPlane(muzzle.forward, Vector3.up).normalized;
        float forwardDistance = Vector3.Dot(landing - muzzle.position, barrelForward);
        if (forwardDistance < .05f)
            landing += barrelForward * (.05f - forwardDistance);

        MortarShell shell = Instantiate(shellPrefab, muzzle.position, muzzle.rotation);
        shell.Launch(landing, damage, splashRadius, flightDuration, enemyMask, transform, followMuzzleAngle: true);
        nextShotTime = Time.time + attackInterval;
    }
}
