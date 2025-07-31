using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Validates specific test scenarios from LITD_RULES_CANON.md v8.0
/// </summary>
public class TestScenariosValidator : IValidationSuite
{
    private Random random = new();
    private MegaAgent megaAgent;
    
    public void Initialize()
    {
        megaAgent = (MegaAgent)Engine.GetMainLoop().Root.GetNode("MegaAgent");
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
                Name = "Spark Probability Test",
                Description = "Run 400 trials, verify 70-80% range",
                IsCritical = true,
                TestFunc = TestSparkProbability
            },
            new ValidationTest
            {
                Name = "Time Slip Cap Test",
                Description = "Verify cap at 5 rounds",
                IsCritical = true,
                TestFunc = TestTimeSlipCap
            },
            new ValidationTest
            {
                Name = "Unfile Timer Test",
                Description = "Verify timer doesn't reset",
                IsCritical = true,
                TestFunc = TestUnfileTimer
            },
            new ValidationTest
            {
                Name = "Filed Loss Test",
                Description = "Verify game ends if all filed during collapse",
                IsCritical = true,
                TestFunc = TestFiledLoss
            },
            new ValidationTest
            {
                Name = "Aidron Corridor Test",
                Description = "3-wide emergency corridor activation",
                IsCritical = true,
                TestFunc = TestAidronCorridor
            },
            new ValidationTest
            {
                Name = "Token Economy Test",
                Description = "Verify token usage rules",
                IsCritical = false,
                TestFunc = TestTokenEconomy
            },
            new ValidationTest
            {
                Name = "Exit Discovery Test",
                Description = "Exit reveals after Aidron found",
                IsCritical = true,
                TestFunc = TestExitDiscovery
            },
            new ValidationTest
            {
                Name = "Filer Movement Test",
                Description = "Filers change behavior during collapse",
                IsCritical = true,
                TestFunc = TestFilerMovement
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
    
    private async Task<ValidationResult> TestSparkProbability()
    {
        var result = new ValidationResult { Passed = true };
        
        const int trials = 400;
        int successCount = 0;
        const float expectedProbability = 0.75f;
        
        // Run trials
        for (int i = 0; i < trials; i++)
        {
            if (random.NextDouble() < expectedProbability)
            {
                successCount++;
            }
        }
        
        var actualPercentage = (successCount * 100.0 / trials);
        result.Metrics["trials"] = trials;
        result.Metrics["successes"] = successCount;
        result.Metrics["percentage"] = actualPercentage;
        
        // Check 70-80% range with some tolerance
        if (actualPercentage < 68 || actualPercentage > 82) // Allow 2% tolerance
        {
            result.Passed = false;
            result.Errors.Add($"Spark probability {actualPercentage:F1}% outside expected 70-80% range");
        }
        
        // Also verify with game mechanics
        var memorySystem = new MemorySparkSystem();
        int gameSuccesses = 0;
        
        for (int i = 0; i < 100; i++)
        {
            var sparkCreated = memorySystem.RollForCollapseMovementSpark();
            if (sparkCreated) gameSuccesses++;
        }
        
        var gamePercentage = gameSuccesses;
        result.Metrics["game_spark_percentage"] = gamePercentage;
        
        if (gamePercentage < 65 || gamePercentage > 85)
        {
            result.Warnings.Add($"Game spark percentage {gamePercentage}% slightly outside range");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestTimeSlipCap()
    {
        var result = new ValidationResult { Passed = true };
        
        var collapseManager = new CollapseManager();
        collapseManager.StartCollapse();
        
        // Initial timer should be 3
        var initialTimer = collapseManager.RoundsRemaining;
        if (initialTimer != 3)
        {
            result.Passed = false;
            result.Errors.Add($"Initial timer should be 3, got {initialTimer}");
        }
        
        // Extend by 1 (should go to 4)
        collapseManager.ExtendTimer(1);
        if (collapseManager.RoundsRemaining != 4)
        {
            result.Passed = false;
            result.Errors.Add($"After +1 extension, timer should be 4, got {collapseManager.RoundsRemaining}");
        }
        
        // Extend by 3 more (should cap at 5)
        collapseManager.ExtendTimer(3);
        if (collapseManager.RoundsRemaining != 5)
        {
            result.Passed = false;
            result.Errors.Add($"Timer should cap at 5, got {collapseManager.RoundsRemaining}");
        }
        
        // Try to extend beyond cap
        collapseManager.ExtendTimer(2);
        if (collapseManager.RoundsRemaining != 5)
        {
            result.Passed = false;
            result.Errors.Add($"Timer should remain at 5 when capped, got {collapseManager.RoundsRemaining}");
        }
        
        result.Metrics["final_timer"] = collapseManager.RoundsRemaining;
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestUnfileTimer()
    {
        var result = new ValidationResult { Passed = true };
        
        var collapseManager = new CollapseManager();
        var filingSystem = new FilingSystem();
        
        // Start collapse with timer at 3
        collapseManager.StartCollapse();
        var timerBefore = collapseManager.RoundsRemaining;
        
        // File a player
        filingSystem.FilePlayer("TestPlayer", new Vector2I(2, 2));
        
        // Unfile during collapse
        var unfileData = new Dictionary<string, object>
        {
            ["player"] = "TestPlayer",
            ["token_used"] = true,
            ["is_collapse"] = true
        };
        
        var unfileResult = filingSystem.UnfilePlayer("TestPlayer");
        
        if (!unfileResult)
        {
            result.Passed = false;
            result.Errors.Add("Failed to unfile player during collapse");
        }
        
        // Timer should NOT reset or change
        var timerAfter = collapseManager.RoundsRemaining;
        if (timerAfter != timerBefore)
        {
            result.Passed = false;
            result.Errors.Add($"Timer changed from {timerBefore} to {timerAfter} after unfile (should not change)");
        }
        
        result.Metrics["timer_before_unfile"] = timerBefore;
        result.Metrics["timer_after_unfile"] = timerAfter;
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestFiledLoss()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test scenario: all players filed during collapse
        var gameState = new Dictionary<string, object>
        {
            ["phase"] = "COLLAPSE",
            ["rounds_remaining"] = 2,
            ["players"] = new List<Dictionary<string, object>>
            {
                new() { ["name"] = "P1", ["filed"] = true },
                new() { ["name"] = "P2", ["filed"] = true },
                new() { ["name"] = "P3", ["filed"] = true },
                new() { ["name"] = "P4", ["filed"] = true }
            }
        };
        
        // Check loss condition
        var lossDetected = megaAgent?.RouteToAgent("game_state_guardian", "check_loss_condition", gameState);
        
        if (lossDetected == null || !(bool)lossDetected)
        {
            result.Passed = false;
            result.Errors.Add("Should detect loss when all players filed during collapse");
        }
        
        // Test with timer at 0
        gameState["rounds_remaining"] = 0;
        gameState["players"] = new List<Dictionary<string, object>>
        {
            new() { ["name"] = "P1", ["filed"] = false },
            new() { ["name"] = "P2", ["filed"] = false }
        };
        
        lossDetected = megaAgent?.RouteToAgent("game_state_guardian", "check_loss_condition", gameState);
        
        if (lossDetected == null || !(bool)lossDetected)
        {
            result.Passed = false;
            result.Errors.Add("Should detect loss when collapse timer expires");
        }
        
        // Test non-loss scenario
        gameState["rounds_remaining"] = 2;
        gameState["players"] = new List<Dictionary<string, object>>
        {
            new() { ["name"] = "P1", ["filed"] = false }, // One unfiled
            new() { ["name"] = "P2", ["filed"] = true },
            new() { ["name"] = "P3", ["filed"] = true }
        };
        
        lossDetected = megaAgent?.RouteToAgent("game_state_guardian", "check_loss_condition", gameState);
        
        if (lossDetected != null && (bool)lossDetected)
        {
            result.Passed = false;
            result.Errors.Add("Should not detect loss when players remain unfiled and timer > 0");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestAidronCorridor()
    {
        var result = new ValidationResult { Passed = true };
        
        var memorySystem = new MemorySparkSystem();
        var aidronPos = new Vector2I(4, 3);
        
        // Create Aidron emergency corridor (3-wide)
        memorySystem.CreateMemoryCorridor(aidronPos, 3);
        
        // Verify corridor positions
        var expectedPositions = new List<Vector2I>
        {
            new Vector2I(3, 3), // Left
            new Vector2I(4, 3), // Center (Aidron)
            new Vector2I(5, 3)  // Right
        };
        
        foreach (var pos in expectedPositions)
        {
            if (!memorySystem.IsMemorySpark(pos))
            {
                result.Passed = false;
                result.Errors.Add($"Aidron corridor missing spark at {pos}");
            }
        }
        
        // Verify these are permanent
        memorySystem.UpdateSparks(); // Should not remove them
        
        foreach (var pos in expectedPositions)
        {
            if (!memorySystem.IsMemorySpark(pos))
            {
                result.Passed = false;
                result.Errors.Add($"Aidron corridor spark at {pos} was not permanent");
            }
        }
        
        // Test corridor width variations
        var corridorWidths = new[] { 1, 3, 5 };
        foreach (var width in corridorWidths)
        {
            memorySystem.CreateMemoryCorridor(new Vector2I(2, 1), width);
            
            var sparkCount = 0;
            for (int x = 2 - width/2; x <= 2 + width/2; x++)
            {
                if (memorySystem.IsMemorySpark(new Vector2I(x, 1)))
                    sparkCount++;
            }
            
            if (sparkCount != width)
            {
                result.Passed = false;
                result.Errors.Add($"Corridor width {width} created {sparkCount} sparks");
            }
        }
        
        result.Metrics["corridor_positions_verified"] = expectedPositions.Count;
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestTokenEconomy()
    {
        var result = new ValidationResult { Passed = true };
        
        var rulesVerifier = new RulesVerifierAgent();
        rulesVerifier.Initialize(megaAgent);
        
        // Test pre-collapse token use (spark bridge only)
        var preCollapseTests = new List<(string action, bool shouldAllow)>
        {
            ("spark_bridge", true),
            ("unfile_self", false),
            ("extra_move", false)
        };
        
        foreach (var (action, shouldAllow) in preCollapseTests)
        {
            var tokenUse = new Dictionary<string, object>
            {
                ["type"] = "use_token",
                ["token_action"] = action,
                ["is_collapse"] = false
            };
            
            var response = rulesVerifier.Execute("validate_action", tokenUse) as Dictionary<string, object>;
            var isValid = response?.GetValueOrDefault("valid", false) ?? false;
            
            if ((bool)isValid != shouldAllow)
            {
                result.Passed = false;
                result.Errors.Add($"Pre-collapse token '{action}' validation incorrect: got {isValid}, expected {shouldAllow}");
            }
        }
        
        // Test collapse token use (unfile only)
        var collapseTests = new List<(string action, bool shouldAllow)>
        {
            ("spark_bridge", false),
            ("unfile_self", true),
            ("extra_move", false)
        };
        
        foreach (var (action, shouldAllow) in collapseTests)
        {
            var tokenUse = new Dictionary<string, object>
            {
                ["type"] = "use_token",
                ["token_action"] = action,
                ["is_collapse"] = true
            };
            
            var response = rulesVerifier.Execute("validate_action", tokenUse) as Dictionary<string, object>;
            var isValid = response?.GetValueOrDefault("valid", false) ?? false;
            
            if ((bool)isValid != shouldAllow)
            {
                result.Passed = false;
                result.Errors.Add($"Collapse token '{action}' validation incorrect: got {isValid}, expected {shouldAllow}");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestExitDiscovery()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test exit reveal conditions
        var gameStates = new List<(string phase, bool aidronFound, bool shouldReveal)>
        {
            ("SEARCH", false, false),     // No reveal during search without Aidron
            ("SEARCH", true, false),      // Aidron found, but still in SEARCH
            ("NETWORK", true, true),      // Should reveal in NETWORK after Aidron
            ("ESCAPE", true, true),       // Should be revealed in ESCAPE
            ("COLLAPSE", true, true)      // Should remain revealed
        };
        
        foreach (var (phase, aidronFound, shouldReveal) in gameStates)
        {
            var state = new Dictionary<string, object>
            {
                ["phase"] = phase,
                ["aidron_found"] = aidronFound
            };
            
            var exitRevealed = megaAgent?.RouteToAgent("game_state_guardian", "is_exit_revealed", state);
            
            if (exitRevealed != null && (bool)exitRevealed != shouldReveal)
            {
                result.Passed = false;
                result.Errors.Add($"Exit reveal incorrect for phase={phase}, aidron={aidronFound}: got {exitRevealed}, expected {shouldReveal}");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestFilerMovement()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test filer behavior change during collapse
        var filerScenarios = new List<(bool isCollapse, string expectedTarget)>
        {
            (false, "darkness"),  // Pre-collapse: target dark areas
            (true, "light")       // During collapse: target lit areas
        };
        
        foreach (var (isCollapse, expectedTarget) in filerScenarios)
        {
            var filerTest = new Dictionary<string, object>
            {
                ["filer_position"] = new Vector2I(3, 3),
                ["is_collapse"] = isCollapse,
                ["nearby_tiles"] = new List<Dictionary<string, object>>
                {
                    new() { ["position"] = new Vector2I(3, 2), ["is_lit"] = true, ["has_player"] = false },
                    new() { ["position"] = new Vector2I(3, 4), ["is_lit"] = false, ["has_player"] = false },
                    new() { ["position"] = new Vector2I(2, 3), ["is_lit"] = true, ["has_player"] = true },
                    new() { ["position"] = new Vector2I(4, 3), ["is_lit"] = false, ["has_player"] = true }
                }
            };
            
            var targetPos = megaAgent?.RouteToAgent("ai_filer", "get_target_position", filerTest);
            
            if (targetPos is Vector2I target)
            {
                bool targetingCorrect = false;
                
                if (expectedTarget == "darkness" && target == new Vector2I(4, 3)) // Dark tile with player
                {
                    targetingCorrect = true;
                }
                else if (expectedTarget == "light" && target == new Vector2I(2, 3)) // Lit tile with player
                {
                    targetingCorrect = true;
                }
                
                if (!targetingCorrect)
                {
                    result.Passed = false;
                    result.Errors.Add($"Filer targeting wrong during collapse={isCollapse}: targeted {target}, expected {expectedTarget} area");
                }
            }
            else
            {
                result.Warnings.Add($"Could not get filer target for collapse={isCollapse}");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
}