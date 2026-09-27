using TMPro;
using UnityEngine;

public sealed class FpsSettingsButton : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button button;
    [SerializeField] private TMP_Text label;

    public void Configure(UnityEngine.UI.Button control, TMP_Text text)
    {
        button = control;
        label = text;
    }

    private void OnEnable()
    {
        button.onClick.AddListener(GamePerformanceSettings.ToggleFps);
        GamePerformanceSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(GamePerformanceSettings.ToggleFps);
        GamePerformanceSettings.Changed -= Refresh;
    }

    private void Refresh() => label.text = GamePerformanceSettings.ShowFps ? "FPS: ON" : "FPS: OFF";
}
