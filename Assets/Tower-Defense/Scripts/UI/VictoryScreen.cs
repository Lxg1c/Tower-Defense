using UnityEngine;

[DisallowMultipleComponent]
public class VictoryScreen : MenuScreenBase
{
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private bool pauseGameOnShow = true;

    private void OnEnable()
    {
        if (spawner == null)
        {
            Debug.LogError("[VictoryScreen] Assign the session spawner.", this);
            return;
        }
        spawner.onAllWavesCompleted.AddListener(Show);
        if (spawner.CurrentPhase == WaveSpawner.Phase.AllCompleted) Show();
    }

    private void OnDisable()
    {
        if (spawner != null) spawner.onAllWavesCompleted.RemoveListener(Show);
    }

    public override void Show()
    {
        base.Show();
        if (pauseGameOnShow) Time.timeScale = 0f;
    }
}
