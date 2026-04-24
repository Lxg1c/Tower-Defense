using UnityEngine;

/// <summary>
/// Swaps the player's material when entering/exiting ghost state.
/// Hook the methods up to PlayerHealth's onGhostEntered / onRespawned events,
/// or let it auto-subscribe via the Inspector reference.
/// </summary>
[DisallowMultipleComponent]
public class GhostVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Renderer     targetRenderer;

    [Header("Materials")]
    private Material aliveMaterial;
    [SerializeField] private Material ghostMaterial;

    private void Awake()
    {
        if (playerHealth   == null) playerHealth   = GetComponent<PlayerHealth>();
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();

        
        aliveMaterial = targetRenderer.sharedMaterial;
    }

    private void OnEnable()
    {
        if (playerHealth == null) return;
        playerHealth.onGhostEntered.AddListener(EnableGhost);
        playerHealth.onRespawned.AddListener(DisableGhost);
    }

    private void OnDisable()
    {
        if (playerHealth == null) return;
        playerHealth.onGhostEntered.RemoveListener(EnableGhost);
        playerHealth.onRespawned.RemoveListener(DisableGhost);
    }

    public void EnableGhost()
    {
        if (targetRenderer != null && ghostMaterial != null)
            targetRenderer.material = ghostMaterial;
    }

    public void DisableGhost()
    {
        if (targetRenderer != null && aliveMaterial != null)
            targetRenderer.material = aliveMaterial;
    }
}
