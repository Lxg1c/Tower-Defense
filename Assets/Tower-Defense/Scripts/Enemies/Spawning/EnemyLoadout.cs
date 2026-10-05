using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Enemy types and their pickup chances for this level.</summary>
[CreateAssetMenu(menuName = "Enemies/Loadout", fileName = "EnemyLoadout")]
public class EnemyLoadout : ScriptableObject
{
    public enum PickupKind { None, SlowAmmo, Shield, Companion }

    [Serializable]
    public struct EnemyEntry
    {
        public EnemyOption enemy;
        [Range(0f, 1f)] public float slowAmmoChance;
        [Min(0.1f)] public float slowAmmoDurationSeconds;
        [Range(0f, 1f)] public float shieldChance;
        [Min(0.1f)] public float shieldDurationSeconds;
        [Range(0f, 1f)] public float companionChance;
        [Min(0.1f)] public float companionDurationSeconds;

        public PickupKind PickDrop(float roll)
        {
            // Random.value can return 1 exactly; keep a full 100% table exhaustive.
            roll = Mathf.Min(roll, 0.99999994f);
            if (roll < slowAmmoChance) return PickupKind.SlowAmmo;
            roll -= slowAmmoChance;
            if (roll < shieldChance) return PickupKind.Shield;
            roll -= shieldChance;
            return roll < companionChance ? PickupKind.Companion : PickupKind.None;
        }
    }

    [Header("Enemies and pickup chances")]
    public EnemyEntry[] enemies;

    [Header("Pickup prefabs")]
    public SlowAmmoPickup slowAmmoPickupPrefab;
    public ShieldPickup shieldPickupPrefab;
    public CompanionTurretPickup companionPickupPrefab;

    public void Validate()
    {
        if (enemies == null || enemies.Length == 0)
            throw new ArgumentException($"{name}: assign at least one enemy entry.");

        var seen = new HashSet<EnemyOption>();
        foreach (EnemyEntry entry in enemies)
        {
            if (entry.enemy == null || !seen.Add(entry.enemy))
                throw new ArgumentException($"{name}: enemy entries must be assigned and unique.");
            ValidateDrop(entry.enemy, "slow ammo", entry.slowAmmoChance,
                entry.slowAmmoDurationSeconds, slowAmmoPickupPrefab);
            ValidateDrop(entry.enemy, "shield", entry.shieldChance,
                entry.shieldDurationSeconds, shieldPickupPrefab);
            ValidateDrop(entry.enemy, "companion turret", entry.companionChance,
                entry.companionDurationSeconds, companionPickupPrefab);
            if (entry.slowAmmoChance + entry.shieldChance + entry.companionChance > 1.00001f)
                throw new ArgumentException($"{name}: {entry.enemy.name} pickup chances must total at most 100%.");
        }
    }

    private void ValidateDrop(EnemyOption enemy, string kind, float chance,
        float duration, UnityEngine.Object prefab)
    {
        if (float.IsNaN(chance) || float.IsInfinity(chance) || chance < 0f || chance > 1f ||
            (chance > 0f && (float.IsNaN(duration) || float.IsInfinity(duration) ||
                              duration <= 0f || prefab == null)))
            throw new ArgumentException($"{name}: {enemy.name} has invalid {kind} drop settings.");
    }

    public bool TryGetEntry(EnemyOption enemy, out EnemyEntry entry)
    {
        if (enemy != null && enemies != null)
            foreach (EnemyEntry candidate in enemies)
                if (candidate.enemy == enemy)
                {
                    entry = candidate;
                    return true;
                }

        entry = default;
        return false;
    }
}
