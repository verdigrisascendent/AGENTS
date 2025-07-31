using Godot;
using System.Collections.Generic;

/// <summary>
/// Debug Menu - Provides access to performance profiling and testing tools
/// </summary>
public partial class DebugMenu : Control
{
    private VBoxContainer menuContainer;
    private RichTextLabel outputLabel;
    private PerformanceProfiler performanceProfiler;
    private MemoryLeakDetector memoryLeakDetector;
    private LEDConnectionTester ledTester;
    
    private bool isVisible = false;
    
    public override void _Ready()
    {
        // Create UI
        CreateDebugUI();
        
        // Get debug tool references
        performanceProfiler = GetNode<PerformanceProfiler>("/root/PerformanceProfiler");
        memoryLeakDetector = GetNode<MemoryLeakDetector>("/root/MemoryLeakDetector");
        ledTester = GetNode<LEDConnectionTester>("/root/LEDConnectionTester");
        
        // Initially hidden
        Visible = false;
        
        // Only available in debug builds
        if (!OS.IsDebugBuild())
        {
            QueueFree();
            return;
        }
    }
    
    private void CreateDebugUI()
    {
        // Background panel
        var panel = new Panel();
        panel.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        panel.Modulate = new Color(0, 0, 0, 0.9f);
        AddChild(panel);
        
        // Main container
        var mainContainer = new HBoxContainer();
        mainContainer.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        mainContainer.AddThemeConstantOverride("separation", 20);
        AddChild(mainContainer);
        
        // Left side - Menu
        var leftPanel = new Panel();
        leftPanel.CustomMinimumSize = new Vector2(300, 0);
        mainContainer.AddChild(leftPanel);
        
        menuContainer = new VBoxContainer();
        menuContainer.Position = new Vector2(10, 10);
        menuContainer.AddThemeConstantOverride("separation", 10);
        leftPanel.AddChild(menuContainer);
        
        // Title
        var title = new Label();
        title.Text = "DEBUG MENU";
        title.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
        title.AddThemeColorOverride("font_color", Colors.Cyan);
        title.AddThemeFontSizeOverride("font_size", 20);
        menuContainer.AddChild(title);
        
        // Separator
        menuContainer.AddChild(new HSeparator());
        
        // Performance section
        AddSectionTitle("Performance");
        AddButton("Toggle Profiler", OnToggleProfiler);
        AddButton("Generate Performance Report", OnGeneratePerformanceReport);
        AddButton("Export Performance Data", OnExportPerformanceData);
        
        // Memory section
        AddSectionTitle("Memory");
        AddButton("Toggle Memory Monitor", OnToggleMemoryMonitor);
        AddButton("Force Garbage Collection", OnForceGC);
        AddButton("Generate Memory Report", OnGenerateMemoryReport);
        AddButton("Export Memory Data", OnExportMemoryData);
        
        // LED Testing section
        AddSectionTitle("LED Hardware");
        AddButton("Run Connection Tests", OnRunLEDTests);
        AddButton("Test LED Patterns", OnTestLEDPatterns);
        AddButton("Get Hardware Status", OnGetHardwareStatus);
        
        // Game Testing section
        AddSectionTitle("Game Testing");
        AddButton("Run Rules Verification", OnRunRulesVerification);
        AddButton("Run Full Validation", OnRunFullValidation);
        AddButton("Show Report Location", OnShowReportLocation);
        AddButton("Open Report Folder", OnOpenReportFolder);
        AddButton("Copy Report to Desktop", OnCopyReportToDesktop);
        AddButton("Simulate Collapse", OnSimulateCollapse);
        AddButton("Add Test Players", OnAddTestPlayers);
        AddButton("Fill Board with Lights", OnFillBoardWithLights);
        AddButton("Clear All Lights", OnClearAllLights);
        
        // Right side - Output
        var rightPanel = new Panel();
        rightPanel.CustomMinimumSize = new Vector2(500, 0);
        mainContainer.AddChild(rightPanel);
        
        var outputContainer = new VBoxContainer();
        outputContainer.Position = new Vector2(10, 10);
        outputContainer.Size = new Vector2(480, 560);
        rightPanel.AddChild(outputContainer);
        
        var outputTitle = new Label();
        outputTitle.Text = "OUTPUT";
        outputTitle.AddThemeColorOverride("font_color", Colors.Green);
        outputContainer.AddChild(outputTitle);
        
        outputContainer.AddChild(new HSeparator());
        
        // Output text
        outputLabel = new RichTextLabel();
        outputLabel.CustomMinimumSize = new Vector2(0, 500);
        outputLabel.BbcodeEnabled = true;
        outputLabel.ScrollFollowing = true;
        outputContainer.AddChild(outputLabel);
        
        // Clear button
        var clearButton = new Button();
        clearButton.Text = "Clear Output";
        clearButton.Pressed += () => outputLabel.Clear();
        outputContainer.AddChild(clearButton);
    }
    
