using Godot;
using System;

public partial class SimpleAutoValidator : Control
{
    private RichTextLabel outputLabel;
    
    public override void _Ready()
    {
        // Create UI programmatically to avoid scene dependency issues
        SetupUI();
        
        // Start validation after a short delay
        var timer = new Timer();
        timer.WaitTime = 1.0;
        timer.Timeout += StartValidation;
        timer.OneShot = true;
        AddChild(timer);
        timer.Start();
    }
    
    private void SetupUI()
    {
        // Set control to full screen
        SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        
        // Add background
        var bg = new ColorRect();
        bg.Color = new Color(0.05f, 0.05f, 0.1f, 1.0f);
        bg.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        AddChild(bg);
        
        // Add output label
        outputLabel = new RichTextLabel();
        outputLabel.BbcodeEnabled = true;
        outputLabel.ScrollFollowing = true;
        outputLabel.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        outputLabel.SetOffsetsPreset(Control.Preset.FullRect, Control.PresetMode.KeepSize, 20);
        outputLabel.AddThemeFontSizeOverride("normal_font_size", 16);
        outputLabel.AddThemeColorOverride("default_color", new Color(0.9f, 0.9f, 0.9f, 1.0f));
        AddChild(outputLabel);
        
        AddOutput("[color=cyan]LIGHTS IN THE DARK - VALIDATION SYSTEM[/color]\n");
        AddOutput("Initializing...\n");
    }
    
    private void StartValidation()
    {
        AddOutput("[color=yellow]=== BASIC VALIDATION ===[/color]\n");
        
        // Test 1: Check if we can create basic game objects
        AddOutput("1. Testing basic object creation:");
        TestBasicObjects();
        
        // Test 2: Check file system
        AddOutput("\n2. Testing file system:");
        TestFileSystem();
        
        // Test 3: Simple rules check
        AddOutput("\n3. Testing rules loading:");
        TestRulesLoading();
        
        AddOutput("\n[color=cyan]=== VALIDATION COMPLETE ===[/color]");
        AddOutput("\nPress [color=yellow]SPACE[/color] to run full validation");
        AddOutput("Press [color=yellow]ESC[/color] to exit");
    }
    
    private void TestBasicObjects()
    {
        try
        {
            // Test Vector2I
            var pos = new Vector2I(5, 5);
            AddOutput($"   ✓ Vector2I creation: {pos}");
            
            // Test dictionary
            var dict = new Godot.Collections.Dictionary();
            dict["test"] = true;
            AddOutput("   ✓ Dictionary creation: OK");
            
            // Test node creation
            var testNode = new Node();
            testNode.QueueFree();
            AddOutput("   ✓ Node creation: OK");
        }
        catch (Exception e)
        {
            AddOutput($"   ✗ Error: {e.Message}");
        }
    }
    
    private void TestFileSystem()
    {
        try
        {
            // Check if rules file exists
            var rulesPath = "res://data/LITD_RULES_CANON.md";
            if (ResourceLoader.Exists(rulesPath))
            {
                AddOutput($"   ✓ Rules file found: {rulesPath}");
            }
            else
            {
                AddOutput($"   ✗ Rules file not found: {rulesPath}");
            }
            
            // Check scripts directory
            var scriptPaths = new[] {
                "res://scripts/game/CollapseManager.cs",
                "res://scripts/game/MemorySparkSystem.cs",
                "res://scripts/game/FilingSystem.cs"
            };
            
            foreach (var path in scriptPaths)
            {
                if (ResourceLoader.Exists(path))
                {
                    AddOutput($"   ✓ Found: {path}");
                }
                else
                {
                    AddOutput($"   ✗ Missing: {path}");
                }
            }
        }
        catch (Exception e)
        {
            AddOutput($"   ✗ Error: {e.Message}");
        }
    }
    
    private void TestRulesLoading()
    {
        try
        {
            // Simple test - just try to read the rules file
            var rulesPath = "res://data/LITD_RULES_CANON.md";
            if (ResourceLoader.Exists(rulesPath))
            {
                AddOutput("   ✓ Rules file accessible");
                
                // Try to parse some basic values
                var text = FileAccess.Open(rulesPath, FileAccess.ModeFlags.Read)?.GetAsText() ?? "";
                if (text.Contains("LIGHTS IN THE DARK"))
                {
                    AddOutput("   ✓ Rules file contains game title");
                }
                if (text.Contains("2-4 players"))
                {
                    AddOutput("   ✓ Rules file contains player count");
                }
                if (text.Contains("vault collapse"))
                {
                    AddOutput("   ✓ Rules file contains collapse mechanics");
                }
            }
            else
            {
                AddOutput("   ✗ Cannot access rules file");
            }
        }
        catch (Exception e)
        {
            AddOutput($"   ✗ Error: {e.Message}");
        }
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
                RunFullValidation();
                SetProcessInput(false);
            }
        }
    }
    
    private async void RunFullValidation()
    {
        AddOutput("\n[color=cyan]=== RUNNING FULL VALIDATION ===[/color]");
        AddOutput("Creating validation runner...\n");
        
        try
        {
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
            
            SetProcessInput(true);
        }
        catch (Exception e)
        {
            AddOutput($"\n[color=red]Validation error: {e.Message}[/color]");
            AddOutput($"[color=red]Stack trace: {e.StackTrace}[/color]");
            SetProcessInput(true);
        }
    }
    
    private void AddOutput(string text)
    {
        if (outputLabel != null)
        {
            outputLabel.AppendText(text + "\n");
        }
        GD.Print(text);
    }
}