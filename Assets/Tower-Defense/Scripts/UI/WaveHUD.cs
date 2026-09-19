using System.Collections.Generic;
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
    [SerializeField] private TMP_Text enemiesLabel;
    [SerializeField] private TMP_Text passiveIncomeLabel;
    [SerializeField] private Button   startWaveButton;
    [SerializeField] private TMP_Text startWaveButtonLabel;

    [Header("Wave Direction Preview")]
    [SerializeField] private RectTransform directionPreviewRoot;
    [SerializeField] private WaveDirectionIndicator directionIndicatorPrefab;

    [Header("Formatting")]
    [SerializeField] private string waveFormat   = "Wave {0} / {1}";
    [SerializeField] private string rewardFormat = "Reward: {0}";
    [SerializeField] private string enemiesFormat = "Enemies: {0}";
    [SerializeField] private string passiveIncomeFormat = "{0}/w";
    [SerializeField] private string startWaveFormat = "Start {0}";

    private bool modalOpen;
    private WaveSpawner spawner;
    private readonly List<WaveSpawner.WavePreviewEntry> previewEntries = new();
    private readonly List<WaveDirectionIndicator> directionIndicators = new();

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

        MineIncome.onIncomeChanged.RemoveListener(UpdatePassiveIncomeLabel);
        BaseUpgrade.OnTownHallChanged -= OnTownHallChanged;
        Base.OnBaseChanged -= OnBaseChanged;

        if (startWaveButton != null)
            startWaveButton.onClick.RemoveListener(OnStartClicked);
        if (TowerSelectionModal.Instance != null)
        {
            TowerSelectionModal.Instance.onOpened.RemoveListener(OnModalOpened);
            TowerSelectionModal.Instance.onClosed.RemoveListener(OnModalClosed);
        }

        ClearDirectionPreview();
    }

    private void Bind()
    {
        ResolveStartWaveButtonLabel();

        spawner.onBuildPhaseStarted.AddListener(OnBuild);
        spawner.onCombatPhaseStarted.AddListener(OnCombat);
        spawner.onAllWavesCompleted.AddListener(OnAllDone);
        MineIncome.onIncomeChanged.AddListener(UpdatePassiveIncomeLabel);
        BaseUpgrade.OnTownHallChanged += OnTownHallChanged;
        Base.OnBaseChanged += OnBaseChanged;

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

        UpdateEnemyCountLabel(spawner.NextWave);
        UpdatePassiveIncomeLabel();
        RefreshDirectionPreview();
    }

    private void Update()
    {
        if (spawner != null && spawner.IsBuildPhase)
            RefreshStartButtonVisibility();
    }

    private void OnBuild(int nextIdx, int total, int reward)
    {
        if (waveLabel != null)
            waveLabel.text = string.Format(waveFormat, nextIdx + 1, total);
        if (rewardLabel != null)
            rewardLabel.text = string.Format(rewardFormat, reward);
        UpdateStartWaveButtonLabel(reward);
        UpdateEnemyCountLabel(spawner != null ? spawner.NextWave : null);
        UpdatePassiveIncomeLabel();
        RefreshDirectionPreview();
        SetButtonVisible(!modalOpen);
    }

    private void OnModalOpened()
    {
        modalOpen = true;
        RefreshStartButtonVisibility();
    }

    private void OnModalClosed()
    {
        modalOpen = false;
        RefreshStartButtonVisibility();
    }

    private void OnTownHallChanged()
    {
        RefreshStartButtonVisibility();
    }

    private void OnBaseChanged()
    {
        RefreshStartButtonVisibility();
        RefreshDirectionPreview();
    }

    private void OnCombat(int idx, int total, int reward)
    {
        if (waveLabel != null)
            waveLabel.text = string.Format(waveFormat, idx + 1, total);
        if (rewardLabel != null)
            rewardLabel.text = string.Format(rewardFormat, reward);
        UpdateEnemyCountLabel(GetWave(idx));
        UpdatePassiveIncomeLabel();
        ClearDirectionPreview();
        SetButtonVisible(false);
    }

    private void OnAllDone()
    {
        if (waveLabel != null) waveLabel.text = "";
        if (rewardLabel != null) rewardLabel.text = "";
        if (enemiesLabel != null) enemiesLabel.text = "";
        UpdatePassiveIncomeLabel();
        ClearDirectionPreview();
        SetButtonVisible(false);
    }

    private void SetButtonVisible(bool on)
    {
        if (startWaveButton != null)
            startWaveButton.gameObject.SetActive(on && Base.Instance != null);
    }

    private void RefreshStartButtonVisibility()
    {
        bool canShow = spawner != null
            && spawner.IsBuildPhase
            && Base.Instance != null
            && !IsAnyModalOpen();

        SetButtonVisible(canShow);
    }

    private bool IsAnyModalOpen()
    {
        return modalOpen
            || (TowerSelectionModal.Instance != null && TowerSelectionModal.Instance.IsOpen)
            || (TowerUpgradeModal.Instance != null && TowerUpgradeModal.Instance.IsOpen)
            || (BaseUpgradeModal.Instance != null && BaseUpgradeModal.Instance.IsOpen);
    }

    private void ResolveStartWaveButtonLabel()
    {
        if (startWaveButtonLabel == null && startWaveButton != null)
            startWaveButtonLabel = startWaveButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void UpdateStartWaveButtonLabel(int reward)
    {
        ResolveStartWaveButtonLabel();

        if (startWaveButtonLabel != null)
            startWaveButtonLabel.text = FormatStartWaveButton(reward);
    }

    private string FormatStartWaveButton(int reward)
    {
        if (string.IsNullOrEmpty(startWaveFormat))
            return reward.ToString();

        try
        {
            return string.Format(startWaveFormat, reward);
        }
        catch (System.FormatException)
        {
            return $"Start {reward}";
        }
    }

    private void UpdateEnemyCountLabel(WaveSpawner.Wave wave)
    {
        if (enemiesLabel == null)
            return;

        enemiesLabel.text = string.Format(enemiesFormat, GetWaveMobCount(wave));
    }

    private void UpdatePassiveIncomeLabel()
    {
        if (passiveIncomeLabel != null)
            passiveIncomeLabel.text = string.Format(passiveIncomeFormat, MineIncome.TotalCoinsPerWave);
    }

    private WaveSpawner.Wave GetWave(int index)
    {
        if (spawner == null || index < 0 || index >= spawner.WaveCount)
            return null;

        return spawner.GetWave(index);
    }

    private static int GetWaveMobCount(WaveSpawner.Wave wave)
    {
        if (wave == null)
            return 0;

        int total = 0;

        if (wave.spawnGroups != null && wave.spawnGroups.Length > 0)
            for (int i = 0; i < wave.spawnGroups.Length; i++)
                total += GetEntriesMobCount(wave.spawnGroups[i]?.entries);
        else
            total += GetEntriesMobCount(wave.entries);

        return total;
    }

    private static int GetEntriesMobCount(WaveSpawner.MobEntry[] entries)
    {
        if (entries == null)
            return 0;

        int total = 0;
        for (int i = 0; i < entries.Length; i++)
            if (entries[i] != null)
                total += Mathf.Max(0, entries[i].count);

        return total;
    }

    private void RefreshDirectionPreview()
    {
        ClearDirectionPreview();

        if (spawner == null
            || !spawner.IsBuildPhase
            || directionPreviewRoot == null
            || directionIndicatorPrefab == null)
            return;

        spawner.GetWavePreviewEntries(spawner.NextWaveIndex, previewEntries);
        if (previewEntries.Count == 0)
            return;

        for (int i = 0; i < previewEntries.Count; i++)
        {
            if (previewEntries[i].spawnPoint == null)
                continue;

            WaveDirectionIndicator indicator = Instantiate(directionIndicatorPrefab, directionPreviewRoot);
            indicator.Bind(previewEntries[i]);
            directionIndicators.Add(indicator);
        }
    }

    private void ClearDirectionPreview()
    {
        for (int i = 0; i < directionIndicators.Count; i++)
        {
            if (directionIndicators[i] != null)
                Destroy(directionIndicators[i].gameObject);
        }

        directionIndicators.Clear();
        previewEntries.Clear();
    }

    private void OnStartClicked()
    {
        if (spawner != null) spawner.StartNextWave();
    }
}
