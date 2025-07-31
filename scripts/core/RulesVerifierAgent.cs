using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Rules Verifier Agent - Validates game rules against LITD_RULES_v8 canon
/// </summary>
public partial class RulesVerifierAgent : RefCounted, ISpecializedAgent
{
    private MegaAgent megaAgent;
    
    // Rule version tracking
    private const string RULES_VERSION = "8.0";
    private const string RULES_HASH = "litd_v8_canon_2024";
    
    // Core game rules from LITD_RULES_v8
    private readonly GameRules canonicalRules = new GameRules
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
    
    private Dictionary<string, List<RuleViolation>> violations = new();
    
    public void Initialize(MegaAgent mega)
    {
        megaAgent = mega;
        GD.Print($"[RulesVerifierAgent] Initialized with LITD Rules v{RULES_VERSION}");
    }
    
    public object Execute(string task, Dictionary<string, object> parameters)
    {
        return task switch
        {
            "validate_action" => ValidateGameAction(parameters),
            "verify_game_state" => VerifyGameState(parameters),
            "check_rule_compliance" => CheckRuleCompliance(parameters),
            "generate_rules_json" => GenerateFlattenedRulesJson(),
            "detect_drift" => DetectRuleDrift(parameters),
            "get_canonical_rules" => canonicalRules,
            _ => null
        };
    }
    
    private ValidationResult ValidateGameAction(Dictionary<string, object> parameters)
    {
        var action = parameters.Get("action", "").ToString();
        var gameState = parameters.Get("game_state") as GameState;
        var playerId = parameters.Get("player_id", "").ToString();
        
        violations.Clear();
        
        switch (action)
        {
            case "ILLUMINATE":
                return ValidateIlluminate(gameState, playerId, parameters);
            case "MOVE":
                return ValidateMovement(gameState, playerId, parameters);
            case "SIGNAL":
                return ValidateSignal(gameState, playerId);
            case "USE_TOKEN":
                return ValidateTokenUse(gameState, playerId, parameters);
            default:
                return new ValidationResult 
                { 
                    IsValid = false, 
                    Message = $"Unknown action: {action}" 
                };
        }
    }
    
    private ValidationResult ValidateIlluminate(GameState state, string playerId, Dictionary<string, object> parameters)
    {
        var player = state.GetPlayer(playerId);
        if (player == null)
            return InvalidResult("Player not found");
            
        // Check illuminate attempts this turn
        if (player.IlluminateAttemptsThisTurn >= canonicalRules.IlluminateAttemptsPerTurn)
        {
            AddViolation("action_economy", "Exceeded illuminate attempts per turn");
            return InvalidResult($"Only {canonicalRules.IlluminateAttemptsPerTurn} illuminate per turn allowed");
        }
        
        // Check token availability
        if (player.Tokens <= 0)
        {
            AddViolation("resource", "No tokens available for illuminate");
            return InvalidResult("No tokens available");
        }
        
        // Validate target position
        var targetPos = (Vector2I)parameters.Get("target_position", Vector2I.Zero);
        if (!IsAdjacent(player.Position, targetPos))
        {
            AddViolation("range", "Illuminate target not adjacent");
            return InvalidResult("Can only illuminate adjacent cells");
        }
        
        return ValidResult();
    }
    
    private ValidationResult ValidateMovement(GameState state, string playerId, Dictionary<string, object> parameters)
    {
        var player = state.GetPlayer(playerId);
        if (player == null)
            return InvalidResult("Player not found");
            
        var targetPos = (Vector2I)parameters.Get("target_position", Vector2I.Zero);
        
        // Check movement this turn
        var maxMovement = state.IsCollapsing ? 
            canonicalRules.CollapseMovementPerTurn : 
            canonicalRules.MovementPerTurn;
            
        if (player.MovesThisTurn >= maxMovement)
        {
            AddViolation("action_economy", $"Exceeded movement per turn (max: {maxMovement})");
            return InvalidResult($"Already moved {maxMovement} times this turn");
        }
        
        // Validate movement rules
        if (!IsAdjacent(player.Position, targetPos))
        {
            AddViolation("movement", "Target not adjacent");
            return InvalidResult("Can only move to adjacent cells");
        }
        
        // Check light/darkness rules
        var targetCell = state.GetCell(targetPos);
        var currentCell = state.GetCell(player.Position);
        
        if (!targetCell.IsLit && !currentCell.IsLit)
        {
            AddViolation("light_rules", "Cannot move from darkness to darkness");
            return InvalidResult("Cannot move from darkness into darkness");
        }
        
        // Check for filers
        if (targetCell.HasFiler && !targetCell.IsLit)
        {
            AddViolation("danger", "Moving into filer in darkness");
            return InvalidResult("Cannot move into cell with filer in darkness");
        }
        
        return ValidResult();
    }
    
