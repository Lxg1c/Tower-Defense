using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TowerHealth))]
[RequireComponent(typeof(Shooter))]
public class Tower : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform turretPivot;
    [SerializeField] private bool faceClosestSpawnOnStart = true;

    private Shooter shooter;

    private void Awake()
    {
        shooter = GetComponent<Shooter>();
    }

    private void Start()
    {
        FaceClosestSpawnPoint();
    }

    private void Update()
    {
        if (!shooter.HasTarget)
            return;

        Transform pivot = turretPivot != null ? turretPivot : transform;
        Quaternion targetRotation = Quaternion.LookRotation(shooter.TargetDirection);
        pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void FaceClosestSpawnPoint()
    {
        if (!faceClosestSpawnOnStart)
            return;

        if (WaveSpawner.Instance == null)
        {
            Invoke(nameof(FaceClosestSpawnPoint), 0.1f);
            return;
        }

        Transform spawnPoint = WaveSpawner.Instance.GetClosestSpawnPoint(transform.position);
        if (spawnPoint == null)
            return;

        Transform pivot = turretPivot != null ? turretPivot : transform;
        Vector3 direction = spawnPoint.position - pivot.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        pivot.rotation = Quaternion.LookRotation(direction.normalized);
    }
}
