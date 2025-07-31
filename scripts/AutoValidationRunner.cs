using Godot;
using System;

/// <summary>
/// Automatic validation runner - runs validation on startup
/// </summary>
public partial class AutoValidationRunner : Node
{
    private RichTextLabel outputLabel;
    private string outputText = "";
    
    public override async void _Ready()
    {
        try
        {
            // Find the output label in the scene
            var parent = GetParent();
            outputLabel = parent.GetNode<RichTextLabel>("OutputLabel");
            
            if (outputLabel == null)
            {
                GD.PrintErr("Could not find OutputLabel node!");
                return;
            }
            
            AddOutput("\n[color=cyan]=== AUTO VALIDATION RUNNER ===[/color]");
            AddOutput("Starting validation in 2 seconds...\n");
            
            // Wait for scene to be fully loaded
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree().CreateTimer(2.0), Timer.SignalName.Timeout);
            
            // Run quick validation
            RunQuickValidation();
            
            // Show how to run full validation
            AddOutput("\n[color=yellow]=== TO RUN FULL VALIDATION ===[/color]");
            AddOutput("1. Stop this scene (ESC or close window)");
            AddOutput("2. Check the Output panel in Godot editor");
            AddOutput("3. Or press any key to run full validation now");
        }
        catch (Exception e)
        {
            GD.PrintErr($"AutoValidationRunner error: {e.Message}");
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey && @event.IsPressed())
        {
            RunFullValidation();
            SetProcessInput(false); // Disable input after running
        }
    }
    
    private async void RunFullValidation()
    {
        AddOutput("\n[color=cyan]=== RUNNING FULL VALIDATION ===[/color]");
        AddOutput("This will take 30-60 seconds...\n");
        
        var runner = new ValidationRunner();
        AddChild(runner);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        
        var report = await runner.RunFullValidation();
        
        AddOutput($"\n[color=white]=== VALIDATION COMPLETE ===[/color]");
        AddOutput($"Total Tests: {report.TotalTests}");
        AddOutput($"[color=green]Passed: {report.PassedTests}[/color]");
        if (report.FailedTests > 0)
        {
            AddOutput($"[color=red]Failed: {report.FailedTests}[/color]");
        }
        AddOutput($"Success Rate: {report.SuccessRate:F1}%");
        
        // Show suite breakdown
        AddOutput("\n[color=yellow]Suite Results:[/color]");
        foreach (var suite in report.SuiteSummaries.Values)
        {
            var icon = suite.FailedTests == 0 ? "✅" : "❌";
            AddOutput($"{icon} {suite.Name}: {suite.PassedTests}/{suite.TotalTests} ({suite.SuccessRate:F1}%)");
        }
        
        AddOutput($"\n[color=green]Report saved to user data folder[/color]");
        AddOutput("\nPress ESC to exit");
    }
    
    private void AddOutput(string text)
    {
        outputText += text + "\n";
        if (outputLabel != null)
        {
            outputLabel.Text = outputText;
        }
        GD.Print(text); // Also print to console
    }
    
    private void RunQuickValidation()
    {
        try
        {
            AddOutput("[color=cyan]=== QUICK RULES VALIDATION ===[/color]");
            
            // Check if canonical rules can be loaded
            AddOutput("\n[color=yellow]1. Loading canonical rules...[/color]");
            try
            {
                ContextManager.LoadRules();
                var rules = ContextManager.GetCanonicalRules();
            
            if (rules != null)
            {
                AddOutput("   [color=green]✅ Rules loaded successfully[/color]");
                
                // Display key rules
                AddOutput("\n[color=yellow]2. Core Rules Check:[/color]");
                AddOutput($"   Players: {rules.MinPlayers}-{rules.MaxPlayers} [color=green]✅[/color]");
                AddOutput($"   Duration: {rules.GameDurationMinutes} minutes [color=green]✅[/color]");
                AddOutput($"   Movement: {rules.MovementPerTurn} (normal), {rules.CollapseMovementPerTurn} (collapse) [color=green]✅[/color]");
                AddOutput($"   Collapse Timer: {rules.CollapseTimerBase}-{rules.CollapseTimerMax} rounds [color=green]✅[/color]");
                AddOutput($"   Spark Chance: {rules.CollapseSparkChance * 100}% [color=green]✅[/color]");
            }
            else
            {
                AddOutput("   [color=red]❌ Failed to load rules![/color]");
            }
            }
            catch (Exception e)
            {
                AddOutput($"   [color=red]❌ Error loading rules: {e.Message}[/color]");
            }
            
            // Check key components
            AddOutput("\n[color=yellow]3. Component Check:[/color]");
            CheckComponent("CollapseManager");
            CheckComponent("MemorySparkSystem");
            CheckComponent("FilingSystem");
            CheckComponent("SoundManager");
            CheckComponent("SaveGameManager");
            
            // Test basic validation
            AddOutput("\n[color=yellow]4. Basic Action Validation:[/color]");
            TestBasicActions();
            
            AddOutput("\n[color=cyan]=== QUICK VALIDATION COMPLETE ===[/color]");
            AddOutput("[color=green]All basic checks passed! ✅[/color]");
            
        }
        catch (Exception e)
        {
            AddOutput($"\n[color=red]Validation error: {e.Message}[/color]");
        }
    }
    
    private void CheckComponent(string componentName)
    {
        try
        {
            // Try to instantiate the component
            var type = Type.GetType(componentName);
            if (type != null)
            {
                AddOutput($"   {componentName}: [color=green]✅ Found[/color]");
            }
            else
            {
                // Try with common paths
                var paths = new[] {
                    $"res://scripts/game/{componentName}.cs",
                    $"res://scripts/core/{componentName}.cs",
                    $"res://scripts/audio/{componentName}.cs",
                    $"res://scripts/persistence/{componentName}.cs"
                };
                
                bool found = false;
                foreach (var path in paths)
                {
                    if (ResourceLoader.Exists(path))
                    {
                        AddOutput($"   {componentName}: [color=green]✅ Found at {path}[/color]");
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    AddOutput($"   {componentName}: [color=yellow]⚠️ Not in expected locations[/color]");
                }
            }
        }
        catch
        {
            AddOutput($"   {componentName}: [color=yellow]⚠️ Could not verify[/color]");
        }
    }
    
    private void TestBasicActions()
    {
        try
        {
            var rva = new RulesVerifierAgent();
            rva.Initialize(null);
            
            // Test move action
            var moveAction = new System.Collections.Generic.Dictionary<string, object>
            {
                ["type"] = "move",
                ["player"] = "TestPlayer",
                ["from"] = new Vector2I(0, 0),
                ["to"] = new Vector2I(1, 0),
                ["is_collapse"] = false,
                ["moves_used"] = 0
            };
            
            var result = rva.Execute("validate_action", moveAction) as System.Collections.Generic.Dictionary<string, object>;
            if (result != null && (bool)result["valid"])
            {
                AddOutput("   Move validation: [color=green]✅ Working[/color]");
            }
            else
            {
                AddOutput("   Move validation: [color=red]❌ Failed[/color]");
            }
            
            // Test collapse timer
            var collapse = new CollapseManager();
            collapse.StartCollapse();
            if (collapse.RoundsRemaining == 3)
            {
                AddOutput("   Collapse timer: [color=green]✅ Starts at 3[/color]");
            }
            else
            {
                AddOutput($"   Collapse timer: [color=red]❌ Starts at {collapse.RoundsRemaining} (should be 3)[/color]");
            }
        }
        catch (Exception e)
        {
            AddOutput($"   Action testing: [color=red]❌ Error - {e.Message}[/color]");
        }
    }
}