using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Performance Profiler - Monitors game performance metrics and identifies bottlenecks
/// </summary>
public partial class PerformanceProfiler : Node
{
    private static PerformanceProfiler instance;
    public static PerformanceProfiler Instance => instance;
    
    // Performance tracking
    private Dictionary<string, PerformanceMetric> metrics = new();
    private Queue<FrameData> frameHistory = new();
    private const int MaxFrameHistory = 300; // 5 seconds at 60 FPS
    
    // Thresholds
    private const float TargetFPS = 60.0f;
    private const float WarningFPS = 45.0f;
    private const float CriticalFPS = 30.0f;
    private const float MemoryWarningThresholdMB = 100.0f;
    
    // Profiling state
    public bool isProfilingEnabled { get; private set; } = false;
    private float updateInterval = 0.5f;
    private float updateTimer = 0.0f;
    
    // Current frame data
    private FrameData currentFrame;
    private Stopwatch frameStopwatch = new();
    
    public override void _Ready()
    {
        instance = this;
        Name = "PerformanceProfiler";
        ProcessMode = ProcessModeEnum.Always;
        
        // Initialize metrics
        InitializeMetrics();
        
        // Enable profiling in debug builds
        if (OS.IsDebugBuild())
        {
            EnableProfiling();
        }
    }
    
    private void InitializeMetrics()
    {
        metrics["fps"] = new PerformanceMetric("FPS", 0, 120);
        metrics["frame_time"] = new PerformanceMetric("Frame Time (ms)", 0, 100);
        metrics["physics_time"] = new PerformanceMetric("Physics Time (ms)", 0, 50);
        metrics["render_time"] = new PerformanceMetric("Render Time (ms)", 0, 50);
        metrics["memory_usage"] = new PerformanceMetric("Memory (MB)", 0, 500);
        metrics["draw_calls"] = new PerformanceMetric("Draw Calls", 0, 1000);
        metrics["vertex_count"] = new PerformanceMetric("Vertices", 0, 100000);
        metrics["texture_memory"] = new PerformanceMetric("Texture Memory (MB)", 0, 200);
        metrics["audio_latency"] = new PerformanceMetric("Audio Latency (ms)", 0, 100);
        metrics["websocket_latency"] = new PerformanceMetric("WebSocket Latency (ms)", 0, 100);
    }
    
    public void EnableProfiling()
    {
        isProfilingEnabled = true;
        frameStopwatch.Start();
        GD.Print("[PerformanceProfiler] Profiling enabled");
    }
    
    public void DisableProfiling()
    {
        isProfilingEnabled = false;
        frameStopwatch.Stop();
        GD.Print("[PerformanceProfiler] Profiling disabled");
    }
    
    public override void _Process(double delta)
    {
        if (!isProfilingEnabled) return;
        
        // Update current frame data
        currentFrame.FrameTime = (float)delta * 1000.0f; // Convert to ms
        currentFrame.FPS = Engine.GetFramesPerSecond();
        
        // Update timer
        updateTimer += (float)delta;
        
        if (updateTimer >= updateInterval)
        {
            UpdateMetrics();
            updateTimer = 0.0f;
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (!isProfilingEnabled) return;
        
        currentFrame.PhysicsTime = (float)delta * 1000.0f;
    }
    
    private void UpdateMetrics()
    {
        // Get performance data from engine
        metrics["fps"].Update(Engine.GetFramesPerSecond());
        metrics["frame_time"].Update(currentFrame.FrameTime);
        metrics["physics_time"].Update(currentFrame.PhysicsTime);
        
        // Memory usage
        var memoryUsed = OS.GetStaticMemoryUsage() / (1024.0f * 1024.0f); // Convert to MB
        metrics["memory_usage"].Update(memoryUsed);
        
        // Rendering metrics
        var renderInfo = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame);
        metrics["draw_calls"].Update(renderInfo);
        
        var vertexCount = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalPrimitivesInFrame);
        metrics["vertex_count"].Update(vertexCount);
        
