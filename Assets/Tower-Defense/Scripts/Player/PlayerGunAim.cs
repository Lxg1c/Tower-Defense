using UnityEngine;

/// <summary>Adds vertical weapon aiming on top of the robot's animated pose.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-10)]
public sealed class PlayerGunAim : MonoBehaviour
{
    [SerializeField] private Shooter shooter;
    [SerializeField] private Transform firstGun;
    [SerializeField] private Transform secondGun;
    [SerializeField, Range(0f, 89f)] private float maxElevation = 80f;
    [SerializeField, Range(0f, 89f)] private float maxDepression = 45f;
    [SerializeField, Min(1f)] private float rotationSpeed = 180f;

    private readonly Quaternion[] animatedRotations = new Quaternion[2];
    private readonly float[] pitches = new float[2];
    private bool poseApplied;

    private void Awake()
    {
        if (shooter == null || firstGun == null || secondGun == null || firstGun == secondGun)
        {
            Debug.LogError("[PlayerGunAim] Assign a shooter and two distinct gun bones.", this);
            enabled = false;
        }
    }

    // Remove our previous offset before the Animator evaluates the next pose.
    // This also prevents accumulation when an animation doesn't key a gun bone.
    private void Update() => RestoreAnimatedPose();

    private void LateUpdate()
    {
        Damageable target = shooter.isActiveAndEnabled ? shooter.CurrentTarget : null;
        if (target != null && !target.IsTargetable) target = null;
        Aim(firstGun, 0, target);
        Aim(secondGun, 1, target);
        poseApplied = true;
    }

    private void Aim(Transform gun, int index, Damageable target)
    {
        float desiredPitch = 0f;
        if (target != null)
        {
            Vector3 direction = target.transform.position - gun.position;
            float horizontalDistance = new Vector2(direction.x, direction.z).magnitude;
            desiredPitch = -Mathf.Atan2(direction.y, horizontalDistance) * Mathf.Rad2Deg;
            desiredPitch = Mathf.Clamp(desiredPitch, -maxElevation, maxDepression);
        }

        pitches[index] = Mathf.MoveTowards(pitches[index], desiredPitch, rotationSpeed * Time.deltaTime);
        animatedRotations[index] = gun.localRotation;
        // The imported bones have different local axes. Pitch both around the
        // player's world-space right axis, preserving their authored orientation.
        gun.rotation = Quaternion.AngleAxis(pitches[index], transform.right) * gun.rotation;
    }

    private void RestoreAnimatedPose()
    {
        if (!poseApplied) return;
        if (firstGun != null) firstGun.localRotation = animatedRotations[0];
        if (secondGun != null) secondGun.localRotation = animatedRotations[1];
        poseApplied = false;
    }

    private void OnDisable()
    {
        RestoreAnimatedPose();
        pitches[0] = pitches[1] = 0f;
    }
}
