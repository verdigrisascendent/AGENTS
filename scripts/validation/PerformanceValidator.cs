using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Validates performance metrics and requirements
/// </summary>
public class PerformanceValidator : IValidationSuite
{
    private PerformanceProfiler profiler;
    private MemoryLeakDetector memoryDetector;
    private const float TargetFPS = 60.0f;
    private const float MinAcceptableFPS = 55.0f;
    private const float MaxFrameTimeMs = 16.7f;
    private const float MaxMemoryGrowthMBPerMinute = 1.0f;
    
    // Test durations
    private const float QuickTestDuration = 5.0f;
    private const float StressTestDuration = 30.0f;
    
    public void Initialize()
    {
        profiler = (PerformanceProfiler)Engine.GetMainLoop().Root.GetNode("PerformanceProfiler");
        memoryDetector = (MemoryLeakDetector)Engine.GetMainLoop().Root.GetNode("MemoryLeakDetector");
        
        // Enable profiling
        profiler?.EnableProfiling();
        memoryDetector?.StartMonitoring();
    }
    
    public void Cleanup()
    {
        // Leave profiling enabled for user
    }
    
    public List<ValidationTest> GetTests()
    {
        return new List<ValidationTest>
        {
            new ValidationTest
            {
                Name = "Base FPS Performance",
                Description = "FPS ≥ 60",
                IsCritical = true,
                TestFunc = TestBaseFPSPerformance
            },
            new ValidationTest
            {
                Name = "Frame Time Consistency",
                Description = "Frame time ≤ 16.7ms avg",
                IsCritical = true,
                TestFunc = TestFrameTimeConsistency
            },
            new ValidationTest
            {
                Name = "Memory Stability",
                Description = "Memory usage stable",
                IsCritical = true,
                TestFunc = TestMemoryStability
            },
            new ValidationTest
            {
                Name = "Collapse Performance",
                Description = "No dropped frames during collapse",
                IsCritical = true,
                TestFunc = TestCollapsePerformance
            },
            new ValidationTest
            {
                Name = "Draw Call Optimization",
                Description = "Draw calls within limits",
                IsCritical = false,
                TestFunc = TestDrawCallOptimization
            },
            new ValidationTest
            {
                Name = "LED Sync Performance",
                Description = "LED updates don't impact FPS",
                IsCritical = false,
                TestFunc = TestLEDSyncPerformance
            },
            new ValidationTest
            {
                Name = "Touch Input Latency",
                Description = "Touch response < 50ms",
                IsCritical = false,
                TestFunc = TestTouchInputLatency
            },
            new ValidationTest
            {
                Name = "Stress Test",
                Description = "Extended performance under load",
                IsCritical = false,
                TestFunc = TestStressPerformance
            }
        };
    }
    
    public int GetTestCount() => GetTests().Count;
    
    public async Task<ValidationResult> RunTest(ValidationTest test)
    {
        var startTime = Time.GetUnixTimeFromSystem();
        var result = await test.TestFunc();
        result.Duration = Time.GetUnixTimeFromSystem() - startTime;
        result.TestName = test.Name;
        result.IsCritical = test.IsCritical;
        return result;
    }
    
