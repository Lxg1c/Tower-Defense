using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Drives the Build / Combat phase loop.
///
/// Flow:
///   Start → BuildPhase (idle, waiting for StartNextWave button)
///   Button → CombatPhase (spawn wave, wait for all mobs dead)
///   Wave done → give coin reward → BuildPhase (or AllCompleted)
///
/// UI, PlayerMovement and TowerHealth subscribe to the phase events.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }

    public enum Phase { Build, Combat, AllCompleted }

    [System.Serializable]
    public class MobEntry
    {
        public MobCore prefab;
        [Min(1)] public int count = 5;
        [Min(0f)] public float spawnInterval = 0.5f;
    }

    [System.Serializable]
    public class Wave
    {
        public MobEntry[] entries;
        [Min(0)] public int coinReward = 50;
    }

    [Header("Spawning")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Wave[] waves;
    [Tooltip("Prewarm this many instances per prefab at startup. 0 = no prewarm.")]
    [SerializeField] private int prewarmPerPrefab = 0;

    [Header("Events (phase-level)")]
    /// <summary>Fired when we enter Build phase. Args: nextWaveIndex (0-based), totalWaves, nextReward.</summary>
    public UnityEvent<int, int, int> onBuildPhaseStarted;
    /// <summary>Fired when the Start Wave button is pressed and combat begins. Args: waveIndex, totalWaves, reward.</summary>
    public UnityEvent<int, int, int> onCombatPhaseStarted;
    /// <summary>Fired right after a wave is cleared. Args: waveIndex, reward (already added to wallet).</summary>
    public UnityEvent<int, int> onWaveCompleted;
    /// <summary>Fired when all waves are cleared.</summary>
    public UnityEvent onAllWavesCompleted;

    // Runtime
    private readonly Dictionary<int, MobPool> pools = new();
    private readonly List<MobHealth> aliveMobs = new();
    private int nextWaveIndex = 0;
    private Coroutine combatRoutine;

    public Phase CurrentPhase { get; private set; } = Phase.Build;
    public int   WaveCount    => waves != null ? waves.Length : 0;
    public int   NextWaveIndex => nextWaveIndex;
    public bool  IsBuildPhase  => CurrentPhase == Phase.Build;
    public bool  IsCombatPhase => CurrentPhase == Phase.Combat;
    public int   AliveMobCount => aliveMobs.Count;

    public Wave NextWave =>
        (waves != null && nextWaveIndex >= 0 && nextWaveIndex < waves.Length) ? waves[nextWaveIndex] : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (prewarmPerPrefab > 0 && spawnPoints != null && spawnPoints.Length > 0 && waves != null)
        {
            Vector3 pos = spawnPoints[0].position;
            foreach (var wave in waves)
            {
                if (wave?.entries == null) continue;
                foreach (var e in wave.entries)
                    if (e?.prefab != null)
                        GetOrCreatePool(e.prefab).Prewarm(prewarmPerPrefab, pos);
            }
        }

        EnterBuildPhase();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Hook this to the UI "Start Wave" button. Does nothing if already in combat
    /// or if no waves remain.
    /// </summary>
    public void StartNextWave()
    {
        if (CurrentPhase != Phase.Build) return;
        if (nextWaveIndex >= WaveCount) return;
        // Don't allow starting a wave while the player is mid-selection.
        if (TowerSelectionModal.Instance != null && TowerSelectionModal.Instance.IsOpen) return;

        if (combatRoutine != null) StopCoroutine(combatRoutine);
        combatRoutine = StartCoroutine(RunCombat(nextWaveIndex));
    }

    // ── Phase transitions ──────────────────────────────────────────────────────

    private void EnterBuildPhase()
    {
        CurrentPhase = Phase.Build;
        Wave w = NextWave;
        int reward = w != null ? w.coinReward : 0;
        onBuildPhaseStarted?.Invoke(nextWaveIndex, WaveCount, reward);
    }

    private IEnumerator RunCombat(int waveIndex)
    {
        CurrentPhase = Phase.Combat;
        Wave wave = waves[waveIndex];
        onCombatPhaseStarted?.Invoke(waveIndex, WaveCount, wave.coinReward);

        yield return StartCoroutine(SpawnWave(wave));

        while (aliveMobs.Count > 0)
            yield return null;

        // Award coins
        if (wave.coinReward > 0 && PlayerWallet.Instance != null)
            PlayerWallet.Instance.AddCoins(wave.coinReward);

        onWaveCompleted?.Invoke(waveIndex, wave.coinReward);

        nextWaveIndex = waveIndex + 1;

        if (nextWaveIndex >= WaveCount)
        {
            CurrentPhase = Phase.AllCompleted;
            onAllWavesCompleted?.Invoke();
        }
        else
        {
            EnterBuildPhase();
        }
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        if (wave?.entries == null) yield break;

        foreach (var entry in wave.entries)
        {
            if (entry?.prefab == null || entry.count <= 0) continue;

            for (int i = 0; i < entry.count; i++)
            {
                SpawnOne(entry.prefab);
                if (entry.spawnInterval > 0f)
                    yield return new WaitForSeconds(entry.spawnInterval);
            }
        }
    }

    private void SpawnOne(MobCore prefab)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[WaveSpawner] No spawn points configured.");
            return;
        }

        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 pos = point.position;

        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            pos = hit.position;

        var pool = GetOrCreatePool(prefab);
        var mob  = pool.Get(pos, point.rotation);

        var member = mob.GetComponent<MobPoolMember>();
        if (member == null) member = mob.gameObject.AddComponent<MobPoolMember>();
        member.Pool = pool;

        var health = mob.Health;
        health.OnDeathHandled -= HandleMobDeath;
        health.OnDeathHandled += HandleMobDeath;
        aliveMobs.Add(health);
    }

    private void HandleMobDeath(MobHealth mob)
    {
        if (mob == null) return;
        aliveMobs.Remove(mob);
        StartCoroutine(ReturnNextFrame(mob));
    }

    private IEnumerator ReturnNextFrame(MobHealth mob)
    {
        yield return null;
        if (mob == null) yield break;

        var member = mob.GetComponent<MobPoolMember>();
        var core   = mob.GetComponent<MobCore>();
        if (member != null && member.Pool != null && core != null)
            member.Pool.Return(core);
        else if (core != null)
            Destroy(core.gameObject);
    }

    private MobPool GetOrCreatePool(MobCore prefab)
    {
        int key = prefab.GetInstanceID();
        if (!pools.TryGetValue(key, out var pool))
        {
            pool = new MobPool(prefab, transform);
            pools[key] = pool;
        }
        return pool;
    }
}
