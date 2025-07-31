using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Game State Guardian - Enforces all game rules and validates state transitions
/// </summary>
public partial class GameStateGuardian : Node, ISpecializedAgent
{
    private MegaAgent megaAgent;
    
    // Game state tracking
    private int currentRound = 1;
    private string currentPlayer = "";
    private bool collapseTriggered = false;
    private int collapseTurnsRemaining = 0;
    private Dictionary<string, PlayerState> players = new();
    private Dictionary<Vector2I, TileState> board = new();
    private List<Vector2I> filerPositions = new();
    
    // Rules constants (LITD_RULES_v8)
    private const int CollapseRounds = 3;
    private const int MaxPlayers = 5;
    private const int TokensPerPlayer = 2;
    
    public void Initialize(MegaAgent mega)
    {
        megaAgent = mega;
        GD.Print("[GameStateGuardian] Initialized - Enforcing game rules v8");
    }
    
    public object Execute(string task, Dictionary<string, object> parameters)
    {
        return task switch
        {
            "validate_action" => ValidateAction(parameters),
            "apply_action" => ApplyAction(parameters),
            "check_filing" => CheckFilingConditions(parameters),
            "advance_round" => AdvanceRound(),
            "trigger_collapse" => TriggerCollapse(),
            "get_game_state" => GetGameState(),
            _ => null
        };
    }
    
    private ValidationResult ValidateAction(Dictionary<string, object> parameters)
    {
        var playerId = parameters["player_id"].ToString();
        var action = parameters["action"].ToString();
        var targetPos = (Vector2I)parameters.GetValueOrDefault("target_position", Vector2I.Zero);
        
        if (!players.ContainsKey(playerId))
            return new ValidationResult(false, "Player not found");
            
        var player = players[playerId];
        
        // Check if it's player's turn
        if (playerId != currentPlayer)
            return new ValidationResult(false, "Not your turn");
            
        // Check if player has already acted
        if (player.HasActedThisTurn)
            return new ValidationResult(false, "Already performed action this turn");
            
        // Validate specific actions
        return action switch
        {
            "MOVE" => ValidateMove(player, targetPos),
            "SIGNAL" => ValidateSignal(player),
            "ILLUMINATE" => ValidateIlluminate(player, targetPos),
            "SPRINT" => ValidateSprint(player, targetPos),
            "MEMORY_SPARK" => ValidateMemorySpark(player, targetPos),
            "END_TURN" => new ValidationResult(true, "Turn ended"),
            _ => new ValidationResult(false, "Unknown action")
        };
    }
    
    private ValidationResult ValidateMove(PlayerState player, Vector2I targetPos)
    {
        // Check if target is adjacent
        var distance = (targetPos - player.Position).Abs();
        if (distance.X > 1 || distance.Y > 1)
            return new ValidationResult(false, "Target not adjacent");
            
        // Check if target is valid tile
        if (!board.ContainsKey(targetPos))
            return new ValidationResult(false, "Invalid target position");
            
        return new ValidationResult(true, "Move allowed");
    }
    
    private ValidationResult ValidateSignal(PlayerState player)
    {
        // Signal is always allowed as major action
        return new ValidationResult(true, "Signal allowed");
    }
    
    private ValidationResult ValidateIlluminate(PlayerState player, Vector2I targetPos)
    {
        // Check if player has token
        if (player.Tokens <= 0)
            return new ValidationResult(false, "No tokens available");
            
        // Check if target is within 2 tiles
        var distance = (targetPos - player.Position).Abs();
        if (distance.X > 2 || distance.Y > 2)
            return new ValidationResult(false, "Target too far");
            
        return new ValidationResult(true, "Illuminate allowed");
    }
    
    private ValidationResult ValidateSprint(PlayerState player, Vector2I targetPos)
    {
        // Check if in collapse mode
        if (!collapseTriggered)
            return new ValidationResult(false, "Sprint only available during collapse");
            
        // Check if target is within 2 tiles straight line
        var diff = targetPos - player.Position;
        if ((diff.X != 0 && diff.Y != 0) || Mathf.Max(Mathf.Abs(diff.X), Mathf.Abs(diff.Y)) > 2)
            return new ValidationResult(false, "Sprint must be straight line, max 2 tiles");
            
        return new ValidationResult(true, "Sprint allowed");
    }
    
    private ValidationResult ValidateMemorySpark(PlayerState player, Vector2I targetPos)
    {
        // Check if player has token
        if (player.Tokens <= 0)
            return new ValidationResult(false, "No tokens available");
            
        // Check if on memory spark location
        if (!board[player.Position].HasMemorySpark)
            return new ValidationResult(false, "No memory spark at current location");
            
        return new ValidationResult(true, "Memory spark allowed");
    }
    
