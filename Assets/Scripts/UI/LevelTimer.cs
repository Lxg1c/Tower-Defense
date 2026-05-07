using TMPro;
using UnityEngine;

/// <summary>
/// Counts elapsed time on the current level and renders it into a TMP_Text.
/// Stops when all waves are completed or the base dies (game over).
/// </summary>
[DisallowMultipleComponent]
public class LevelTimer : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [Tooltip("If true, timer ticks only during Combat phase. False = it runs the whole level.")]
    [SerializeField] private bool combatOnly = false;
    [SerializeField] private string format = "{0:00}:{1:00}";

    public float ElapsedSeconds { get; private set; }

    private bool stopped;
    private bool subscribed;

    private void Start()
    {
        TrySubscribe();
        Render();
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
        Render();
    }

    /// <summary>Hook this to Base.onDied or any "level over" event to freeze the timer.</summary>
    public void Stop() => stopped = true;

    /// <summary>Reset the timer to 0 and resume counting.</summary>
    public void ResetTimer()
    {
        ElapsedSeconds = 0f;
        stopped = false;
        Render();
    }

    private void Render()
    {
        if (label == null) return;
        int minutes = Mathf.FloorToInt(ElapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(ElapsedSeconds % 60f);
        label.text = string.Format(format, minutes, seconds);
    }
}
