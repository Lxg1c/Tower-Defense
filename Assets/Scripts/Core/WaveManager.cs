using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject prefab;
        public int count = 3;
    }

    [System.Serializable]
    public class Wave
    {
        public EnemySpawnInfo[] enemies;
        public float spawnInterval = 0.5f;
    }

    public enum Phase { Prep, Combat, Finished }

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    [Header("References")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Base targetBase;

    public static WaveManager Instance { get; private set; }

    public int CurrentWave { get; private set; }
    public int TotalWaves => waves.Length;
    public Phase CurrentPhase { get; private set; }
    public float PrepTimeRemaining { get; private set; }

    public bool IsPrepPhase => CurrentPhase == Phase.Prep;

    public UnityEvent<int> onWaveStarted;
    public UnityEvent onPrepPhaseStarted;
    public UnityEvent onAllWavesCompleted;

    private bool waveRequested;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    /// <summary>
    /// Call from UI button to start the next wave.
    /// </summary>
    public void StartNextWave()
    {
        waveRequested = true;
    }

    private IEnumerator RunWaves()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            // Prep phase: wait for player to press the button
            CurrentPhase = Phase.Prep;
            waveRequested = false;
            onPrepPhaseStarted?.Invoke();

            yield return new WaitUntil(() => waveRequested);

            // Combat phase
            CurrentWave = i + 1;
            CurrentPhase = Phase.Combat;
            onWaveStarted?.Invoke(CurrentWave);

            yield return StartCoroutine(SpawnWave(waves[i]));

            yield return new WaitUntil(() =>
                FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None).Length == 0
            );
        }

        CurrentPhase = Phase.Finished;
        onAllWavesCompleted?.Invoke();
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        foreach (EnemySpawnInfo info in wave.enemies)
        {
            for (int i = 0; i < info.count; i++)
            {
                SpawnEnemy(info.prefab);
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        Vector3 spawnPos = GetRandomSpawnPoint();
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        EnemyMover mover = enemy.GetComponent<EnemyMover>();
        if (mover != null)
            mover.Init(spawnPos, targetBase);
    }

    private Vector3 GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return transform.position;

        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }
}
