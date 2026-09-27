using TMPro;
using UnityEngine;

public sealed class FpsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    private float elapsed;
    private int frames;

    public void Configure(TMP_Text text) => label = text;

    private void OnEnable()
    {
        GamePerformanceSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable() => GamePerformanceSettings.Changed -= Refresh;

    private void Refresh()
    {
        label.enabled = GamePerformanceSettings.ShowFps;
        elapsed = 0;
        frames = 0;
        label.text = "FPS: ...";
    }

    private void Update()
    {
        if (!GamePerformanceSettings.ShowFps) return;
        elapsed += Time.unscaledDeltaTime;
        frames++;
        if (elapsed < 0.25f) return;
        label.text = $"FPS: {Mathf.RoundToInt(frames / elapsed)}";
        elapsed = 0;
        frames = 0;
    }
}
