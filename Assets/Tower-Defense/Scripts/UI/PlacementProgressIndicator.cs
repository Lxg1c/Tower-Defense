using UnityEngine;
using UnityEngine.UI;

/// <summary>Projects a building point's hold progress onto the HUD, above scenery.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class PlacementProgressIndicator : MonoBehaviour
{
    [SerializeField] private TowerPlacementZone zone;
    private Camera worldCamera;
    [SerializeField] private Image fill;
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 64f);
    private CanvasGroup group;
    private RectTransform rect;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        worldCamera = Camera.main;
        rect = (RectTransform)transform;
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;
        if (zone == null || worldCamera == null || fill == null)
        {
            Debug.LogError("[PlacementProgressIndicator] Assign the zone and fill image, and tag the gameplay camera MainCamera.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        bool visible = zone != null && zone.isActiveAndEnabled && fill.fillAmount > 0f;
        if (!visible) { group.alpha = 0f; return; }
        Vector3 screen = worldCamera.WorldToScreenPoint(zone.transform.position);
        group.alpha = screen.z > 0f ? 1f : 0f;
        if (screen.z <= 0f) return;
        // Parent belongs to a Screen Space Overlay Canvas; offsets use its UI units.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rect.parent, screen, null, out Vector2 local);
        rect.anchoredPosition = local + screenOffset;
    }

    private void OnDisable()
    {
        if (group != null) group.alpha = 0f;
    }
}
