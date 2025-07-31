using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Validates game state transitions and behaviors
/// </summary>
public class GameStateValidator : IValidationSuite
{
    private MegaAgent megaAgent;
    private GameStateGuardian stateGuardian;
    
    // Expected phase order
    private readonly List<string> expectedPhases = new()
    {
        "OPENING", "SEARCH", "NETWORK", "ESCAPE", "COLLAPSE", "ENDED"
    };
    
    public void Initialize()
    {
        megaAgent = (MegaAgent)Engine.GetMainLoop().Root.GetNode("MegaAgent");
        
        // Get GameStateGuardian through MegaAgent
        var guardianResponse = megaAgent?.RouteToAgent("game_state_guardian", "get_instance", new Dictionary<string, object>());
        stateGuardian = guardianResponse as GameStateGuardian;
    }
    
    public void Cleanup()
    {
        // No cleanup needed
    }
    
    public List<ValidationTest> GetTests()
    {
        return new List<ValidationTest>
        {
            new ValidationTest
            {
                Name = "Phase Transitions",
                Description = "OPENING → SEARCH → NETWORK → ESCAPE → COLLAPSE → ENDED",
                IsCritical = true,
                TestFunc = TestPhaseTransitions
            },
            new ValidationTest
            {
                Name = "Budget Enforcement Per Turn",
                Description = "Action budgets reset and enforce correctly",
                IsCritical = true,
                TestFunc = TestBudgetEnforcement
            },
            new ValidationTest
            {
                Name = "Filer Behavior Pre-Collapse",
                Description = "Filers target darkness before collapse",
                IsCritical = true,
                TestFunc = TestFilerBehaviorPreCollapse
            },
            new ValidationTest
            {
                Name = "Filer Behavior During Collapse",
                Description = "Filers target lit squares during collapse",
                IsCritical = true,
                TestFunc = TestFilerBehaviorDuringCollapse
            },
            new ValidationTest
            {
                Name = "Light Persistence",
                Description = "Permanent vs temporary lights",
                IsCritical = true,
                TestFunc = TestLightPersistence
            },
            new ValidationTest
            {
                Name = "Win Condition Detection",
                Description = "All players reach exit",
                IsCritical = true,
                TestFunc = TestWinCondition
            },
            new ValidationTest
            {
                Name = "Loss Condition Detection",
                Description = "All filed or timer expires",
                IsCritical = true,
                TestFunc = TestLossCondition
            },
            new ValidationTest
            {
                Name = "Turn Order Management",
                Description = "Players take turns correctly",
                IsCritical = false,
                TestFunc = TestTurnOrder
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
    
    private async Task<ValidationResult> TestPhaseTransitions()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test phase transition logic
        var phaseTests = new List<(string from, string to, Dictionary<string, object> conditions)>
        {
            ("OPENING", "SEARCH", new() { ["all_players_placed"] = true }),
            ("SEARCH", "NETWORK", new() { ["aidron_found"] = true }),
            ("NETWORK", "ESCAPE", new() { ["exit_revealed"] = true }),
            ("ESCAPE", "COLLAPSE", new() { ["first_player_exited"] = true }),
            ("COLLAPSE", "ENDED", new() { ["all_players_exited"] = true })
        };
        
        foreach (var (fromPhase, toPhase, conditions) in phaseTests)
        {
            var transitionData = new Dictionary<string, object>
            {
                ["from_phase"] = fromPhase,
                ["to_phase"] = toPhase,
                ["conditions"] = conditions
            };
            
            var response = megaAgent?.RouteToAgent("game_state_guardian", "validate_transition", transitionData);
            
            if (response == null || !(bool)response)
            {
                result.Passed = false;
                result.Errors.Add($"Invalid transition: {fromPhase} → {toPhase}");
            }
        }
        
        // Test invalid transitions
        var invalidTransitions = new List<(string from, string to)>
        {
            ("OPENING", "COLLAPSE"), // Can't skip to collapse
            ("SEARCH", "ENDED"),     // Can't end from search
            ("COLLAPSE", "SEARCH")   // Can't go backwards
        };
        
        foreach (var (fromPhase, toPhase) in invalidTransitions)
        {
            var transitionData = new Dictionary<string, object>
            {
                ["from_phase"] = fromPhase,
                ["to_phase"] = toPhase
            };
            
            var response = megaAgent?.RouteToAgent("game_state_guardian", "validate_transition", transitionData);
            
            if (response != null && (bool)response)
            {
                result.Passed = false;
                result.Errors.Add($"Should not allow transition: {fromPhase} → {toPhase}");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestBudgetEnforcement()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test normal turn budget
        var normalBudget = megaAgent?.RouteToAgent("game_state_guardian", "get_action_budget", 
            new Dictionary<string, object> { ["is_collapse"] = false }) as Dictionary<string, object>;
        
        if (normalBudget == null)
        {
            result.Passed = false;
            result.Errors.Add("Failed to get normal turn budget");
        }
        else
        {
            var movement = (int)normalBudget.GetValueOrDefault("movement", 0);
            var illuminate = (int)normalBudget.GetValueOrDefault("illuminate", 0);
            var additional = (int)normalBudget.GetValueOrDefault("additional", 0);
            
            if (movement != 1 || illuminate != 1 || additional != 1)
            {
                result.Passed = false;
                result.Errors.Add($"Normal budget incorrect: Move={movement}, Illuminate={illuminate}, Additional={additional}");
            }
        }
        
        // Test collapse budget
        var collapseBudget = megaAgent?.RouteToAgent("game_state_guardian", "get_action_budget", 
            new Dictionary<string, object> { ["is_collapse"] = true }) as Dictionary<string, object>;
        
        if (collapseBudget == null)
        {
            result.Passed = false;
            result.Errors.Add("Failed to get collapse budget");
        }
        else
        {
            var movement = (int)collapseBudget.GetValueOrDefault("movement", 0);
            var illuminate = (int)collapseBudget.GetValueOrDefault("illuminate", 0);
            var additional = (int)collapseBudget.GetValueOrDefault("additional", 0);
            
            if (movement != 2 || illuminate != 0 || additional != 0)
            {
                result.Passed = false;
                result.Errors.Add($"Collapse budget incorrect: Move={movement}, Illuminate={illuminate}, Additional={additional}");
            }
        }
        
        // Test budget enforcement
        var enforcementTest = new Dictionary<string, object>
        {
            ["action_type"] = "move",
            ["actions_used"] = new Dictionary<string, int> { ["movement"] = 1 },
            ["is_collapse"] = false
        };
        
        var canPerform = megaAgent?.RouteToAgent("game_state_guardian", "can_perform_action", enforcementTest);
        
        if (canPerform != null && (bool)canPerform)
        {
            result.Passed = false;
            result.Errors.Add("Should not allow movement after budget spent");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestFilerBehaviorPreCollapse()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test filer targeting logic pre-collapse
        var filerTest = new Dictionary<string, object>
        {
            ["filer_position"] = new Vector2I(3, 3),
            ["is_collapse"] = false,
            ["nearby_tiles"] = new List<Dictionary<string, object>>
            {
                new() { ["position"] = new Vector2I(3, 2), ["is_lit"] = true },
                new() { ["position"] = new Vector2I(3, 4), ["is_lit"] = false }, // Dark tile
                new() { ["position"] = new Vector2I(2, 3), ["is_lit"] = true }
            }
        };
        
        var targetResponse = megaAgent?.RouteToAgent("ai_filer", "get_target_position", filerTest);
        
        if (targetResponse is Vector2I target)
        {
            // Should target the dark tile
            if (target != new Vector2I(3, 4))
            {
                result.Passed = false;
                result.Errors.Add($"Filer should target darkness pre-collapse, targeted {target}");
            }
        }
        else
        {
            result.Passed = false;
            result.Errors.Add("Failed to get filer target position");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestFilerBehaviorDuringCollapse()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test filer targeting logic during collapse
        var filerTest = new Dictionary<string, object>
        {
            ["filer_position"] = new Vector2I(3, 3),
            ["is_collapse"] = true,
            ["nearby_tiles"] = new List<Dictionary<string, object>>
            {
                new() { ["position"] = new Vector2I(3, 2), ["is_lit"] = true }, // Lit tile
                new() { ["position"] = new Vector2I(3, 4), ["is_lit"] = false },
                new() { ["position"] = new Vector2I(2, 3), ["is_lit"] = false }
            }
        };
        
        var targetResponse = megaAgent?.RouteToAgent("ai_filer", "get_target_position", filerTest);
        
        if (targetResponse is Vector2I target)
        {
            // Should target the lit tile during collapse
            if (target != new Vector2I(3, 2))
            {
                result.Passed = false;
                result.Errors.Add($"Filer should target lit areas during collapse, targeted {target}");
            }
        }
        else
        {
            result.Passed = false;
            result.Errors.Add("Failed to get filer target position during collapse");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestLightPersistence()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test different light types
        var lightTypes = new List<(string type, bool isPermanent)>
        {
            ("aidron", true),
            ("exit", true),
            ("memory_spark", true),
            ("illuminate_spell", false),
            ("signal_light", false)
        };
        
        foreach (var (type, expectedPermanent) in lightTypes)
        {
            var lightTest = new Dictionary<string, object>
            {
                ["light_type"] = type
            };
            
            var response = megaAgent?.RouteToAgent("light_manager", "is_permanent_light", lightTest);
            
            if (response == null)
            {
                result.Warnings.Add($"Could not verify {type} light persistence");
            }
            else if ((bool)response != expectedPermanent)
            {
                result.Passed = false;
                result.Errors.Add($"{type} light should be {(expectedPermanent ? "permanent" : "temporary")}");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestWinCondition()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test win condition detection
        var winScenarios = new List<Dictionary<string, object>>
        {
            // All players at exit
            new()
            {
                ["players"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "P1", ["at_exit"] = true, ["filed"] = false },
                    new() { ["name"] = "P2", ["at_exit"] = true, ["filed"] = false },
                    new() { ["name"] = "P3", ["at_exit"] = true, ["filed"] = false },
                    new() { ["name"] = "P4", ["at_exit"] = true, ["filed"] = false }
                },
                ["phase"] = "COLLAPSE"
            }
        };
        
        foreach (var scenario in winScenarios)
        {
            var response = megaAgent?.RouteToAgent("game_state_guardian", "check_win_condition", scenario);
            
            if (response == null || !(bool)response)
            {
                result.Passed = false;
                result.Errors.Add("Failed to detect win condition when all players at exit");
            }
        }
        
        // Test non-win scenarios
        var nonWinScenarios = new List<Dictionary<string, object>>
        {
            // One player not at exit
            new()
            {
                ["players"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "P1", ["at_exit"] = true, ["filed"] = false },
                    new() { ["name"] = "P2", ["at_exit"] = false, ["filed"] = false },
                    new() { ["name"] = "P3", ["at_exit"] = true, ["filed"] = false },
                    new() { ["name"] = "P4", ["at_exit"] = true, ["filed"] = false }
                },
                ["phase"] = "COLLAPSE"
            }
        };
        
        foreach (var scenario in nonWinScenarios)
        {
            var response = megaAgent?.RouteToAgent("game_state_guardian", "check_win_condition", scenario);
            
            if (response != null && (bool)response)
            {
                result.Passed = false;
                result.Errors.Add("Should not detect win when not all players at exit");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestLossCondition()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test loss condition scenarios
        var lossScenarios = new List<(string desc, Dictionary<string, object> scenario)>
        {
            // All players filed
            ("All filed", new()
            {
                ["players"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "P1", ["filed"] = true },
                    new() { ["name"] = "P2", ["filed"] = true },
                    new() { ["name"] = "P3", ["filed"] = true },
                    new() { ["name"] = "P4", ["filed"] = true }
                },
                ["phase"] = "COLLAPSE",
                ["rounds_remaining"] = 2
            }),
            
            // Timer expired
            ("Timer expired", new()
            {
                ["players"] = new List<Dictionary<string, object>>
                {
                    new() { ["name"] = "P1", ["filed"] = false },
                    new() { ["name"] = "P2", ["filed"] = false }
                },
                ["phase"] = "COLLAPSE",
                ["rounds_remaining"] = 0
            })
        };
        
        foreach (var (desc, scenario) in lossScenarios)
        {
            var response = megaAgent?.RouteToAgent("game_state_guardian", "check_loss_condition", scenario);
            
            if (response == null || !(bool)response)
            {
                result.Passed = false;
                result.Errors.Add($"Failed to detect loss condition: {desc}");
            }
        }
        
        // Test non-loss scenario
        var continuePlaying = new Dictionary<string, object>
        {
            ["players"] = new List<Dictionary<string, object>>
            {
                new() { ["name"] = "P1", ["filed"] = false },
                new() { ["name"] = "P2", ["filed"] = true },
                new() { ["name"] = "P3", ["filed"] = false }
            },
            ["phase"] = "COLLAPSE",
            ["rounds_remaining"] = 2
        };
        
        var continueResponse = megaAgent?.RouteToAgent("game_state_guardian", "check_loss_condition", continuePlaying);
        
        if (continueResponse != null && (bool)continueResponse)
        {
            result.Passed = false;
            result.Errors.Add("Should not detect loss when players remain and timer not expired");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestTurnOrder()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test turn order management
        var turnTest = new Dictionary<string, object>
        {
            ["players"] = new List<string> { "P1", "P2", "P3", "P4" },
            ["current_player"] = "P1"
        };
        
        // Get next player
        var nextPlayer = megaAgent?.RouteToAgent("turn_manager", "get_next_player", turnTest);
        
        if (nextPlayer == null || (string)nextPlayer != "P2")
        {
            result.Passed = false;
            result.Errors.Add($"Expected next player P2, got {nextPlayer}");
        }
        
        // Test wrap around
        turnTest["current_player"] = "P4";
        nextPlayer = megaAgent?.RouteToAgent("turn_manager", "get_next_player", turnTest);
        
        if (nextPlayer == null || (string)nextPlayer != "P1")
        {
            result.Passed = false;
            result.Errors.Add($"Expected wrap around to P1, got {nextPlayer}");
        }
        
        // Test filed player skip
        turnTest["current_player"] = "P1";
        turnTest["filed_players"] = new List<string> { "P2" };
        nextPlayer = megaAgent?.RouteToAgent("turn_manager", "get_next_player", turnTest);
        
        if (nextPlayer == null || (string)nextPlayer != "P3")
        {
            result.Passed = false;
            result.Errors.Add($"Should skip filed player P2, got {nextPlayer}");
        }
        
        await Task.CompletedTask;
        return result;
    }
}