using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
public sealed class CompanionTurretPickup : MonoBehaviour
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
        CompanionTurretController controller = other.GetComponentInParent<CompanionTurretController>();
        if (controller != null && controller.Grant(durationSeconds)) Destroy(gameObject);
    }
}