    private ValidationResult ValidateSignal(GameState state, string playerId)
    {
        var player = state.GetPlayer(playerId);
        if (player == null)
            return InvalidResult("Player not found");
            
        // Check additional actions
        if (player.AdditionalActionsThisTurn >= canonicalRules.AdditionalActionsPerTurn)
        {
            AddViolation("action_economy", "Exceeded additional actions per turn");
            return InvalidResult("No additional actions remaining");
        }
        
        return ValidResult();
    }
    
    private ValidationResult ValidateTokenUse(GameState state, string playerId, Dictionary<string, object> parameters)
    {
        var tokenType = parameters.Get("token_type", "").ToString();
        var player = state.GetPlayer(playerId);
        
        if (player == null)
            return InvalidResult("Player not found");
            
        switch (tokenType)
        {
            case "MEMORY":
                if (player.MemoryTokens <= 0)
                {
                    AddViolation("resource", "No memory tokens available");
                    return InvalidResult("No memory tokens available");
                }
                
                // Validate memory bridge creation
                if (state.IsCollapsing)
                {
                    AddViolation("timing", "Cannot create memory bridges during collapse");
                    return InvalidResult("Cannot create memory bridges during collapse");
                }
                break;
                
            case "UNFILE":
                if (!player.IsFiled)
                {
                    AddViolation("state", "Player not filed");
                    return InvalidResult("Player is not filed");
                }
                
                if (!state.IsCollapsing)
                {
                    AddViolation("timing", "Can only unfile during collapse");
                    return InvalidResult("Can only use unfile tokens during collapse");
                }
                break;
        }
        
        return ValidResult();
    }
    
    private bool VerifyGameState(Dictionary<string, object> parameters)
    {
        var state = parameters.Get("game_state") as GameState;
        if (state == null) return false;
        
        violations.Clear();
        
        // Verify player count
        if (state.Players.Count < canonicalRules.MinPlayers || 
            state.Players.Count > canonicalRules.MaxPlayers)
        {
            AddViolation("structure", $"Invalid player count: {state.Players.Count}");
        }
        
        // Verify collapse state
        if (state.IsCollapsing)
        {
            if (state.CollapseRoundsRemaining < 0 || 
                state.CollapseRoundsRemaining > canonicalRules.CollapseTimerMax)
            {
                AddViolation("collapse", $"Invalid collapse timer: {state.CollapseRoundsRemaining}");
            }
        }
        
        // Verify filer behavior
        var expectedFilerMode = GetExpectedFilerMode(state.NoiseLevel);
        if (state.FilerMode != expectedFilerMode)
        {
            AddViolation("filer", $"Filer mode mismatch. Expected: {expectedFilerMode}, Actual: {state.FilerMode}");
        }
        
        // Verify light mechanics
        foreach (var cell in state.Cells)
        {
            if (cell.LightDuration < 0)
            {
                AddViolation("light", $"Invalid light duration at {cell.Position}");
            }
        }
        
        return violations.Count == 0;
    }
    
    private string GetExpectedFilerMode(int noiseLevel)
    {
        if (noiseLevel <= canonicalRules.FilerDormantNoiseThreshold)
            return "DORMANT";
        else if (noiseLevel <= canonicalRules.FilerAlertNoiseThreshold)
            return "ALERT";
        else if (noiseLevel <= canonicalRules.FilerHuntingNoiseThreshold)
            return "HUNTING";
        else
            return "CRISIS";
    }
    
    private Dictionary<string, object> CheckRuleCompliance(Dictionary<string, object> parameters)
    {
        var targetAgent = parameters.Get("agent", "").ToString();
        var targetRules = parameters.Get("rules") as Dictionary<string, object>;
        
        var complianceReport = new Dictionary<string, object>
        {
            ["version"] = RULES_VERSION,
            ["agent"] = targetAgent,
            ["timestamp"] = Time.GetUnixTimeFromSystem(),
            ["compliant"] = true,
            ["violations"] = new List<string>()
        };
        
        // Compare against canonical rules
        var driftEntries = CompareRules(canonicalRules, targetRules);
        
        if (driftEntries.Count > 0)
        {
            complianceReport["compliant"] = false;
            complianceReport["violations"] = driftEntries;
        }
        
        return complianceReport;
    }
    
    private List<string> CompareRules(GameRules canonical, Dictionary<string, object> target)
    {
        var drifts = new List<string>();
        
        // Check each canonical rule
        foreach (var prop in typeof(GameRules).GetProperties())
        {
            var canonValue = prop.GetValue(canonical);
            var targetValue = target.GetValueOrDefault(prop.Name);
            
            if (targetValue == null || !canonValue.Equals(targetValue))
            {
                drifts.Add($"{prop.Name}: expected {canonValue}, got {targetValue}");
            }
        }
        
        return drifts;
    }
    
