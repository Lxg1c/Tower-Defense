using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Level", fileName = "Level")]
public sealed class LevelDefinition : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public string difficulty;
    [Tooltip("Next authored level after victory. Leave empty for the final level.")]
    public LevelDefinition nextLevel;
    [Min(0)] public int startingCoins = 10;
    [Tooltip("Groups run in order. Point 0 is the first entry in the scene's Spawn Points array.")]
    public LevelWave[] waves;

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

    public void Validate(int spawnPointCount = int.MaxValue)
    {
        if (string.IsNullOrWhiteSpace(displayName) || startingCoins < 0 || waves == null || waves.Length == 0)
            throw new ArgumentException($"{name}: assign a level name, nonnegative starting coins and at least one wave.");
        for (int w = 0; w < waves.Length; w++)
        {
            var wave = waves[w];
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

    public WaveSpawner.Wave[] CreateWaves(int spawnPointCount)
    {
        Validate(spawnPointCount);
        var result = new WaveSpawner.Wave[waves.Length];
        for (int w = 0; w < waves.Length; w++)
        {
            var groups = waves[w].spawnGroups;
            var copy = new WaveSpawner.SpawnGroup[groups.Length];
            for (int g = 0; g < groups.Length; g++)
            {
                var entries = groups[g].entries;
                var cloned = new WaveSpawner.MobEntry[entries.Length];
                for (int e = 0; e < entries.Length; e++)
                    cloned[e] = new WaveSpawner.MobEntry { enemy = entries[e].enemy, count = entries[e].count, spawnInterval = entries[e].spawnInterval };
                copy[g] = new WaveSpawner.SpawnGroup { spawnPointIndex = groups[g].spawnPointIndex, entries = cloned };
            }
            result[w] = new WaveSpawner.Wave { coinReward = waves[w].coinReward, spawnGroups = copy };
        }
        return result;
    }
}
