using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float rotateSpeed = 200f;
    [SerializeField] private float lifeTime = 5f;

    private Damageable target;
    private float damage;
    private float lifeTimer;

    public void Init(Damageable target, float damage)
    {
        this.target = target;
        this.damage = damage;
        lifeTimer = lifeTime;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (target == null || !target.IsAlive)
        {
            // Target gone — fly forward and expire
            transform.position += transform.forward * speed * Time.deltaTime;
            return;
        }

        // Homing: rotate towards target
        Vector3 dir = target.transform.position - transform.position;
        if (dir.sqrMagnitude < 0.1f)
        {
            Hit();
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        transform.position += transform.forward * speed * Time.deltaTime;

        // Check if close enough to hit
        if (dir.sqrMagnitude < 0.5f)
            Hit();
    }

    private void Hit()
    {
        if (target != null && target.IsAlive)
            target.TakeDamage(damage);

        Destroy(gameObject);
    }
}
