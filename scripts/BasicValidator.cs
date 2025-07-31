using Godot;
using System;

public partial class BasicValidator : Node2D
{
    private Label statusLabel;
    private int testCount = 0;
    private int passCount = 0;
    
    public override void _Ready()
    {
        GD.Print("BasicValidator starting...");
        
        // Create a simple label
        statusLabel = new Label();
        statusLabel.Position = new Vector2(50, 50);
        statusLabel.AddThemeFontSizeOverride("font_size", 24);
        statusLabel.Text = "LIGHTS IN THE DARK - VALIDATION\n\nStarting tests...";
        AddChild(statusLabel);
        
        // Run tests after a frame
        CallDeferred(nameof(RunTests));
    }
    
    private void RunTests()
    {
        var results = "VALIDATION RESULTS:\n\n";
        
        // Test 1: Basic Godot functionality
        testCount++;
        try
        {
            var testVec = new Vector2(1, 1);
            results += "✓ Basic Vector2 creation works\n";
            passCount++;
        }
        catch
        {
            results += "✗ Basic Vector2 creation failed\n";
        }
        
        // Test 2: Check if rules file exists
        testCount++;
        var rulesPath = "res://data/LITD_RULES_CANON.md";
        if (FileAccess.FileExists(rulesPath))
        {
            results += "✓ Rules file found\n";
            passCount++;
        }
        else
        {
            results += "✗ Rules file not found at " + rulesPath + "\n";
        }
        
        // Test 3: Check key scripts
        var scriptsToCheck = new[] {
            "res://scripts/game/CollapseManager.cs",
            "res://scripts/game/MemorySparkSystem.cs",
            "res://scripts/game/FilingSystem.cs"
        };
        
        foreach (var script in scriptsToCheck)
        {
            testCount++;
            if (ResourceLoader.Exists(script))
            {
                results += $"✓ Found {script}\n";
                passCount++;
            }
            else
            {
                results += $"✗ Missing {script}\n";
            }
        }
        
        results += $"\n\nPASSED: {passCount}/{testCount} tests\n";
        results += "\nPress ESC to exit\n";
        results += "Press SPACE for detailed validation";
        
        statusLabel.Text = results;
        
        // Also print to console
        GD.Print(results);
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Escape)
            {
                GetTree().Quit();
            }
            else if (keyEvent.Keycode == Key.Space)
            {
                statusLabel.Text = "Running full validation...\nCheck console for details";
                RunFullValidation();
            }
        }
    }
    
    private void RunFullValidation()
    {
        GD.Print("\n=== FULL VALIDATION ===\n");
        
        // Check all game components
        CheckComponent("CollapseManager");
        CheckComponent("MemorySparkSystem");
        CheckComponent("FilingSystem");
        CheckComponent("SoundManager");
        CheckComponent("SaveGameManager");
        CheckComponent("HardwareBridgeEngineer");
        CheckComponent("TouchInputManager");
        CheckComponent("TPadController");
        
        GD.Print("\n=== VALIDATION COMPLETE ===");
        GD.Print("Check the Godot console for full details");
    }
    
    private void CheckComponent(string name)
    {
        var paths = new[] {
            $"res://scripts/game/{name}.cs",
            $"res://scripts/core/{name}.cs",
            $"res://scripts/hardware/{name}.cs",
            $"res://scripts/input/{name}.cs",
            $"res://scripts/audio/{name}.cs",
            $"res://scripts/persistence/{name}.cs"
        };
        
        foreach (var path in paths)
        {
            if (ResourceLoader.Exists(path))
            {
                GD.Print($"✓ {name} found at: {path}");
                return;
            }
        }
        
        GD.Print($"✗ {name} not found in expected locations");
    }
}