using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float rotateSpeed = 200f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float hitDistance = 0.5f;
    [Tooltip("Extra distance after the initial target point where a projectile without a live target is destroyed.")]
    [SerializeField] private float missOvershootDistance = 1f;

    private Damageable target;
    private float damage;
    private float lifeTimer;
    private Vector3 lockedTargetPoint;
    private Vector3 fallbackDirection;
    private float maxTravelDistance;
    private float traveledDistance;
    private bool homingEnabled;

    public void Init(Damageable target, float damage)
    {
        this.target = target;
        this.damage = damage;
        lifeTimer = lifeTime;

        lockedTargetPoint = target != null ? target.transform.position : transform.position + transform.forward;
        fallbackDirection = lockedTargetPoint - transform.position;
        if (fallbackDirection.sqrMagnitude < 0.001f)
            fallbackDirection = transform.forward;

        fallbackDirection.Normalize();
        maxTravelDistance = Vector3.Distance(transform.position, lockedTargetPoint) + Mathf.Max(0f, missOvershootDistance);
        traveledDistance = 0f;
        homingEnabled = target != null && target.IsAlive;

        if (fallbackDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(fallbackDirection);
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (!homingEnabled || target == null || !target.IsAlive)
        {
            // Target gone — fly forward and expire
            homingEnabled = false;
            FlyStraight();
            return;
        }

        // Homing: rotate towards target
        Vector3 dir = target.transform.position - transform.position;
        if (dir.sqrMagnitude <= hitDistance * hitDistance)
        {
            Hit();
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        Move(transform.forward);

        // Check if close enough to hit
        if (dir.sqrMagnitude <= hitDistance * hitDistance)
            Hit();
    }

    private void FlyStraight()
    {
        Move(fallbackDirection);

        if (traveledDistance >= maxTravelDistance)
            Destroy(gameObject);
    }

    private void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        float distance = speed * Time.deltaTime;
        transform.position += direction.normalized * distance;
        traveledDistance += distance;
    }

    private void Hit()
    {
        if (target != null && target.IsAlive)
            target.TakeDamage(damage);

        Destroy(gameObject);
    }
}