    private void AddSectionTitle(string title)
    {
        menuContainer.AddChild(new HSeparator());
        
        var label = new Label();
        label.Text = title;
        label.AddThemeColorOverride("font_color", Colors.Yellow);
        label.AddThemeFontSizeOverride("font_size", 16);
        menuContainer.AddChild(label);
    }
    
    private void AddButton(string text, System.Action callback)
    {
        var button = new Button();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(280, 30);
        button.Pressed += () => 
        {
            callback?.Invoke();
            AddOutput($"[color=cyan]> {text}[/color]");
        };
        menuContainer.AddChild(button);
    }
    
    private void AddOutput(string text)
    {
        outputLabel.AppendText(text + "\n");
    }
    
    public override void _Input(InputEvent @event)
    {
        // Toggle with F12
        if (@event.IsActionPressed("ui_debug_menu"))
        {
            ToggleVisibility();
            GetViewport().SetInputAsHandled();
        }
    }
    
    private void ToggleVisibility()
    {
        isVisible = !isVisible;
        Visible = isVisible;
        
        if (isVisible)
        {
            AddOutput("[color=green]Debug menu opened[/color]");
        }
    }
    
    // Performance callbacks
    private void OnToggleProfiler()
    {
        if (performanceProfiler == null) return;
        
        if (performanceProfiler.isProfilingEnabled)
        {
            performanceProfiler.DisableProfiling();
            AddOutput("[color=yellow]Profiler disabled[/color]");
        }
        else
        {
            performanceProfiler.EnableProfiling();
            AddOutput("[color=green]Profiler enabled[/color]");
        }
    }
    
    private void OnGeneratePerformanceReport()
    {
        if (performanceProfiler == null) return;
        
        var report = performanceProfiler.GenerateReport();
        AddOutput("[color=white]" + report.Summary + "[/color]");
        
        if (report.Recommendations.Count > 0)
        {
            AddOutput("\n[color=yellow]Recommendations:[/color]");
            foreach (var rec in report.Recommendations)
            {
                AddOutput("  • " + rec);
            }
        }
    }
    
    private void OnExportPerformanceData()
    {
        if (performanceProfiler == null) return;
        
        var path = "user://performance_report.json";
        performanceProfiler.ExportProfileData(path);
        AddOutput($"[color=green]Performance data exported to {path}[/color]");
    }
    
    // Memory callbacks
    private void OnToggleMemoryMonitor()
    {
        if (memoryLeakDetector == null) return;
        
        if (memoryLeakDetector.isMonitoring)
        {
            memoryLeakDetector.StopMonitoring();
            AddOutput("[color=yellow]Memory monitoring stopped[/color]");
        }
        else
        {
            memoryLeakDetector.StartMonitoring();
            AddOutput("[color=green]Memory monitoring started[/color]");
        }
    }
    
    private void OnForceGC()
    {
        if (memoryLeakDetector == null) return;
        
        memoryLeakDetector.ForceGarbageCollection();
        AddOutput("[color=green]Garbage collection completed[/color]");
    }
    
    private void OnGenerateMemoryReport()
    {
        if (memoryLeakDetector == null) return;
        
        var report = memoryLeakDetector.GetMemoryReport();
        AddOutput($"[color=white]Memory Report:[/color]");
        AddOutput($"  Current Memory: {report.CurrentMemoryUsage:F1} MB");
        AddOutput($"  Memory Delta: {report.MemoryDelta:+0.0;-0.0} MB");
        AddOutput($"  Node Count: {report.NodeCount}");
        AddOutput($"  Texture Memory: {report.TextureMemory:F1} MB");
        AddOutput($"  Tracked Resources: {report.TrackedResourceCount}");
    }
    
    private void OnExportMemoryData()
    {
        if (memoryLeakDetector == null) return;
        
        var path = "user://memory_report.json";
        memoryLeakDetector.ExportMemoryReport(path);
        AddOutput($"[color=green]Memory data exported to {path}[/color]");
    }
    
