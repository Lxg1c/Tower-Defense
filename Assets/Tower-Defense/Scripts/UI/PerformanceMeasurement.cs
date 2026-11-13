using System;
using Unity.Profiling;
using UnityEngine;

internal sealed class PerformanceMeasurement : IDisposable
{
    private const double FrameBudget = 1000d / 60d;
    private readonly TimingDistribution frames = new();
    private readonly TimingDistribution mainThread = new();
    private readonly TimingDistribution renderThread = new();
    private readonly TimingDistribution gpu = new();
    private readonly FrameTiming[] timings = new FrameTiming[1];
    private ProfilerRecorder drawCalls;
    private ProfilerRecorder memory;
    private ProfilerRecorder gc;
    private ulong lastTimestamp;
    private double combatSeconds;
    private int firstWave;
    private int lastWave;
    private int peakEnemies;
    private int missedBudget;
    private long totalDrawCalls;
    private int drawCallSamples;
    private long peakDrawCalls;
    private long peakMemory;
    private long peakGc;
    private bool hasMemory;
    private bool hasGc;

    internal double Elapsed { get; private set; }
    internal bool HasFrames => frames.Count != 0;

    internal PerformanceMeasurement()
    {
        drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
        memory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory", 1);
        gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
    }

    internal void Tick(float deltaTime, int wave, bool combat, int enemies)
    {
        if (deltaTime <= 0f || !float.IsFinite(deltaTime)) return;
        if (FrameTimingManager.IsFeatureEnabled())
        {
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timings) != 0 &&
                timings[0].frameStartTimestamp != lastTimestamp)
            {
                lastTimestamp = timings[0].frameStartTimestamp;
                mainThread.Add(timings[0].cpuMainThreadFrameTime);
                renderThread.Add(timings[0].cpuRenderThreadFrameTime);
                gpu.Add(timings[0].gpuFrameTime);
            }
        }

        Elapsed += deltaTime;
        if (combat) combatSeconds += deltaTime;
        if (!HasFrames) firstWave = wave;
        lastWave = wave;
        peakEnemies = Mathf.Max(peakEnemies, enemies);
        double milliseconds = deltaTime * 1000d;
        frames.Add(milliseconds);
        if (milliseconds > FrameBudget) missedBudget++;
        if (drawCalls.Valid && drawCalls.Count > 0)
        {
            long count = drawCalls.LastValue;
            totalDrawCalls += count;
            drawCallSamples++;
            peakDrawCalls = Math.Max(peakDrawCalls, count);
        }
        ReadPeak(memory, ref peakMemory, ref hasMemory);
        ReadPeak(gc, ref peakGc, ref hasGc);
    }

    internal string Summary() => HasFrames
        ? GameLocalization.Format("perf-run-summary", "{0:0.0} с · {1:0.0} FPS · P95 {2:0.0} мс",
            Elapsed, frames.Count / Elapsed, frames.Percentile95)
        : GameLocalization.Text("perf-no-frames", "Не успели собрать кадры для отчёта.");

    internal static string DeviceReport()
    {
        string lighting = GamePerformanceSettings.Lighting == GamePerformanceSettings.LightingMode.Simple
            ? GameLocalization.Text("graphics-simple", "Упрощённое") : GameLocalization.Text("graphics-standard", "Обычное");
        string normals = GamePerformanceSettings.NormalMaps
            ? GameLocalization.Text("perf-on", "вкл.") : GameLocalization.Text("perf-off", "выкл.");
        return GameLocalization.Format("perf-device", "{0} · {1} · {2} × {3}",
            SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceType, Screen.width, Screen.height) + "\n" +
            GameLocalization.Format("perf-graphics", "Освещение: {0} · карты нормалей: {1}", lighting, normals);
    }

    internal string Report(bool benchmark = false)
    {
        if (!HasFrames) return Summary();
        double frameP95 = frames.Percentile95;
        double cpuP95 = mainThread.Percentile95;
        double renderP95 = renderThread.Percentile95;
        double gpuP95 = gpu.Percentile95;
        string unavailable = GameLocalization.Text("perf-unavailable", "нет данных");
        string Metric(double value) => value > 0d ? value.ToString("0.0") : unavailable;

        double cpuLimit = Math.Max(cpuP95, renderP95);
        string diagnosis;
        if (frameP95 <= FrameBudget + 0.35d)
            diagnosis = GameLocalization.Text("perf-ok", "На этом отрезке 95% кадров уложились примерно в бюджет 60 FPS.");
        else if (gpuP95 > FrameBudget && cpuLimit > 0d && gpuP95 >= cpuLimit * 1.1d)
            diagnosis = GameLocalization.Text("perf-gpu-limit", "Вероятный упор в GPU. Проверь освещение, тени, прозрачные эффекты и масштаб рендера.");
        else if (cpuLimit > FrameBudget && gpuP95 > 0d && cpuLimit >= gpuP95 * 1.1d)
            diagnosis = benchmark
                ? GameLocalization.Text("perf-benchmark-cpu", "Вероятный упор в CPU. Проверь время подготовки отрисовки, анимации и сборку мусора.")
                : GameLocalization.Text("perf-cpu-limit", "Вероятный упор в CPU. Проверь поведение врагов, физику, вызовы отрисовки и сборку мусора.");
        else
            diagnosis = GameLocalization.Text("perf-uncertain", "Есть медленные кадры. По этим данным узкое место не удалось выделить.");

        string context = benchmark
            ? GameLocalization.Format("perf-benchmark-stage", "Врагов: {0} · {1:0.0} с", peakEnemies, Elapsed)
            : GameLocalization.Format("perf-run-context", "{0:0.0} с · волны {1}–{2} · в бою {3:0.0} с\nМаксимум врагов одновременно: {4}",
                Elapsed, firstWave, lastWave, combatSeconds, peakEnemies);
        return string.Join("\n", context,
            GameLocalization.Format("perf-fps", "Средний FPS: {0:0.0}\nКадр P95: {1:0.0} мс · самый долгий: {2:0.0} мс\nКадров дольше бюджета 60 FPS: {3:0.0}%",
                frames.Count / Elapsed, frameP95, frames.Max, missedBudget * 100d / frames.Count),
            GameLocalization.Format("perf-compact-timings", "P95, мс · CPU (главный / рендер): {0} / {1} · GPU: {2}",
                Metric(cpuP95), Metric(renderP95), Metric(gpuP95)),
            GameLocalization.Format("perf-frame-counters", "Вызовы отрисовки, среднее / пик: {0} / {1}\nПик памяти Unity, МБ: {2}\nПик выделений GC за кадр, КБ: {3}",
                drawCallSamples > 0 ? ((double)totalDrawCalls / drawCallSamples).ToString("0") : unavailable,
                drawCallSamples > 0 ? peakDrawCalls.ToString() : unavailable,
                hasMemory ? (peakMemory / 1048576d).ToString("0.0") : unavailable,
                hasGc ? (peakGc / 1024d).ToString("0.0") : unavailable),
            diagnosis);
    }

    private static void ReadPeak(ProfilerRecorder recorder, ref long peak, ref bool available)
    {
        if (!recorder.Valid || recorder.Count == 0) return;
        available = true;
        peak = Math.Max(peak, recorder.LastValue);
    }

    // Quarter-millisecond buckets through 256 ms, then powers of two for hitches.
    // Memory stays constant even if the player spends a long time in preparation.
    private sealed class TimingDistribution
    {
        private const int FineBuckets = 1024;
        private readonly int[] buckets = new int[FineBuckets + 32];
        internal int Count { get; private set; }
        internal double Max { get; private set; }

        internal void Add(double ms)
        {
            if (ms <= 0d || !double.IsFinite(ms)) return;
            int index = ms <= 256d ? Math.Max(0, (int)Math.Ceiling(ms * 4d) - 1)
                : Math.Min(buckets.Length - 1, FineBuckets + (int)Math.Ceiling(Math.Log(ms / 256d, 2d)) - 1);
            buckets[index]++;
            Count++;
            Max = Math.Max(Max, ms);
        }

        internal double Percentile95
        {
            get
            {
                int target = (int)Math.Ceiling(Count * 0.95d);
                if (target == 0) return 0d;
                for (int i = 0; i < buckets.Length; i++)
                {
                    target -= buckets[i];
                    if (target <= 0)
                        return Math.Min(Max, i < FineBuckets ? (i + 1) / 4d : 256d * Math.Pow(2d, i - FineBuckets + 1));
                }
                return Max;
            }
        }
    }

    public void Dispose()
    {
        if (drawCalls.Valid) drawCalls.Dispose();
        if (memory.Valid) memory.Dispose();
        if (gc.Valid) gc.Dispose();
    }
}
