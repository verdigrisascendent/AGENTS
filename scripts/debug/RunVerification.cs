using Godot;
using System.Threading.Tasks;

/// <summary>
/// Run Verification - Executes the rules verification system
/// </summary>
public partial class RunVerification : Node
{
    public override async void _Ready()
    {
        GD.Print("\n=== RUNNING RULES VERIFICATION ===");
        GD.Print("Loading canonical rules...");
        
        // Load rules first
        ContextManager.LoadRules();
        
        // Wait a frame to ensure loading completes
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        
        // Run verification
        GD.Print("\nRunning verification suite...");
        var report = RulesVerifier.Run();
        
        // Display results
        GD.Print("\n=== VERIFICATION COMPLETE ===");
        GD.Print($"Overall Result: {(report.AllPassed ? "✅ ALL TESTS PASSED" : "❌ FAILURES DETECTED")}");
        
        // Check for report file
        var reportPath = "user://rules_verification_report.txt";
        if (FileAccess.FileExists(reportPath))
        {
            var file = FileAccess.Open(reportPath, FileAccess.ModeFlags.Read);
            var content = file.GetAsText();
            file.Close();
            
            GD.Print("\n=== DETAILED REPORT ===");
            GD.Print(content);
        }
        
        // Run health checks
        GD.Print("\n=== RUNNING HEALTH CHECKS ===");
        RunHealthChecks();
        
        // Test Game State Guardian
        GD.Print("\n=== TESTING GAME STATE GUARDIAN ===");
        TestGameStateGuardian();
        
        // Check for any violations
        if (!report.AllPassed)
        {
            GD.PrintErr("\n⚠️  VIOLATIONS FOUND - Review the report above for details");
        }
        else
        {
            GD.Print("\n✅ All systems are compliant with LITD_RULES_v8!");
        }
    }
    
    private void RunHealthChecks()
    {
        // Check if rules are loaded
        var rulesLoaded = ContextManager.GetCanonicalRules() != null;
        GD.Print($"Rules Loaded: {(rulesLoaded ? "✅" : "❌")}");
        
        if (rulesLoaded)
        {
            var rules = ContextManager.GetCanonicalRules();
            
            // Check action economy
            GD.Print("\nAction Economy Check:");
            GD.Print($"  Pre-collapse movement: {rules.MovementPerTurn} (expected: 1)");
            GD.Print($"  Collapse movement: {rules.CollapseMovementPerTurn} (expected: 2)");
            GD.Print($"  Illuminate attempts: {rules.IlluminateAttemptsPerTurn} (expected: 1)");
            GD.Print($"  Additional actions: {rules.AdditionalActionsPerTurn} (expected: 1)");
            
            // Verify values
            var movementCorrect = rules.MovementPerTurn == 1;
            var collapseCorrect = rules.CollapseMovementPerTurn == 2;
            var illuminateCorrect = rules.IlluminateAttemptsPerTurn == 1;
            var actionsCorrect = rules.AdditionalActionsPerTurn == 1;
            
            if (movementCorrect && collapseCorrect && illuminateCorrect && actionsCorrect)
            {
                GD.Print("  Result: ✅ Action economy matches canon");
            }
            else
            {
                GD.PrintErr("  Result: ❌ Action economy mismatch detected");
            }
        }
    }
    
    private void TestGameStateGuardian()
    {
        // Get the MegaAgent to access GameStateGuardian
        var megaAgent = GetNode<MegaAgent>("/root/MegaAgent");
        if (megaAgent == null)
        {
            GD.PrintErr("MegaAgent not found in autoload");
            return;
        }
        
        // Get GameStateGuardian
        var guardianResult = megaAgent.RouteToAgent("game_state_guardian", "get_status", 
            new System.Collections.Generic.Dictionary<string, object>());
        
        if (guardianResult != null)
        {
            GD.Print("GameStateGuardian Status:");
            var status = guardianResult as System.Collections.Generic.Dictionary<string, object>;
            if (status != null)
            {
                GD.Print($"  Agent Active: {status.GetValueOrDefault("active", false)}");
                GD.Print($"  Rules Loaded: {status.GetValueOrDefault("rules_loaded", false)}");
                
                // Check action budgets
                var actionBudgets = megaAgent.RouteToAgent("game_state_guardian", "get_action_budget",
                    new System.Collections.Generic.Dictionary<string, object> { ["is_collapse"] = false });
                    
                if (actionBudgets != null)
                {
                    var budgets = actionBudgets as System.Collections.Generic.Dictionary<string, object>;
                    GD.Print("\n  Action Budgets (Normal):");
                    GD.Print($"    Movement: {budgets.GetValueOrDefault("movement", 0)} (expected: 1)");
                    GD.Print($"    Illuminate: {budgets.GetValueOrDefault("illuminate", 0)} (expected: 1)");
                    GD.Print($"    Additional: {budgets.GetValueOrDefault("additional", 0)} (expected: 1)");
                }
                
                // Check collapse budgets
                var collapseBudgets = megaAgent.RouteToAgent("game_state_guardian", "get_action_budget",
                    new System.Collections.Generic.Dictionary<string, object> { ["is_collapse"] = true });
                    
                if (collapseBudgets != null)
                {
                    var budgets = collapseBudgets as System.Collections.Generic.Dictionary<string, object>;
                    GD.Print("\n  Action Budgets (Collapse):");
                    GD.Print($"    Movement: {budgets.GetValueOrDefault("movement", 0)} (expected: 2)");
                    GD.Print($"    Illuminate: {budgets.GetValueOrDefault("illuminate", 0)} (expected: 0)");
                    GD.Print($"    Additional: {budgets.GetValueOrDefault("additional", 0)} (expected: 0)");
                }
            }
        }
        else
        {
            GD.PrintErr("GameStateGuardian not responding or not initialized");
        }
    }
}