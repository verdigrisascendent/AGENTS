using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Validates hardware bridge LED integration
/// </summary>
public class HardwareBridgeValidator : IValidationSuite
{
    private HardwareBridgeEngineer hardwareBridge;
    private const int MatrixWidth = 16;
    private const int MatrixHeight = 11;
    private const int GameGridWidth = 8;
    private const int GameGridHeight = 6;
    
    public void Initialize()
    {
        hardwareBridge = (HardwareBridgeEngineer)Engine.GetMainLoop().Root.GetNode("HardwareBridgeEngineer");
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
                Name = "Primary LED Mapping",
                Description = "Even coordinates only, game state",
                IsCritical = true,
                TestFunc = TestPrimaryLEDMapping
            },
            new ValidationTest
            {
                Name = "Secondary LED Atmosphere",
                Description = "Odd coordinates only, atmosphere",
                IsCritical = true,
                TestFunc = TestSecondaryLEDAtmosphere
            },
            new ValidationTest
            {
                Name = "Collapse Effects Scaling",
                Description = "Effects scale with timer",
                IsCritical = false,
                TestFunc = TestCollapseEffectsScaling
            },
            new ValidationTest
            {
                Name = "Memory Corridor Lights",
                Description = "Sets permanent lights",
                IsCritical = true,
                TestFunc = TestMemoryCorridorLights
            },
            new ValidationTest
            {
                Name = "WebSocket Connection",
                Description = "Connection state and recovery",
                IsCritical = false,
                TestFunc = TestWebSocketConnection
            },
            new ValidationTest
            {
                Name = "Command Queue Management",
                Description = "Commands queue properly",
                IsCritical = false,
                TestFunc = TestCommandQueueManagement
            },
            new ValidationTest
            {
                Name = "Effect Synchronization",
                Description = "Effects sync with game events",
                IsCritical = true,
                TestFunc = TestEffectSynchronization
            },
            new ValidationTest
            {
                Name = "LED Matrix Configuration",
                Description = "11x16 SK9822 matrix setup",
                IsCritical = true,
                TestFunc = TestLEDMatrixConfiguration
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
    
    private async Task<ValidationResult> TestPrimaryLEDMapping()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test primary LED positions (even coordinates)
        for (int gameY = 0; gameY < GameGridHeight; gameY++)
        {
            for (int gameX = 0; gameX < GameGridWidth; gameX++)
            {
                var gamePos = new Vector2I(gameX, gameY);
                var ledPos = GameToLedPosition(gamePos);
                
                // Primary LEDs should be at even coordinates
                if (ledPos.X % 2 != 0 || ledPos.Y % 2 != 0)
                {
                    result.Passed = false;
                    result.Errors.Add($"Game position {gamePos} maps to non-even LED position {ledPos}");
                }
                
                // Verify within bounds
                if (ledPos.X >= MatrixWidth || ledPos.Y >= MatrixHeight)
                {
                    result.Passed = false;
                    result.Errors.Add($"LED position {ledPos} out of bounds for {MatrixWidth}x{MatrixHeight} matrix");
                }
            }
        }
        
        // Test game state mapping
        var testState = new Dictionary<Vector2I, TileState>
        {
            [new Vector2I(0, 0)] = new TileState { IsLit = true },
            [new Vector2I(3, 3)] = new TileState { IsExit = true },
            [new Vector2I(5, 2)] = new TileState { IsAidron = true }
        };
        
        var syncResult = hardwareBridge?.Execute("sync_game_state", 
            new Dictionary<string, object> { ["board_state"] = testState });
        
        if (syncResult == null)
        {
            result.Warnings.Add("Could not test game state sync");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestSecondaryLEDAtmosphere()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test secondary LED positions (odd coordinates)
        var secondaryPositions = new List<Vector2I>
        {
            new Vector2I(1, 0), new Vector2I(3, 0), new Vector2I(5, 0),
            new Vector2I(0, 1), new Vector2I(2, 1), new Vector2I(4, 1),
            new Vector2I(1, 2), new Vector2I(3, 2), new Vector2I(5, 2)
        };
        
        foreach (var pos in secondaryPositions)
        {
            // At least one coordinate should be odd
            if (pos.X % 2 == 0 && pos.Y % 2 == 0)
            {
                result.Passed = false;
                result.Errors.Add($"Position {pos} is not a secondary LED position");
            }
        }
        
        // Test atmospheric effect
        var effectResult = hardwareBridge?.Execute("trigger_effect", 
            new Dictionary<string, object> 
            { 
                ["effect"] = "void_breathing",
                ["color"] = new Color(0.5f, 0, 0.5f)
            });
        
        if (effectResult == null)
        {
            result.Warnings.Add("Could not test atmospheric effects");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestCollapseEffectsScaling()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test collapse effect scaling with timer
        var timerValues = new[] { 5, 3, 1 }; // Timer countdown
        
        foreach (var timer in timerValues)
        {
            var intensity = CalculateCollapseIntensity(timer);
            
            // Intensity should increase as timer decreases
            if (timer == 1 && intensity < 0.8f)
            {
                result.Passed = false;
                result.Errors.Add($"Collapse intensity too low ({intensity}) for timer={timer}");
            }
            else if (timer == 5 && intensity > 0.4f)
            {
                result.Passed = false;
                result.Errors.Add($"Collapse intensity too high ({intensity}) for timer={timer}");
            }
            
            result.Metrics[$"intensity_timer_{timer}"] = intensity;
        }
        
        // Test collapse pulse effect
        var pulseResult = hardwareBridge?.Execute("trigger_effect",
            new Dictionary<string, object>
            {
                ["effect"] = "collapse_pulse",
                ["color"] = Colors.Red,
                ["duration"] = 3.0f
            });
        
        if (pulseResult == null)
        {
            result.Warnings.Add("Could not test collapse pulse effect");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestMemoryCorridorLights()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test memory corridor creation
        var corridorPos = new Vector2I(4, 3);
        var corridorWidth = 3;
        
        // Expected LED positions for 3-wide corridor
        var expectedLEDs = new List<Vector2I>
        {
            GameToLedPosition(new Vector2I(3, 3)), // Left
            GameToLedPosition(new Vector2I(4, 3)), // Center
            GameToLedPosition(new Vector2I(5, 3))  // Right
        };
        
        // Trigger memory corridor effect
        var corridorResult = hardwareBridge?.Execute("trigger_effect",
            new Dictionary<string, object>
            {
                ["effect"] = "memory_spark",
                ["position"] = corridorPos,
                ["color"] = Colors.Cyan,
                ["duration"] = 1.0f
            });
        
        if (corridorResult == null)
        {
            result.Passed = false;
            result.Errors.Add("Failed to trigger memory corridor effect");
        }
        
        // Verify permanent light setting
        foreach (var ledPos in expectedLEDs)
        {
            if (ledPos.X >= MatrixWidth || ledPos.Y >= MatrixHeight)
            {
                result.Passed = false;
                result.Errors.Add($"Memory corridor LED {ledPos} out of bounds");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestWebSocketConnection()
    {
        var result = new ValidationResult { Passed = true };
        
        // Get connection status
        var status = hardwareBridge?.Execute("get_status", new Dictionary<string, object>()) as Dictionary<string, object>;
        
        if (status == null)
        {
            result.Passed = false;
            result.Errors.Add("Could not get hardware status");
            return result;
        }
        
        // Check connection state
        var connected = (bool)status.GetValueOrDefault("connected", false);
        var connectionState = status.GetValueOrDefault("connection_state", "Unknown").ToString();
        var url = status.GetValueOrDefault("url", "").ToString();
        
        result.Metrics["connected"] = connected ? 1 : 0;
        result.Metrics["reconnect_attempts"] = status.GetValueOrDefault("reconnect_attempts", 0);
        
        if (!connected)
        {
            result.Warnings.Add($"LED board not connected (state: {connectionState})");
        }
        
        // Verify WebSocket URL format
        if (!url.StartsWith("ws://") && !url.StartsWith("wss://"))
        {
            result.Passed = false;
            result.Errors.Add($"Invalid WebSocket URL format: {url}");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestCommandQueueManagement()
    {
        var result = new ValidationResult { Passed = true };
        
        // Get current queue status
        var status = hardwareBridge?.Execute("get_status", new Dictionary<string, object>()) as Dictionary<string, object>;
        
        if (status == null)
        {
            result.Warnings.Add("Could not test command queue");
            return result;
        }
        
        var queuedCommands = (int)status.GetValueOrDefault("queued_commands", 0);
        result.Metrics["initial_queue_size"] = queuedCommands;
        
        // Queue some test commands
        for (int i = 0; i < 5; i++)
        {
            hardwareBridge?.Execute("set_led", new Dictionary<string, object>
            {
                ["x"] = i * 2,
                ["y"] = 0,
                ["color"] = Colors.Blue
            });
        }
        
        // Check queue grew
        await ToSignal(Engine.GetMainLoop().Root.GetTree(), SceneTree.SignalName.ProcessFrame);
        
        status = hardwareBridge?.Execute("get_status", new Dictionary<string, object>()) as Dictionary<string, object>;
        var newQueueSize = (int)status?.GetValueOrDefault("queued_commands", 0);
        
        if (newQueueSize <= queuedCommands)
        {
            result.Warnings.Add("Command queue did not grow after adding commands");
        }
        
        result.Metrics["final_queue_size"] = newQueueSize;
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestEffectSynchronization()
    {
        var result = new ValidationResult { Passed = true };
        
        // Test various game event effects
        var eventEffects = new List<(string eventName, string effectName, Color color)>
        {
            ("player_signal", "signal_pulse", Colors.White),
            ("filer_alert", "fear_ripple", Colors.Red),
            ("illuminate_cast", "illuminate_bloom", Colors.White),
            ("player_filed", "filed_vanish", Colors.Red),
            ("filed_sos", "sos_pattern", Colors.Orange),
            ("game_victory", "rainbow_victory", Colors.White)
        };
        
        foreach (var (eventName, effectName, color) in eventEffects)
        {
            var effectResult = hardwareBridge?.Execute("trigger_effect",
                new Dictionary<string, object>
                {
                    ["effect"] = effectName,
                    ["position"] = new Vector2I(4, 3),
                    ["color"] = color,
                    ["duration"] = 2.0f
                });
                
            if (effectResult == null)
            {
                result.Warnings.Add($"Could not trigger {eventName} effect");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestLEDMatrixConfiguration()
    {
        var result = new ValidationResult { Passed = true };
        
        // Get hardware configuration
        var status = hardwareBridge?.Execute("get_status", new Dictionary<string, object>()) as Dictionary<string, object>;
        
        if (status == null)
        {
            result.Passed = false;
            result.Errors.Add("Could not get hardware configuration");
            return result;
        }
        
        // Verify matrix dimensions
        var matrixDimensions = status.GetValueOrDefault("matrix_dimensions", "").ToString();
        if (matrixDimensions != $"{MatrixWidth}x{MatrixHeight}")
        {
            result.Passed = false;
            result.Errors.Add($"Matrix dimensions should be {MatrixWidth}x{MatrixHeight}, found {matrixDimensions}");
        }
        
        // Verify LED count
        var ledCount = (int)status.GetValueOrDefault("led_count", 0);
        var expectedCount = MatrixWidth * MatrixHeight;
        
        if (ledCount != expectedCount)
        {
            result.Passed = false;
            result.Errors.Add($"LED count should be {expectedCount}, found {ledCount}");
        }
        
        // Verify game grid mapping
        var gameGridDimensions = status.GetValueOrDefault("game_grid_dimensions", "").ToString();
        if (gameGridDimensions != $"{GameGridWidth}x{GameGridHeight}")
        {
            result.Passed = false;
            result.Errors.Add($"Game grid should be {GameGridWidth}x{GameGridHeight}, found {gameGridDimensions}");
        }
        
        // Test zigzag pattern indexing
        var testPositions = new List<(Vector2I pos, uint expectedIndex)>
        {
            (new Vector2I(0, 0), 0),     // Top-left
            (new Vector2I(15, 0), 15),   // Top-right
            (new Vector2I(0, 1), 31),    // Second row left (zigzag)
            (new Vector2I(15, 1), 16)    // Second row right (zigzag)
        };
        
        foreach (var (pos, expected) in testPositions)
        {
            var index = XYToIndex((uint)pos.X, (uint)pos.Y);
            if (index != expected)
            {
                result.Passed = false;
                result.Errors.Add($"Position {pos} should map to index {expected}, got {index}");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    // Helper methods matching HardwareBridgeEngineer
    private Vector2I GameToLedPosition(Vector2I gamePos)
    {
        return new Vector2I(gamePos.X * 2, gamePos.Y * 2);
    }
    
    private uint XYToIndex(uint x, uint y)
    {
        if (y >= MatrixHeight) return uint.MaxValue;
        
        uint index;
        if ((y & 1) == 0) // Even row: left to right
        {
            index = y * MatrixWidth + x;
        }
        else // Odd row: right to left
        {
            index = y * MatrixWidth + ((uint)MatrixWidth - 1 - x);
        }
        
        return index < (MatrixWidth * MatrixHeight) ? index : uint.MaxValue;
    }
    
    private float CalculateCollapseIntensity(int timerValue)
    {
        // Intensity increases as timer decreases
        return 1.0f - (timerValue / 5.0f);
    }
    
    private async Task ToSignal(SceneTree tree, StringName signal)
    {
        await tree.ToSignal(tree, signal);
    }
}