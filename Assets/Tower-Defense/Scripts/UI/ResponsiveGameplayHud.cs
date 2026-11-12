using UnityEngine;

/// <summary>Selects the UI Toolkit layout for the build target or Editor preview.</summary>
[DisallowMultipleComponent]
public sealed class ResponsiveGameplayHud : MonoBehaviour
{
    public enum EditorPreviewMode { Automatic, Mobile, Desktop }

    [Tooltip("Editor-only preview. Player builds use their target platform's layout.")]
    [SerializeField] private EditorPreviewMode editorPreview = EditorPreviewMode.Automatic;

    public bool IsDesktopLayout { get; private set; }
    private void Awake() => IsDesktopLayout = UseDesktopLayout();
    private void Update() => IsDesktopLayout = UseDesktopLayout();

    internal bool UseDesktopLayout()
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
}
