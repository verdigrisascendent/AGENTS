using Godot;
using System;

/// <summary>
/// Collapse Manager - Handles vault collapse mechanics per LITD_RULES_v8
/// </summary>
public partial class CollapseManager : Node
{
    [Signal]
    public delegate void CollapseStartedEventHandler(int rounds);
    
    [Signal]
    public delegate void CollapseUpdateEventHandler(int roundsLeft);
    
    [Signal]
    public delegate void CollapseEndedEventHandler();
    
    [Signal]
    public delegate void CollapseEventTriggeredEventHandler(CollapseEventResult result);
    
    // State
    private bool active = false;
    private int roundsRemaining = 3;
    private const int BaseTimer = 3;
    private const int MaxTimer = 5;
    
    public bool IsActive => active;
    public int RoundsRemaining => roundsRemaining;
    
    public void StartCollapse()
    {
        if (active) return;
        
        active = true;
        roundsRemaining = BaseTimer;
        
        EmitSignal(SignalName.CollapseStarted, roundsRemaining);
        GD.Print($"[CollapseManager] Vault collapse started! {roundsRemaining} rounds remaining!");
    }
    
    public void ProcessRound()
    {
        if (!active) return;
        
        roundsRemaining--;
        
        if (roundsRemaining <= 0)
        {
            EndCollapse();
        }
        else
        {
            EmitSignal(SignalName.CollapseUpdate, roundsRemaining);
            GD.Print($"[CollapseManager] {roundsRemaining} rounds until collapse!");
            
            // Roll for collapse event
            TriggerCollapseEvent();
        }
    }
    
    private void TriggerCollapseEvent()
    {
        var eventType = CollapseEvents.RollEvent();
        GD.Print($"[CollapseManager] Collapse Event: {eventType}!");
        
        var context = new CollapseEventContext
        {
            CurrentRound = 0, // Would need to track this
            RoundsRemaining = roundsRemaining,
            GridWidth = 8,
            GridHeight = 6
        };
        
        var result = CollapseEvents.ProcessEvent(eventType, context);
        
        // Handle Time Slip
        if (result.ExtraRounds > 0)
        {
            ExtendTimer(result.ExtraRounds);
        }
        
        EmitSignal(SignalName.CollapseEventTriggered, result);
    }
    
    public void ExtendTimer(int rounds)
    {
        if (!active) return;
        
        var newTimer = roundsRemaining + rounds;
        roundsRemaining = Mathf.Min(newTimer, MaxTimer);
        
        GD.Print($"[CollapseManager] Timer extended to {roundsRemaining} rounds!");
    }
    
    private void EndCollapse()
    {
        active = false;
        EmitSignal(SignalName.CollapseEnded);
        GD.Print("[CollapseManager] Vault has collapsed!");
    }
}