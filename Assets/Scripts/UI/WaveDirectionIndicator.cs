using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WaveDirectionIndicator : MonoBehaviour
{
    [SerializeField] private Image mobIcon;
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private string countFormat = "x{0}";
    [SerializeField] private float screenEdgePadding = 72f;
    [SerializeField] private Vector2 visibleSpawnPointOffset = new(0f, 48f);
    [SerializeField] private Vector2 stackOffset = new(0f, -56f);

    private RectTransform root;
    private RectTransform canvasRect;
    private Camera canvasCamera;
    private Camera worldCamera;
    private Transform spawnPoint;
    private int stackIndex;

    private void Awake()
    {
        if (root == null)
            root = (RectTransform)transform;
    }

    public void Bind(WaveSpawner.WavePreviewEntry entry)
    {
        spawnPoint = entry.spawnPoint;
        stackIndex = Mathf.Max(0, entry.stackIndex);
        ResolveSceneReferences();

        if (mobIcon != null)
        {
            mobIcon.sprite = entry.icon;
            mobIcon.enabled = mobIcon.sprite != null;
        }

        if (countLabel != null)
            countLabel.text = string.Format(countFormat, Mathf.Max(0, entry.count));

        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (root == null || spawnPoint == null)
            return;

        ResolveSceneReferences();
        if (canvasRect == null)
            return;

        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        if (cam == null)
            return;

        Vector3 viewport = cam.WorldToViewportPoint(spawnPoint.position);
        bool behindCamera = viewport.z < 0f;
        Vector2 rawScreenPoint = new(viewport.x * Screen.width, viewport.y * Screen.height);
        Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);

        if (behindCamera)
            rawScreenPoint = screenCenter - (rawScreenPoint - screenCenter);

        bool spawnPointVisible = !behindCamera
            && viewport.x >= 0f && viewport.x <= 1f
            && viewport.y >= 0f && viewport.y <= 1f;

        Vector2 markerScreenPoint = spawnPointVisible
            ? rawScreenPoint + visibleSpawnPointOffset
            : ClampToScreenEdge(rawScreenPoint, screenCenter);
        markerScreenPoint += stackOffset * stackIndex;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, markerScreenPoint, canvasCamera, out Vector2 localPoint))
            return;

        root.anchoredPosition = localPoint;
    }

    private Vector2 ClampToScreenEdge(Vector2 screenPoint, Vector2 screenCenter)
    {
        Vector2 direction = screenPoint - screenCenter;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.up;

        float minX = screenEdgePadding;
        float maxX = Screen.width - screenEdgePadding;
        float minY = screenEdgePadding;
        float maxY = Screen.height - screenEdgePadding;

        float scaleX = Mathf.Abs(direction.x) > 0.001f
            ? (direction.x > 0f
                ? (maxX - screenCenter.x) / direction.x
                : (minX - screenCenter.x) / direction.x)
            : float.PositiveInfinity;
        float scaleY = Mathf.Abs(direction.y) > 0.001f
            ? (direction.y > 0f
                ? (maxY - screenCenter.y) / direction.y
                : (minY - screenCenter.y) / direction.y)
            : float.PositiveInfinity;

        float scale = Mathf.Min(Mathf.Abs(scaleX), Mathf.Abs(scaleY));
        Vector2 clamped = screenCenter + direction * scale;
        clamped.x = Mathf.Clamp(clamped.x, minX, maxX);
        clamped.y = Mathf.Clamp(clamped.y, minY, maxY);
        return clamped;
    }

    private void ResolveSceneReferences()
    {
        if (root == null)
            root = transform as RectTransform;

        if (canvasRect == null || worldCamera == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvasRect = canvas.transform as RectTransform;
                canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                worldCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }
        }

        if (worldCamera == null)
            worldCamera = Camera.main;
    }
}
