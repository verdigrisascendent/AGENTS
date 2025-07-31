using Godot;
using System.Collections.Generic;

/// <summary>
/// Context Manager - Manages game rules and context loading for LITD
/// </summary>
public partial class ContextManager : RefCounted
{
    private static ContextManager instance;
    private GameRules loadedRules;
    private bool rulesLoaded = false;
    
    // Singleton pattern
    public static ContextManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ContextManager();
            }
            return instance;
        }
    }
    
    /// <summary>
    /// Load the canonical game rules from LITD_RULES_v8
    /// </summary>
    public static void LoadRules()
    {
        GD.Print("[ContextManager] Loading LITD_RULES_v8 canonical rules...");
        
        var manager = Instance;
        
        // Load canonical rules (matching RulesVerifierAgent)
        manager.loadedRules = new GameRules
        {
            // Game structure
            MinPlayers = 4,
            MaxPlayers = 5,
            GameDurationMinutes = 20,
            
            // Action economy
            IlluminateAttemptsPerTurn = 1,
            AdditionalActionsPerTurn = 1,
            MovementPerTurn = 1,
            CollapseMovementPerTurn = 2,
            
            // Collapse mechanics
            CollapseTimerBase = 3,
            CollapseTimerMax = 5,
            CollapseSparkChance = 0.75f,
            
            // Filer behavior
            FilerDormantNoiseThreshold = 4,
            FilerAlertNoiseThreshold = 7,
            FilerHuntingNoiseThreshold = 12,
            FilerCrisisNoiseThreshold = 13,
            
            // Memory mechanics
            MemoryTokensPerPlayer = 2,
            MemoryBridgeDuration = 3,
            
            // Light mechanics
            SignalLightDuration = 2,
            IlluminatePermanent = true,
            AidronLightRadius = 3
        };
        
        manager.rulesLoaded = true;
        
        GD.Print("[ContextManager] Rules loaded successfully:");
        GD.Print($"  - Version: LITD_RULES_v8");
        GD.Print($"  - Players: {manager.loadedRules.MinPlayers}-{manager.loadedRules.MaxPlayers}");
        GD.Print($"  - Game Duration: {manager.loadedRules.GameDurationMinutes} minutes");
        GD.Print($"  - Action Economy: {manager.loadedRules.IlluminateAttemptsPerTurn} illuminate, {manager.loadedRules.MovementPerTurn} move");
        GD.Print($"  - Collapse: {manager.loadedRules.CollapseTimerBase}-{manager.loadedRules.CollapseTimerMax} rounds");
    }
    
    /// <summary>
    /// Get the loaded rules
    /// </summary>
    public static GameRules GetRules()
    {
        if (!Instance.rulesLoaded)
        {
            GD.PrintErr("[ContextManager] Rules not loaded! Call LoadRules() first.");
            return null;
        }
        return Instance.loadedRules;
    }
    
    /// <summary>
    /// Check if rules are loaded
    /// </summary>
    public static bool AreRulesLoaded()
    {
        return Instance.rulesLoaded;
    }
    
    /// <summary>
    /// Export rules as dictionary for verification
    /// </summary>
    public static Dictionary<string, object> ExportRulesAsDictionary()
    {
        if (!Instance.rulesLoaded)
        {
            GD.PrintErr("[ContextManager] Rules not loaded!");
            return null;
        }
        
        var rules = Instance.loadedRules;
        return new Dictionary<string, object>
        {
            // Game structure
            ["MinPlayers"] = rules.MinPlayers,
            ["MaxPlayers"] = rules.MaxPlayers,
            ["GameDurationMinutes"] = rules.GameDurationMinutes,
            
            // Action economy
            ["IlluminateAttemptsPerTurn"] = rules.IlluminateAttemptsPerTurn,
            ["AdditionalActionsPerTurn"] = rules.AdditionalActionsPerTurn,
            ["MovementPerTurn"] = rules.MovementPerTurn,
            ["CollapseMovementPerTurn"] = rules.CollapseMovementPerTurn,
            
            // Collapse mechanics
            ["CollapseTimerBase"] = rules.CollapseTimerBase,
            ["CollapseTimerMax"] = rules.CollapseTimerMax,
            ["CollapseSparkChance"] = rules.CollapseSparkChance,
            
            // Filer behavior
            ["FilerDormantNoiseThreshold"] = rules.FilerDormantNoiseThreshold,
            ["FilerAlertNoiseThreshold"] = rules.FilerAlertNoiseThreshold,
            ["FilerHuntingNoiseThreshold"] = rules.FilerHuntingNoiseThreshold,
            ["FilerCrisisNoiseThreshold"] = rules.FilerCrisisNoiseThreshold,
            
            // Memory mechanics
            ["MemoryTokensPerPlayer"] = rules.MemoryTokensPerPlayer,
            ["MemoryBridgeDuration"] = rules.MemoryBridgeDuration,
            
            // Light mechanics
            ["SignalLightDuration"] = rules.SignalLightDuration,
            ["IlluminatePermanent"] = rules.IlluminatePermanent,
            ["AidronLightRadius"] = rules.AidronLightRadius
        };
    }
}