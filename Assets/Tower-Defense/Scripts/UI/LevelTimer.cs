using UnityEngine;

/// <summary>
/// Counts elapsed time on the current level for the UI Toolkit HUD.
/// Stops when all waves are completed or the base dies (game over).
/// </summary>
[DisallowMultipleComponent]
public class LevelTimer : MonoBehaviour
{
    [Tooltip("If true, timer ticks only during Combat phase. False = it runs the whole level.")]
    [SerializeField] private bool combatOnly = false;

    public float ElapsedSeconds { get; private set; }

    private bool stopped;
    private bool subscribed;

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed && WaveSpawner.Instance != null)
            WaveSpawner.Instance.onAllWavesCompleted.RemoveListener(Stop);
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (WaveSpawner.Instance == null) return;
        WaveSpawner.Instance.onAllWavesCompleted.AddListener(Stop);
        subscribed = true;
    }

    private void Update()
    {
        if (!subscribed) TrySubscribe();
        if (stopped) return;

        if (combatOnly && WaveSpawner.Instance != null && !WaveSpawner.Instance.IsCombatPhase)
            return;

        ElapsedSeconds += Time.deltaTime;
    }

    /// <summary>Hook this to Base.onDied or any "level over" event to freeze the timer.</summary>
    public void Stop() => stopped = true;

    /// <summary>Reset the timer to 0 and resume counting.</summary>
    public void ResetTimer()
    {
        ElapsedSeconds = 0f;
        stopped = false;
    }
}