    private async Task<ValidationResult> TestBaseFPSPerformance()
    {
        var result = new ValidationResult { Passed = true };
        
        // Collect FPS samples
        var fpsSamples = new List<float>();
        var testDuration = QuickTestDuration;
        var startTime = Time.GetUnixTimeFromSystem();
        
        while (Time.GetUnixTimeFromSystem() - startTime < testDuration)
        {
            await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
            fpsSamples.Add(Engine.GetFramesPerSecond());
        }
        
        // Analyze results
        var avgFPS = fpsSamples.Average();
        var minFPS = fpsSamples.Min();
        var maxFPS = fpsSamples.Max();
        var percentile95 = fpsSamples.OrderBy(f => f).ElementAt((int)(fpsSamples.Count * 0.95));
        
        result.Metrics["avg_fps"] = avgFPS;
        result.Metrics["min_fps"] = minFPS;
        result.Metrics["max_fps"] = maxFPS;
        result.Metrics["95th_percentile_fps"] = percentile95;
        
        // Check requirements
        if (avgFPS < TargetFPS)
        {
            result.Passed = false;
            result.Errors.Add($"Average FPS {avgFPS:F1} below target {TargetFPS}");
        }
        
        if (minFPS < MinAcceptableFPS)
        {
            result.Passed = false;
            result.Errors.Add($"Minimum FPS {minFPS:F1} below acceptable {MinAcceptableFPS}");
        }
        
        // Check for frame drops
        var droppedFrames = fpsSamples.Count(f => f < MinAcceptableFPS);
        if (droppedFrames > fpsSamples.Count * 0.05) // More than 5% dropped
        {
            result.Warnings.Add($"High frame drop rate: {droppedFrames}/{fpsSamples.Count}");
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestFrameTimeConsistency()
    {
        var result = new ValidationResult { Passed = true };
        
        // Get frame time data from profiler
        var metrics = profiler?.GetCurrentMetrics();
        if (metrics == null)
        {
            result.Warnings.Add("Profiler not available");
            return result;
        }
        
        var frameTime = metrics.GetValueOrDefault("frame_time", 0f);
        var physicsTime = metrics.GetValueOrDefault("physics_time", 0f);
        
        result.Metrics["avg_frame_time"] = frameTime;
        result.Metrics["avg_physics_time"] = physicsTime;
        
        // Check frame time
        if (frameTime > MaxFrameTimeMs)
        {
            result.Passed = false;
            result.Errors.Add($"Average frame time {frameTime:F1}ms exceeds target {MaxFrameTimeMs}ms");
        }
        
        // Measure frame time variance
        var frameTimeSamples = new List<float>();
        for (int i = 0; i < 60; i++) // 1 second of samples
        {
            var stopwatch = Stopwatch.StartNew();
            await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
            stopwatch.Stop();
            frameTimeSamples.Add((float)stopwatch.Elapsed.TotalMilliseconds);
        }
        
        var variance = CalculateVariance(frameTimeSamples);
        result.Metrics["frame_time_variance"] = variance;
        
        if (variance > 5.0f) // High variance indicates stuttering
        {
            result.Warnings.Add($"High frame time variance: {variance:F1}ms");
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestMemoryStability()
    {
        var result = new ValidationResult { Passed = true };
        
        // Force garbage collection before test
        memoryDetector?.ForceGarbageCollection();
        await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
        
        // Get initial memory
        var initialReport = memoryDetector?.GetMemoryReport();
        if (initialReport == null)
        {
            result.Warnings.Add("Memory detector not available");
            return result;
        }
        
        var initialMemory = initialReport.CurrentMemoryUsage;
        
        // Run for test duration
        var testDuration = QuickTestDuration;
        var startTime = Time.GetUnixTimeFromSystem();
        
        while (Time.GetUnixTimeFromSystem() - startTime < testDuration)
        {
            await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        
        // Get final memory
        var finalReport = memoryDetector.GetMemoryReport();
        var finalMemory = finalReport.CurrentMemoryUsage;
        var memoryGrowth = finalMemory - initialMemory;
        var growthRate = (memoryGrowth / testDuration) * 60; // MB per minute
        
        result.Metrics["initial_memory_mb"] = initialMemory;
        result.Metrics["final_memory_mb"] = finalMemory;
        result.Metrics["memory_growth_mb"] = memoryGrowth;
        result.Metrics["growth_rate_mb_per_min"] = growthRate;
        
        // Check for leaks
        if (growthRate > MaxMemoryGrowthMBPerMinute)
        {
            result.Passed = false;
            result.Errors.Add($"Memory growth rate {growthRate:F2} MB/min exceeds limit {MaxMemoryGrowthMBPerMinute} MB/min");
        }
        
        if (finalReport.IsLeakDetected)
        {
            result.Passed = false;
            result.Errors.Add("Memory leak detected by analyzer");
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestCollapsePerformance()
    {
        var result = new ValidationResult { Passed = true };
        
        // Find game screen and trigger collapse
        var gameScreen = Engine.GetMainLoop().Root.GetNode("MainGameScreen") as MainGameScreen;
        if (gameScreen == null)
        {
            result.Warnings.Add("Game screen not active, cannot test collapse performance");
            return result;
        }
        
        // Start profiling collapse
        profiler?.StartTimer("collapse_performance");
        
        // Trigger collapse
        gameScreen.TriggerVaultCollapse();
        
        // Monitor performance during collapse
        var collapseFpsSamples = new List<float>();
        var collapseFrameTimes = new List<float>();
        
        for (int i = 0; i < 120; i++) // 2 seconds of collapse
        {
            var frameStart = Stopwatch.StartNew();
            await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
            frameStart.Stop();
            
            collapseFpsSamples.Add(Engine.GetFramesPerSecond());
            collapseFrameTimes.Add((float)frameStart.Elapsed.TotalMilliseconds);
        }
        
        profiler?.StopTimer("collapse_performance");
        
        // Analyze collapse performance
        var avgCollapseFPS = collapseFpsSamples.Average();
        var minCollapseFPS = collapseFpsSamples.Min();
        var maxCollapseFrameTime = collapseFrameTimes.Max();
        var droppedCollapseFrames = collapseFpsSamples.Count(f => f < MinAcceptableFPS);
        
        result.Metrics["collapse_avg_fps"] = avgCollapseFPS;
        result.Metrics["collapse_min_fps"] = minCollapseFPS;
        result.Metrics["collapse_max_frame_time"] = maxCollapseFrameTime;
        result.Metrics["collapse_dropped_frames"] = droppedCollapseFrames;
        
        // Check performance during collapse
        if (avgCollapseFPS < MinAcceptableFPS)
        {
            result.Passed = false;
            result.Errors.Add($"Collapse FPS {avgCollapseFPS:F1} below minimum {MinAcceptableFPS}");
        }
        
        if (droppedCollapseFrames > 5) // Allow max 5 dropped frames
        {
            result.Passed = false;
            result.Errors.Add($"Too many dropped frames during collapse: {droppedCollapseFrames}");
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestDrawCallOptimization()
    {
        var result = new ValidationResult { Passed = true };
        
        // Get rendering metrics
        var drawCalls = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalDrawCallsInFrame);
        var vertices = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TotalPrimitivesInFrame);
        var textureMemory = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TextureMemUsed) / (1024.0f * 1024.0f);
        
        result.Metrics["draw_calls"] = drawCalls;
        result.Metrics["vertices"] = vertices;
        result.Metrics["texture_memory_mb"] = textureMemory;
        
        // Check optimization thresholds
        const int MaxDrawCalls = 100;
        const int MaxVertices = 50000;
        const float MaxTextureMemoryMB = 50.0f;
        
        if (drawCalls > MaxDrawCalls)
        {
            result.Warnings.Add($"High draw call count: {drawCalls} (target < {MaxDrawCalls})");
        }
        
        if (vertices > MaxVertices)
        {
            result.Warnings.Add($"High vertex count: {vertices} (target < {MaxVertices})");
        }
        
        if (textureMemory > MaxTextureMemoryMB)
        {
            result.Warnings.Add($"High texture memory: {textureMemory:F1} MB (target < {MaxTextureMemoryMB} MB)");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestLEDSyncPerformance()
    {
        var result = new ValidationResult { Passed = true };
        
        var hardwareBridge = Engine.GetMainLoop().Root.GetNode("HardwareBridgeEngineer") as HardwareBridgeEngineer;
        if (hardwareBridge == null)
        {
            result.Warnings.Add("Hardware bridge not available");
            return result;
        }
        
        // Measure FPS with LED sync disabled
        var baselineFPS = new List<float>();
        for (int i = 0; i < 60; i++)
        {
            await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
            baselineFPS.Add(Engine.GetFramesPerSecond());
        }
        
        // Trigger heavy LED sync
        for (int i = 0; i < 10; i++)
        {
            hardwareBridge.Execute("trigger_effect", new Dictionary<string, object>
            {
                ["effect"] = "signal_pulse",
                ["position"] = new Vector2I(i % 8, i % 6),
                ["color"] = Colors.White
            });
        }
        
        // Measure FPS during LED sync
        var syncFPS = new List<float>();
        for (int i = 0; i < 60; i++)
        {
            await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
            syncFPS.Add(Engine.GetFramesPerSecond());
        }
        
        // Compare performance
        var baselineAvg = baselineFPS.Average();
        var syncAvg = syncFPS.Average();
        var fpsImpact = baselineAvg - syncAvg;
        var impactPercent = (fpsImpact / baselineAvg) * 100;
        
        result.Metrics["baseline_fps"] = baselineAvg;
        result.Metrics["led_sync_fps"] = syncAvg;
        result.Metrics["fps_impact"] = fpsImpact;
        result.Metrics["impact_percent"] = impactPercent;
        
        // Check impact
        if (impactPercent > 5.0f) // More than 5% FPS loss
        {
            result.Warnings.Add($"LED sync impacts FPS by {impactPercent:F1}%");
        }
        
        if (syncAvg < MinAcceptableFPS)
        {
            result.Passed = false;
            result.Errors.Add($"FPS during LED sync {syncAvg:F1} below minimum {MinAcceptableFPS}");
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestTouchInputLatency()
    {
        var result = new ValidationResult { Passed = true };
        
        // Note: Actual touch latency measurement requires hardware
        // This is a simulated test based on input processing time
        
        var touchManager = Engine.GetMainLoop().Root.GetNode("TouchInputManager") as TouchInputManager;
        if (touchManager == null)
        {
            result.Warnings.Add("Touch input manager not available");
            return result;
        }
        
        // Measure input processing time
        var processingTimes = new List<float>();
        
        for (int i = 0; i < 10; i++)
        {
            var inputEvent = new InputEventScreenTouch
            {
                Position = new Vector2(100 + i * 10, 100 + i * 10),
                Pressed = true,
                Index = 0
            };
            
            var stopwatch = Stopwatch.StartNew();
            touchManager._Input(inputEvent);
            stopwatch.Stop();
            
            processingTimes.Add((float)stopwatch.Elapsed.TotalMilliseconds);
            
            await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        
        var avgProcessingTime = processingTimes.Average();
        var maxProcessingTime = processingTimes.Max();
        
        result.Metrics["avg_input_processing_ms"] = avgProcessingTime;
        result.Metrics["max_input_processing_ms"] = maxProcessingTime;
        
        // Estimate total latency (processing + frame time)
        var estimatedLatency = avgProcessingTime + MaxFrameTimeMs;
        result.Metrics["estimated_touch_latency_ms"] = estimatedLatency;
        
        if (estimatedLatency > 50.0f)
        {
            result.Warnings.Add($"Estimated touch latency {estimatedLatency:F1}ms may feel sluggish");
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestStressPerformance()
    {
        var result = new ValidationResult { Passed = true };
        
        // Extended performance test
        GD.Print("Running stress test (30 seconds)...");
        
        var stressFPS = new List<float>();
        var stressMemory = new List<float>();
        var startTime = Time.GetUnixTimeFromSystem();
        
        // Create stress conditions
        var gameScreen = Engine.GetMainLoop().Root.GetNode("MainGameScreen") as MainGameScreen;
        if (gameScreen != null)
        {
            // Trigger multiple effects
            gameScreen.TriggerVaultCollapse();
            
            // Add multiple memory sparks
            var memorySystem = gameScreen.GetNode<MemorySparkSystem>("MemorySparkSystem");
            if (memorySystem != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    memorySystem.CreateMemorySpark(new Vector2I(i % 8, i % 6), 5);
                }
            }
        }
        
        // Monitor performance under stress
        while (Time.GetUnixTimeFromSystem() - startTime < StressTestDuration)
        {
            await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
            
            stressFPS.Add(Engine.GetFramesPerSecond());
            
            if (memoryDetector != null)
            {
                var memReport = memoryDetector.GetMemoryReport();
                stressMemory.Add(memReport.CurrentMemoryUsage);
            }
        }
        
        // Analyze stress test results
        var stressAvgFPS = stressFPS.Average();
        var stressMinFPS = stressFPS.Min();
        var stress95thPercentile = stressFPS.OrderBy(f => f).ElementAt((int)(stressFPS.Count * 0.05)); // 5th percentile
        
        var memoryStart = stressMemory.First();
        var memoryEnd = stressMemory.Last();
        var memoryPeak = stressMemory.Max();
        
        result.Metrics["stress_avg_fps"] = stressAvgFPS;
        result.Metrics["stress_min_fps"] = stressMinFPS;
        result.Metrics["stress_5th_percentile_fps"] = stress95thPercentile;
        result.Metrics["stress_memory_growth_mb"] = memoryEnd - memoryStart;
        result.Metrics["stress_memory_peak_mb"] = memoryPeak;
        
        // Check stress performance
        if (stressAvgFPS < MinAcceptableFPS)
        {
            result.Passed = false;
            result.Errors.Add($"Stress test average FPS {stressAvgFPS:F1} below minimum {MinAcceptableFPS}");
        }
        
        if (stress95thPercentile < 30.0f) // 95% of frames should be above 30 FPS
        {
            result.Warnings.Add($"Poor stress test performance: 5% of frames below {stress95thPercentile:F1} FPS");
        }
        
        return result;
    }
    
    private float CalculateVariance(List<float> values)
    {
        var avg = values.Average();
        var sumSquaredDiff = values.Sum(v => (v - avg) * (v - avg));
        return Mathf.Sqrt(sumSquaredDiff / values.Count);
    }
    
    private async Task ToSignal(SceneTree tree, StringName signal)
    {
        await tree.ToSignal(tree, signal);
    }
}