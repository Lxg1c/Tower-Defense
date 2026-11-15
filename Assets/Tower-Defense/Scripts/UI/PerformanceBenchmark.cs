using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

internal sealed class PerformanceBenchmark : IDisposable
{
    private static readonly int[] EnemyCounts = { 0, 16, 32, 64 };
    private const float WarmupSeconds = 1f;
    private const float StageSeconds = 6f;
    private const int GridSize = 8;
    private const float ReducedRenderScale = 0.65f;

    private readonly struct Stage
    {
        internal readonly string ProfileKey;
        internal readonly int Enemies;

        internal Stage(string profileKey, int enemies)
        {
            ProfileKey = profileKey;
            Enemies = enemies;
        }
    }

    private readonly GameObject container;
    private readonly GameObject[] models = new GameObject[EnemyCounts[^1]];
    private readonly List<Stage> stages = new();
    private readonly List<string> reports = new();
    private readonly List<string> graphicsReports = new();
    private readonly List<PerformanceMeasurement.StageMetrics> metrics = new();
    private readonly UniversalRenderPipelineAsset pipeline;
    private readonly UniversalAdditionalCameraData cameraData;
    private readonly ScreenSpaceAmbientOcclusion ssao;
    private readonly float originalScale;
    private readonly bool originalShadows;
    private readonly bool originalSsao;
    private bool disposed;
    private PerformanceMeasurement measurement;
    private int stage;
    private float warmup;

    internal bool IsComplete => stage == StageCount;
    internal int EnemyCount => IsComplete ? 0 : stages[stage].Enemies;
    internal int StageNumber => stage + 1;
    internal int StageCount => stages.Count;
    internal string CurrentProfileKey => stages[stage].ProfileKey;
    internal int SecondsRemaining => Mathf.CeilToInt(warmup + StageSeconds - (float)(measurement?.Elapsed ?? 0d));
    internal string Report { get; private set; }

    internal int EnemiesAtStage(int index) => stages[index].Enemies;
    internal string ProfileAtStage(int index) => stages[index].ProfileKey;
    internal string GraphicsAtStage(int index) => graphicsReports[index];
    internal PerformanceMeasurement.StageMetrics MetricsAt(int index) => metrics[index];

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

        pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        camera.TryGetComponent(out cameraData);
        if (pipeline != null)
        {
            originalScale = pipeline.renderScale;
            // The benchmark camera uses the default renderer in the project's single-renderer profiles.
            if (pipeline.rendererDataList.Length == 1 && pipeline.rendererDataList[0] != null)
                pipeline.rendererDataList[0].TryGetRendererFeature(out ssao);
        }
        originalSsao = ssao != null && ssao.isActive;
        originalShadows = cameraData != null && cameraData.renderShadows;
        foreach (int count in EnemyCounts)
            stages.Add(new Stage(LocalizationKeys.PerfProfileBaseline, count));
        if (originalSsao) AddComparison(LocalizationKeys.PerfProfileNoSsao);
        if (ShadowsEnabled) AddComparison(LocalizationKeys.PerfProfileNoShadows);
        if (pipeline != null && originalScale > ReducedRenderScale) AddComparison(LocalizationKeys.PerfProfileReducedScale);

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
        if (disposed || IsComplete || deltaTime <= 0f) return;
        if (warmup > 0f)
        {
            warmup = Mathf.Max(0f, warmup - deltaTime);
            return;
        }
        measurement.Tick(deltaTime, 0, false, EnemyCount);
        if (measurement.Elapsed < StageSeconds) return;

        string graphics = GraphicsReport();
        graphicsReports.Add(graphics);
        reports.Add(GameLocalization.Text(CurrentProfileKey) + "\n" + graphics + "\n" +
            measurement.Report(benchmark: true));
        metrics.Add(measurement.Metrics);
        measurement.Dispose();
        measurement = null;
        stage++;
        if (IsComplete)
        {
            RestoreGraphics();
            string reportHeader = PerformanceMeasurement.DeviceReport() + "\n" +
                GameLocalization.Text(LocalizationKeys.PerfBenchmarkScope);
            Report = reportHeader + "\n\n" + string.Join("\n\n────────────\n\n", reports);
        }
        else StartStage();
    }

    private void StartStage()
    {
        ApplyGraphics(CurrentProfileKey);
        for (int i = 0; i < models.Length; i++)
            models[i].SetActive(i < EnemyCount);
        measurement = new PerformanceMeasurement();
        warmup = WarmupSeconds;
    }

    private void AddComparison(string profileKey)
    {
        stages.Add(new Stage(profileKey, EnemyCounts[0]));
        stages.Add(new Stage(profileKey, EnemyCounts[^1]));
    }

    private bool ShadowsEnabled => cameraData != null && cameraData.renderShadows &&
        pipeline != null && pipeline.shadowDistance > 0f &&
        (pipeline.supportsMainLightShadows || pipeline.supportsAdditionalLightShadows);

    private void ApplyGraphics(string profileKey)
    {
        if (ssao != null) ssao.SetActive(originalSsao && profileKey != LocalizationKeys.PerfProfileNoSsao);
        if (cameraData != null)
            cameraData.renderShadows = originalShadows && profileKey != LocalizationKeys.PerfProfileNoShadows;
        if (pipeline != null)
            pipeline.renderScale = profileKey == LocalizationKeys.PerfProfileReducedScale ? ReducedRenderScale : originalScale;
    }

    private void RestoreGraphics() => ApplyGraphics(LocalizationKeys.PerfProfileBaseline);

    private string GraphicsReport()
    {
        string Toggle(bool enabled) => GameLocalization.Text(enabled ? LocalizationKeys.PerfOn : LocalizationKeys.PerfOff);
        return GameLocalization.Format(LocalizationKeys.PerfRenderSettings, pipeline != null ? pipeline.renderScale : 1f,
            Toggle(ssao != null && ssao.isActive), Toggle(ShadowsEnabled));
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        measurement?.Dispose();
        measurement = null;
        if (!IsComplete) RestoreGraphics();
        if (container != null) Object.Destroy(container);
    }
}
