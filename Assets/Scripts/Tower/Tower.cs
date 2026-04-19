using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TowerHealth))]
[RequireComponent(typeof(Shooter))]
public class Tower : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private Transform turretPivot;

    private Shooter shooter;

    private void Awake()
    {
        shooter = GetComponent<Shooter>();
    }

    private void Update()
    {
        if (!shooter.HasTarget)
            return;

        Transform pivot = turretPivot != null ? turretPivot : transform;
        Quaternion targetRotation = Quaternion.LookRotation(shooter.TargetDirection);
        pivot.rotation = Quaternion.Slerp(pivot.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
