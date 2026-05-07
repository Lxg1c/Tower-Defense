using UnityEngine;

public class Base : Damageable
{
    private bool subscribed;

    protected override void OnEnable()
    {
        base.OnEnable();
        TrySubscribe();
    }

    protected override void Start()
    {
        base.Start();
        TrySubscribe();   // fallback if WaveSpawner wasn't ready in OnEnable
    }

    private void OnDisable()
    {
        if (subscribed && WaveSpawner.Instance != null)
            WaveSpawner.Instance.onWaveCompleted.RemoveListener(OnWaveCompleted);
        subscribed = false;
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
