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
        ? GameLocalization.Format(LocalizationKeys.PerfRunSummary,
            Elapsed, frames.Count / Elapsed, frames.Percentile95)
        : GameLocalization.Text(LocalizationKeys.PerfNoFrames);

    internal static string DeviceReport()
    {
        string lighting = GamePerformanceSettings.Lighting == GamePerformanceSettings.LightingMode.Simple
            ? GameLocalization.Text(LocalizationKeys.GraphicsSimple) : GameLocalization.Text(LocalizationKeys.GraphicsStandard);
        string normals = GamePerformanceSettings.NormalMaps
            ? GameLocalization.Text(LocalizationKeys.PerfOn) : GameLocalization.Text(LocalizationKeys.PerfOff);
        return GameLocalization.Format(LocalizationKeys.PerfDevice,
            SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceType, Screen.width, Screen.height) + "\n" +
            GameLocalization.Format(LocalizationKeys.PerfGraphics, lighting, normals);
    }

    internal string Report(bool benchmark = false)
    {
        if (!HasFrames) return Summary();
        double frameP95 = frames.Percentile95;
        double cpuP95 = mainThread.Percentile95;
        double renderP95 = renderThread.Percentile95;
        double gpuP95 = gpu.Percentile95;
        string unavailable = GameLocalization.Text(LocalizationKeys.PerfUnavailable);
        string Metric(double value) => value > 0d ? value.ToString("0.0") : unavailable;

        double cpuLimit = Math.Max(cpuP95, renderP95);
        string diagnosis;
        if (frameP95 <= FrameBudget + 0.35d)
            diagnosis = GameLocalization.Text(LocalizationKeys.PerfOk);
        else if (gpuP95 > FrameBudget && cpuLimit > 0d && gpuP95 >= cpuLimit * 1.1d)
            diagnosis = GameLocalization.Text(LocalizationKeys.PerfGpuLimit);
        else if (cpuLimit > FrameBudget && gpuP95 > 0d && cpuLimit >= gpuP95 * 1.1d)
            diagnosis = benchmark
                ? GameLocalization.Text(LocalizationKeys.PerfBenchmarkCpu)
                : GameLocalization.Text(LocalizationKeys.PerfCpuLimit);
        else
            diagnosis = GameLocalization.Text(LocalizationKeys.PerfUncertain);

        string context = benchmark
            ? GameLocalization.Format(LocalizationKeys.PerfBenchmarkStage, peakEnemies, Elapsed)
            : GameLocalization.Format(LocalizationKeys.PerfRunContext,
                Elapsed, firstWave, lastWave, combatSeconds, peakEnemies);
        return string.Join("\n", context,
            GameLocalization.Format(LocalizationKeys.PerfFps,
                frames.Count / Elapsed, frameP95, frames.Max, missedBudget * 100d / frames.Count),
            GameLocalization.Format(LocalizationKeys.PerfCompactTimings,
                Metric(cpuP95), Metric(renderP95), Metric(gpuP95)),
            GameLocalization.Format(LocalizationKeys.PerfFrameCounters,
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
