using UnityEngine;

/// <summary>
/// Toggles automatic firing.
/// Disable this component to stop the Shooter (e.g. when the player is a ghost).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Shooter))]
public class AutoShooter : MonoBehaviour
{
    private Shooter shooter;

    public bool    HasTarget       => shooter != null && shooter.HasTarget;
    public Vector3 TargetDirection => shooter != null ? shooter.TargetDirection : Vector3.forward;

    private void Awake()
    {
        shooter = GetComponent<Shooter>();
    }

    private void OnEnable()
    {
        if (shooter != null) shooter.enabled = true;
    }

    private void OnDisable()
    {
        if (shooter != null) shooter.enabled = false;
    }
}
