using UnityEngine;

public class Base : Damageable
{
    public static Base Instance { get; private set; }
    public static event System.Action OnBaseChanged;

    private bool subscribed;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Base] Duplicate base instance detected.", this);
            return;
        }

        Instance = this;
        OnBaseChanged?.Invoke();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (Instance == null)
        {
            Instance = this;
            OnBaseChanged?.Invoke();
        }

        TrySubscribe();
    }

    protected override void Start()
    {
        base.Start();
        TrySubscribe();   // fallback if WaveSpawner wasn't ready in OnEnable
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (subscribed && WaveSpawner.Instance != null)
            WaveSpawner.Instance.onWaveCompleted.RemoveListener(OnWaveCompleted);
        subscribed = false;

        if (Instance == this)
        {
            Instance = null;
            OnBaseChanged?.Invoke();
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (WaveSpawner.Instance == null) return;
        WaveSpawner.Instance.onWaveCompleted.AddListener(OnWaveCompleted);
        subscribed = true;
    }

    private void OnWaveCompleted(int waveIndex, int reward)
    {
        if (IsAlive) ResetToMaxHealth();
    }

    protected override void OnDeath()
    {
        // Game over handled via inherited onDied event in inspector.
    }
}
