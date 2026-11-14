using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

internal sealed class PerformanceBenchmark : IDisposable
{
    private static readonly int[] EnemyCounts = { 0, 16, 32, 64 };
    private const float WarmupSeconds = 1f;
    private const float StageSeconds = 6f;
    private const int GridSize = 8;

    private readonly GameObject container;
    private readonly GameObject[] models = new GameObject[EnemyCounts[^1]];
    private readonly string[] reports = new string[EnemyCounts.Length];
    private PerformanceMeasurement measurement;
    private int stage;
    private float warmup;

    internal bool IsComplete => stage == EnemyCounts.Length;
    internal int EnemyCount => IsComplete ? 0 : EnemyCounts[stage];
    internal int StageNumber => stage + 1;
    internal int StageCount => EnemyCounts.Length;
    internal int SecondsRemaining => Mathf.CeilToInt(warmup + StageSeconds - (float)(measurement?.Elapsed ?? 0d));
    internal string Report { get; private set; }
    private string reportHeader;

    internal int EnemiesAtStage(int index) => EnemyCounts[index];
    internal string ReportForStage(int index) => reportHeader + "\n\n" + reports[index];

    internal PerformanceBenchmark(WaveSpawner spawner, Camera camera, Vector3 referencePosition)
    {
        var prefabs = new List<MobCore>();
        var weights = new List<int>();
        int totalWeight = 0;
        for (int wave = 0; wave < spawner.WaveCount; wave++)
        {
            foreach (var group in spawner.GetWave(wave).spawnGroups)
                foreach (var entry in group.entries)
                {
                    var prefab = entry.Prefab;
                    if (prefab == null) continue;
                    int index = prefabs.IndexOf(prefab);
                    if (index < 0)
                    {
                        prefabs.Add(prefab);
                        weights.Add(entry.count);
                    }
                    else weights[index] += entry.count;
                    totalWeight += entry.count;
                }
        }
        if (prefabs.Count == 0 || camera == null)
            throw new InvalidOperationException("The benchmark needs configured enemy prefabs and a camera.");

        container = new GameObject("Performance benchmark models");
        container.SetActive(false);
        var ground = new Plane(Vector3.up, referencePosition);
        var placed = new int[prefabs.Count];
        for (int i = 0; i < models.Length; i++)
        {
            // Keep the mix close to the authored waves at every stage.
            int type = 0;
            long biggestDeficit = long.MinValue;
            for (int candidate = 0; candidate < prefabs.Count; candidate++)
            {
                long deficit = (i + 1L) * weights[candidate] - (long)placed[candidate] * totalWeight;
                if (deficit <= biggestDeficit) continue;
                biggestDeficit = deficit;
                type = candidate;
            }
            placed[type]++;

            // Interleave the grid so every stage fills the same part of the view.
            int cell = (i * 27) % models.Length;
            float x = Mathf.Lerp(0.17f, 0.83f, (cell % GridSize) / (GridSize - 1f));
            float y = Mathf.Lerp(0.14f, 0.73f, (cell / GridSize) / (GridSize - 1f));
            Ray ray = camera.ViewportPointToRay(new Vector3(x, y));
            Vector3 position = ground.Raycast(ray, out float distance) ? ray.GetPoint(distance) : referencePosition;
            var model = Object.Instantiate(prefabs[type].gameObject, position,
                Quaternion.Euler(0f, 180f, 0f), container.transform);
            foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            foreach (var agent in model.GetComponentsInChildren<NavMeshAgent>(true))
                agent.enabled = false;
            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }
            GraphicsMaterialProfiles.Apply(model);
            model.SetActive(false);
            models[i] = model;
        }
        container.SetActive(true);
        StartStage();
    }

    internal void Tick(float deltaTime)
    {
        if (IsComplete || deltaTime <= 0f) return;
        if (warmup > 0f)
        {
            warmup = Mathf.Max(0f, warmup - deltaTime);
            return;
        }
        measurement.Tick(deltaTime, 0, false, EnemyCount);
        if (measurement.Elapsed < StageSeconds) return;

        reports[stage] = measurement.Report(benchmark: true);
        measurement.Dispose();
        measurement = null;
        stage++;
        if (IsComplete)
        {
            reportHeader = PerformanceMeasurement.DeviceReport() + "\n" +
                GameLocalization.Text(LocalizationKeys.PerfBenchmarkScope);
            Report = reportHeader + "\n\n" + string.Join("\n\n────────────\n\n", reports);
        }
        else StartStage();
    }

    private void StartStage()
    {
        for (int i = 0; i < models.Length; i++)
            models[i].SetActive(i < EnemyCount);
        measurement = new PerformanceMeasurement();
        warmup = WarmupSeconds;
    }

    public void Dispose()
    {
        measurement?.Dispose();
        measurement = null;
        if (container != null) Object.Destroy(container);
    }
}
