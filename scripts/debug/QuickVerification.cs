using Godot;
using System.Linq;

/// <summary>
/// Quick Verification - Runs a quick check of rules compliance
/// </summary>
public static class QuickVerification
{
    public static void RunQuickCheck()
    {
        GD.Print("\n🚀 RUNNING QUICK RULES VERIFICATION\n");
        
        // Load rules
        ContextManager.LoadRules();
        var rules = ContextManager.GetCanonicalRules();
        
        if (rules == null)
        {
            GD.PrintErr("❌ Failed to load canonical rules!");
            return;
        }
        
        GD.Print("✅ Canonical rules loaded successfully");
        
        // Check core values
        GD.Print("\n📋 CORE RULES CHECK:");
        var coreChecks = new[]
        {
            ($"Players: {rules.MinPlayers}-{rules.MaxPlayers}", rules.MinPlayers == 4 && rules.MaxPlayers == 5),
            ($"Game Duration: {rules.GameDurationMinutes} min", rules.GameDurationMinutes == 20),
            ($"Movement/Turn: {rules.MovementPerTurn}", rules.MovementPerTurn == 1),
            ($"Collapse Movement: {rules.CollapseMovementPerTurn}", rules.CollapseMovementPerTurn == 2),
            ($"Illuminate Attempts: {rules.IlluminateAttemptsPerTurn}", rules.IlluminateAttemptsPerTurn == 1),
            ($"Additional Actions: {rules.AdditionalActionsPerTurn}", rules.AdditionalActionsPerTurn == 1),
            ($"Collapse Timer: {rules.CollapseTimerBase}-{rules.CollapseTimerMax}", 
                rules.CollapseTimerBase == 3 && rules.CollapseTimerMax == 5),
            ($"Collapse Spark Chance: {rules.CollapseSparkChance:P0}", rules.CollapseSparkChance == 0.75f)
        };
        
        var corePass = true;
        foreach (var (desc, passed) in coreChecks)
        {
            GD.Print($"  {desc} - {(passed ? "✅" : "❌")}");
            if (!passed) corePass = false;
        }
        
        // Check game components
        GD.Print("\n🎮 GAME COMPONENT CHECK:");
        
        // Check if key components exist
        var components = new[]
        {
            ("CollapseManager", CheckComponentExists("CollapseManager")),
            ("MemorySparkSystem", CheckComponentExists("MemorySparkSystem")),
            ("FilingSystem", CheckComponentExists("FilingSystem")),
            ("SoundManager", CheckComponentExists("SoundManager")),
            ("SaveGameManager", CheckComponentExists("SaveGameManager")),
            ("HardwareBridgeEngineer", CheckComponentExists("HardwareBridgeEngineer")),
            ("TouchInputManager", CheckComponentExists("TouchInputManager")),
            ("TPadController", CheckComponentExists("TPadController"))
        };
        
        var componentPass = true;
        foreach (var (name, exists) in components)
        {
            GD.Print($"  {name}: {(exists ? "✅ Found" : "❌ Missing")}");
            if (!exists) componentPass = false;
        }
        
        // Quick action validation
        GD.Print("\n🌯 ACTION VALIDATION:");
        var rva = new RulesVerifierAgent();
        rva.Initialize(null); // Initialize with null MegaAgent for testing
        
        // Test basic actions
        var actionTests = new[]
        {
            ("Move Action", TestMoveAction(rva)),
            ("Illuminate Action", TestIlluminateAction(rva)),
            ("Signal Action", TestSignalAction(rva)),
            ("Token Usage", TestTokenUsage(rva))
        };
        
        var actionPass = true;
        foreach (var (test, passed) in actionTests)
        {
            GD.Print($"  {test}: {(passed ? "✅ Valid" : "❌ Invalid")}");
            if (!passed) actionPass = false;
        }
        
        // Summary
        GD.Print("\n📦 VERIFICATION SUMMARY:");
        GD.Print($"  Core Rules: {(corePass ? "✅ PASS" : "❌ FAIL")}");
        GD.Print($"  Components: {(componentPass ? "✅ PASS" : "❌ FAIL")}");
        GD.Print($"  Actions: {(actionPass ? "✅ PASS" : "❌ FAIL")}");
        
        var overallPass = corePass && componentPass && actionPass;
        GD.Print($"\n🎆 OVERALL RESULT: {(overallPass ? "✅ ALL SYSTEMS COMPLIANT" : "❌ VIOLATIONS DETECTED")}");
        
        if (!overallPass)
        {
            GD.PrintErr("\n⚠️  Review failures above and fix implementations to match LITD_RULES_v8");
        }
    }
    
    private static bool CheckComponentExists(string className)
    {
        // Check if the class can be instantiated
        try
        {
            var script = GD.Load($"res://scripts/game/{className}.cs") ??
                        GD.Load($"res://scripts/core/{className}.cs") ??
                        GD.Load($"res://scripts/audio/{className}.cs") ??
                        GD.Load($"res://scripts/persistence/{className}.cs") ??
                        GD.Load($"res://scripts/input/{className}.cs");
            
            return script != null;
        }
        catch
        {
            return false;
        }
    }
    
    private static bool TestMoveAction(RulesVerifierAgent rva)
    {
        var action = new System.Collections.Generic.Dictionary<string, object>
        {
            ["type"] = "move",
            ["player"] = "TestPlayer",
            ["from"] = new Vector2I(0, 0),
            ["to"] = new Vector2I(1, 0),
            ["is_collapse"] = false,
            ["moves_used"] = 0
        };
        
        var result = rva.Execute("validate_action", action) as System.Collections.Generic.Dictionary<string, object>;
        return result != null && (bool)result["valid"];
    }
    
    private static bool TestIlluminateAction(RulesVerifierAgent rva)
    {
        var action = new System.Collections.Generic.Dictionary<string, object>
        {
            ["type"] = "illuminate",
            ["player"] = "TestPlayer",
            ["position"] = new Vector2I(1, 1),
            ["attempts_used"] = 0
        };
        
        var result = rva.Execute("validate_action", action) as System.Collections.Generic.Dictionary<string, object>;
        return result != null && (bool)result["valid"];
    }
    
    private static bool TestSignalAction(RulesVerifierAgent rva)
    {
        var action = new System.Collections.Generic.Dictionary<string, object>
        {
            ["type"] = "signal",
            ["player"] = "TestPlayer",
            ["position"] = new Vector2I(2, 2),
            ["additional_used"] = 0
        };
        
        var result = rva.Execute("validate_action", action) as System.Collections.Generic.Dictionary<string, object>;
        return result != null && (bool)result["valid"];
    }
    
    private static bool TestTokenUsage(RulesVerifierAgent rva)
    {
        var action = new System.Collections.Generic.Dictionary<string, object>
        {
            ["type"] = "move",
            ["player"] = "TestPlayer",
            ["from"] = new Vector2I(0, 0),
            ["to"] = new Vector2I(2, 0), // 2 spaces = requires token
            ["is_collapse"] = false,
            ["moves_used"] = 0,
            ["tokens_available"] = 1
        };
        
        var result = rva.Execute("validate_action", action) as System.Collections.Generic.Dictionary<string, object>;
        return result != null && (bool)result["valid"] && (int)result["tokens_required"] == 1;
    }
}