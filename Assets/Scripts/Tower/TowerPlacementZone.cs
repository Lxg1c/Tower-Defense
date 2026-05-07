using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Tower placement zone.
///   - Player walks in → progress bar fills over enterDelay.
///   - Fills → opens the scene-wide TowerSelectionModal with this zone's options.
///   - Player leaves zone → modal closes, progress resets.
///   - Player picks a tower in the modal → cost deducted, tower built, zone hides.
///   - During Combat phase the zone disappears entirely (visualRoot off, collider gated).
/// </summary>
[DisallowMultipleComponent]
public class TowerPlacementZone : MonoBehaviour
{
    [Header("Options")]
    [Tooltip("Player loadout — set of buildable towers. Single source of truth shared across zones.")]
    [SerializeField] private TowerLoadout loadout;

    [Header("Unlock")]
    [Tooltip("Zone stays hidden until this many waves have been completed. " +
             "0 = available from the start. 2 = available starting from the build phase before wave 3.")]
    [Min(0)] [SerializeField] private int unlockAfterCompletedWaves = 0;

    [Header("Placement")]
    [SerializeField] private float enterDelay = 1.5f;
    [SerializeField] private Transform spawnPoint;

    [Header("Zone")]
    [SerializeField] private float zoneRadius = 2f;
    [SerializeField] private Color idleColor      = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color highlightColor = new Color(0f, 1f, 0f, 0.6f);
    [SerializeField] private Color buildingColor  = new Color(1f, 0.7f, 0f, 0.6f);

    [Header("Visuals (toggled off during combat)")]
    [Tooltip("Parent GO holding the zone's meshes, world-space UI, collider — everything player should not see / touch during a wave.")]
    [SerializeField] private GameObject visualRoot;

    [Header("UI (per-zone, above the zone)")]
    [SerializeField] private GameObject progressBarRoot;
    [SerializeField] private Image      progressFill;

    public bool IsBuilt { get; private set; }
    public TowerUpgrade BuiltTower { get; private set; }
    /// <summary>True after the player has used this zone (built or upgraded) in the current build phase.</summary>
    public bool UsedThisPhase { get; private set; }

    public UnityEvent onTowerBuilt;

    private bool playerInZone;
    private PlayerWallet wallet;
    private float enterTimer;
    private bool modalOpenedByMe;
    private Color currentColor;

