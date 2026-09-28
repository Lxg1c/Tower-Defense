using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public sealed class SlowAmmoPickup : MonoBehaviour
{
    [SerializeField, Min(0f)] private float lifetime = 12f;
    [SerializeField] private float rotationSpeed = 90f;
    private float durationSeconds;

    public void Configure(float duration)
    {
        durationSeconds = Mathf.Max(0f, duration);
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void Update() => transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

    private void OnTriggerEnter(Collider other)
    {
        SlowAmmoInventory inventory = other.GetComponentInParent<SlowAmmoInventory>();
        if (inventory == null) return;
        PlayerHealth health = inventory.GetComponent<PlayerHealth>();
        if (health != null && !health.IsAlive) return;
        if (inventory.Add(durationSeconds) > 0f) Destroy(gameObject);
    }
}
