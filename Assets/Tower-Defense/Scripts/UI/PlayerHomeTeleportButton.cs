using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerHomeTeleportButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [Tooltip("Optional. If empty, the button finds PlayerWallet in the scene.")]
    private Transform player;
    [Tooltip("Optional. If empty, the player's scene start position is used.")]
    [SerializeField] private Transform homePointOverride;
    [SerializeField] private bool restoreStartRotation;
    [SerializeField] private bool clearVelocity = true;

    private Rigidbody playerRigidbody;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private bool homeCaptured;
    private WaveSpawner spawner;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(TeleportToHome);
    }

    private void Start()
    {
        if (!CaptureHome())
            StartCoroutine(CaptureHomeWhenPlayerAppears());

        BindSpawner();
        RefreshVisibility();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(TeleportToHome);

        UnbindSpawner();
    }

    public void TeleportToHome()
    {
        if (spawner != null && !spawner.IsBuildPhase)
            return;

        if (!homeCaptured && !CaptureHome())
            return;

        Transform target = ResolvePlayer();
        if (target == null)
            return;

        if (playerRigidbody == null)
            playerRigidbody = target.GetComponent<Rigidbody>();

        if (playerRigidbody != null)
        {
            if (clearVelocity)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }

            playerRigidbody.position = homePosition;
            if (restoreStartRotation)
                playerRigidbody.rotation = homeRotation;
        }
        else
        {
            target.position = homePosition;
            if (restoreStartRotation)
                target.rotation = homeRotation;
        }

        Physics.SyncTransforms();
    }

    private void BindSpawner()
    {
        if (spawner != null)
            return;

        spawner = WaveSpawner.Instance;
        if (spawner == null)
        {
            Invoke(nameof(BindSpawner), 0.1f);
            return;
        }

        spawner.onBuildPhaseStarted.AddListener(OnBuildPhaseStarted);
        spawner.onCombatPhaseStarted.AddListener(OnCombatPhaseStarted);
        spawner.onAllWavesCompleted.AddListener(OnAllWavesCompleted);
    }

    private void UnbindSpawner()
    {
        if (spawner == null)
            return;

        spawner.onBuildPhaseStarted.RemoveListener(OnBuildPhaseStarted);
        spawner.onCombatPhaseStarted.RemoveListener(OnCombatPhaseStarted);
        spawner.onAllWavesCompleted.RemoveListener(OnAllWavesCompleted);
        spawner = null;
    }

    private void OnBuildPhaseStarted(int nextWaveIndex, int totalWaves, int reward)
    {
        RefreshVisibility();
    }

    private void OnCombatPhaseStarted(int waveIndex, int totalWaves, int reward)
    {
        RefreshVisibility();
    }

    private void OnAllWavesCompleted()
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool visible = spawner == null || spawner.IsBuildPhase;
        gameObject.SetActive(visible);
    }

    private IEnumerator CaptureHomeWhenPlayerAppears()
    {
        while (!homeCaptured)
        {
            if (CaptureHome())
                yield break;

            yield return null;
        }
    }

    private bool CaptureHome()
    {
        Transform target = ResolvePlayer();
        if (target == null)
            return false;

        playerRigidbody = target.GetComponent<Rigidbody>();
        homePosition = homePointOverride != null ? homePointOverride.position : target.position;
        homeRotation = homePointOverride != null ? homePointOverride.rotation : target.rotation;
        homeCaptured = true;
        return true;
    }

    private Transform ResolvePlayer()
    {
        if (player != null)
            return player;

        PlayerWallet wallet = PlayerWallet.Instance;
        if (wallet == null)
            wallet = FindFirstObjectByType<PlayerWallet>();

        player = wallet != null ? wallet.transform : null;
        return player;
    }
}
