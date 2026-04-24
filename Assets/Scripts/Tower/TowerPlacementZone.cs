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
    [SerializeField] private TowerOption[] options;

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
    [SerializeField] private TMP_Text   hintLabel; // optional: e.g. "Stand here"

    public bool IsBuilt { get; private set; }

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

    private void OnBuildPhase(int nextIdx, int total, int reward) => SetVisualActive(true);
    private void OnCombatPhase(int idx, int total, int reward)
    {
        CancelProgress();
        SetVisualActive(false);
    }

    private void SyncWithPhase()
    {
        bool buildPhase = WaveSpawner.Instance == null || WaveSpawner.Instance.IsBuildPhase;
        SetVisualActive(buildPhase);
    }

    private void SetVisualActive(bool on)
    {
        if (visualRoot != null) visualRoot.SetActive(on);
        if (!on) CancelProgress();
    }

    private void Update()
    {
        if (IsBuilt) return;

        // During combat we are fully hidden — skip all interaction.
        if (WaveSpawner.Instance != null && !WaveSpawner.Instance.IsBuildPhase)
            return;

        DetectPlayer();

        if (!playerInZone)
        {
            if (modalOpenedByMe) CloseModal();
            CancelProgress();
            currentColor = idleColor;
            return;
        }

        // Player is in zone: tick progress (if not already showing modal).
        if (modalOpenedByMe)
        {
            currentColor = buildingColor;
            return;
        }

        currentColor = highlightColor;
        enterTimer  += Time.deltaTime;

        if (progressBarRoot != null) progressBarRoot.SetActive(true);
        if (progressFill    != null) progressFill.fillAmount = Mathf.Clamp01(enterTimer / enterDelay);

        if (enterTimer >= enterDelay)
            OpenModal();
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
        if (options == null || options.Length == 0)
        {
            Debug.LogWarning("[TowerPlacementZone] No tower options configured.", this);
            CancelProgress();
            return;
        }

        modalOpenedByMe = true;
        TowerSelectionModal.Instance.Open(options, HandleOptionPicked);
    }

    private void CloseModal()
    {
        if (TowerSelectionModal.Instance != null && modalOpenedByMe && TowerSelectionModal.Instance.IsOpen)
            TowerSelectionModal.Instance.Close();
        modalOpenedByMe = false;
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

        Instantiate(opt.prefab, pos, rot);

        IsBuilt = true;
        onTowerBuilt?.Invoke();
        gameObject.SetActive(false);
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
