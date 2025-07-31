using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Memory Leak Detector - Monitors memory usage patterns and detects potential leaks
/// </summary>
public partial class MemoryLeakDetector : Node
{
    private static MemoryLeakDetector instance;
    public static MemoryLeakDetector Instance => instance;
    
    // Memory tracking
    private Dictionary<string, TrackedResource> trackedResources = new();
    private Queue<MemorySample> memorySamples = new();
    private const int MaxSamples = 120; // 2 minutes at 1 sample/second
    
    // Detection parameters
    private const float LeakDetectionThreshold = 10.0f; // MB
    private const float LeakGrowthRateThreshold = 0.5f; // MB/minute
    private const int MinSamplesForDetection = 30;
    
    // Monitoring state
    public bool isMonitoring { get; private set; } = false;
    private float sampleInterval = 1.0f;
    private float sampleTimer = 0.0f;
    
    // Node tracking
    private Dictionary<NodePath, WeakRef> trackedNodes = new();
    private int lastNodeCount = 0;
    
    public override void _Ready()
    {
        instance = this;
        Name = "MemoryLeakDetector";
        ProcessMode = ProcessModeEnum.Always;
        
        // Start monitoring in debug builds
        if (OS.IsDebugBuild())
        {
            StartMonitoring();
        }
    }
    
    public void StartMonitoring()
    {
        isMonitoring = true;
        GD.Print("[MemoryLeakDetector] Monitoring started");
        
        // Take initial sample
        TakeMemorySample();
    }
    
    public void StopMonitoring()
    {
        isMonitoring = false;
        GD.Print("[MemoryLeakDetector] Monitoring stopped");
    }
    
    public override void _Process(double delta)
    {
        if (!isMonitoring) return;
        
        sampleTimer += (float)delta;
        
        if (sampleTimer >= sampleInterval)
        {
            TakeMemorySample();
            CheckForLeaks();
            sampleTimer = 0.0f;
        }
    }
    
    private void TakeMemorySample()
    {
        var sample = new MemorySample
        {
            Timestamp = Time.GetUnixTimeFromSystem(),
            StaticMemory = OS.GetStaticMemoryUsage() / (1024.0f * 1024.0f), // MB
            DynamicMemory = OS.GetDynamicMemoryUsage() / (1024.0f * 1024.0f), // MB
            NodeCount = GetNodeCount(),
            ResourceCount = GetResourceCount(),
            TextureMemory = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.TextureMemUsed) / (1024.0f * 1024.0f),
            VertexMemory = RenderingServer.GetRenderingInfo(RenderingServer.RenderingInfo.VertexMemUsed) / (1024.0f * 1024.0f)
        };
        
        memorySamples.Enqueue(sample);
        
        // Maintain sample history
        while (memorySamples.Count > MaxSamples)
        {
            memorySamples.Dequeue();
        }
        
