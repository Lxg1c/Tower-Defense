using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class WaveDirectionIndicator : MonoBehaviour
{
    [SerializeField] private Image mobIcon;
    [SerializeField] private TMP_Text countLabel;
    [SerializeField] private string countFormat = "x{0}";
    [SerializeField] private float screenEdgePadding = 72f;
    [SerializeField] private Vector2 visibleSpawnPointOffset = new(0f, 48f);
    [SerializeField] private Vector2 stackOffset = new(0f, -56f);
    [SerializeField] private Vector2 visibleCardSize = new(154f, 76f);
    [SerializeField] private Vector2 visibleIconSize = new(68f, 68f);
    [SerializeField] private Vector2 visibleIconPosition = new(-34f, 0f);
    [SerializeField] private Vector2 visibleLabelSize = new(58f, 54f);
    [SerializeField] private Vector2 visibleLabelPosition = new(35f, 0f);
    [SerializeField] private float visibleLabelFontSize = 25f;
    [SerializeField] private RectTransform directionArrow;
    [SerializeField] private Sprite offscreenCircleSprite;
    [SerializeField] private Image offscreenRing;
    [SerializeField] private Vector2 offscreenBadgeSize = new(108f, 108f);
    [SerializeField, Range(0f, 0.45f)] private float bottomReservedFraction = 0.36f;

    private RectTransform root;
    private RectTransform canvasRect;
    private Camera canvasCamera;
    private Camera worldCamera;
    private Transform spawnPoint;
    private int stackIndex;
    private int stackCount;
    private int visibleCount;
    private int totalAtSpawn;
    private Sprite visibleBackgroundSprite;
    private Image background;
    private CanvasGroup canvasGroup;
    private bool? lastOffscreen;

    private void Awake()
    {
        root = (RectTransform)transform;
        background = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        visibleBackgroundSprite = background != null ? background.sprite : null;
    }

    public void Bind(WaveSpawner.WavePreviewEntry entry)
    {
        Bind(entry, entry.count);
    }

    public void Bind(WaveSpawner.WavePreviewEntry entry, int spawnTotal)
    {
        spawnPoint = entry.spawnPoint;
        stackIndex = Mathf.Max(0, entry.stackIndex);
        stackCount = Mathf.Max(1, entry.stackCount);
        totalAtSpawn = Mathf.Max(0, spawnTotal);
        lastOffscreen = null;
        ResolveSceneReferences();

        if (mobIcon != null)
        {
            mobIcon.sprite = entry.icon;
            mobIcon.enabled = mobIcon.sprite != null;
        }

        visibleCount = Mathf.Max(0, entry.count);

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

        // Only the first entry at a spawn represents that spawn off screen.
        canvasGroup.alpha = spawnPointVisible || stackIndex == 0 ? 1f : 0f;
        if (canvasGroup.alpha == 0f)
            return;

        SetPresentation(!spawnPointVisible);

        float edgeX = Mathf.Max(screenEdgePadding, root.rect.width * root.lossyScale.x * 0.5f + 8f);
        float arrowHalfSize = directionArrow != null
            ? Mathf.Max(directionArrow.rect.width, directionArrow.rect.height) * root.lossyScale.y * 0.5f
            : 0f;
        float edgeY = spawnPointVisible
            ? Mathf.Max(screenEdgePadding, root.rect.height * root.lossyScale.y * 0.5f + 40f)
            : Mathf.Max(screenEdgePadding,
                offscreenBadgeSize.y * root.lossyScale.y * 0.58f + arrowHalfSize + 8f);

        Vector2 markerScreenPoint = spawnPointVisible
            ? rawScreenPoint + visibleSpawnPointOffset
            : ClampToScreenEdge(rawScreenPoint, screenCenter, edgeX, edgeY);
        // Visible spawn cards avoid touch controls; edge badges stay at the screen edge.
        Rect safe = Screen.safeArea;
        markerScreenPoint.x = Mathf.Clamp(markerScreenPoint.x,
            safe.xMin + edgeX, safe.xMax - edgeX);
        float bottom = safe.yMin + edgeY;
        if (spawnPointVisible)
            bottom += safe.height * bottomReservedFraction;
        float top = safe.yMax - edgeY;
        float spacing = Mathf.Max(Mathf.Abs(stackOffset.y) * root.lossyScale.y,
            root.rect.height * root.lossyScale.y + 8f);
        int displayedCount = spawnPointVisible ? stackCount : 1;
        if (displayedCount > 1)
            spacing = Mathf.Min(spacing, Mathf.Max(0f, top - bottom) / (displayedCount - 1));
        float firstY = Mathf.Clamp(markerScreenPoint.y, bottom,
            Mathf.Max(bottom, top - spacing * (displayedCount - 1)));
        markerScreenPoint.y = firstY + spacing * (spawnPointVisible ? stackIndex : 0);
        if (!spawnPointVisible && directionArrow != null)
        {
            Vector2 direction = rawScreenPoint - markerScreenPoint;
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.up;
            directionArrow.anchoredPosition = direction.normalized * (offscreenBadgeSize.x * 0.58f);
            directionArrow.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, markerScreenPoint, canvasCamera, out Vector2 localPoint))
            return;

        root.anchoredPosition = localPoint;
    }

    private void SetPresentation(bool offscreen)
    {
        if (lastOffscreen == offscreen)
            return;
        lastOffscreen = offscreen;
        root.sizeDelta = offscreen ? offscreenBadgeSize : visibleCardSize;
        if (background != null)
            background.sprite = offscreen ? offscreenCircleSprite : visibleBackgroundSprite;
        if (offscreenRing != null)
            offscreenRing.enabled = offscreen;
        if (mobIcon != null)
        {
            mobIcon.enabled = !offscreen && mobIcon.sprite != null;
            if (!offscreen)
            {
                mobIcon.rectTransform.anchoredPosition = visibleIconPosition;
                mobIcon.rectTransform.sizeDelta = visibleIconSize;
            }
        }
        if (countLabel != null)
        {
            countLabel.text = offscreen ? totalAtSpawn.ToString() : string.Format(countFormat, visibleCount);
            countLabel.fontSize = offscreen ? 34f : visibleLabelFontSize;
            countLabel.rectTransform.anchoredPosition = offscreen ? Vector2.zero : visibleLabelPosition;
            countLabel.rectTransform.sizeDelta = offscreen ? offscreenBadgeSize : visibleLabelSize;
            countLabel.margin = Vector4.zero;
            countLabel.alignment = TextAlignmentOptions.Center;
        }
        if (directionArrow != null)
            directionArrow.gameObject.SetActive(offscreen);
    }

    private Vector2 ClampToScreenEdge(Vector2 screenPoint, Vector2 screenCenter,
        float edgeX, float edgeY)
    {
        Vector2 direction = screenPoint - screenCenter;
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.up;

        float minX = edgeX;
        float maxX = Screen.width - edgeX;
        float minY = edgeY;
        float maxY = Screen.height - edgeY;

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
