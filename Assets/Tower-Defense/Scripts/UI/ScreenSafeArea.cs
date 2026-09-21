using UnityEngine;

/// <summary>Fits a direct child of a full-screen Canvas to the device safe area.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public sealed class ScreenSafeArea : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastArea;
    private Vector2Int lastSize;

    private void OnEnable()
    {
        rect = (RectTransform)transform;
        Apply();
    }
    private void Update()
    {
        if (lastArea != Screen.safeArea || lastSize.x != Screen.width || lastSize.y != Screen.height)
            Apply();
    }

    private void Apply()
    {
        if (Screen.width == 0 || Screen.height == 0) return;
        lastArea = Screen.safeArea;
        lastSize = new Vector2Int(Screen.width, Screen.height);
        rect.anchorMin = new Vector2(lastArea.xMin / Screen.width, lastArea.yMin / Screen.height);
        rect.anchorMax = new Vector2(lastArea.xMax / Screen.width, lastArea.yMax / Screen.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
