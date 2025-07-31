using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Validates rules compliance against LITD_RULES_CANON.md v8.0
/// </summary>
public class RulesComplianceValidator : IValidationSuite
{
    private MegaAgent megaAgent;
    private RulesVerifierAgent rulesVerifier;
    private GameRules canonicalRules;
    
    public void Initialize()
    {
        // Load canonical rules
        ContextManager.LoadRules();
        canonicalRules = ContextManager.GetCanonicalRules();
        
        // Get agent references
        megaAgent = (MegaAgent)Engine.GetMainLoop().Root.GetNode("MegaAgent");
        rulesVerifier = new RulesVerifierAgent();
        rulesVerifier.Initialize(megaAgent);
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
                Name = "Core Rules Values",
                Description = "Verify all core rule values match canon",
                IsCritical = true,
                TestFunc = TestCoreRules
            },
            new ValidationTest
            {
                Name = "Action Economy Pre-Collapse",
                Description = "1 illuminate, 1 other, 1 move",
                IsCritical = true,
                TestFunc = TestActionEconomyPreCollapse
            },
            new ValidationTest
            {
                Name = "Action Economy During Collapse",
                Description = "2 moves, 1 illuminate, 1 other",
                IsCritical = true,
                TestFunc = TestActionEconomyDuringCollapse
            },
            new ValidationTest
            {
                Name = "Collapse Timer Base",
                Description = "Base timer should be 3 rounds",
                IsCritical = true,
                TestFunc = TestCollapseTimerBase
            },
            new ValidationTest
            {
                Name = "Collapse Timer Cap",
                Description = "Timer cap should be 5 rounds",
                IsCritical = true,
                TestFunc = TestCollapseTimerCap
            },
            new ValidationTest
            {
                Name = "Spark Chance",
                Description = "75% chance on collapse movement",
                IsCritical = true,
                TestFunc = TestSparkChance
            },
            new ValidationTest
            {
                Name = "Token Uses Pre-Collapse",
                Description = "Spark bridge only",
                IsCritical = false,
                TestFunc = TestTokenUsesPreCollapse
            },
            new ValidationTest
            {
                Name = "Token Uses During Collapse",
                Description = "Unfile self only",
                IsCritical = false,
                TestFunc = TestTokenUsesDuringCollapse
            },
            new ValidationTest
            {
                Name = "Aidron Emergency Corridor",
                Description = "Auto-activates 3-wide during collapse",
                IsCritical = true,
                TestFunc = TestAidronEmergencyCorridor
            },
            new ValidationTest
            {
                Name = "Loss Condition",
                Description = "All players filed before timer ends",
                IsCritical = true,
                TestFunc = TestLossCondition
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
    
    private async Task<ValidationResult> TestCoreRules()
    {
        var result = new ValidationResult { Passed = true };
        
        // Check all core values
        var checks = new List<(string name, int expected, int actual)>
        {
            ("MinPlayers", 4, canonicalRules.MinPlayers),
            ("MaxPlayers", 5, canonicalRules.MaxPlayers),
            ("GameDurationMinutes", 20, canonicalRules.GameDurationMinutes),
            ("MovementPerTurn", 1, canonicalRules.MovementPerTurn),
            ("CollapseMovementPerTurn", 2, canonicalRules.CollapseMovementPerTurn),
            ("IlluminateAttemptsPerTurn", 1, canonicalRules.IlluminateAttemptsPerTurn),
            ("AdditionalActionsPerTurn", 1, canonicalRules.AdditionalActionsPerTurn),
            ("CollapseTimerBase", 3, canonicalRules.CollapseTimerBase),
            ("CollapseTimerMax", 5, canonicalRules.CollapseTimerMax)
        };
        
        foreach (var (name, expected, actual) in checks)
        {
            if (actual != expected)
            {
                result.Passed = false;
                result.Errors.Add($"{name}: Expected {expected}, got {actual}");
            }
        }
        
        // Check float values
        if (Math.Abs(canonicalRules.CollapseSparkChance - 0.75f) > 0.001f)
        {
            result.Passed = false;
            result.Errors.Add($"CollapseSparkChance: Expected 0.75, got {canonicalRules.CollapseSparkChance}");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestActionEconomyPreCollapse()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test pre-collapse action budget
        var action = new Dictionary<string, object>
        {
            ["type"] = "validate_action",
            ["action_type"] = "move",
            ["is_collapse"] = false,
            ["moves_used"] = 0
        };
        
        var response = rulesVerifier.Execute("validate_action", action) as Dictionary<string, object>;
        
        if (response == null || !(bool)response.GetValueOrDefault("valid", false))
        {
            result.Passed = false;
            result.Errors.Add("Failed to validate pre-collapse movement");
        }
        
        // Test exceeding movement budget
        action["moves_used"] = 1;
        response = rulesVerifier.Execute("validate_action", action) as Dictionary<string, object>;
        
        if (response != null && (bool)response.GetValueOrDefault("valid", false))
        {
            result.Passed = false;
            result.Errors.Add("Should not allow more than 1 movement pre-collapse");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestActionEconomyDuringCollapse()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test collapse action budget - 2 moves allowed
        var action = new Dictionary<string, object>
        {
            ["type"] = "validate_action",
            ["action_type"] = "move",
            ["is_collapse"] = true,
            ["moves_used"] = 1
        };
        
        var response = rulesVerifier.Execute("validate_action", action) as Dictionary<string, object>;
        
        if (response == null || !(bool)response.GetValueOrDefault("valid", false))
        {
            result.Passed = false;
            result.Errors.Add("Should allow 2 movements during collapse");
        }
        
        // Test exceeding collapse movement
        action["moves_used"] = 2;
        response = rulesVerifier.Execute("validate_action", action) as Dictionary<string, object>;
        
        if (response != null && (bool)response.GetValueOrDefault("valid", false))
        {
            result.Passed = false;
            result.Errors.Add("Should not allow more than 2 movements during collapse");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestCollapseTimerBase()
    {
        var result = new ValidationResult { Passed = true };
        
        var collapseManager = new CollapseManager();
        collapseManager.StartCollapse();
        
        if (collapseManager.RoundsRemaining != 3)
        {
            result.Passed = false;
            result.Errors.Add($"Collapse timer should start at 3, got {collapseManager.RoundsRemaining}");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestCollapseTimerCap()
    {
        var result = new ValidationResult { Passed = true };
        
        var collapseManager = new CollapseManager();
        collapseManager.StartCollapse();
        
        // Try to extend beyond cap
        collapseManager.ExtendTimer(5); // Should cap at 5
        
        if (collapseManager.RoundsRemaining != 5)
        {
            result.Passed = false;
            result.Errors.Add($"Collapse timer should cap at 5, got {collapseManager.RoundsRemaining}");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestSparkChance()
    {
        var result = new ValidationResult { Passed = true };
        
        // Run multiple trials to verify probability
        const int trials = 1000;
        int sparks = 0;
        var random = new Random();
        
        for (int i = 0; i < trials; i++)
        {
            if (random.NextDouble() < canonicalRules.CollapseSparkChance)
            {
                sparks++;
            }
        }
        
        var percentage = (sparks * 100.0 / trials);
        
        // Allow 5% variance
        if (percentage < 70 || percentage > 80)
        {
            result.Passed = false;
            result.Errors.Add($"Spark chance should be ~75%, got {percentage:F1}% in {trials} trials");
        }
        
        // Also verify the exact value
        if (Math.Abs(canonicalRules.CollapseSparkChance - 0.75f) > 0.001f)
        {
            result.Passed = false;
            result.Errors.Add($"Spark chance value should be 0.75, got {canonicalRules.CollapseSparkChance}");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestTokenUsesPreCollapse()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test spark bridge token use
        var action = new Dictionary<string, object>
        {
            ["type"] = "use_token",
            ["token_action"] = "spark_bridge",
            ["is_collapse"] = false
        };
        
        var response = rulesVerifier.Execute("validate_action", action) as Dictionary<string, object>;
        
        if (response == null || !(bool)response.GetValueOrDefault("valid", false))
        {
            result.Passed = false;
            result.Errors.Add("Should allow spark bridge token use pre-collapse");
        }
        
        // Test unfile token use (should fail pre-collapse)
        action["token_action"] = "unfile_self";
        response = rulesVerifier.Execute("validate_action", action) as Dictionary<string, object>;
        
        if (response != null && (bool)response.GetValueOrDefault("valid", false))
        {
            result.Passed = false;
            result.Errors.Add("Should not allow unfile token use pre-collapse");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestTokenUsesDuringCollapse()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test unfile token use during collapse
        var action = new Dictionary<string, object>
        {
            ["type"] = "use_token",
            ["token_action"] = "unfile_self",
            ["is_collapse"] = true
        };
        
        var response = rulesVerifier.Execute("validate_action", action) as Dictionary<string, object>;
        
        if (response == null || !(bool)response.GetValueOrDefault("valid", false))
        {
            result.Passed = false;
            result.Errors.Add("Should allow unfile token use during collapse");
        }
        
        // Test spark bridge token use (should fail during collapse)
        action["token_action"] = "spark_bridge";
        response = rulesVerifier.Execute("validate_action", action) as Dictionary<string, object>;
        
        if (response != null && (bool)response.GetValueOrDefault("valid", false))
        {
            result.Passed = false;
            result.Errors.Add("Should not allow spark bridge token use during collapse");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestAidronEmergencyCorridor()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test Aidron corridor activation during collapse
        var memorySystem = new MemorySparkSystem();
        
        // Simulate Aidron emergency corridor
        var aidronPos = new Vector2I(4, 3);
        memorySystem.CreateMemoryCorridor(aidronPos, 3); // Should create 3-wide corridor
        
        // Verify corridor width
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
                result.Errors.Add($"Aidron corridor missing at position {pos}");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestLossCondition()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test loss condition detection
        var gameState = new Dictionary<string, object>
        {
            ["all_players_filed"] = true,
            ["collapse_active"] = true,
            ["rounds_remaining"] = 2
        };
        
        // This should trigger loss condition
        var checkResult = megaAgent?.RouteToAgent("game_state_guardian", "check_loss_condition", gameState);
        
        if (checkResult == null || !(bool)checkResult)
        {
            result.Passed = false;
            result.Errors.Add("Failed to detect loss condition when all players filed during collapse");
        }
        
        await Task.CompletedTask;
        return result;
    }
}