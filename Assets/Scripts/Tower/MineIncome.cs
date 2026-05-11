using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Economy building: pays coins when a wave is cleared, but only if it was not
/// destroyed during that wave.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Damageable))]
public class MineIncome : MonoBehaviour
{
    public static readonly UnityEvent onIncomeChanged = new();

    [SerializeField] private int coinsPerWave = 25;

    private Damageable health;
    private WaveSpawner spawner;
    private bool survivedCurrentWave;

    private static readonly System.Collections.Generic.List<MineIncome> activeMines = new();

    public int CoinsPerWave
    {
        get => coinsPerWave;
        set
        {
            int next = Mathf.Max(0, value);
            if (coinsPerWave == next)
                return;

            coinsPerWave = next;
            onIncomeChanged?.Invoke();
        }
    }

    public bool IsPaying => health == null || health.IsAlive;

    public static int ActiveMineCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < activeMines.Count; i++)
            {
                MineIncome mine = activeMines[i];
                if (mine != null && mine.isActiveAndEnabled && mine.IsPaying)
                    count++;
            }

            return count;
        }
    }

    public static int TotalCoinsPerWave
    {
        get
        {
            int total = 0;
            for (int i = 0; i < activeMines.Count; i++)
            {
                MineIncome mine = activeMines[i];
                if (mine != null && mine.isActiveAndEnabled && mine.IsPaying)
                    total += mine.CoinsPerWave;
            }

            return total;
        }
    }

    private void Awake()
    {
        health = GetComponent<Damageable>();
    }

    private void OnEnable()
    {
        if (!activeMines.Contains(this))
            activeMines.Add(this);

        BindSpawner();
        if (spawner == null)
            Invoke(nameof(BindSpawner), 0.1f);

        if (health != null)
            health.onDied.AddListener(OnDestroyed);

        onIncomeChanged?.Invoke();
    }

    private void OnDisable()
    {
        activeMines.Remove(this);
        UnbindSpawner();

        if (health != null)
            health.onDied.RemoveListener(OnDestroyed);

        onIncomeChanged?.Invoke();
    }

    private void BindSpawner()
    {
        UnbindSpawner();

        spawner = WaveSpawner.Instance;
        if (spawner == null)
            return;

        spawner.onCombatPhaseStarted.AddListener(OnCombatStarted);
        spawner.onWaveCompleted.AddListener(OnWaveCompleted);
    }

    private void UnbindSpawner()
    {
        if (spawner == null)
            return;

        spawner.onCombatPhaseStarted.RemoveListener(OnCombatStarted);
        spawner.onWaveCompleted.RemoveListener(OnWaveCompleted);
    }

    private void OnCombatStarted(int waveIndex, int totalWaves, int reward)
    {
        survivedCurrentWave = health == null || health.IsAlive;
    }

    private void OnDestroyed()
    {
        survivedCurrentWave = false;
        onIncomeChanged?.Invoke();
    }

    private void OnWaveCompleted(int waveIndex, int reward)
    {
        if (!survivedCurrentWave)
            return;

        if (health != null && !health.IsAlive)
            return;

        if (coinsPerWave > 0 && PlayerWallet.Instance != null)
            PlayerWallet.Instance.AddCoins(coinsPerWave);
    }
}
