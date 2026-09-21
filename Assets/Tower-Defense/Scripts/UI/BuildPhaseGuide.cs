using TMPro;
using UnityEngine;

/// <summary>Shows the next build-phase action without owning any session rules.</summary>
[DisallowMultipleComponent]
public sealed class BuildPhaseGuide : MonoBehaviour
{
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject panel;

    private void OnEnable()
    {
        if (spawner == null || label == null || panel == null)
        {
            Debug.LogError("[BuildPhaseGuide] Assign the session spawner, label and panel.", this);
            enabled = false;
            return;
        }
        spawner.onBuildPhaseStarted.AddListener(OnPhase);
        spawner.onCombatPhaseStarted.AddListener(OnPhase);
        spawner.onAllWavesCompleted.AddListener(Refresh);
        spawner.onDefeated.AddListener(Refresh);
        Base.OnBaseChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.onBuildPhaseStarted.RemoveListener(OnPhase);
            spawner.onCombatPhaseStarted.RemoveListener(OnPhase);
            spawner.onAllWavesCompleted.RemoveListener(Refresh);
            spawner.onDefeated.RemoveListener(Refresh);
        }
        Base.OnBaseChanged -= Refresh;
    }

    private void OnPhase(int index, int count, int reward) => Refresh();

    private void Refresh()
    {
        panel.SetActive(spawner.IsBuildPhase);
        label.text = Base.Instance == null
            ? "Stand on the home marker to build your base"
            : "Prepare your defenses";
    }
}