        var textureMemory = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TextureMemUsed) / (1024.0f * 1024.0f);
        metrics["texture_memory"].Update(textureMemory);
        
        // Audio metrics
        var audioLatency = AudioServer.GetOutputLatency() * 1000.0f; // Convert to ms
        metrics["audio_latency"].Update(audioLatency);
        
        // Store frame data
        StoreFrameData();
        
        // Check for performance issues
        CheckPerformanceIssues();
    }
    
    private void StoreFrameData()
    {
        var frameData = new FrameData
        {
            Timestamp = Time.GetUnixTimeFromSystem(),
            FPS = currentFrame.FPS,
            FrameTime = currentFrame.FrameTime,
            PhysicsTime = currentFrame.PhysicsTime,
            MemoryUsage = metrics["memory_usage"].Current,
            DrawCalls = (int)metrics["draw_calls"].Current
        };
        
        frameHistory.Enqueue(frameData);
        
        // Maintain history size
        while (frameHistory.Count > MaxFrameHistory)
        {
            frameHistory.Dequeue();
        }
    }
    
    private void CheckPerformanceIssues()
    {
        var fps = metrics["fps"].Current;
        var memoryUsage = metrics["memory_usage"].Current;
        
        // FPS warnings
        if (fps < CriticalFPS)
        {
            GD.PrintErr($"[PerformanceProfiler] CRITICAL: FPS dropped to {fps:F1}");
            AnalyzePerformanceBottleneck();
        }
        else if (fps < WarningFPS)
        {
            GD.Print($"[PerformanceProfiler] WARNING: FPS at {fps:F1}");
        }
        
        // Memory warnings
        if (memoryUsage > MemoryWarningThresholdMB)
        {
            GD.Print($"[PerformanceProfiler] WARNING: Memory usage at {memoryUsage:F1} MB");
        }
        
        // Check for frame time spikes
        var frameTime = metrics["frame_time"].Current;
        if (frameTime > 33.3f) // More than 2x target frame time
        {
            GD.Print($"[PerformanceProfiler] Frame time spike: {frameTime:F1} ms");
        }
    }
    
    private void AnalyzePerformanceBottleneck()
    {
        var analysis = new List<string>();
        
        // Check physics time
        if (metrics["physics_time"].Current > 16.0f)
        {
            analysis.Add("Physics processing is taking too long");
        }
        
        // Check draw calls
        if (metrics["draw_calls"].Current > 500)
        {
            analysis.Add($"High draw call count: {metrics["draw_calls"].Current}");
        }
        
        // Check texture memory
        if (metrics["texture_memory"].Current > 100)
        {
            analysis.Add($"High texture memory usage: {metrics["texture_memory"].Current:F1} MB");
        }
        
        if (analysis.Count > 0)
        {
            GD.Print("[PerformanceProfiler] Bottleneck analysis:");
            foreach (var issue in analysis)
            {
                GD.Print($"  - {issue}");
            }
        }
    }
    
    // Public API for performance tracking
    public void StartTimer(string timerName)
    {
        if (!isProfilingEnabled) return;
        
        if (!metrics.ContainsKey(timerName))
        {
            metrics[timerName] = new PerformanceMetric(timerName, 0, 100);
        }
        
        metrics[timerName].StartTimer();
    }
    
    public void StopTimer(string timerName)
    {
        if (!isProfilingEnabled) return;
        
        if (metrics.TryGetValue(timerName, out var metric))
        {
            metric.StopTimer();
        }
    }
    
    public Dictionary<string, float> GetCurrentMetrics()
    {
        var result = new Dictionary<string, float>();
        
        foreach (var kvp in metrics)
        {
            result[kvp.Key] = kvp.Value.Current;
        }
        
        return result;
    }
    
    public PerformanceReport GenerateReport()
    {
        var report = new PerformanceReport();
        
        if (frameHistory.Count == 0)
        {
            report.Summary = "No performance data collected";
            return report;
        }
        
        // Calculate averages
        var avgFPS = frameHistory.Average(f => f.FPS);
        var avgFrameTime = frameHistory.Average(f => f.FrameTime);
        var avgMemory = frameHistory.Average(f => f.MemoryUsage);
        var maxMemory = frameHistory.Max(f => f.MemoryUsage);
        var minFPS = frameHistory.Min(f => f.FPS);
        var maxDrawCalls = frameHistory.Max(f => f.DrawCalls);
        
        // Count frame drops
        var frameDrops = frameHistory.Count(f => f.FPS < WarningFPS);
        var severeDrops = frameHistory.Count(f => f.FPS < CriticalFPS);
        
        report.Summary = $"Performance Report:\n" +
                        $"  Average FPS: {avgFPS:F1} (Min: {minFPS:F1})\n" +
                        $"  Average Frame Time: {avgFrameTime:F2} ms\n" +
                        $"  Memory Usage: {avgMemory:F1} MB (Peak: {maxMemory:F1} MB)\n" +
                        $"  Max Draw Calls: {maxDrawCalls}\n" +
                        $"  Frame Drops: {frameDrops} ({severeDrops} severe)";
        
        report.Metrics = GetCurrentMetrics();
        report.FrameHistory = new List<FrameData>(frameHistory);
        
        // Add recommendations
        if (avgFPS < TargetFPS)
        {
            report.Recommendations.Add("Consider reducing visual quality settings");
        }
        
        if (maxDrawCalls > 500)
        {
            report.Recommendations.Add("High draw call count detected - consider batching");
        }
        
        if (maxMemory > MemoryWarningThresholdMB)
        {
            report.Recommendations.Add("Memory usage is high - check for leaks");
        }
        
        return report;
    }
    
    public void ExportProfileData(string filePath)
    {
        var report = GenerateReport();
        
        var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(report.ToJson());
            file.Close();
            GD.Print($"[PerformanceProfiler] Profile data exported to {filePath}");
        }
    }
}

// Helper classes
public class PerformanceMetric
{
    public string Name { get; set; }
    public float Current { get; private set; }
    public float Average { get; private set; }
    public float Min { get; private set; }
    public float Max { get; private set; }
    
    private Queue<float> history = new();
    private const int HistorySize = 60;
    private Stopwatch timer;
    
    public PerformanceMetric(string name, float min, float max)
    {
        Name = name;
        Min = min;
        Max = max;
        timer = new Stopwatch();
    }
    
    public void Update(float value)
    {
        Current = value;
        history.Enqueue(value);
        
        while (history.Count > HistorySize)
        {
            history.Dequeue();
        }
        
        if (history.Count > 0)
        {
            Average = history.Average();
        }
    }
    
    public void StartTimer()
    {
        timer.Restart();
    }
    
    public void StopTimer()
    {
        if (timer.IsRunning)
        {
            timer.Stop();
            Update((float)timer.Elapsed.TotalMilliseconds);
        }
    }
}

public class FrameData
{
    public double Timestamp { get; set; }
    public float FPS { get; set; }
    public float FrameTime { get; set; }
    public float PhysicsTime { get; set; }
    public float MemoryUsage { get; set; }
    public int DrawCalls { get; set; }
}

public class PerformanceReport
{
    public string Summary { get; set; }
    public Dictionary<string, float> Metrics { get; set; } = new();
    public List<FrameData> FrameHistory { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    
    public string ToJson()
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["summary"] = Summary,
            ["metrics"] = Metrics,
            ["recommendations"] = Recommendations,
            ["timestamp"] = Time.GetUnixTimeFromSystem()
        };
        
        return Json.Stringify(dict);
    }
}