using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Rules Verifier - Static class to run verification of all implementations against canon
/// </summary>
public static class RulesVerifier
{
    /// <summary>
    /// Run complete rules verification to check all implementations match canon
    /// </summary>
    public static void Run()
    {
        GD.Print("\n=== RULES VERIFICATION AGENT (RVA) ===");
        GD.Print($"[RVA] Starting verification at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        // Ensure rules are loaded
        if (!ContextManager.AreRulesLoaded())
        {
            GD.PrintErr("[RVA] ERROR: Rules not loaded! Run ContextManager.LoadRules() first.");
            return;
        }
        
        var canonicalRules = ContextManager.GetRules();
        GD.Print($"[RVA] Canonical rules loaded: LITD_RULES_v8");
        
        // Initialize verification components
        var megaAgent = new MegaAgent();
        megaAgent._Ready();
        
        var rulesVerifierAgent = new RulesVerifierAgent();
        megaAgent.RegisterAgent("RulesVerifier", rulesVerifierAgent);
        
        // Step 1: Verify core rules match
        GD.Print("\n[RVA] Step 1: Verifying core rules...");
        VerifyCoreRules(canonicalRules);
        
        // Step 2: Check agent implementations
        GD.Print("\n[RVA] Step 2: Checking agent implementations...");
        var agentResults = CheckAgentImplementations(megaAgent, rulesVerifierAgent);
        
        // Step 3: Validate game mechanics
        GD.Print("\n[RVA] Step 3: Validating game mechanics...");
        var mechanicsResults = ValidateGameMechanics(megaAgent, rulesVerifierAgent);
        
        // Step 4: Test edge cases
        GD.Print("\n[RVA] Step 4: Testing edge cases...");
        var edgeCaseResults = TestEdgeCases(megaAgent, rulesVerifierAgent);
        
        // Generate final report
        GenerateFinalReport(agentResults, mechanicsResults, edgeCaseResults);
    }
    
    private static void VerifyCoreRules(GameRules rules)
    {
        var violations = new List<string>();
        
        // Verify game structure
        if (rules.MinPlayers != 4) violations.Add("MinPlayers should be 4");
        if (rules.MaxPlayers != 5) violations.Add("MaxPlayers should be 5");
        if (rules.GameDurationMinutes != 20) violations.Add("GameDurationMinutes should be 20");
        
        // Verify action economy
        if (rules.IlluminateAttemptsPerTurn != 1) violations.Add("IlluminateAttemptsPerTurn should be 1");
        if (rules.AdditionalActionsPerTurn != 1) violations.Add("AdditionalActionsPerTurn should be 1");
        if (rules.MovementPerTurn != 1) violations.Add("MovementPerTurn should be 1");
        if (rules.CollapseMovementPerTurn != 2) violations.Add("CollapseMovementPerTurn should be 2");
        
        // Verify collapse mechanics
        if (rules.CollapseTimerBase != 3) violations.Add("CollapseTimerBase should be 3");
        if (rules.CollapseTimerMax != 5) violations.Add("CollapseTimerMax should be 5");
        if (Math.Abs(rules.CollapseSparkChance - 0.75f) > 0.001f) violations.Add("CollapseSparkChance should be 0.75");
        
        // Verify filer thresholds
        if (rules.FilerDormantNoiseThreshold != 4) violations.Add("FilerDormantNoiseThreshold should be 4");
        if (rules.FilerAlertNoiseThreshold != 7) violations.Add("FilerAlertNoiseThreshold should be 7");
        if (rules.FilerHuntingNoiseThreshold != 12) violations.Add("FilerHuntingNoiseThreshold should be 12");
        if (rules.FilerCrisisNoiseThreshold != 13) violations.Add("FilerCrisisNoiseThreshold should be 13");
        
        // Report results
        if (violations.Count == 0)
        {
            GD.Print("[RVA] ✓ Core rules verification PASSED");
        }
        else
        {
            GD.PrintErr($"[RVA] ✗ Core rules verification FAILED with {violations.Count} violations:");
            foreach (var violation in violations)
            {
                GD.PrintErr($"  - {violation}");
            }
        }
    }
    
    private static Dictionary<string, bool> CheckAgentImplementations(MegaAgent megaAgent, RulesVerifierAgent verifier)
    {
        var results = new Dictionary<string, bool>();
        var agents = new List<string> { "GameStateGuardian", "AmigaAestheticEnforcer", "HardwareBridgeEngineer" };
        
        foreach (var agentName in agents)
        {
            GD.Print($"\n[RVA] Checking {agentName}...");
            
            // Request implemented rules from agent
            var agentRules = megaAgent.RouteToAgent<Dictionary<string, object>>(
                agentName,
                "get_implemented_rules",
                new Dictionary<string, object>()
            );
            
            if (agentRules != null)
            {
                var complianceReport = megaAgent.RouteToAgent<Dictionary<string, object>>(
                    "RulesVerifier",
                    "check_rule_compliance",
                    new Dictionary<string, object>
                    {
                        ["agent"] = agentName,
                        ["rules"] = agentRules
                    }
                );
                
                var isCompliant = (bool)complianceReport["compliant"];
                results[agentName] = isCompliant;
                
                if (isCompliant)
                {
                    GD.Print($"[RVA] ✓ {agentName} is compliant with canon");
                }
                else
                {
                    GD.PrintErr($"[RVA] ✗ {agentName} has violations:");
                    var violations = complianceReport["violations"] as List<string>;
                    foreach (var violation in violations)
                    {
                        GD.PrintErr($"    - {violation}");
                    }
                }
            }
            else
            {
                GD.PrintErr($"[RVA] ✗ {agentName} did not provide rules implementation");
                results[agentName] = false;
            }
        }
        
        return results;
    }
    
    private static Dictionary<string, bool> ValidateGameMechanics(MegaAgent megaAgent, RulesVerifierAgent verifier)
    {
        var results = new Dictionary<string, bool>();
        
        // Create test game state
        var testState = CreateTestGameState();
        
        // Test illuminate mechanics
        GD.Print("\n[RVA] Testing illuminate mechanics...");
        results["illuminate_valid"] = TestAction(megaAgent, "ILLUMINATE", testState, "player1", 
            new Dictionary<string, object> { ["target_position"] = new Vector2I(5, 6) }, true);
        
        results["illuminate_range"] = TestAction(megaAgent, "ILLUMINATE", testState, "player1",
            new Dictionary<string, object> { ["target_position"] = new Vector2I(10, 10) }, false);
        
        // Test movement mechanics
        GD.Print("\n[RVA] Testing movement mechanics...");
        results["movement_valid"] = TestAction(megaAgent, "MOVE", testState, "player1",
            new Dictionary<string, object> { ["target_position"] = new Vector2I(5, 6) }, true);
        
        // Test signal mechanics
        GD.Print("\n[RVA] Testing signal mechanics...");
        results["signal_valid"] = TestAction(megaAgent, "SIGNAL", testState, "player1",
            new Dictionary<string, object>(), true);
        
        // Test token mechanics
        GD.Print("\n[RVA] Testing token mechanics...");
        results["memory_token"] = TestAction(megaAgent, "USE_TOKEN", testState, "player1",
            new Dictionary<string, object> { ["token_type"] = "MEMORY" }, true);
        
        return results;
    }
    
    private static Dictionary<string, bool> TestEdgeCases(MegaAgent megaAgent, RulesVerifierAgent verifier)
    {
        var results = new Dictionary<string, bool>();
        
        GD.Print("\n[RVA] Testing edge case: No tokens for illuminate...");
        var noTokenState = CreateTestGameState();
        noTokenState.Players[0].Tokens = 0;
        results["no_tokens"] = TestAction(megaAgent, "ILLUMINATE", noTokenState, "player1",
            new Dictionary<string, object> { ["target_position"] = new Vector2I(5, 6) }, false);
        
        GD.Print("\n[RVA] Testing edge case: Exceeded action limit...");
        var maxActionsState = CreateTestGameState();
        maxActionsState.Players[0].IlluminateAttemptsThisTurn = 1;
        results["max_actions"] = TestAction(megaAgent, "ILLUMINATE", maxActionsState, "player1",
            new Dictionary<string, object> { ["target_position"] = new Vector2I(5, 6) }, false);
        
        GD.Print("\n[RVA] Testing edge case: Collapse movement...");
        var collapseState = CreateTestGameState();
        collapseState.IsCollapsing = true;
        collapseState.Players[0].MovesThisTurn = 1;
        results["collapse_movement"] = TestAction(megaAgent, "MOVE", collapseState, "player1",
            new Dictionary<string, object> { ["target_position"] = new Vector2I(5, 6) }, true);
        
        return results;
    }
    
    private static bool TestAction(MegaAgent megaAgent, string action, GameState state, 
        string playerId, Dictionary<string, object> extraParams, bool expectedValid)
    {
        var parameters = new Dictionary<string, object>
        {
            ["action"] = action,
            ["game_state"] = state,
            ["player_id"] = playerId
        };
        
        foreach (var kvp in extraParams)
        {
            parameters[kvp.Key] = kvp.Value;
        }
        
        var result = megaAgent.RouteToAgent<ValidationResult>(
            "RulesVerifier",
            "validate_action",
            parameters
        );
        
        bool passed = result.IsValid == expectedValid;
        string status = passed ? "✓ PASSED" : "✗ FAILED";
        
        GD.Print($"[RVA] {status}: {action} - Expected: {expectedValid}, Got: {result.IsValid}");
        if (!passed)
        {
            GD.PrintErr($"      Message: {result.Message}");
        }
        
        return passed;
    }
    
    private static GameState CreateTestGameState()
    {
        var state = new GameState
        {
            NoiseLevel = 3,
            IsCollapsing = false,
            CollapseRoundsRemaining = 0,
            FilerMode = "DORMANT"
        };
        
        // Add test players
        state.Players.Add(new Player
        {
            Id = "player1",
            Position = new Vector2I(5, 5),
            Tokens = 3,
            MemoryTokens = 2,
            IsFiled = false,
            IlluminateAttemptsThisTurn = 0,
            AdditionalActionsThisTurn = 0,
            MovesThisTurn = 0
        });
        
        // Add test cells
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                state.Cells.Add(new Cell
                {
                    Position = new Vector2I(x, y),
                    IsLit = (x == 5 && y == 5), // Player's position is lit
                    LightDuration = 0,
                    HasFiler = false
                });
            }
        }
        
