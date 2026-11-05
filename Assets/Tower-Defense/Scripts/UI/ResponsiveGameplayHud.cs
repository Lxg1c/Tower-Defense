using UnityEngine;

/// <summary>Uses the authored touch layout on mobile builds and a compact layout on desktop builds.</summary>
[DisallowMultipleComponent]
public sealed class ResponsiveGameplayHud : MonoBehaviour
{
    public enum EditorPreviewMode { Automatic, Mobile, Desktop }

    [Tooltip("Editor-only preview. Player builds always use their target platform's layout.")]
    [SerializeField] private EditorPreviewMode editorPreview = EditorPreviewMode.Automatic;
    [SerializeField] private RectTransform floatingJoystick;
    [SerializeField] private RectTransform rightSide;
    [SerializeField] private RectTransform ultimateButton;
    [SerializeField] private RectTransform homeButton;
    [SerializeField] private RectTransform rallyButton;
    [SerializeField] private RectTransform effectStatusRow;
    [SerializeField] private GameObject desktopHint;

    public bool IsDesktopLayout { get; private set; }
    public Vector2 DesktopActionAreaPixels => rightSide == null ? Vector2.zero : new Vector2(
        rightSide.rect.width * rightSide.lossyScale.x,
        rightSide.rect.height * rightSide.lossyScale.y);

    private RectLayout mobileRightSide;
    private RectLayout mobileUltimateButton;
    private RectLayout mobileHomeButton;
    private RectLayout mobileRallyButton;
    private RectLayout mobileEffectStatusRow;
    private bool mobileJoystickActive;

    private void Awake()
    {
        if (floatingJoystick == null || rightSide == null || ultimateButton == null ||
            homeButton == null || rallyButton == null ||
            effectStatusRow == null || desktopHint == null)
        {
            Debug.LogError("[ResponsiveGameplayHud] Assign the touch controls, action buttons, status row and desktop hint.", this);
            enabled = false;
            return;
        }

        mobileRightSide = new RectLayout(rightSide);
        mobileUltimateButton = new RectLayout(ultimateButton);
        mobileHomeButton = new RectLayout(homeButton);
        mobileRallyButton = new RectLayout(rallyButton);
        mobileEffectStatusRow = new RectLayout(effectStatusRow);
        mobileJoystickActive = floatingJoystick.gameObject.activeSelf;

        ApplyLayout(UseDesktopLayout());
    }

    private void Update()
    {
        bool desktop = UseDesktopLayout();
        if (desktop != IsDesktopLayout)
            ApplyLayout(desktop);
        else if (desktop && floatingJoystick.gameObject.activeSelf)
            floatingJoystick.gameObject.SetActive(false);
    }

    private void ApplyLayout(bool desktop)
    {
        IsDesktopLayout = desktop;
        desktopHint.SetActive(desktop);
        floatingJoystick.gameObject.SetActive(!desktop && mobileJoystickActive);
        if (!desktop)
        {
            mobileRightSide.Apply(rightSide);
            mobileUltimateButton.Apply(ultimateButton);
            mobileHomeButton.Apply(homeButton);
            mobileRallyButton.Apply(rallyButton);
            mobileEffectStatusRow.Apply(effectStatusRow);
            return;
        }

        rightSide.sizeDelta = new Vector2(420f, 420f);
        Place(ultimateButton, -125f, 125f, 0.6f);
        Place(homeButton, -325f, 95f, 0.6f);
        Place(rallyButton, -100f, 320f, 0.55f);

        effectStatusRow.anchorMin = effectStatusRow.anchorMax = new Vector2(0f, 1f);
        effectStatusRow.pivot = new Vector2(0f, 1f);
        effectStatusRow.anchoredPosition = new Vector2(24f, -115f);
        effectStatusRow.localScale = Vector3.one * 0.75f;
    }

    private bool UseDesktopLayout()
    {
#if UNITY_EDITOR
        if (editorPreview == EditorPreviewMode.Mobile) return false;
        if (editorPreview == EditorPreviewMode.Desktop) return true;
        var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
        return target != UnityEditor.BuildTarget.Android && target != UnityEditor.BuildTarget.iOS;
#elif UNITY_ANDROID || UNITY_IOS
        return false;
#else
        return true;
#endif
    }

    private static void Place(RectTransform rect, float x, float y, float scale)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.localScale = Vector3.one * scale;
    }

    private readonly struct RectLayout
    {
        private readonly Vector2 anchorMin;
        private readonly Vector2 anchorMax;
        private readonly Vector2 pivot;
        private readonly Vector2 anchoredPosition;
        private readonly Vector2 sizeDelta;
        private readonly Vector3 localScale;

        public RectLayout(RectTransform rect)
        {
            anchorMin = rect.anchorMin;
            anchorMax = rect.anchorMax;
            pivot = rect.pivot;
            anchoredPosition = rect.anchoredPosition;
            sizeDelta = rect.sizeDelta;
            localScale = rect.localScale;
        }

        public void Apply(RectTransform rect)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.localScale = localScale;
        }
    }
}
