using Godot;
using System.Collections.Generic;

/// <summary>
/// Memory Spark System - Handles memory corridors and spark mechanics
/// </summary>
public partial class MemorySparkSystem : Node
{
    [Signal]
    public delegate void SparkCreatedEventHandler(Vector2I position);
    
    [Signal]
    public delegate void CorridorFormedEventHandler(Vector2I start, Vector2I end);
    
    // Active sparks and corridors
    private Dictionary<Vector2I, MemorySpark> activeSparks = new();
    private List<MemoryCorridor> activeCorridors = new();
    
    // References
    private GameGrid gameGrid;
    private HardwareBridgeEngineer hardwareBridge;
    
    public override void _Ready()
    {
        hardwareBridge = GetNode<HardwareBridgeEngineer>("/root/GameInitializer/HardwareBridge");
    }
    
    public void SetGameGrid(GameGrid grid)
    {
        gameGrid = grid;
    }
    
    public void CreateLightSpark(Vector2I position)
    {
        // Temporary light spark that lasts 1 round (for collapse movement)
        var spark = new MemorySpark
        {
            Position = position,
            Duration = 1,
            RoundsRemaining = 1,
            IsPermanent = false
        };
        
        activeSparks[position] = spark;
        gameGrid?.SetCellLight(position, true, false);
        
        // Trigger brief LED effect
        var effectParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "light_spark",
            ["position"] = position,
            ["duration"] = 1000
        };
        hardwareBridge?.Execute("trigger_effect", effectParams);
        
        GD.Print($"[MemorySparkSystem] Light spark created at {position} (1 round)");
    }
    
    public void CreateMemorySparkFromToken(Vector2I position, string playerId, bool isCollapse)
    {
        if (isCollapse)
        {
            GD.PrintErr($"[MemorySparkSystem] Cannot create Memory Sparks during collapse!");
            return;
        }
        
        // This creates a permanent Memory Spark using a Memory Token
        CreateMemorySpark(position);
        
        GD.Print($"[MemorySparkSystem] {playerId} spent Memory Token to create permanent light at {position}");
    }
    
    public void CreateMemorySpark(Vector2I position, int duration = -1)
    {
        if (activeSparks.ContainsKey(position)) return;
        
        var spark = new MemorySpark
        {
            Position = position,
            Duration = duration,
            RoundsRemaining = duration,
            IsPermanent = true
        };
        
        activeSparks[position] = spark;
        
        // Light the cell permanently
        gameGrid?.SetCellLight(position, true, true);
        
        // Trigger LED effect
        var effectParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "memory_spark",
            ["position"] = position
        };
        hardwareBridge?.Execute("trigger_effect", effectParams);
        
        EmitSignal(SignalName.SparkCreated, position);
        GD.Print($"[MemorySparkSystem] Spark created at {position}");
    }
    
    public void CreateMemoryCorridor(Vector2I start, Vector2I end)
    {
        var corridor = new MemoryCorridor
        {
            Start = start,
            End = end,
            Duration = -1,
            RoundsRemaining = -1,
            IsPermanent = true
        };
        
        activeCorridors.Add(corridor);
        
        // Light all cells in corridor permanently
        var cells = GetCorridorCells(start, end);
        foreach (var cell in cells)
        {
            gameGrid?.SetCellLight(cell, true, true);
        }
        
        EmitSignal(SignalName.CorridorFormed, start, end);
        GD.Print($"[MemorySparkSystem] Corridor formed from {start} to {end}");
    }
    
    public void ProcessRound()
    {
        // Process temporary sparks (Light Sparks from movement)
        var expiredSparks = new List<Vector2I>();
        
        foreach (var kvp in activeSparks)
        {
            var spark = kvp.Value;
            if (!spark.IsPermanent)
            {
                spark.RoundsRemaining--;
                if (spark.RoundsRemaining <= 0)
                {
                    expiredSparks.Add(kvp.Key);
                }
            }
        }
        
        // Remove expired temporary sparks
        foreach (var pos in expiredSparks)
        {
            activeSparks.Remove(pos);
            gameGrid?.SetCellLight(pos, false);
            GD.Print($"[MemorySparkSystem] Light spark expired at {pos}");
        }
        
        // Log status
        var permanentCount = activeSparks.Count(kvp => kvp.Value.IsPermanent);
        var temporaryCount = activeSparks.Count - permanentCount;
        GD.Print($"[MemorySparkSystem] {permanentCount} permanent memory sparks, {temporaryCount} temporary light sparks");
        GD.Print($"[MemorySparkSystem] {activeCorridors.Count} permanent memory corridors active");
    }
    
    private List<Vector2I> GetCorridorCells(Vector2I start, Vector2I end)
    {
        var cells = new List<Vector2I>();
        
        // Simple line between points
        var diff = end - start;
        var steps = Mathf.Max(Mathf.Abs(diff.X), Mathf.Abs(diff.Y));
        
        if (steps > 0)
        {
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(start.X, end.X, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(start.Y, end.Y, t));
                cells.Add(new Vector2I(x, y));
            }
        }
        else
        {
            cells.Add(start);
        }
        
        return cells;
    }
    
    public bool HasMemoryLight(Vector2I position)
    {
        if (activeSparks.ContainsKey(position))
            return true;
            
        foreach (var corridor in activeCorridors)
        {
            var cells = GetCorridorCells(corridor.Start, corridor.End);
            if (cells.Contains(position))
                return true;
        }
        
        return false;
    }
}

// Helper classes
public class MemorySpark
{
    public Vector2I Position { get; set; }
    public int Duration { get; set; }
    public int RoundsRemaining { get; set; }
    public bool IsPermanent { get; set; }
}

public class MemoryCorridor
{
    public Vector2I Start { get; set; }
    public Vector2I End { get; set; }
    public int Duration { get; set; }
    public int RoundsRemaining { get; set; }
    public bool IsPermanent { get; set; }
}