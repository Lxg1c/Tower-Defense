using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerSkill { Vitality, Firepower, RapidFire }

/// <summary>Permanent upgrades and replayable victory rewards.</summary>
[Serializable]
public sealed class SkillTreeState
{
    public const int MaxRank = 3;

    [Serializable]
    private sealed class LevelClear
    {
        public string levelId;
        public int highestDifficulty;
    }

    [Serializable]
    private sealed class LegacySave
    {
        public int version;
        public int points;
        public int vitalityRank;
        public int firepowerRank;
        public int rapidFireRank;
        public List<string> claimedLevels;
    }

    [SerializeField] private int version = 2;
    [SerializeField] private int money;
    [SerializeField] private int vitalityRank;
    [SerializeField] private int firepowerRank;
    [SerializeField] private int rapidFireRank;
    [SerializeField] private List<LevelClear> clearedLevels = new();

    public int Money => money;
    public float HealthMultiplier => 1f + .15f * vitalityRank;
    public float DamageMultiplier => 1f + .10f * firepowerRank;
    public float FireRateMultiplier => 1f + .08f * rapidFireRank;

    public int Rank(PlayerSkill skill) => skill switch
    {
        PlayerSkill.Vitality => vitalityRank,
        PlayerSkill.Firepower => firepowerRank,
        PlayerSkill.RapidFire => rapidFireRank,
        _ => throw new ArgumentOutOfRangeException(nameof(skill))
    };

    public static SkillTreeState MigrateLegacy(string json)
    {
        var old = JsonUtility.FromJson<LegacySave>(json);
        if (old == null || old.version != 1 || old.points < 0 ||
            old.vitalityRank < 0 || old.vitalityRank > MaxRank ||
            old.firepowerRank < 0 || old.firepowerRank > MaxRank ||
            old.rapidFireRank < 0 || old.rapidFireRank > MaxRank || old.claimedLevels == null)
            throw new InvalidOperationException("Saved skill progression is invalid.");
        var result = new SkillTreeState
        {
            money = checked(old.points * 100),
            vitalityRank = old.vitalityRank,
            firepowerRank = old.firepowerRank,
            rapidFireRank = old.rapidFireRank
        };
        foreach (string id in old.claimedLevels)
        {
            if (string.IsNullOrWhiteSpace(id) || result.HighestClearedDifficulty(id) != 0)
                throw new InvalidOperationException("Saved level claims are invalid.");
            result.clearedLevels.Add(new LevelClear { levelId = id, highestDifficulty = 1 });
        }
        result.Validate();
        return result;
    }

    public int HighestClearedDifficulty(string levelId)
    {
        if (string.IsNullOrWhiteSpace(levelId)) return 0;
        return clearedLevels.Find(clear => clear.levelId == levelId)?.highestDifficulty ?? 0;
    }

    public bool IsDifficultyUnlocked(string levelId, int difficulty) =>
        difficulty >= 1 && difficulty <= 3 && HighestClearedDifficulty(levelId) >= difficulty - 1;

    public void RecordVictory(string levelId, int difficulty, int reward)
    {
        if (string.IsNullOrWhiteSpace(levelId)) throw new ArgumentException("A stable level ID is required.", nameof(levelId));
        if (difficulty < 1 || difficulty > 3) throw new ArgumentOutOfRangeException(nameof(difficulty));
        if (reward < 0) throw new ArgumentOutOfRangeException(nameof(reward));
        if (!IsDifficultyUnlocked(levelId, difficulty)) throw new InvalidOperationException("This difficulty is locked.");
        int newBalance = checked(money + reward);
        var clear = clearedLevels.Find(entry => entry.levelId == levelId);
        if (clear == null) clearedLevels.Add(new LevelClear { levelId = levelId, highestDifficulty = difficulty });
        else clear.highestDifficulty = Math.Max(clear.highestDifficulty, difficulty);
        money = newBalance;
    }

    public int NextCost(PlayerSkill skill)
    {
        int rank = Rank(skill);
        return rank >= MaxRank ? 0 : (rank + 1) * 100;
    }

    public bool IsUnlocked(PlayerSkill skill) =>
        skill == PlayerSkill.Vitality || (Rank(skill) >= 0 && vitalityRank > 0);

    public bool TryUpgrade(PlayerSkill skill)
    {
        int cost = NextCost(skill);
        if (cost == 0 || !IsUnlocked(skill) || !TrySpend(cost)) return false;
        switch (skill)
        {
            case PlayerSkill.Vitality: vitalityRank++; break;
            case PlayerSkill.Firepower: firepowerRank++; break;
            case PlayerSkill.RapidFire: rapidFireRank++; break;
        }
        return true;
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (money < amount) return false;
        money -= amount;
        return true;
    }

    public void Validate()
    {
        if (version != 2 || money < 0 || clearedLevels == null ||
            vitalityRank < 0 || vitalityRank > MaxRank ||
            firepowerRank < 0 || firepowerRank > MaxRank ||
            rapidFireRank < 0 || rapidFireRank > MaxRank)
            throw new InvalidOperationException("Saved skill progression is invalid.");
        var ids = new HashSet<string>();
        foreach (var clear in clearedLevels)
            if (clear == null || string.IsNullOrWhiteSpace(clear.levelId) ||
                clear.highestDifficulty < 1 || clear.highestDifficulty > 3 || !ids.Add(clear.levelId))
                throw new InvalidOperationException("Saved level progression is invalid.");
    }
}
