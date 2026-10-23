using System;
using UnityEngine;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class LevelSetup : MonoBehaviour
{
    [Tooltip("Explicit level to run when entering this scene directly in the Editor.")]
    [SerializeField] private LevelDefinition directPlayLevel;
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private PlayerWallet wallet;
    private bool restorePlayerEnemyCollision;
    public LevelDefinition CurrentLevel { get; private set; }
    public int CurrentDifficulty { get; private set; }
    private readonly VictoryRewardGate rewardGate = new();
    public bool HasAwardedVictory => rewardGate.HasAwarded;
    public int VictoryMoneyReward { get; private set; }

    private void Awake()
    {
        try
        {
            if (spawner == null || wallet == null) throw new ArgumentException("Assign the spawner and wallet.");
            var level = RunSelection.Resolve(gameObject.scene.name, directPlayLevel);
            int difficulty = RunSelection.Difficulty;
            if (!PlayerProgression.State.IsDifficultyUnlocked(level.progressionId, difficulty))
                throw new ArgumentException($"Difficulty {difficulty} is locked for {level.displayName}.");
            spawner.ConfigureLevel(level, difficulty);
            wallet.ConfigureStartingCoins(level.GetStartingCoins(difficulty));
            CurrentLevel = level;
            CurrentDifficulty = difficulty;
            int playerLayer = LayerMask.NameToLayer("Player");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (playerLayer < 0 || enemyLayer < 0)
                throw new ArgumentException("Define Player and Enemy physics layers.");
            restorePlayerEnemyCollision = !Physics.GetIgnoreLayerCollision(playerLayer, enemyLayer);
            Physics.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        }
        catch (ArgumentException exception)
        {
            if (spawner != null) spawner.enabled = false;
            Debug.LogError($"[LevelSetup] {exception.Message}", this);
            enabled = false;
        }
    }

    public bool TryAwardVictory()
    {
        if (CurrentLevel == null || spawner == null ||
            spawner.CurrentPhase != WaveSpawner.Phase.AllCompleted) return false;
        int reward = CurrentLevel.GetMoneyReward(CurrentDifficulty);
        return rewardGate.TryAward(() =>
        {
            PlayerProgression.RecordVictory(CurrentLevel.progressionId, CurrentDifficulty, reward);
            VictoryMoneyReward = reward;
        });
    }

    private void OnDestroy()
    {
        if (!restorePlayerEnemyCollision) return;
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
    }
}