        return state;
    }
    
    private static void GenerateFinalReport(Dictionary<string, bool> agentResults, 
        Dictionary<string, bool> mechanicsResults, Dictionary<string, bool> edgeCaseResults)
    {
        GD.Print("\n=== FINAL VERIFICATION REPORT ===");
        
        int totalTests = agentResults.Count + mechanicsResults.Count + edgeCaseResults.Count;
        int passedTests = agentResults.Values.Count(v => v) + 
                         mechanicsResults.Values.Count(v => v) + 
                         edgeCaseResults.Values.Count(v => v);
        
        GD.Print($"\nTotal Tests: {totalTests}");
        GD.Print($"Passed: {passedTests}");
        GD.Print($"Failed: {totalTests - passedTests}");
        GD.Print($"Success Rate: {(passedTests * 100.0 / totalTests):F1}%");
        
        // Agent compliance summary
        GD.Print("\nAgent Compliance:");
        foreach (var kvp in agentResults)
        {
            string status = kvp.Value ? "✓ COMPLIANT" : "✗ NON-COMPLIANT";
            GD.Print($"  {kvp.Key}: {status}");
        }
        
        // Mechanics validation summary
        GD.Print("\nMechanics Validation:");
        foreach (var kvp in mechanicsResults)
        {
            string status = kvp.Value ? "✓ PASSED" : "✗ FAILED";
            GD.Print($"  {kvp.Key}: {status}");
        }
        
        // Edge cases summary
        GD.Print("\nEdge Cases:");
        foreach (var kvp in edgeCaseResults)
        {
            string status = kvp.Value ? "✓ PASSED" : "✗ FAILED";
            GD.Print($"  {kvp.Key}: {status}");
        }
        
        // Final verdict
        GD.Print("\nFINAL VERDICT:");
        if (passedTests == totalTests)
        {
            GD.Print("✓ ALL IMPLEMENTATIONS MATCH CANON - VERIFICATION PASSED");
        }
        else
        {
            GD.PrintErr($"✗ VERIFICATION FAILED - {totalTests - passedTests} issues found");
            GD.PrintErr("  Please review violations and update implementations to match LITD_RULES_v8");
        }
        
        GD.Print($"\n[RVA] Verification completed at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        GD.Print("=== END OF VERIFICATION REPORT ===\n");
    }
}