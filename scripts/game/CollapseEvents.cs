using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Collapse Events Table - Handles special events during vault collapse per LITD_RULES_v8
/// </summary>
public static class CollapseEvents
{
    public enum EventType
    {
        LightCascade = 1,      // All lights flicker (may extinguish)
        DebrisPath = 2,        // Path blocked, must detour
        MemoryEcho = 3,        // Brief glimpse of all filed players
        TimeSlip = 4,          // +1 round (max 5 total)
        ShatteredPath = 5      // Random tiles collapse permanently
    }
    
    private static readonly RandomNumberGenerator rng = new RandomNumberGenerator();
    
    static CollapseEvents()
    {
        rng.Randomize();
    }
    
    public static EventType RollEvent()
    {
        int roll = rng.RandiRange(1, 6);
        
        // 6 = Player's choice
        if (roll == 6)
        {
            GD.Print("[CollapseEvents] Rolled 6 - Player's choice!");
            // For now, default to Time Slip as most beneficial
            return EventType.TimeSlip;
        }
        
        return (EventType)roll;
    }
    
    public static CollapseEventResult ProcessEvent(EventType eventType, CollapseEventContext context)
    {
        var result = new CollapseEventResult
        {
            EventType = eventType,
            Success = true
        };
        
        switch (eventType)
        {
            case EventType.LightCascade:
                result.Message = "Light Cascade! All lights flicker dangerously!";
                result.AffectedTiles = GetAllLitTiles(context);
                result.Effect = "flicker_lights";
                // 50% chance each light extinguishes
                foreach (var tile in result.AffectedTiles)
                {
                    if (rng.Randf() < 0.5f)
                    {
                        result.ExtinguishedLights.Add(tile);
                    }
                }
                break;
                
            case EventType.DebrisPath:
                result.Message = "Debris Path! Rubble blocks the way!";
                // Block 2-4 random tiles
                int debrisCount = rng.RandiRange(2, 4);
                for (int i = 0; i < debrisCount; i++)
                {
                    var tile = new Vector2I(
                        rng.RandiRange(0, context.GridWidth - 1),
                        rng.RandiRange(0, context.GridHeight - 1)
                    );
                    result.BlockedTiles.Add(tile);
                }
                result.Effect = "debris_fall";
                break;
                
            case EventType.MemoryEcho:
                result.Message = "Memory Echo! Ghostly forms appear!";
                result.ShowFiledPlayers = true;
                result.Duration = 2000; // 2 seconds
                result.Effect = "memory_echo";
                break;
                
            case EventType.TimeSlip:
                result.Message = "Time Slip! The collapse slows momentarily!";
                result.ExtraRounds = 1;
                result.Effect = "time_distortion";
                break;
                
            case EventType.ShatteredPath:
                result.Message = "Shattered Path! The floor gives way!";
                // Collapse 3-5 random tiles permanently
                int shatterCount = rng.RandiRange(3, 5);
                for (int i = 0; i < shatterCount; i++)
                {
                    var tile = new Vector2I(
                        rng.RandiRange(0, context.GridWidth - 1),
                        rng.RandiRange(0, context.GridHeight - 1)
                    );
                    result.CollapsedTiles.Add(tile);
                }
                result.Effect = "floor_collapse";
                break;
        }
        
        return result;
    }
    
    private static List<Vector2I> GetAllLitTiles(CollapseEventContext context)
    {
        var litTiles = new List<Vector2I>();
        
        // This would need to interface with the actual game grid
        // For now, return empty list
        // TODO: Connect to GameGrid.GetLitTiles()
        
        return litTiles;
    }
}

public class CollapseEventContext
{
    public int GridWidth { get; set; } = 8;
    public int GridHeight { get; set; } = 6;
    public int CurrentRound { get; set; }
    public int RoundsRemaining { get; set; }
    public List<Vector2I> LitTiles { get; set; } = new();
    public List<string> FiledPlayers { get; set; } = new();
}

public class CollapseEventResult
{
    public CollapseEvents.EventType EventType { get; set; }
    public string Message { get; set; } = "";
    public bool Success { get; set; }
    public string Effect { get; set; } = "";
    public int Duration { get; set; } = 1000;
    
    // Event-specific results
    public List<Vector2I> AffectedTiles { get; set; } = new();
    public List<Vector2I> ExtinguishedLights { get; set; } = new();
    public List<Vector2I> BlockedTiles { get; set; } = new();
    public List<Vector2I> CollapsedTiles { get; set; } = new();
    public bool ShowFiledPlayers { get; set; }
    public int ExtraRounds { get; set; }
}