    private ActionResult ApplyAction(Dictionary<string, object> parameters)
    {
        var validation = ValidateAction(parameters);
        if (!validation.IsValid)
            return new ActionResult(false, validation.Message);
            
        var playerId = parameters["player_id"].ToString();
        var action = parameters["action"].ToString();
        var player = players[playerId];
        
        switch (action)
        {
            case "MOVE":
                var targetPos = (Vector2I)parameters["target_position"];
                player.Position = targetPos;
                player.HasActedThisTurn = true;
                
                // Check filing conditions after move
                var filingResult = CheckFilingConditions(new Dictionary<string, object> 
                { 
                    ["player_id"] = playerId 
                });
                
                if (filingResult != null && (bool)filingResult)
                {
                    return new ActionResult(false, "Player was filed!");
                }
                break;
                
            case "SIGNAL":
                player.HasActedThisTurn = true;
                // Signal logic handled by other systems
                break;
                
            case "ILLUMINATE":
                player.Tokens--;
                player.HasActedThisTurn = true;
                var illuminatePos = (Vector2I)parameters["target_position"];
                board[illuminatePos].IsLit = true;
                board[illuminatePos].LightDuration = 1;
                break;
                
            case "END_TURN":
                EndPlayerTurn(playerId);
                break;
        }
        
        return new ActionResult(true, $"{action} completed");
    }
    
    private bool CheckFilingConditions(Dictionary<string, object> parameters)
    {
        var playerId = parameters["player_id"].ToString();
        var player = players[playerId];
        
        // Player must be in darkness
        if (board[player.Position].IsLit)
            return false;
            
        // Check if any filer is adjacent
        foreach (var filerPos in filerPositions)
        {
            var distance = (filerPos - player.Position).Abs();
            if (distance.X <= 1 && distance.Y <= 1)
            {
                // Filer attempts filing
                GD.Print($"[GameStateGuardian] Filer at {filerPos} attempts to file {playerId}");
                
                // For now, automatic success - would implement dice roll
                player.IsFiled = true;
                players.Remove(playerId);
                
                return true;
            }
        }
        
        return false;
    }
    
    private void EndPlayerTurn(string playerId)
    {
        players[playerId].HasActedThisTurn = false;
        
        // Advance to next player
        var playerList = players.Keys.ToList();
        var currentIndex = playerList.IndexOf(playerId);
        var nextIndex = (currentIndex + 1) % playerList.Count;
        currentPlayer = playerList[nextIndex];
        
        // Check if round complete
        if (nextIndex == 0)
        {
            AdvanceRound();
        }
    }
    
    private object AdvanceRound()
    {
        currentRound++;
        GD.Print($"[GameStateGuardian] Advanced to round {currentRound}");
        
        // Decay temporary lights
        foreach (var tile in board.Values)
        {
            if (tile.LightDuration > 0)
            {
                tile.LightDuration--;
                if (tile.LightDuration == 0 && !tile.IsPermanentLight)
                {
                    tile.IsLit = false;
                }
            }
        }
        
        // Handle collapse mode
        if (collapseTriggered)
        {
            collapseTurnsRemaining--;
            if (collapseTurnsRemaining <= 0)
            {
                return new { game_over = true, reason = "Vault collapsed!" };
            }
        }
        
        return new { round = currentRound, collapse_remaining = collapseTurnsRemaining };
    }
    
    private object TriggerCollapse()
    {
        if (!collapseTriggered)
        {
            collapseTriggered = true;
            collapseTurnsRemaining = CollapseRounds;
            GD.Print("[GameStateGuardian] COLLAPSE TRIGGERED! 3 rounds remaining!");
        }
        
        return new { collapse_triggered = true, rounds_remaining = collapseTurnsRemaining };
    }
    
    private Dictionary<string, object> GetGameState()
    {
        return new Dictionary<string, object>
        {
            ["round"] = currentRound,
            ["current_player"] = currentPlayer,
            ["collapse_mode"] = collapseTriggered,
            ["collapse_remaining"] = collapseTurnsRemaining,
            ["players"] = players,
            ["board"] = board
        };
    }
}

// Helper classes
public class PlayerState
{
    public Vector2I Position { get; set; }
    public int Tokens { get; set; } = 2;
    public bool HasActedThisTurn { get; set; }
    public bool IsFiled { get; set; }
    public bool HasExited { get; set; }
}

public class TileState
{
    public bool IsLit { get; set; }
    public bool IsPermanentLight { get; set; }
    public int LightDuration { get; set; }
    public bool HasMemorySpark { get; set; }
    public bool IsExit { get; set; }
    public bool IsAidron { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; }
    public string Message { get; }
    
    public ValidationResult(bool isValid, string message)
    {
        IsValid = isValid;
        Message = message;
    }
}

public class ActionResult
{
    public bool Success { get; }
    public string Message { get; }
    
    public ActionResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}