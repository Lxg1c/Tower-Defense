using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single UI component for the wave HUD:
///   - "Wave X / Y" label
///   - "Reward: N" label (coins awarded when the next wave is cleared)
///   - Start Wave button (visible only during Build phase)
///   - Optional "All waves completed!" label
///
/// Wire the WaveSpawner's events manually in the inspector, or let this
/// component find the singleton at runtime.
/// </summary>
public class WaveHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text waveLabel;
    [SerializeField] private TMP_Text rewardLabel;
    [SerializeField] private Button   startWaveButton;

    [Header("Formatting")]
    [SerializeField] private string waveFormat   = "Wave {0} / {1}";
    [SerializeField] private string rewardFormat = "Reward: {0}";

    private bool modalOpen;
    private WaveSpawner spawner;

    private void OnEnable()
    {
        spawner = WaveSpawner.Instance;
        if (spawner == null) { Invoke(nameof(LateBind), 0.1f); return; }

        Bind();
    }

    private void LateBind()
    {
        spawner = WaveSpawner.Instance;
        if (spawner != null) Bind();
    }

    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.onBuildPhaseStarted.RemoveListener(OnBuild);
            spawner.onCombatPhaseStarted.RemoveListener(OnCombat);
            spawner.onAllWavesCompleted.RemoveListener(OnAllDone);
        }
        if (startWaveButton != null)
            startWaveButton.onClick.RemoveListener(OnStartClicked);
        if (TowerSelectionModal.Instance != null)
        {
            TowerSelectionModal.Instance.onOpened.RemoveListener(OnModalOpened);
            TowerSelectionModal.Instance.onClosed.RemoveListener(OnModalClosed);
        }
    }

    private void Bind()
    {
        spawner.onBuildPhaseStarted.AddListener(OnBuild);
        spawner.onCombatPhaseStarted.AddListener(OnCombat);
        spawner.onAllWavesCompleted.AddListener(OnAllDone);

        if (startWaveButton != null)
            startWaveButton.onClick.AddListener(OnStartClicked);

        if (TowerSelectionModal.Instance != null)
        {
            TowerSelectionModal.Instance.onOpened.AddListener(OnModalOpened);
            TowerSelectionModal.Instance.onClosed.AddListener(OnModalClosed);
            modalOpen = TowerSelectionModal.Instance.IsOpen;
        }

        // Initial state sync
        switch (spawner.CurrentPhase)
        {
            case WaveSpawner.Phase.Build:
                var w = spawner.NextWave;
                OnBuild(spawner.NextWaveIndex, spawner.WaveCount, w != null ? w.coinReward : 0);
                break;
            case WaveSpawner.Phase.Combat:
                OnCombat(spawner.NextWaveIndex, spawner.WaveCount, 0);
                break;
            case WaveSpawner.Phase.AllCompleted:
                OnAllDone();
                break;
        }
    }

    private void OnBuild(int nextIdx, int total, int reward)
    {
        if (waveLabel != null)
            waveLabel.text = string.Format(waveFormat, nextIdx + 1, total);
        if (rewardLabel != null)
            rewardLabel.text = string.Format(rewardFormat, reward);
        SetButtonVisible(!modalOpen);
    }

    private void OnModalOpened()
    {
        modalOpen = true;
        if (spawner != null && spawner.IsBuildPhase)
            SetButtonVisible(false);
    }

    private void OnModalClosed()
    {
        modalOpen = false;
        if (spawner != null && spawner.IsBuildPhase)
            SetButtonVisible(true);
    }

    private void OnCombat(int idx, int total, int reward)
    {
        if (waveLabel != null)
            waveLabel.text = string.Format(waveFormat, idx + 1, total);
        if (rewardLabel != null)
            rewardLabel.text = string.Format(rewardFormat, reward);
        SetButtonVisible(false);
    }

    private void OnAllDone()
    {
        if (waveLabel != null) waveLabel.text = "";
        if (rewardLabel != null) rewardLabel.text = "";
        SetButtonVisible(false);
    }

    private void SetButtonVisible(bool on)
    {
        if (startWaveButton != null)
            startWaveButton.gameObject.SetActive(on);
    }

    private void OnStartClicked()
    {
        if (spawner != null) spawner.StartNextWave();
    }
}
