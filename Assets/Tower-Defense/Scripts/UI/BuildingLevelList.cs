using System.Collections.Generic;
using UnityEngine;

/// <summary>Read-only progression preview from the authored prefab, without spawning a building.</summary>
public static class BuildingLevelList
{
    public readonly struct Level
    {
        public readonly float? Health;
        public readonly float? Damage;
        public readonly int? Income;
        public Level(float? health, float? damage, int? income)
        { Health = health; Damage = damage; Income = income; }
    }

    public static IReadOnlyList<Level> Read(TowerOption option)
    {
        var prefab = option.prefab;
        var hp = prefab.GetComponentInChildren<Damageable>(true);
        var shooter = prefab.GetComponentInChildren<Shooter>(true);
        var pulse = prefab.GetComponentInChildren<PulseTower>(true);
        var mine = prefab.GetComponentInChildren<MineIncome>(true);
        float? health = hp != null ? hp.MaxHealth : (float?)null;
        float? damage = shooter != null ? shooter.Damage : pulse != null ? pulse.Damage : (float?)null;
        int? income = mine != null ? mine.CoinsPerWave : (int?)null;
        var result = new List<Level> { new Level(health, damage, income) };
        var tower = prefab.GetComponentInChildren<TowerUpgrade>(true);
        var townHall = prefab.GetComponentInChildren<BaseUpgrade>(true);
        if (townHall != null && townHall.Levels != null)
            foreach (var level in townHall.Levels)
                result.Add(new Level(health * Mathf.Max(0, level.hpMul), null, null));
        else if (tower != null && tower.Levels != null)
            foreach (var level in tower.Levels)
                result.Add(new Level(health * Mathf.Max(0, level.hpMul),
                    damage * Mathf.Max(0, level.damageMul), income.HasValue
                        ? Mathf.RoundToInt(income.Value * Mathf.Max(0, level.incomeMul)) : (int?)null));
        return result;
    }
}
