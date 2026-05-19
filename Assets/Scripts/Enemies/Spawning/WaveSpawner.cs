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
        public EnemyOption enemy;
        [Tooltip("Legacy prefab fallback. Used when Enemy is empty.")]
        public MobCore prefab;
        [Min(1)] public int count = 5;
        [Min(0f)] public float spawnInterval = 0.5f;
        [Tooltip("Legacy icon fallback. Used when Enemy has no icon.")]
        public Sprite previewIcon;

        public MobCore Prefab => enemy != null && enemy.prefab != null ? enemy.prefab : prefab;
        public Sprite PreviewIcon => enemy != null && enemy.icon != null ? enemy.icon : previewIcon;
    }

    [System.Serializable]
    public class SpawnGroup
    {
        [Tooltip("Index in WaveSpawner Spawn Points. Spawn points themselves are configured only once on the spawner.")]
        [Min(0)] public int spawnPointIndex;
        public MobEntry[] entries;
    }

    [System.Serializable]
    public class Wave
    {
        [Tooltip("Legacy entries. Used when Spawn Groups are empty.")]
        public MobEntry[] entries;
        [Tooltip("Use this to choose exact spawn points and enemy counts for this wave.")]
        public SpawnGroup[] spawnGroups;
        [Min(0)] public int coinReward = 50;
    }

    public struct WavePreviewEntry
    {
        public Transform spawnPoint;
        public MobCore prefab;
        public Sprite icon;
        public int count;
        public int stackIndex;
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

    public Wave GetWave(int index)
    {
        return waves != null && index >= 0 && index < waves.Length ? waves[index] : null;
    }

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
                if (wave == null) continue;
                PrewarmEntries(wave.entries, pos);

                if (wave.spawnGroups == null) continue;
                foreach (var group in wave.spawnGroups)
                    PrewarmEntries(group?.entries, pos);
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
        if (Base.Instance == null) return;
        // Don't allow starting a wave while the player is mid-selection.
        if (TowerSelectionModal.Instance != null && TowerSelectionModal.Instance.IsOpen) return;

        if (combatRoutine != null) StopCoroutine(combatRoutine);
        combatRoutine = StartCoroutine(RunCombat(nextWaveIndex));
    }

    public Transform GetClosestSpawnPoint(Vector3 position)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        Transform closest = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (point == null)
                continue;

            float sqrDistance = (point.position - position).sqrMagnitude;
            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closest = point;
            }
        }

        return closest;
    }

    public void GetWavePreviewEntries(int waveIndex, List<WavePreviewEntry> results)
    {
        results.Clear();

        Wave wave = GetWave(waveIndex);
        if (wave == null)
            return;

        if (wave.spawnGroups != null && wave.spawnGroups.Length > 0)
        {
            for (int i = 0; i < wave.spawnGroups.Length; i++)
            {
                SpawnGroup group = wave.spawnGroups[i];
                if (group == null)
                    continue;

                AddPreviewEntries(GetSpawnPoint(group.spawnPointIndex), group.entries, results);
            }

            return;
        }

        Transform point = GetDefaultSpawnPoint();
        AddPreviewEntries(point, wave.entries, results);
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
        if (wave == null) yield break;

        if (wave.spawnGroups != null && wave.spawnGroups.Length > 0)
        {
            foreach (var group in wave.spawnGroups)
            {
                if (group?.entries == null) continue;
                Transform point = GetSpawnPoint(group.spawnPointIndex);

                foreach (var entry in group.entries)
                {
                    MobCore prefab = entry?.Prefab;
                    if (prefab == null || entry.count <= 0) continue;

                    for (int i = 0; i < entry.count; i++)
                    {
                        SpawnOne(prefab, point);
                        if (entry.spawnInterval > 0f)
                            yield return new WaitForSeconds(entry.spawnInterval);
                    }
                }
            }

            yield break;
        }

        if (wave.entries == null) yield break;

        foreach (var entry in wave.entries)
        {
            MobCore prefab = entry?.Prefab;
            if (prefab == null || entry.count <= 0) continue;

            for (int i = 0; i < entry.count; i++)
            {
                SpawnOne(prefab, GetRandomSpawnPoint());
                if (entry.spawnInterval > 0f)
                    yield return new WaitForSeconds(entry.spawnInterval);
            }
        }
    }

    private void SpawnOne(MobCore prefab, Transform point)
    {
        if (point == null)
        {
            Debug.LogWarning("[WaveSpawner] No spawn points configured.");
            return;
        }

        Vector3 pos = point.position;

        // Snap to NavMesh only if the mob actually uses an Agent (ground mobs).
        if (prefab.GetComponent<FlyingNav>() == null &&
            prefab.GetComponent<NavMeshAgent>() != null &&
            NavMesh.SamplePosition(pos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
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

    private Transform GetDefaultSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        for (int i = 0; i < spawnPoints.Length; i++)
            if (spawnPoints[i] != null)
                return spawnPoints[i];

        return null;
    }

    private Transform GetSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        if (index >= 0 && index < spawnPoints.Length && spawnPoints[index] != null)
            return spawnPoints[index];

        return GetDefaultSpawnPoint();
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        for (int attempt = 0; attempt < spawnPoints.Length; attempt++)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (point != null)
                return point;
        }

        return GetDefaultSpawnPoint();
    }

    private static void AddPreviewEntries(Transform point, MobEntry[] entries, List<WavePreviewEntry> results)
    {
        if (entries == null)
            return;

        int stackIndex = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            MobEntry entry = entries[i];
            if (entry == null || entry.Prefab == null || entry.count <= 0)
                continue;

            results.Add(new WavePreviewEntry
            {
                spawnPoint = point,
                prefab = entry.Prefab,
                icon = entry.PreviewIcon,
                count = entry.count,
                stackIndex = stackIndex
            });
            stackIndex++;
        }
    }

    private void PrewarmEntries(MobEntry[] entries, Vector3 position)
    {
        if (entries == null)
            return;

        foreach (var e in entries)
        {
            MobCore prefab = e?.Prefab;
            if (prefab != null)
                GetOrCreatePool(prefab).Prewarm(prewarmPerPrefab, position);
        }
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
