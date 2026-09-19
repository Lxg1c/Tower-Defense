using UnityEngine;

[DisallowMultipleComponent]
public class GameOverScreen : MenuScreenBase
{
    [SerializeField] private WaveSpawner spawner;

    private void OnEnable()
    {
        if (spawner == null)
        {
            Debug.LogError("[GameOverScreen] Assign the session spawner.", this);
            return;
        }
        spawner.onDefeated.AddListener(Show);
        if (spawner.CurrentPhase == WaveSpawner.Phase.Defeated) Show();
    }

    private void OnDisable()
    {
        if (spawner != null) spawner.onDefeated.RemoveListener(Show);
    }

    public override void Show()
    {
        base.Show();
        Time.timeScale = 0f;
    }
}