    // LED Testing callbacks
    private void OnRunLEDTests()
    {
        if (ledTester == null) return;
        
        ledTester.StartTestSuite();
        AddOutput("[color=green]LED connection test suite started[/color]");
        AddOutput("[color=yellow]Check console for detailed results[/color]");
    }
    
    private void OnTestLEDPatterns()
    {
        var hardwareBridge = GetNode<HardwareBridgeEngineer>("/root/HardwareBridgeEngineer");
        if (hardwareBridge == null) return;
        
        // Test various LED effects
        var effects = new[] { "collapse_pulse", "memory_spark", "signal_pulse", "victory" };
        var randomEffect = effects[GD.RandRange(0, effects.Length - 1)];
        
        hardwareBridge.Execute("trigger_effect", new Dictionary<string, object>
        {
            ["effect"] = randomEffect,
            ["position"] = new Vector2I(4, 3),
            ["color"] = Colors.Cyan
        });
        
        AddOutput($"[color=green]Triggered LED effect: {randomEffect}[/color]");
    }
    
    private void OnGetHardwareStatus()
    {
        var hardwareBridge = GetNode<HardwareBridgeEngineer>("/root/HardwareBridgeEngineer");
        if (hardwareBridge == null) return;
        
        var status = hardwareBridge.Execute("get_status", new Dictionary<string, object>()) as Dictionary<string, object>;
        
        if (status != null)
        {
            AddOutput("[color=white]Hardware Status:[/color]");
            AddOutput($"  Connected: {status["connected"]}");
            AddOutput($"  State: {status["connection_state"]}");
            AddOutput($"  URL: {status["url"]}");
            AddOutput($"  Queued Commands: {status["queued_commands"]}");
            AddOutput($"  Active LEDs: {status["active_leds"]}");
        }
    }
    
    // Game Testing callbacks
    private void OnRunRulesVerification()
    {
        AddOutput("[color=cyan]Running Rules Verification...[/color]");
        
        // Run quick verification
        QuickVerification.RunQuickCheck();
        
        AddOutput("[color=green]Verification complete![/color]");
        AddOutput("[color=yellow]Check console output for detailed results[/color]");
    }
    
    private async void OnRunFullValidation()
    {
        AddOutput("[color=cyan]Running Full Validation Suite...[/color]");
        AddOutput("[color=yellow]This may take 30-60 seconds[/color]");
        
        // Create validation runner
        var runner = new ValidationRunner();
        GetTree().Root.AddChild(runner);
        runner.Ready();
        
        // Run full validation
        var report = await runner.RunFullValidation();
        
        // Show summary in debug menu
        AddOutput($"\n[color=white]VALIDATION COMPLETE[/color]");
        AddOutput($"Total Tests: {report.TotalTests}");
        AddOutput($"[color=green]Passed: {report.PassedTests}[/color]");
        if (report.FailedTests > 0)
        {
            AddOutput($"[color=red]Failed: {report.FailedTests}[/color]");
        }
        AddOutput($"Success Rate: {report.SuccessRate:F1}%");
        AddOutput($"\nReport saved to: user://validation_report.json");
        AddOutput("[color=yellow]Check console for detailed results[/color]");
        
        // Clean up
        runner.QueueFree();
    }
    
    private void OnShowReportLocation()
    {
        ShowValidationReport.ShowReportLocation();
        AddOutput("[color=yellow]Check console for report location details[/color]");
    }
    
    private void OnOpenReportFolder()
    {
        ShowValidationReport.OpenReportLocation();
        AddOutput("[color=green]Opened user data folder[/color]");
    }
    
    private void OnCopyReportToDesktop()
    {
        ShowValidationReport.CopyReportToDesktop();
        AddOutput("[color=green]Report copied to desktop (if it exists)[/color]");
        AddOutput("[color=yellow]Check console for details[/color]");
    }
    
    private void OnSimulateCollapse()
    {
        var gameScreen = GetNode<MainGameScreen>("/root/MainGameScreen");
        if (gameScreen == null) return;
        
        gameScreen.TriggerVaultCollapse();
        AddOutput("[color=red]Vault collapse triggered![/color]");
    }
    
    private void OnAddTestPlayers()
    {
        // This would add test players to the game
        AddOutput("[color=yellow]Test players feature not yet implemented[/color]");
    }
    
    private void OnFillBoardWithLights()
    {
        // This would fill the board with lights for testing
        AddOutput("[color=yellow]Fill board feature not yet implemented[/color]");
    }
    
    private void OnClearAllLights()
    {
        // This would clear all lights from the board
        AddOutput("[color=yellow]Clear lights feature not yet implemented[/color]");
    }
}