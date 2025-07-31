using Godot;
using System.Collections.Generic;

/// <summary>
/// LED Test Screen - Debug interface for hardware bridge testing
/// </summary>
public partial class LEDTestScreen : Control
{
    // UI References
    private RichTextLabel statusLabel;
    private GridContainer gridContainer;
    private Button backButton;
    private Button connectButton;
    private Button clearButton;
    private Button testButton;
    private Timer updateTimer;
    
    // LED grid buttons
    private Dictionary<Vector2I, Button> ledButtons = new();
    
    // Hardware bridge
    private HardwareBridgeEngineer hardwareBridge;
    
    // Test patterns
    private int currentTestPattern = 0;
    private float testAnimationTime = 0.0f;
    
    public override void _Ready()
    {
        GetUIReferences();
        CreateLEDGrid();
        ConnectSignals();
        
        // Get hardware bridge
        hardwareBridge = GetNode<HardwareBridgeEngineer>("/root/GameInitializer/HardwareBridge");
        
        UpdateStatus();
        
        GD.Print("[LEDTestScreen] Ready - Hardware testing interface active");
    }
    
    private void GetUIReferences()
    {
        statusLabel = GetNode<RichTextLabel>("VBoxContainer/StatusPanel/StatusLabel");
        gridContainer = GetNode<GridContainer>("VBoxContainer/GridContainer");
        backButton = GetNode<Button>("VBoxContainer/ButtonContainer/BackButton");
        connectButton = GetNode<Button>("VBoxContainer/ButtonContainer/ConnectButton");
        clearButton = GetNode<Button>("VBoxContainer/ButtonContainer/ClearButton");
        testButton = GetNode<Button>("VBoxContainer/ButtonContainer/TestButton");
        updateTimer = GetNode<Timer>("UpdateTimer");
    }
    
    private void CreateLEDGrid()
    {
        // Create 13x13 grid of LED buttons
        for (int y = 0; y < 13; y++)
        {
            for (int x = 0; x < 13; x++)
            {
                var button = new Button();
                button.CustomMinimumSize = new Vector2(32, 32);
                button.Text = "";
                
                var pos = new Vector2I(x, y);
                button.Pressed += () => OnLEDPressed(pos);
                
                // Style as LED
                var style = new StyleBoxFlat();
                style.BgColor = Colors.Black;
                style.SetBorderWidthAll(1);
                style.BorderColor = new Color("#444444");
                style.SetCornerRadiusAll(4);
                button.AddThemeStyleboxOverride("normal", style);
                
                gridContainer.AddChild(button);
                ledButtons[pos] = button;
            }
        }
    }
    
