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
        if (muzzle == null || shellPrefab == null || enemyMask.value == 0)
        {
            Debug.LogError("[MortarTower] Assign a muzzle, MortarShell prefab, and enemy mask.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (Time.time < nextShotTime)
            return;

        Damageable target = detection.GetClosest();
        if (target == null)
            return;

        Vector3 landing = target.transform.position;
        MortarShell shell = Instantiate(shellPrefab, muzzle.position, Quaternion.identity);
        shell.Launch(landing, damage, splashRadius, flightDuration, enemyMask);
        nextShotTime = Time.time + attackInterval;
    }
}
