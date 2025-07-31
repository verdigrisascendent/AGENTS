using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Validates UI/Frontend compliance
/// </summary>
public class UIFrontendValidator : IValidationSuite
{
    private Node currentScene;
    private MainGameScreen gameScreen;
    
    public void Initialize()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        currentScene = tree?.CurrentScene;
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
                Name = "Action Button Budget Enforcement",
                Description = "Buttons disable after budget spent",
                IsCritical = true,
                RequiresScreenshot = true,
                TestFunc = TestActionButtonBudget
            },
            new ValidationTest
            {
                Name = "Movement UI Collapse Indicator",
                Description = "Shows 2-step indicator during collapse",
                IsCritical = true,
                RequiresScreenshot = true,
                TestFunc = TestMovementUICollapse
            },
            new ValidationTest
            {
                Name = "T-Pad Visibility",
                Description = "T-Pad only visible during gameplay",
                IsCritical = false,
                RequiresScreenshot = true,
                TestFunc = TestTPadVisibility
            },
            new ValidationTest
            {
                Name = "Collapse Timer Display",
                Description = "Timer displays correctly (3→5)",
                IsCritical = true,
                RequiresScreenshot = true,
                TestFunc = TestCollapseTimerDisplay
            },
            new ValidationTest
            {
                Name = "Token Affordance Switching",
                Description = "Spark Bridge → Unfile based on phase",
                IsCritical = true,
                RequiresScreenshot = true,
                TestFunc = TestTokenAffordance
            },
            new ValidationTest
            {
                Name = "Player Status Display",
                Description = "Shows filed/unfiled state correctly",
                IsCritical = false,
                RequiresScreenshot = true,
                TestFunc = TestPlayerStatusDisplay
            },
            new ValidationTest
            {
                Name = "Exit Beacon Visual",
                Description = "Exit beacon pulses correctly",
                IsCritical = false,
                RequiresScreenshot = true,
                TestFunc = TestExitBeaconVisual
            },
            new ValidationTest
            {
                Name = "Memory Spark Indicators",
                Description = "Temporary lights show duration",
                IsCritical = false,
                RequiresScreenshot = true,
                TestFunc = TestMemorySparkIndicators
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
    
    private async Task<ValidationResult> TestActionButtonBudget()
    {
        var result = new ValidationResult { Passed = true };
        
        // Find game screen
        gameScreen = currentScene as MainGameScreen;
        if (gameScreen == null)
        {
            result.Warnings.Add("MainGameScreen not active, skipping UI test");
            return result;
        }
        
        // Simulate using up action budget
        var actionButtons = gameScreen.GetNode("UI/ActionPanel")?.GetChildren();
        if (actionButtons == null)
        {
            result.Passed = false;
            result.Errors.Add("Could not find action buttons");
            return result;
        }
        
        // Check button states after budget spent
        foreach (Node node in actionButtons)
        {
            if (node is Button button && button.Name == "IlluminateButton")
            {
                // Simulate illuminate action
                gameScreen.Call("_on_illuminate_pressed");
                await ToSignal(gameScreen.GetTree(), SceneTree.SignalName.ProcessFrame);
                
                // Button should be disabled after use
                if (!button.Disabled)
                {
                    result.Passed = false;
                    result.Errors.Add("Illuminate button should be disabled after use");
                }
            }
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestMovementUICollapse()
    {
        var result = new ValidationResult { Passed = true };
        
        if (gameScreen == null)
        {
            result.Warnings.Add("MainGameScreen not active, skipping test");
            return result;
        }
        
        // Trigger collapse
        gameScreen.TriggerVaultCollapse();
        await ToSignal(gameScreen.GetTree(), SceneTree.SignalName.ProcessFrame);
        
        // Check movement indicator
        var movementIndicator = gameScreen.GetNode("UI/MovementIndicator");
        if (movementIndicator == null)
        {
            result.Passed = false;
            result.Errors.Add("Movement indicator not found");
            return result;
        }
        
        // During collapse, should show "2 moves"
        if (movementIndicator is Label label)
        {
            if (!label.Text.Contains("2"))
            {
                result.Passed = false;
                result.Errors.Add($"Movement indicator should show 2 moves during collapse, shows: {label.Text}");
            }
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestTPadVisibility()
    {
        var result = new ValidationResult { Passed = true };
        
        // Check T-Pad visibility in different scenes
        var tpad = currentScene?.GetNode("TPadController");
        
        if (currentScene is MainGameScreen)
        {
            // Should be visible during gameplay
            if (tpad == null || !tpad.Visible)
            {
                result.Passed = false;
                result.Errors.Add("T-Pad should be visible during gameplay");
            }
        }
        else if (currentScene.Name.ToString().Contains("Menu"))
        {
            // Should not be visible in menus
            if (tpad != null && tpad.Visible)
            {
                result.Passed = false;
                result.Errors.Add("T-Pad should not be visible in menus");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestCollapseTimerDisplay()
    {
        var result = new ValidationResult { Passed = true };
        
        if (gameScreen == null)
        {
            result.Warnings.Add("MainGameScreen not active, skipping test");
            return result;
        }
        
        // Find collapse timer display
        var timerDisplay = gameScreen.GetNode("UI/CollapseTimer/Label");
        if (timerDisplay == null)
        {
            result.Passed = false;
            result.Errors.Add("Collapse timer display not found");
            return result;
        }
        
        // Start collapse and check initial value
        var collapseManager = gameScreen.GetNode<CollapseManager>("CollapseManager");
        if (collapseManager != null)
        {
            collapseManager.StartCollapse();
            await ToSignal(gameScreen.GetTree(), SceneTree.SignalName.ProcessFrame);
            
            if (timerDisplay is Label label)
            {
                if (!label.Text.Contains("3"))
                {
                    result.Passed = false;
                    result.Errors.Add($"Timer should start at 3, shows: {label.Text}");
                }
                
                // Test timer extension
                collapseManager.ExtendTimer(3);
                await ToSignal(gameScreen.GetTree(), SceneTree.SignalName.ProcessFrame);
                
                if (!label.Text.Contains("5"))
                {
                    result.Passed = false;
                    result.Errors.Add($"Timer should cap at 5, shows: {label.Text}");
                }
            }
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestTokenAffordance()
    {
        var result = new ValidationResult { Passed = true };
        
        if (gameScreen == null)
        {
            result.Warnings.Add("MainGameScreen not active, skipping test");
            return result;
        }
        
        // Find token button
        var tokenButton = gameScreen.GetNode("UI/ActionPanel/TokenButton") as Button;
        if (tokenButton == null)
        {
            result.Passed = false;
            result.Errors.Add("Token button not found");
            return result;
        }
        
        // Check pre-collapse state
        if (!tokenButton.Text.Contains("Spark Bridge"))
        {
            result.Passed = false;
            result.Errors.Add($"Token button should show 'Spark Bridge' pre-collapse, shows: {tokenButton.Text}");
        }
        
        // Trigger collapse
        gameScreen.TriggerVaultCollapse();
        await ToSignal(gameScreen.GetTree(), SceneTree.SignalName.ProcessFrame);
        
        // Check collapse state
        if (!tokenButton.Text.Contains("Unfile"))
        {
            result.Passed = false;
            result.Errors.Add($"Token button should show 'Unfile' during collapse, shows: {tokenButton.Text}");
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestPlayerStatusDisplay()
    {
        var result = new ValidationResult { Passed = true };
        
        if (gameScreen == null)
        {
            result.Warnings.Add("MainGameScreen not active, skipping test");
            return result;
        }
        
        // Find player status display
        var playerList = gameScreen.GetNode("UI/PlayerList");
        if (playerList == null)
        {
            result.Passed = false;
            result.Errors.Add("Player list display not found");
            return result;
        }
        
        // Check filed indicator
        var firstPlayer = playerList.GetChild(0);
        if (firstPlayer != null)
        {
            var filedIndicator = firstPlayer.GetNode("FiledIndicator");
            if (filedIndicator == null)
            {
                result.Passed = false;
                result.Errors.Add("Filed indicator not found on player display");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestExitBeaconVisual()
    {
        var result = new ValidationResult { Passed = true };
        
        if (gameScreen == null)
        {
            result.Warnings.Add("MainGameScreen not active, skipping test");
            return result;
        }
        
        // Find exit beacon
        var grid = gameScreen.GetNode("GameBoard/Grid");
        if (grid == null)
        {
            result.Passed = false;
            result.Errors.Add("Game grid not found");
            return result;
        }
        
        // Look for exit tile visual effects
        bool foundExitEffect = false;
        foreach (Node child in grid.GetChildren())
        {
            if (child.Name.ToString().Contains("Exit") && child.HasMethod("is_pulsing"))
            {
                foundExitEffect = true;
                break;
            }
        }
        
        if (!foundExitEffect)
        {
            result.Warnings.Add("Exit beacon pulse effect not verified");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestMemorySparkIndicators()
    {
        var result = new ValidationResult { Passed = true };
        
        if (gameScreen == null)
        {
            result.Warnings.Add("MainGameScreen not active, skipping test");
            return result;
        }
        
        // Create a memory spark
        var memorySystem = gameScreen.GetNode<MemorySparkSystem>("MemorySparkSystem");
        if (memorySystem != null)
        {
            memorySystem.CreateMemorySpark(new Vector2I(2, 2), 3);
            await ToSignal(gameScreen.GetTree(), SceneTree.SignalName.ProcessFrame);
            
            // Check for duration indicator
            var grid = gameScreen.GetNode("GameBoard/Grid");
            bool foundDurationIndicator = false;
            
            foreach (Node child in grid.GetChildren())
            {
                if (child.Name.ToString().Contains("MemorySpark"))
                {
                    var durationLabel = child.GetNode("DurationLabel");
                    if (durationLabel != null)
                    {
                        foundDurationIndicator = true;
                        break;
                    }
                }
            }
            
            if (!foundDurationIndicator)
            {
                result.Warnings.Add("Memory spark duration indicators not found");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task ToSignal(SceneTree tree, StringName signal)
    {
        await tree.ToSignal(tree, signal);
    }
}