using UnityEngine;

/// <summary>
/// Shows a victory panel when WaveSpawner reports that all waves are complete.
/// </summary>
[DisallowMultipleComponent]
public class VictoryScreen : MenuScreenBase
{
    [Header("Behaviour")]
    [SerializeField] private bool pauseGameOnShow = true;

    private WaveSpawner spawner;

    private void OnEnable()
    {
        BindSpawner();
        if (spawner == null)
            Invoke(nameof(BindSpawner), 0.1f);
    }

    private void OnDisable()
    {
        if (spawner != null)
            spawner.onAllWavesCompleted.RemoveListener(Show);
    }

    public override void Show()
    {
        base.Show();

        if (pauseGameOnShow)
            Time.timeScale = 0f;
    }

    private void BindSpawner()
    {
        if (spawner != null)
            spawner.onAllWavesCompleted.RemoveListener(Show);

        spawner = WaveSpawner.Instance;
        if (spawner == null)
            return;

        spawner.onAllWavesCompleted.AddListener(Show);

        if (spawner.CurrentPhase == WaveSpawner.Phase.AllCompleted)
            Show();
    }
}
