using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

/// <summary>Runs a repeatable live engagement independent of campaign waves.</summary>
internal sealed class PerformanceBenchmarkCombat : IDisposable
{
    private const float SpawnInterval = .07f;
    private const float StructureHealth = 10000f;
    private readonly GameObject container;
    private readonly MobCore[] sequence;
    private readonly Vector3[] spawnPositions;
    private readonly Dictionary<MobCore, MobPool> pools = new();
    private readonly List<(MobCore Mob, MobPool Pool)> active = new();
    private readonly Quaternion heading;
    private float spawnTimer;

    internal int AliveEnemies => active.Count;
    internal int SpawnedEnemies { get; private set; }
    internal int PeakEnemies { get; private set; }

    internal PerformanceBenchmarkCombat(TowerLoadout loadout, Vector3 center, Vector3 approach,
        MobCore[] sequence, int enemyCount, int defenseCount, Transform parent)
    {
        GameObject core = loadout.options.First(option => option.slotType == BuildSlotType.TownHall).prefab;
        GameObject[] defenses = loadout.options.Where(option => option.slotType == BuildSlotType.Defense)
            .Select(option => option.prefab).ToArray();
        this.sequence = sequence;
        container = new GameObject("Benchmark combat");
        container.transform.SetParent(parent, false);
        if (approach.sqrMagnitude < .001f) approach = Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, approach);
        heading = Quaternion.LookRotation(-approach);
        spawnPositions = new Vector3[enemyCount];
        for (int i = 0; i < spawnPositions.Length; i++)
        {
            Vector3 position = center + approach * (5.5f + i / 8 * 1.25f) + side * ((i % 8 - 3.5f) * 1.45f);
            spawnPositions[i] = GroundPosition(position);
        }

        Build(core, center - approach * 6.2f, Quaternion.LookRotation(approach));
        for (int i = 0; i < defenseCount; i++)
        {
            Vector3 position = center + approach * (-2.5f + i / 4 * 2.8f) + side * ((i % 4 - 1.5f) * 3.2f);
            Build(defenses[i % defenses.Length], position, Quaternion.LookRotation(approach));
        }
    }

    private void Build(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject building = Object.Instantiate(prefab, GroundPosition(position), rotation, container.transform);
        building.GetComponent<Damageable>().SetMaxHealth(StructureHealth);
        GraphicsMaterialProfiles.Apply(building);
    }

    internal void Tick(float deltaTime)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var entry = active[i];
            if (entry.Mob != null && entry.Mob.Health.IsAlive) continue;
            entry.Pool.Return(entry.Mob);
            active.RemoveAt(i);
        }

        if (active.Count >= spawnPositions.Length)
        {
            spawnTimer = 0f;
            return;
        }

        spawnTimer -= deltaTime;
        while (spawnTimer <= 0f && active.Count < spawnPositions.Length)
        {
            MobCore prefab = sequence[SpawnedEnemies % sequence.Length];
            if (!pools.TryGetValue(prefab, out MobPool pool))
            {
                pool = new MobPool(prefab, container.transform);
                pools.Add(prefab, pool);
            }

            MobCore mob = pool.Get(spawnPositions[SpawnedEnemies % spawnPositions.Length], heading);
            active.Add((mob, pool));
            SpawnedEnemies++;
            spawnTimer += SpawnInterval;
        }
        PeakEnemies = Mathf.Max(PeakEnemies, active.Count);
    }

    private static Vector3 GroundPosition(Vector3 position) =>
        NavMesh.SamplePosition(position, out var hit, 2.5f, NavMesh.AllAreas) ? hit.position : position;

    public void Dispose()
    {
        if (container == null) return;
        container.SetActive(false);
        Object.Destroy(container);
    }
}
