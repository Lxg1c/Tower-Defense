using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
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
    [SerializeField] private BuildSlotType allowedType = BuildSlotType.Defense;
    [Tooltip("Required town hall level for this zone to appear. 0 = available before town hall is built. 1 = after town hall is built. 2 = after first town hall upgrade.")]
    [Min(0)] [SerializeField] private int requiredTownHallLevel = 0;

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

    [Header("World-space floor circle")]
    [Tooltip("Image on the world-space canvas. Set Image Type = Filled and Fill Method = Radial 360.")]
    [FormerlySerializedAs("progressFill")]
    [SerializeField] private Image placementCircleFill;

    [Header("Zone Icon")]
    [SerializeField] private Image zoneIcon;
    [SerializeField] private Sprite townHallIcon;
    [SerializeField] private Sprite defenseIcon;
    [SerializeField] private Sprite mineIcon;

    public bool IsBuilt { get; private set; }
    public TowerUpgrade BuiltTower { get; private set; }
    public BaseUpgrade BuiltBase { get; private set; }
    /// <summary>True after the player has used this zone (built or upgraded) in the current build phase.</summary>
    public bool UsedThisPhase { get; private set; }

    public UnityEvent onTowerBuilt;

    private bool playerInZone;
    private bool wasPlayerInZone;
    private PlayerWallet wallet;
    private float enterTimer;
    private bool modalOpenedByMe;
    private Color currentColor;
    private readonly List<TowerOption> filteredOptions = new();

    private void Start()
    {
        if (placementCircleFill != null)
            placementCircleFill.fillAmount = 0f;
        UpdateZoneIcon();

        currentColor = idleColor;
        SyncWithPhase();

        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.onBuildPhaseStarted.AddListener(OnBuildPhase);
            WaveSpawner.Instance.onCombatPhaseStarted.AddListener(OnCombatPhase);
        }

        BaseUpgrade.OnTownHallChanged += OnTownHallChanged;
    }

    private void OnDestroy()
    {
        BaseUpgrade.OnTownHallChanged -= OnTownHallChanged;

        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.onBuildPhaseStarted.RemoveListener(OnBuildPhase);
            WaveSpawner.Instance.onCombatPhaseStarted.RemoveListener(OnCombatPhase);
        }
    }

    private void OnValidate()
    {
        UpdateZoneIcon();
    }

    private void OnBuildPhase(int nextIdx, int total, int reward)
    {
        UsedThisPhase = false;
        SetVisualActive(IsUnlocked() && CanUseBuiltObject());
    }
    private void OnCombatPhase(int idx, int total, int reward)
    {
        CancelProgress();
        SetVisualActive(false);
    }

    private void OnTownHallChanged()
    {
        SyncWithPhase();
    }

    private void SyncWithPhase()
    {
        bool buildPhase = WaveSpawner.Instance == null || WaveSpawner.Instance.IsBuildPhase;
        SetVisualActive(buildPhase && IsUnlocked() && CanUseBuiltObject());
    }

    public bool IsUnlocked()
    {
        return GetTownHallLevel() >= requiredTownHallLevel;
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
            if (wasPlayerInZone)
                CancelProgress();

            wasPlayerInZone = false;
            currentColor = idleColor;
            return;
        }

        if (!wasPlayerInZone)
            wasPlayerInZone = true;

        if (modalOpenedByMe)
        {
            currentColor = buildingColor;
            return;
        }

        currentColor = highlightColor;
        enterTimer  += Time.deltaTime;
        UpdateFloorCircle();

        if (enterTimer >= enterDelay)
        {
            if (IsBuilt) OpenBuiltObjectModal();
            else         OpenModal();
        }
    }

    private void UpdateFloorCircle()
    {
        if (placementCircleFill != null)
            placementCircleFill.fillAmount = Mathf.Clamp01(enterTimer / enterDelay);
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

        IReadOnlyList<TowerOption> options = GetFilteredOptions();
        if (options.Count == 0)
        {
            Debug.LogWarning($"[TowerPlacementZone] No build options for slot type {allowedType}.", this);
            CancelProgress();
            return;
        }

        modalOpenedByMe = true;
        TowerSelectionModal.Instance.Open(options, HandleOptionPicked);
    }

    private void CloseModal()
    {
        if (modalOpenedByMe)
        {
            if (TowerSelectionModal.Instance != null && TowerSelectionModal.Instance.IsOpen)
                TowerSelectionModal.Instance.Close();
            if (TowerUpgradeModal.Instance != null && TowerUpgradeModal.Instance.IsOpen)
                TowerUpgradeModal.Instance.Close();
            if (BaseUpgradeModal.Instance != null && BaseUpgradeModal.Instance.IsOpen)
                BaseUpgradeModal.Instance.Close();
        }
        modalOpenedByMe = false;
    }

    private void OpenBuiltObjectModal()
    {
        if (BuiltBase != null) OpenBaseUpgradeModal();
        else                   OpenUpgradeModal();
    }

    private void OpenUpgradeModal()
    {
        if (BuiltTower == null)
        {
            CancelProgress();
            return;
        }

        if (TowerUpgradeModal.Instance == null)
        {
            Debug.LogWarning("[TowerPlacementZone] No TowerUpgradeModal in scene.", this);
            CancelProgress();
            return;
        }
        modalOpenedByMe = true;
        TowerUpgradeModal.Instance.Open(BuiltTower, OnTowerUpgraded);
    }

    private void OpenBaseUpgradeModal()
    {
        if (BuiltBase == null)
        {
            CancelProgress();
            return;
        }

        if (BaseUpgradeModal.Instance == null)
        {
            Debug.LogWarning("[TowerPlacementZone] No BaseUpgradeModal in scene.", this);
            CancelProgress();
            return;
        }

        modalOpenedByMe = true;
        BaseUpgradeModal.Instance.Open(BuiltBase, OnBaseUpgraded);
    }

    private void OnBaseUpgraded(BaseUpgrade baseUpgrade)
    {
        modalOpenedByMe = false;
        if (BaseUpgradeModal.Instance != null && BaseUpgradeModal.Instance.IsOpen)
            BaseUpgradeModal.Instance.Close();
        MarkUsedAndHide();
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
        if (placementCircleFill != null)
            placementCircleFill.fillAmount = 0f;
    }

    private void HandleOptionPicked(TowerOption opt)
    {
        modalOpenedByMe = false;

        if (opt == null || opt.prefab == null) return;
        if (opt.slotType != allowedType)
        {
            Debug.LogWarning($"[TowerPlacementZone] Rejected {opt.displayName}: option type {opt.slotType} does not match zone type {allowedType}.", this);
            return;
        }

        if (wallet == null || !wallet.TrySpend(opt.cost))
            return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        var go = Instantiate(opt.prefab, pos, rot);
        BuiltTower = go.GetComponent<TowerUpgrade>();
        BuiltBase = go.GetComponent<BaseUpgrade>();

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

    private bool CanUseBuiltObject()
    {
        return !IsBuilt || BuiltTower != null || BuiltBase != null;
    }

    private IReadOnlyList<TowerOption> GetFilteredOptions()
    {
        filteredOptions.Clear();

        if (loadout == null || loadout.options == null)
            return filteredOptions;

        foreach (TowerOption option in loadout.options)
        {
            if (option == null || option.prefab == null)
                continue;

            if (option.slotType == allowedType)
                filteredOptions.Add(option);
        }

        return filteredOptions;
    }

    private static int GetTownHallLevel()
    {
        return BaseUpgrade.Instance != null ? BaseUpgrade.Instance.TownHallLevel : 0;
    }

    private void UpdateZoneIcon()
    {
        if (zoneIcon == null)
            return;

        Sprite icon = allowedType switch
        {
            BuildSlotType.TownHall => townHallIcon,
            BuildSlotType.Defense => defenseIcon,
            BuildSlotType.Mine => mineIcon,
            _ => null
        };

        zoneIcon.sprite = icon;
        zoneIcon.enabled = icon != null;
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