    private string GenerateFlattenedRulesJson()
    {
        var flatRules = new Dictionary<string, object>();
        
        // Flatten canonical rules
        foreach (var prop in typeof(GameRules).GetProperties())
        {
            flatRules[ToSnakeCase(prop.Name)] = prop.GetValue(canonicalRules);
        }
        
        // Add metadata
        flatRules["_version"] = RULES_VERSION;
        flatRules["_hash"] = RULES_HASH;
        flatRules["_generated"] = Time.GetUnixTimeFromSystem();
        
        return Json.Stringify(flatRules);
    }
    
    private Dictionary<string, object> DetectRuleDrift(Dictionary<string, object> parameters)
    {
        var agents = parameters.Get("agents") as List<string> ?? new List<string> 
        { 
            "GameStateGuardian", 
            "FrontendDeveloper", 
            "HardwareBridgeEngineer" 
        };
        
        var driftReport = new Dictionary<string, object>
        {
            ["version"] = RULES_VERSION,
            ["timestamp"] = Time.GetUnixTimeFromSystem(),
            ["agents"] = new Dictionary<string, object>()
        };
        
        foreach (var agentName in agents)
        {
            // Query each agent for their rules
            var agentRules = megaAgent.RouteToAgent<Dictionary<string, object>>(
                agentName, 
                "get_implemented_rules", 
                new Dictionary<string, object>()
            );
            
            if (agentRules != null)
            {
                var drifts = CompareRules(canonicalRules, agentRules);
                driftReport["agents"][agentName] = new Dictionary<string, object>
                {
                    ["drift_count"] = drifts.Count,
                    ["drifts"] = drifts
                };
            }
        }
        
        return driftReport;
    }
    
    private void AddViolation(string category, string message)
    {
        if (!violations.ContainsKey(category))
            violations[category] = new List<RuleViolation>();
            
        violations[category].Add(new RuleViolation
        {
            Category = category,
            Message = message,
            Timestamp = Time.GetUnixTimeFromSystem()
        });
    }
    
    private bool IsAdjacent(Vector2I from, Vector2I to)
    {
        var diff = to - from;
        return Math.Abs(diff.X) <= 1 && Math.Abs(diff.Y) <= 1;
    }
    
    private string ToSnakeCase(string input)
    {
        return string.Concat(input.Select((c, i) => 
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()
        ));
    }
    
    private ValidationResult ValidResult(string message = "Valid")
    {
        return new ValidationResult { IsValid = true, Message = message };
    }
    
    private ValidationResult InvalidResult(string message)
    {
        return new ValidationResult { IsValid = false, Message = message };
    }
}

// Rule data structures
public class GameRules
{
    // Game structure
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int GameDurationMinutes { get; set; }
    
    // Action economy
    public int IlluminateAttemptsPerTurn { get; set; }
    public int AdditionalActionsPerTurn { get; set; }
    public int MovementPerTurn { get; set; }
    public int CollapseMovementPerTurn { get; set; }
    
    // Collapse mechanics
    public int CollapseTimerBase { get; set; }
    public int CollapseTimerMax { get; set; }
    public float CollapseSparkChance { get; set; }
    
    // Filer behavior thresholds
    public int FilerDormantNoiseThreshold { get; set; }
    public int FilerAlertNoiseThreshold { get; set; }
    public int FilerHuntingNoiseThreshold { get; set; }
    public int FilerCrisisNoiseThreshold { get; set; }
    
    // Memory mechanics
    public int MemoryTokensPerPlayer { get; set; }
    public int MemoryBridgeDuration { get; set; }
    
    // Light mechanics
    public int SignalLightDuration { get; set; }
    public bool IlluminatePermanent { get; set; }
    public int AidronLightRadius { get; set; }
}

public class RuleViolation
{
    public string Category { get; set; }
    public string Message { get; set; }
    public double Timestamp { get; set; }
}

public class GameState
{
    public List<Player> Players { get; set; } = new();
    public List<Cell> Cells { get; set; } = new();
    public int NoiseLevel { get; set; }
    public bool IsCollapsing { get; set; }
    public int CollapseRoundsRemaining { get; set; }
    public string FilerMode { get; set; }
    
    public Player GetPlayer(string id) => Players.FirstOrDefault(p => p.Id == id);
    public Cell GetCell(Vector2I pos) => Cells.FirstOrDefault(c => c.Position == pos);
}

public class Player
{
    public string Id { get; set; }
    public Vector2I Position { get; set; }
    public int Tokens { get; set; }
    public int MemoryTokens { get; set; }
    public bool IsFiled { get; set; }
    public int IlluminateAttemptsThisTurn { get; set; }
    public int AdditionalActionsThisTurn { get; set; }
    public int MovesThisTurn { get; set; }
}

public class Cell
{
    public Vector2I Position { get; set; }
    public bool IsLit { get; set; }
    public int LightDuration { get; set; }
    public bool HasFiler { get; set; }
}