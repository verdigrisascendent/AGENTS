using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Filing System - Handles player filing mechanics per LITD_RULES_v8
/// </summary>
public partial class FilingSystem : Node
{
    [Signal]
    public delegate void PlayerFiledEventHandler(string playerName, Vector2I position);
    
    [Signal]
    public delegate void PlayerUnfiledEventHandler(string playerName);
    
    [Signal]
    public delegate void FilingAttemptEventHandler(string playerName, bool success);
    
    // Filed players tracking
    private Dictionary<string, FiledPlayer> filedPlayers = new();
    
    // References
    private RulesVerifierAgent rulesVerifier;
    private HardwareBridgeEngineer hardwareBridge;
    
    public override void _Ready()
    {
        var megaAgent = GetNode<MegaAgent>("/root/GameInitializer/MegaAgent");
        rulesVerifier = new RulesVerifierAgent();
        rulesVerifier.Initialize(megaAgent);
        
        hardwareBridge = GetNode<HardwareBridgeEngineer>("/root/GameInitializer/HardwareBridge");
    }
    
    public bool AttemptFiling(string playerName, Vector2I playerPos, Vector2I filerPos, bool isPlayerInDarkness)
    {
        // Validate filing attempt
        if (!isPlayerInDarkness)
        {
            GD.Print($"[FilingSystem] Cannot file {playerName} - player is in light");
            EmitSignal(SignalName.FilingAttempt, playerName, false);
            return false;
        }
        
        // Check adjacency
        var diff = playerPos - filerPos;
        if (Mathf.Abs(diff.X) > 1 || Mathf.Abs(diff.Y) > 1)
        {
            GD.Print($"[FilingSystem] Cannot file {playerName} - not adjacent to filer");
            EmitSignal(SignalName.FilingAttempt, playerName, false);
            return false;
        }
        
        // File the player
        FilePlayer(playerName, playerPos);
        return true;
    }
    
    private void FilePlayer(string playerName, Vector2I position)
    {
        if (filedPlayers.ContainsKey(playerName))
        {
            GD.Print($"[FilingSystem] {playerName} is already filed");
            return;
        }
        
        var filedPlayer = new FiledPlayer
        {
            Name = playerName,
            FiledPosition = position,
            CanUnfileInCollapse = true
        };
        
        filedPlayers[playerName] = filedPlayer;
        
        // Trigger effects
        EmitSignal(SignalName.PlayerFiled, playerName, position);
        
        // LED effect - player "disappears"
        var effectParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "player_filed",
            ["position"] = position
        };
        hardwareBridge?.Execute("trigger_effect", effectParams);
        
        GD.Print($"[FilingSystem] {playerName} has been FILED at {position}!");
        EmitSignal(SignalName.FilingAttempt, playerName, true);
    }
    
    public bool AttemptUnfile(string playerName, bool isCollapsing, bool hasMemoryToken)
    {
        if (!filedPlayers.ContainsKey(playerName))
        {
            GD.Print($"[FilingSystem] {playerName} is not filed");
            return false;
        }
        
        var filedPlayer = filedPlayers[playerName];
        
        // Check unfiling rules
        if (!isCollapsing)
        {
            GD.Print($"[FilingSystem] Can only unfile during collapse");
            return false;
        }
        
        if (!hasMemoryToken)
        {
            GD.Print($"[FilingSystem] Need memory token to unfile");
            return false;
        }
        
        if (!filedPlayer.CanUnfileInCollapse)
        {
            GD.Print($"[FilingSystem] {playerName} cannot be unfiled");
            return false;
        }
        
        // Unfile the player
        UnfilePlayer(playerName);
        return true;
    }
    
    private void UnfilePlayer(string playerName)
    {
        if (!filedPlayers.ContainsKey(playerName))
            return;
            
        var filedPlayer = filedPlayers[playerName];
        filedPlayers.Remove(playerName);
        
        EmitSignal(SignalName.PlayerUnfiled, playerName);
        
        // LED effect - player "reappears"
        var effectParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "player_unfiled",
            ["position"] = filedPlayer.FiledPosition
        };
        hardwareBridge?.Execute("trigger_effect", effectParams);
        
        GD.Print($"[FilingSystem] {playerName} has been UNFILED!");
    }
    
    public bool IsPlayerFiled(string playerName)
    {
        return filedPlayers.ContainsKey(playerName);
    }
    
    public Vector2I? GetFiledPosition(string playerName)
    {
        if (filedPlayers.TryGetValue(playerName, out var filedPlayer))
        {
            return filedPlayer.FiledPosition;
        }
        return null;
    }
    
    public List<string> GetFiledPlayers()
    {
        return filedPlayers.Keys.ToList();
    }
    
    public int GetFiledCount()
    {
        return filedPlayers.Count;
    }
    
    public void ResetFilingSystem()
    {
        filedPlayers.Clear();
        GD.Print("[FilingSystem] Filing system reset");
    }
    
    // SOS Pattern for filed players
    public void TriggerFiledSOS(string playerName)
    {
        if (!filedPlayers.ContainsKey(playerName))
            return;
            
        var position = filedPlayers[playerName].FiledPosition;
        
        // Trigger SOS LED pattern
        var sosParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "filed_sos",
            ["position"] = position
        };
        hardwareBridge?.Execute("trigger_effect", sosParams);
        
        GD.Print($"[FilingSystem] {playerName} sends SOS from filing!");
    }
}

// Helper class
public class FiledPlayer
{
    public string Name { get; set; }
    public Vector2I FiledPosition { get; set; }
    public bool CanUnfileInCollapse { get; set; }
}