    private void ConnectSignals()
    {
        backButton.Pressed += OnBackPressed;
        connectButton.Pressed += OnConnectPressed;
        clearButton.Pressed += OnClearPressed;
        testButton.Pressed += OnTestPressed;
        updateTimer.Timeout += UpdateStatus;
    }
    
    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/screens/main_menu.tscn");
    }
    
    private void OnConnectPressed()
    {
        GD.Print("[LEDTestScreen] Manual connection attempt");
        var connectParams = new Godot.Collections.Dictionary<string, object>();
        hardwareBridge.Execute("connect", connectParams);
    }
    
    private void OnClearPressed()
    {
        // Clear all LEDs
        foreach (var kvp in ledButtons)
        {
            SetLEDColor(kvp.Key, Colors.Black);
        }
        
        // Send clear command to hardware
        var clearParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "clear"
        };
        hardwareBridge.Execute("trigger_effect", clearParams);
    }
    
    private void OnTestPressed()
    {
        currentTestPattern = (currentTestPattern + 1) % 4;
        RunTestPattern();
    }
    
    private void OnLEDPressed(Vector2I pos)
    {
        // Toggle LED on/off
        var button = ledButtons[pos];
        var style = button.GetThemeStylebox("normal") as StyleBoxFlat;
        
        var newColor = style.BgColor == Colors.Black ? Colors.White : Colors.Black;
        SetLEDColor(pos, newColor);
        
        // Send to hardware
        var ledParams = new Godot.Collections.Dictionary<string, object>
        {
            ["x"] = pos.X,
            ["y"] = pos.Y,
            ["color"] = newColor
        };
        hardwareBridge.Execute("set_led", ledParams);
    }
    
    private void SetLEDColor(Vector2I pos, Color color)
    {
        if (!ledButtons.ContainsKey(pos)) return;
        
        var button = ledButtons[pos];
        var style = button.GetThemeStylebox("normal").Duplicate() as StyleBoxFlat;
        style.BgColor = color;
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
    }
    
    private void UpdateStatus()
    {
        // Get hardware status
        var statusParams = new Godot.Collections.Dictionary<string, object>();
        var status = hardwareBridge.Execute("get_status", statusParams) as Dictionary<string, object>;
        
        if (status == null)
        {
            statusLabel.Text = "[color=red]ERROR: Could not get hardware status[/color]";
            return;
        }
        
        // Format status display
        var connected = (bool)status["connected"];
        var connectionState = status["connection_state"].ToString();
        var url = status["url"].ToString();
        var queuedCommands = status["queued_commands"];
        var activeLeds = status["active_leds"];
        var reconnectAttempts = status["reconnect_attempts"];
        var maxReconnects = status["max_reconnect_attempts"];
        
        var statusText = "[color=yellow]CONNECTION STATUS:[/color]\n";
        
        if (connected)
        {
            statusText += $"[color=green]● CONNECTED[/color]\n";
        }
        else
        {
            statusText += $"[color=red]● DISCONNECTED[/color] ({connectionState})\n";
        }
        
        statusText += $"URL: {url}\n";
        statusText += $"Queued Commands: {queuedCommands}\n";
        statusText += $"Active LEDs: {activeLeds}\n";
        
        if (!connected && (int)reconnectAttempts > 0)
        {
            statusText += $"\n[color=yellow]Reconnecting... ({reconnectAttempts}/{maxReconnects})[/color]";
        }
        
        statusLabel.Text = statusText;
        
        // Update button states
        connectButton.Disabled = connected;
        clearButton.Disabled = !connected;
        testButton.Disabled = !connected;
    }
    
    private void RunTestPattern()
    {
        switch (currentTestPattern)
        {
            case 0: // All white
                testButton.Text = "TEST: WAVE";
                for (int y = 0; y < 13; y++)
                {
                    for (int x = 0; x < 13; x++)
                    {
                        var pos = new Vector2I(x, y);
                        SetLEDColor(pos, Colors.White);
                        
                        var ledParams = new Godot.Collections.Dictionary<string, object>
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["color"] = Colors.White
                        };
                        hardwareBridge.Execute("set_led", ledParams);
                    }
                }
                break;
                
            case 1: // Wave pattern
                testButton.Text = "TEST: RAINBOW";
                StartWaveAnimation();
                break;
                
            case 2: // Rainbow
                testButton.Text = "TEST: CHESS";
                for (int y = 0; y < 13; y++)
                {
                    for (int x = 0; x < 13; x++)
                    {
                        var hue = (x + y) / 26.0f;
                        var color = Color.FromHsv(hue, 1.0f, 1.0f);
                        var pos = new Vector2I(x, y);
                        SetLEDColor(pos, color);
                        
                        var ledParams = new Godot.Collections.Dictionary<string, object>
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["color"] = color
                        };
                        hardwareBridge.Execute("set_led", ledParams);
                    }
                }
                break;
                
            case 3: // Checkerboard
                testButton.Text = "TEST: ALL";
                for (int y = 0; y < 13; y++)
                {
                    for (int x = 0; x < 13; x++)
                    {
                        var color = (x + y) % 2 == 0 ? Colors.White : Colors.Black;
                        var pos = new Vector2I(x, y);
                        SetLEDColor(pos, color);
                        
                        var ledParams = new Godot.Collections.Dictionary<string, object>
                        {
                            ["x"] = x,
                            ["y"] = y,
                            ["color"] = color
                        };
                        hardwareBridge.Execute("set_led", ledParams);
                    }
                }
                break;
        }
    }
    
    private async void StartWaveAnimation()
    {
        for (int wave = 0; wave < 13; wave++)
        {
            for (int y = 0; y < 13; y++)
            {
                for (int x = 0; x < 13; x++)
                {
                    var distance = Mathf.Abs(x - wave);
                    var brightness = Mathf.Max(0, 1 - distance * 0.3f);
                    var color = new Color(brightness, brightness, brightness);
                    
                    var pos = new Vector2I(x, y);
                    SetLEDColor(pos, color);
                    
                    var ledParams = new Godot.Collections.Dictionary<string, object>
                    {
                        ["x"] = x,
                        ["y"] = y,
                        ["color"] = color
                    };
                    hardwareBridge.Execute("set_led", ledParams);
                }
            }
            
            await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        }
    }
}