        // Check for node leaks
        CheckNodeLeaks(sample.NodeCount);
    }
    
    private int GetNodeCount()
    {
        return GetTree().GetNodeCount();
    }
    
    private int GetResourceCount()
    {
        // Count loaded resources
        int count = 0;
        var loadedResources = new List<Resource>();
        
        // This is a simplified version - in practice you'd need to track resources more carefully
        return count;
    }
    
    private void CheckNodeLeaks(int currentNodeCount)
    {
        if (currentNodeCount > lastNodeCount + 100)
        {
            GD.Print($"[MemoryLeakDetector] WARNING: Node count increased by {currentNodeCount - lastNodeCount}");
            AnalyzeNodeTree();
        }
        
        lastNodeCount = currentNodeCount;
    }
    
    private void AnalyzeNodeTree()
    {
        var nodeCounts = new Dictionary<string, int>();
        
        // Count nodes by type
        AnalyzeNodeBranch(GetTree().Root, nodeCounts);
        
        // Report high counts
        var suspicious = nodeCounts.Where(kvp => kvp.Value > 50)
                                  .OrderByDescending(kvp => kvp.Value);
        
        if (suspicious.Any())
        {
            GD.Print("[MemoryLeakDetector] Node type analysis:");
            foreach (var kvp in suspicious.Take(5))
            {
                GD.Print($"  {kvp.Key}: {kvp.Value} instances");
            }
        }
    }
    
    private void AnalyzeNodeBranch(Node node, Dictionary<string, int> counts)
    {
        if (node == null) return;
        
        var typeName = node.GetType().Name;
        if (!counts.ContainsKey(typeName))
            counts[typeName] = 0;
        counts[typeName]++;
        
        foreach (var child in node.GetChildren())
        {
            AnalyzeNodeBranch(child, counts);
        }
    }
    
    private void CheckForLeaks()
    {
        if (memorySamples.Count < MinSamplesForDetection)
            return;
        
        // Calculate memory growth rate
        var oldestSample = memorySamples.First();
        var newestSample = memorySamples.Last();
        
        var timeSpan = newestSample.Timestamp - oldestSample.Timestamp;
        if (timeSpan <= 0) return;
        
        var memoryGrowth = newestSample.TotalMemory - oldestSample.TotalMemory;
        var growthRate = (memoryGrowth / timeSpan) * 60.0f; // MB per minute
        
        // Check for consistent growth
        if (growthRate > LeakGrowthRateThreshold)
        {
            GD.PrintErr($"[MemoryLeakDetector] LEAK DETECTED: Memory growing at {growthRate:F2} MB/min");
            GenerateLeakReport();
        }
        
        // Check absolute memory usage
        if (newestSample.TotalMemory - oldestSample.TotalMemory > LeakDetectionThreshold)
        {
            GD.Print($"[MemoryLeakDetector] WARNING: Memory increased by {memoryGrowth:F1} MB");
        }
    }
    
    private void GenerateLeakReport()
    {
        var report = new List<string>
        {
            "Memory Leak Analysis:",
            $"  Total Samples: {memorySamples.Count}",
            $"  Time Span: {(memorySamples.Last().Timestamp - memorySamples.First().Timestamp):F1} seconds"
        };
        
        // Memory statistics
        var avgStaticMem = memorySamples.Average(s => s.StaticMemory);
        var avgDynamicMem = memorySamples.Average(s => s.DynamicMemory);
        var maxTotalMem = memorySamples.Max(s => s.TotalMemory);
        
        report.Add($"  Average Static Memory: {avgStaticMem:F1} MB");
        report.Add($"  Average Dynamic Memory: {avgDynamicMem:F1} MB");
        report.Add($"  Peak Total Memory: {maxTotalMem:F1} MB");
        
        // Node statistics
        var avgNodeCount = memorySamples.Average(s => s.NodeCount);
        var maxNodeCount = memorySamples.Max(s => s.NodeCount);
        
        report.Add($"  Average Node Count: {avgNodeCount:F0}");
        report.Add($"  Max Node Count: {maxNodeCount}");
        
        // Texture memory
        var avgTexMem = memorySamples.Average(s => s.TextureMemory);
        var maxTexMem = memorySamples.Max(s => s.TextureMemory);
        
        report.Add($"  Average Texture Memory: {avgTexMem:F1} MB");
        report.Add($"  Max Texture Memory: {maxTexMem:F1} MB");
        
        foreach (var line in report)
        {
            GD.Print(line);
        }
    }
    
    // Resource tracking API
    public void TrackResource(string name, Resource resource)
    {
        if (!isMonitoring) return;
        
        trackedResources[name] = new TrackedResource
        {
            Name = name,
            ResourceRef = GodotObject.WeakRef(resource),
            CreatedAt = Time.GetUnixTimeFromSystem(),
            InitialMemorySize = EstimateResourceSize(resource)
        };
    }
    
    public void UntrackResource(string name)
    {
        trackedResources.Remove(name);
    }
    
    private float EstimateResourceSize(Resource resource)
    {
        // Estimate size based on resource type
        if (resource is ImageTexture texture)
        {
            var image = texture.GetImage();
            if (image != null)
            {
                return (image.GetWidth() * image.GetHeight() * 4) / (1024.0f * 1024.0f); // RGBA bytes to MB
            }
        }
        else if (resource is Mesh mesh)
        {
            // Simplified mesh size estimation
            return 0.1f; // Placeholder
        }
        
        return 0.01f; // Default small size
    }
    
    public void CheckTrackedResources()
    {
        var leakedResources = new List<string>();
        
        foreach (var kvp in trackedResources)
        {
            var tracked = kvp.Value;
            var resource = tracked.ResourceRef.GetRef() as Resource;
            
            if (resource == null)
            {
                // Resource was freed
                continue;
            }
            
            var age = Time.GetUnixTimeFromSystem() - tracked.CreatedAt;
            if (age > 300) // 5 minutes
            {
                leakedResources.Add($"{tracked.Name} (aged {age:F0}s)");
            }
        }
        
        if (leakedResources.Count > 0)
        {
            GD.Print("[MemoryLeakDetector] Long-lived resources detected:");
            foreach (var name in leakedResources)
            {
                GD.Print($"  - {name}");
            }
        }
    }
    
    // Manual leak testing utilities
    public void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        GD.Print("[MemoryLeakDetector] Forced garbage collection completed");
    }
    
    public MemoryReport GetMemoryReport()
    {
        var report = new MemoryReport();
        
        if (memorySamples.Count > 0)
        {
            var latest = memorySamples.Last();
            report.CurrentMemoryUsage = latest.TotalMemory;
            report.NodeCount = latest.NodeCount;
            report.TextureMemory = latest.TextureMemory;
            report.VertexMemory = latest.VertexMemory;
            
            if (memorySamples.Count >= 2)
            {
                var previous = memorySamples.ElementAt(memorySamples.Count - 2);
                report.MemoryDelta = latest.TotalMemory - previous.TotalMemory;
            }
        }
        
        report.TrackedResourceCount = trackedResources.Count;
        report.IsLeakDetected = false; // Set by leak detection logic
        
        return report;
    }
    
    public void ExportMemoryReport(string filePath)
    {
        var report = GetMemoryReport();
        var samples = memorySamples.ToList();
        
        var data = new Godot.Collections.Dictionary
        {
            ["report"] = report.ToDict(),
            ["samples"] = samples.Select(s => s.ToDict()).ToArray(),
            ["timestamp"] = Time.GetUnixTimeFromSystem()
        };
        
        var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(Json.Stringify(data));
            file.Close();
            GD.Print($"[MemoryLeakDetector] Memory report exported to {filePath}");
        }
    }
}