    private void Start()
    {
        if (progressBarRoot != null)
            progressBarRoot.SetActive(false);

        currentColor = idleColor;
        SyncWithPhase();

        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.onBuildPhaseStarted.AddListener(OnBuildPhase);
            WaveSpawner.Instance.onCombatPhaseStarted.AddListener(OnCombatPhase);
        }
    }

    private void OnDestroy()
    {
        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.onBuildPhaseStarted.RemoveListener(OnBuildPhase);
            WaveSpawner.Instance.onCombatPhaseStarted.RemoveListener(OnCombatPhase);
        }
    }

    private void OnBuildPhase(int nextIdx, int total, int reward)
    {
        UsedThisPhase = false;
        SetVisualActive(IsUnlocked());
    }
    private void OnCombatPhase(int idx, int total, int reward)
    {
        CancelProgress();
        SetVisualActive(false);
    }

    private void SyncWithPhase()
    {
        bool buildPhase = WaveSpawner.Instance == null || WaveSpawner.Instance.IsBuildPhase;
        SetVisualActive(buildPhase && IsUnlocked());
    }

    /// <summary>
    /// True when enough waves have been completed for this zone to appear.
    /// WaveSpawner.NextWaveIndex equals "number of waves already completed" (0-based).
    /// </summary>
    public bool IsUnlocked()
    {
        if (WaveSpawner.Instance == null) return unlockAfterCompletedWaves == 0;
        return WaveSpawner.Instance.NextWaveIndex >= unlockAfterCompletedWaves;
    }

    private void SetVisualActive(bool on)
    {
        if (visualRoot != null) visualRoot.SetActive(on);
        if (!on) CancelProgress();
    }

    private void Update()
    {
        // Hidden during combat OR while still locked — skip all interaction.
        if (WaveSpawner.Instance != null && !WaveSpawner.Instance.IsBuildPhase) return;
        if (!IsUnlocked()) return;
        // One action per build phase — once used, zone goes idle and visual is hidden.
        if (UsedThisPhase) return;

        DetectPlayer();

        if (!playerInZone)
        {
            if (modalOpenedByMe) CloseModal();
            DrainProgress();
            currentColor = idleColor;
            return;
        }

        if (modalOpenedByMe)
        {
            currentColor = buildingColor;
            return;
        }

        currentColor = highlightColor;
        enterTimer  += Time.deltaTime;
        UpdateProgressBar();

        if (enterTimer >= enterDelay)
        {
            if (IsBuilt) OpenUpgradeModal();
            else         OpenModal();
        }
    }

    private void UpdateProgressBar()
    {
        if (progressBarRoot != null) progressBarRoot.SetActive(enterTimer > 0f);
        if (progressFill    != null) progressFill.fillAmount = Mathf.Clamp01(enterTimer / enterDelay);
    }

    /// <summary>Smoothly decreases the progress bar from where it stopped instead of snapping to 0.</summary>
    private void DrainProgress()
    {
        if (enterTimer <= 0f) return;
        enterTimer = Mathf.Max(0f, enterTimer - Time.deltaTime);
        UpdateProgressBar();
    }

    private void DetectPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, zoneRadius);
        PlayerWallet foundWallet = null;
        foreach (Collider c in hits)
        {
            var w = c.GetComponentInParent<PlayerWallet>();
            if (w != null) { foundWallet = w; break; }
        }

        wallet = foundWallet;
        playerInZone = foundWallet != null;
    }

    private void OpenModal()
    {
        if (TowerSelectionModal.Instance == null)
        {
            Debug.LogWarning("[TowerPlacementZone] No TowerSelectionModal in scene.", this);
            CancelProgress();
            return;
        }
        if (loadout == null || loadout.options == null || loadout.options.Length == 0)
        {
            Debug.LogWarning("[TowerPlacementZone] No loadout assigned or empty.", this);
            CancelProgress();
            return;
        }

        modalOpenedByMe = true;
        TowerSelectionModal.Instance.Open(loadout.options, HandleOptionPicked);
    }

    private void CloseModal()
    {
        if (modalOpenedByMe)
        {
            if (TowerSelectionModal.Instance != null && TowerSelectionModal.Instance.IsOpen)
                TowerSelectionModal.Instance.Close();
            if (TowerUpgradeModal.Instance != null && TowerUpgradeModal.Instance.IsOpen)
                TowerUpgradeModal.Instance.Close();
        }
        modalOpenedByMe = false;
    }

    private void OpenUpgradeModal()
    {
        if (TowerUpgradeModal.Instance == null)
        {
            Debug.LogWarning("[TowerPlacementZone] No TowerUpgradeModal in scene.", this);
            CancelProgress();
            return;
        }
        modalOpenedByMe = true;
        TowerUpgradeModal.Instance.Open(BuiltTower, OnTowerUpgraded);
    }

    private void OnTowerUpgraded(TowerUpgrade tower)
    {
        // In-place upgrade — same instance. Zone is "used" for this build phase.
        modalOpenedByMe = false;
        if (TowerUpgradeModal.Instance != null && TowerUpgradeModal.Instance.IsOpen)
            TowerUpgradeModal.Instance.Close();
        MarkUsedAndHide();
    }

    private void CancelProgress()
    {
        enterTimer = 0f;
        if (progressBarRoot != null) progressBarRoot.SetActive(false);
        if (progressFill    != null) progressFill.fillAmount = 0f;
    }

    private void HandleOptionPicked(TowerOption opt)
    {
        modalOpenedByMe = false;

        if (opt == null || opt.prefab == null) return;
        if (wallet == null || !wallet.TrySpend(opt.cost))
            return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        var go = Instantiate(opt.prefab, pos, rot);
        BuiltTower = go.GetComponent<TowerUpgrade>();

        IsBuilt = true;
        MarkUsedAndHide();
        onTowerBuilt?.Invoke();
    }

    private void MarkUsedAndHide()
    {
        UsedThisPhase = true;
        CancelProgress();
        SetVisualActive(false);
    }

    private void OnDrawGizmos()
    {
        Color c = Application.isPlaying ? currentColor : idleColor;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, zoneRadius);

        Gizmos.color = new Color(c.r, c.g, c.b, 1f);
        Gizmos.DrawWireSphere(transform.position, zoneRadius);
    }
}
