using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Level", fileName = "Level")]
public sealed class LevelDefinition : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public string difficulty;
    [Tooltip("Stable ID for saved difficulty progress. Keep it unchanged after release.")]
    public string progressionId;
    [Min(0)] public int moneyReward = 100;
    [Tooltip("Next authored level after victory. Leave empty for the final level.")]
    public LevelDefinition nextLevel;
    [Min(0)] public int startingCoins = 10;
    [Tooltip("Groups run in order. Point 0 is the first entry in the scene's Spawn Points array.")]
    public LevelWave[] waves;
    [Tooltip("Exactly two authored variants: difficulty 2, then difficulty 3.")]
    public DifficultyVariant[] additionalDifficulties;

    [Serializable]
    public sealed class DifficultyVariant
    {
        [Min(0)] public int startingCoins;
        [Min(0)] public int moneyReward;
        public LevelWave[] waves;
    }

    [Serializable]
    public sealed class LevelWave
    {
        [Min(0)] public int coinReward = 4;
        public LevelSpawnGroup[] spawnGroups;
    }

    [Serializable]
    public sealed class LevelSpawnGroup
    {
        [Min(0)] public int spawnPointIndex;
        public LevelEnemyEntry[] entries;
    }

    [Serializable]
    public sealed class LevelEnemyEntry
    {
        public EnemyOption enemy;
        [Min(1)] public int count = 1;
        [Min(0)] public float spawnInterval = 1;
    }

    public int GetStartingCoins(int difficulty) =>
        difficulty == 1 ? startingCoins : Variant(difficulty).startingCoins;

    public int GetMoneyReward(int difficulty) =>
        difficulty == 1 ? moneyReward : Variant(difficulty).moneyReward;

    public LevelWave[] GetWaves(int difficulty) =>
        difficulty == 1 ? waves : Variant(difficulty).waves;

    private DifficultyVariant Variant(int difficulty)
    {
        if (difficulty < 2 || difficulty > 3 || additionalDifficulties == null ||
            additionalDifficulties.Length != 2 || additionalDifficulties[difficulty - 2] == null)
            throw new ArgumentException($"{name}: configure difficulty {difficulty}.");
        return additionalDifficulties[difficulty - 2];
    }

    public void Validate(int spawnPointCount = int.MaxValue, int difficulty = 1)
    {
        if (difficulty < 1 || difficulty > 3) throw new ArgumentOutOfRangeException(nameof(difficulty));
        var selectedWaves = GetWaves(difficulty);
        if (string.IsNullOrWhiteSpace(displayName) || GetStartingCoins(difficulty) < 0 ||
            selectedWaves == null || selectedWaves.Length == 0)
            throw new ArgumentException($"{name}: assign a level name, nonnegative starting coins and at least one wave.");
        if (GetMoneyReward(difficulty) < 0 || string.IsNullOrWhiteSpace(progressionId))
            throw new ArgumentException($"{name}: assign a stable progression ID and nonnegative money reward.");
        for (int w = 0; w < selectedWaves.Length; w++)
        {
            var wave = selectedWaves[w];
            if (wave == null || wave.coinReward < 0 || wave.spawnGroups == null || wave.spawnGroups.Length == 0)
                throw new ArgumentException($"{name}, wave {w + 1}: assign spawn groups and a nonnegative reward.");
            foreach (var group in wave.spawnGroups)
            {
                if (group == null || group.spawnPointIndex < 0 || group.spawnPointIndex >= spawnPointCount ||
                    group.entries == null || group.entries.Length == 0)
                    throw new ArgumentException($"{name}, wave {w + 1}: a group has an invalid spawn point or no enemies.");
                foreach (var entry in group.entries)
                    if (entry == null || entry.enemy == null || entry.enemy.prefab == null ||
                        entry.enemy.prefab.GetComponent<MobHealth>() == null || entry.count <= 0 ||
                        float.IsNaN(entry.spawnInterval) || float.IsInfinity(entry.spawnInterval) || entry.spawnInterval < 0)
                        throw new ArgumentException($"{name}, wave {w + 1}: assign a valid enemy, positive count and finite nonnegative interval.");
            }
        }
    }

    public void ValidateAll(int spawnPointCount = int.MaxValue)
    {
        for (int difficulty = 1; difficulty <= 3; difficulty++)
            Validate(spawnPointCount, difficulty);
    }

    public WaveSpawner.Wave[] CreateWaves(int spawnPointCount, int difficulty = 1)
    {
        Validate(spawnPointCount, difficulty);
        var selectedWaves = GetWaves(difficulty);
        var result = new WaveSpawner.Wave[selectedWaves.Length];
        for (int w = 0; w < selectedWaves.Length; w++)
        {
            var groups = selectedWaves[w].spawnGroups;
            var copy = new WaveSpawner.SpawnGroup[groups.Length];
            for (int g = 0; g < groups.Length; g++)
            {
                var entries = groups[g].entries;
                var cloned = new WaveSpawner.MobEntry[entries.Length];
                for (int e = 0; e < entries.Length; e++)
                    cloned[e] = new WaveSpawner.MobEntry { enemy = entries[e].enemy, count = entries[e].count, spawnInterval = entries[e].spawnInterval };
                copy[g] = new WaveSpawner.SpawnGroup { spawnPointIndex = groups[g].spawnPointIndex, entries = cloned };
            }
            result[w] = new WaveSpawner.Wave { coinReward = selectedWaves[w].coinReward, spawnGroups = copy };
        }
        return result;
    }
}
