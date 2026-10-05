using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerShieldVisual : MonoBehaviour
{
    [SerializeField] private MeshRenderer sphereRenderer;
    private PlayerHealth health;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        if (sphereRenderer == null)
        {
            var spherePrefab = Resources.Load<GameObject>("PlayerShieldSphere");
            if (spherePrefab != null)
            {
                var sphere = Instantiate(spherePrefab, transform, false);
                sphere.name = "ShieldSphere";
                sphereRenderer = sphere.GetComponent<MeshRenderer>();
            }
            if (sphereRenderer == null)
            {
                Debug.LogError("[PlayerShieldVisual] PlayerShieldSphere resource needs a MeshRenderer.", this);
                enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        if (health == null || sphereRenderer == null) return;
        health.onShieldChanged.AddListener(OnShieldChanged);
        OnShieldChanged(health.CurrentShield);
    }

    private void OnDisable()
    {
        if (health != null) health.onShieldChanged.RemoveListener(OnShieldChanged);
    }

    private void OnShieldChanged(float current)
    {
        sphereRenderer.enabled = current > 0f;
    }
}