// Helper classes
public class MemorySample
{
    public double Timestamp { get; set; }
    public float StaticMemory { get; set; }
    public float DynamicMemory { get; set; }
    public float TextureMemory { get; set; }
    public float VertexMemory { get; set; }
    public int NodeCount { get; set; }
    public int ResourceCount { get; set; }
    
    public float TotalMemory => StaticMemory + DynamicMemory;
    
    public Godot.Collections.Dictionary ToDict()
    {
        return new Godot.Collections.Dictionary
        {
            ["timestamp"] = Timestamp,
            ["static_memory"] = StaticMemory,
            ["dynamic_memory"] = DynamicMemory,
            ["texture_memory"] = TextureMemory,
            ["vertex_memory"] = VertexMemory,
            ["node_count"] = NodeCount,
            ["resource_count"] = ResourceCount,
            ["total_memory"] = TotalMemory
        };
    }
}

public class TrackedResource
{
    public string Name { get; set; }
    public WeakRef ResourceRef { get; set; }
    public double CreatedAt { get; set; }
    public float InitialMemorySize { get; set; }
}

public class MemoryReport
{
    public float CurrentMemoryUsage { get; set; }
    public float MemoryDelta { get; set; }
    public int NodeCount { get; set; }
    public float TextureMemory { get; set; }
    public float VertexMemory { get; set; }
    public int TrackedResourceCount { get; set; }
    public bool IsLeakDetected { get; set; }
    
    public Godot.Collections.Dictionary ToDict()
    {
        return new Godot.Collections.Dictionary
        {
            ["current_memory"] = CurrentMemoryUsage,
            ["memory_delta"] = MemoryDelta,
            ["node_count"] = NodeCount,
            ["texture_memory"] = TextureMemory,
            ["vertex_memory"] = VertexMemory,
            ["tracked_resources"] = TrackedResourceCount,
            ["leak_detected"] = IsLeakDetected
        };
    }
}