using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Shared list of enemies available for wave setup.
/// Create via Assets -> Create -> Enemies -> Loadout.
/// </summary>
[CreateAssetMenu(menuName = "Enemies/Loadout", fileName = "EnemyLoadout")]
public class EnemyLoadout : ScriptableObject
{
    public EnemyOption[] options;

    [System.Serializable]
    public struct SlowAmmoDrop
    {
        public EnemyOption enemy;
        [Range(0f, 1f)] public float chance;
        [FormerlySerializedAs("amount"), Min(0.1f)] public float durationSeconds;
    }

    [Header("Slow ammo drops")]
    public SlowAmmoPickup slowAmmoPickupPrefab;
    public SlowAmmoDrop[] slowAmmoDrops;

    public void Validate()
    {
        if (options == null || options.Length == 0 || slowAmmoDrops == null)
            throw new ArgumentException($"{name}: assign enemy options and a drop entry for each one.");

        var seen = new HashSet<EnemyOption>();
        foreach (SlowAmmoDrop drop in slowAmmoDrops)
        {
            if (drop.enemy == null || !seen.Add(drop.enemy))
                throw new ArgumentException($"{name}: drop entries need unique enemy options.");
            if (float.IsNaN(drop.chance) || drop.chance < 0f || drop.chance > 1f ||
                (drop.chance > 0f && (float.IsNaN(drop.durationSeconds) ||
                                      float.IsInfinity(drop.durationSeconds) ||
                                      drop.durationSeconds <= 0f || slowAmmoPickupPrefab == null)))
                throw new ArgumentException($"{name}: {drop.enemy.name} has invalid slow ammo drop settings.");
        }

        foreach (EnemyOption option in options)
            if (option == null || !seen.Contains(option))
                throw new ArgumentException($"{name}: every enemy option needs a drop entry (use 0% for no drop).");
    }

    public bool TryGetSlowAmmoDrop(EnemyOption enemy, out SlowAmmoDrop drop)
    {
        if (enemy != null && slowAmmoDrops != null)
        {
            foreach (SlowAmmoDrop candidate in slowAmmoDrops)
            {
                if (candidate.enemy != enemy) continue;
                drop = candidate;
                return true;
            }
        }

        drop = default;
        return false;
    }
}
