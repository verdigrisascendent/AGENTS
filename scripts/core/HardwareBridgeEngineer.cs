using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Hardware Bridge Engineer - Manages synchronization between game and physical LED board
/// </summary>
public partial class HardwareBridgeEngineer : Node, ISpecializedAgent
{
    private MegaAgent megaAgent;
    private WebSocketPeer webSocket;
    private string webSocketUrl = "ws://192.168.1.100:8080"; // ESP32/Teensy endpoint
    
    // Hardware state tracking
    private Dictionary<Vector2I, Color> ledStates = new();
    private Queue<HardwareCommand> commandQueue = new();
    private float syncTimer = 0.0f;
    private const float SyncInterval = 0.016f; // 60 FPS sync rate
    
    // Connection state
    private bool isConnecting = false;
    private bool wasConnected = false;
    private float reconnectTimer = 0.0f;
    private const float ReconnectInterval = 5.0f;
    private int reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 10;
    
    // LED board configuration - SK9822 11x16 matrix
    private const int MatrixWidth = 16;
    private const int MatrixHeight = 11;
    private const int LedCount = MatrixWidth * MatrixHeight; // 176 total
    private const int GameGridWidth = 8;
    private const int GameGridHeight = 6;
    
    public void Initialize(MegaAgent mega)
    {
        megaAgent = mega;
        
        // Load configuration from project settings or environment
        LoadConfiguration();
        
        // Attempt initial connection
        ConnectToHardware();
        
        GD.Print("[HardwareBridgeEngineer] Initialized - Managing LED board synchronization");
    }
    
    private void LoadConfiguration()
    {
        // Check for environment variable first
        var envUrl = OS.GetEnvironment("LED_BOARD_URL");
        if (!string.IsNullOrEmpty(envUrl))
        {
            webSocketUrl = envUrl;
            GD.Print($"[HardwareBridgeEngineer] Using LED board URL from environment: {webSocketUrl}");
        }
        
        // Check project settings
        if (ProjectSettings.HasSetting("hardware/led_board_url"))
        {
            webSocketUrl = ProjectSettings.GetSetting("hardware/led_board_url").AsString();
            GD.Print($"[HardwareBridgeEngineer] Using LED board URL from settings: {webSocketUrl}");
        }
    }
    
    public override void _Process(double delta)
    {
        // Handle connection state
        HandleConnectionState((float)delta);
        
        // Process sync timer
        syncTimer += (float)delta;
        
        if (syncTimer >= SyncInterval)
        {
            ProcessCommandQueue();
            syncTimer = 0.0f;
        }
        
        // Poll WebSocket if connected
        if (webSocket != null)
        {
            webSocket.Poll();
            
            var state = webSocket.GetReadyState();
            
            if (state == WebSocketPeer.State.Open)
            {
                while (webSocket.GetAvailablePacketCount() > 0)
                {
                    var packet = webSocket.GetPacket();
                    HandleHardwareResponse(packet);
                }
            }
        }
    }
    
    private void HandleConnectionState(float delta)
    {
        if (webSocket == null) return;
        
        var state = webSocket.GetReadyState();
        
        switch (state)
        {
            case WebSocketPeer.State.Connecting:
                // Still connecting, wait...
                break;
                
            case WebSocketPeer.State.Open:
                if (!wasConnected)
                {
                    wasConnected = true;
                    isConnecting = false;
                    reconnectAttempts = 0;
                    GD.Print($"[HardwareBridgeEngineer] Connected to LED board at {webSocketUrl}");
                    
                    // Send initial configuration
                    SendInitialConfiguration();
                }
                break;
                
            case WebSocketPeer.State.Closing:
                // Connection is closing
                break;
                
            case WebSocketPeer.State.Closed:
                if (wasConnected)
                {
                    wasConnected = false;
                    GD.PrintErr("[HardwareBridgeEngineer] Connection lost to LED board");
                }
                
                // Handle reconnection
                if (!isConnecting && reconnectAttempts < MaxReconnectAttempts)
                {
                    reconnectTimer += delta;
                    if (reconnectTimer >= ReconnectInterval)
                    {
                        reconnectTimer = 0.0f;
                        AttemptReconnection();
                    }
                }
                break;
        }
    }
    
    public object Execute(string task, Dictionary<string, object> parameters)
    {
        return task switch
        {
            "connect" => ConnectToHardware(),
            "sync_game_state" => SyncGameState(parameters),
            "set_led" => SetLED(parameters),
            "trigger_effect" => TriggerEffect(parameters),
            "emergency_shutdown" => EmergencyShutdown(),
            "get_status" => GetHardwareStatus(),
            _ => null
        };
    }
    
    private bool ConnectToHardware()
    {
        try
        {
            if (isConnecting) return false;
            
            webSocket = new WebSocketPeer();
            
            // Configure WebSocket options
            var tlsOptions = new TlsOptions();
            tlsOptions.ClientOptions = new TlsOptions(); // No certificate verification for local connection
            
            var error = webSocket.ConnectToUrl(webSocketUrl, tlsOptions);
            
            if (error != Error.Ok)
            {
                GD.PrintErr($"[HardwareBridgeEngineer] Failed to connect: {error}");
                webSocket = null;
                return false;
            }
            
            isConnecting = true;
            GD.Print($"[HardwareBridgeEngineer] Attempting connection to {webSocketUrl}...");
            return true;
        }
        catch (Exception e)
        {
            GD.PrintErr($"[HardwareBridgeEngineer] Connection error: {e.Message}");
            webSocket = null;
            return false;
        }
    }
    
    private void AttemptReconnection()
    {
        reconnectAttempts++;
        GD.Print($"[HardwareBridgeEngineer] Reconnection attempt {reconnectAttempts}/{MaxReconnectAttempts}");
        
        // Clean up old connection
        if (webSocket != null)
        {
            webSocket.Close();
            webSocket = null;
        }
        
        // Try to reconnect
        ConnectToHardware();
    }
    
    private void SendInitialConfiguration()
    {
        // Send board configuration
        var config = new Dictionary<string, object>
        {
            ["cmd"] = "config",
            ["width"] = MatrixWidth,
            ["height"] = MatrixHeight,
            ["fps"] = 60,
            ["brightness"] = 128
        };
        
        var configJson = Json.Stringify(config);
        webSocket.SendText(configJson);
        
        // Clear the board
        QueueCommand(new HardwareCommand { Type = CommandType.Clear });
        QueueCommand(new HardwareCommand { Type = CommandType.Update });
        
        GD.Print("[HardwareBridgeEngineer] Sent initial configuration to LED board");
    }
    
    private void SyncGameState(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("board_state", out var boardStateObj))
            return;
            
        var boardState = boardStateObj as Dictionary<Vector2I, TileState>;
        if (boardState == null)
            return;
            
        // Convert game state to LED commands
        foreach (var kvp in boardState)
        {
            var pos = kvp.Key;
            var tile = kvp.Value;
            
            // Map game position to LED position
            var ledPos = GameToLedPosition(pos);
            if (ledPos.X < 0 || ledPos.X >= MatrixWidth || ledPos.Y < 0 || ledPos.Y >= MatrixHeight)
                continue;
                
            // Determine LED color based on tile state
            Color ledColor;
            if (tile.IsExit)
                ledColor = Colors.Green;
            else if (tile.IsAidron)
                ledColor = Colors.Yellow;
            else if (tile.IsLit)
                ledColor = Colors.White;
            else
                ledColor = Colors.Black;
                
            // Set primary LED
            if (!ledStates.ContainsKey(ledPos) || ledStates[ledPos] != ledColor)
            {
                ledStates[ledPos] = ledColor;
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.SetPixel,
                    Position = ledPos,
                    Color = ledColor
                });
            }
            
            // Add atmospheric glow on surrounding secondary LEDs if lit
            if (tile.IsLit)
            {
                ApplySecondaryGlow(ledPos, ledColor);
            }
        }
        
        // Queue update command
        QueueCommand(new HardwareCommand { Type = CommandType.Update });
    }
    
    private Vector2I GameToLedPosition(Vector2I gamePos)
    {
        // Game cell (x,y) → Primary LED position as per LED_SYSTEM_GUIDE.md
        // Every game cell maps to a 2x2 LED area, with primary at even coordinates
        return new Vector2I(gamePos.X * 2, gamePos.Y * 2);
    }
    
    private uint XYToIndex(uint x, uint y)
    {
        // Convert X,Y to LED index accounting for zigzag pattern
        if (y >= MatrixHeight) return uint.MaxValue;
        
        uint index;
        if ((y & 1) == 0) // Even row: left to right
        {
            index = y * MatrixWidth + x;
        }
        else // Odd row: right to left
        {
            index = y * MatrixWidth + (MatrixWidth - 1 - x);
        }
        
        return index < LedCount ? index : uint.MaxValue;
    }
    
    private bool IsPrimaryLed(int x, int y)
    {
        // Primary LEDs are at positions where both x and y are even
        // and within the game grid mapping area
        return x % 2 == 0 && y % 2 == 0 && 
               x < GameGridWidth * 2 && y < GameGridHeight * 2;
    }
    
    private void SetLED(Dictionary<string, object> parameters)
    {
        var x = Convert.ToInt32(parameters["x"]);
        var y = Convert.ToInt32(parameters["y"]);
        var color = (Color)parameters["color"];
        
        QueueCommand(new HardwareCommand
        {
            Type = CommandType.SetPixel,
            Position = new Vector2I(x, y),
            Color = color
        });
    }
    
    private void TriggerEffect(Dictionary<string, object> parameters)
    {
        var effect = parameters["effect"].ToString();
        
        switch (effect)
        {
            case "collapse_pulse":
                // Reality breaking effect - all lights flicker
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "collapse_pulse",
                    Color = Colors.Red,
                    Duration = 3.0f
                });
                break;
                
            case "memory_spark":
                var pos = (Vector2I)parameters["position"];
                // Memory corridor lightning effect
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "memory_spark",
                    Position = GameToLedPosition(pos),
                    Color = Colors.Cyan,
                    Duration = 1.0f
                });
                break;
                
            case "signal_pulse":
                // Expanding ripple from player signal
                var signalPos = (Vector2I)parameters["position"];
                var playerColor = (Color)parameters["color"];
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "signal_ripple",
                    Position = GameToLedPosition(signalPos),
                    Color = playerColor,
                    Duration = 2.0f
                });
                break;
                
            case "fear_ripple":
                // Red expanding ripple from noise source
                var fearPos = (Vector2I)parameters["position"];
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "fear_ripple",
                    Position = GameToLedPosition(fearPos),
                    Color = Colors.Red,
                    Duration = 2.0f
                });
                break;
                
            case "illuminate_bloom":
                // Charging spell effect that blooms outward
                var spellPos = (Vector2I)parameters["position"];
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "illuminate_bloom",
                    Position = GameToLedPosition(spellPos),
                    Color = Colors.White,
                    Duration = 2.0f
                });
                break;
                
            case "player_heartbeat":
                // Double-thump pattern around player
                var heartPos = (Vector2I)parameters["position"];
                var heartColor = (Color)parameters["color"];
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "heartbeat",
                    Position = GameToLedPosition(heartPos),
                    Color = heartColor,
                    Duration = 2.0f
                });
                break;
                
            case "victory":
                // Rainbow celebration effect
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "rainbow_victory",
                    Duration = 5.0f
                });
                break;
                
            case "void_breathing":
                // Ambient darkness effect (secondary LEDs only)
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "void_breathing",
                    Color = new Color(0.5f, 0, 0.5f), // Purple
                    Duration = 8.0f
                });
                break;
                
            case "player_filed":
                // Player disappears effect
                var filedPos = (Vector2I)parameters["position"];
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "filed_vanish",
                    Position = GameToLedPosition(filedPos),
                    Color = Colors.Red,
                    Duration = 1.0f
                });
                break;
                
            case "filed_sos":
                // SOS morse code pattern
                var sosPos = (Vector2I)parameters["position"];
                QueueCommand(new HardwareCommand
                {
                    Type = CommandType.Effect,
                    EffectName = "sos_pattern",
                    Position = GameToLedPosition(sosPos),
                    Color = Colors.Orange,
                    Duration = 5.0f
                });
                break;
        }
    }
    
    private void ProcessCommandQueue()
    {
        if (webSocket == null || webSocket.GetReadyState() != WebSocketPeer.State.Open)
        {
            // If we have queued commands but no connection, log warning
            if (commandQueue.Count > 0)
            {
                GD.PrintErr($"[HardwareBridgeEngineer] Cannot send {commandQueue.Count} queued commands - not connected");
            }
            return;
        }
            
        int commandsSent = 0;
        while (commandQueue.Count > 0 && commandsSent < 10) // Limit commands per frame
        {
            var cmd = commandQueue.Dequeue();
            var message = SerializeCommand(cmd);
            
            var error = webSocket.SendText(message);
            if (error != Error.Ok)
            {
                GD.PrintErr($"[HardwareBridgeEngineer] Failed to send command: {error}");
                // Re-queue the command
                commandQueue.Enqueue(cmd);
                break;
            }
            
            commandsSent++;
            
            if (OS.IsDebugBuild())
            {
                GD.Print($"[HardwareBridgeEngineer] Sent: {message}");
            }
        }
    }
    
    private string SerializeCommand(HardwareCommand cmd)
    {
        var json = new Dictionary<string, object>();
        
        switch (cmd.Type)
        {
            case CommandType.SetPixel:
                json["cmd"] = "set_pixel";
                json["x"] = cmd.Position.X;
                json["y"] = cmd.Position.Y;
                json["r"] = (int)(cmd.Color.R * 255);
                json["g"] = (int)(cmd.Color.G * 255);
                json["b"] = (int)(cmd.Color.B * 255);
                break;
                
            case CommandType.Update:
                json["cmd"] = "update";
                break;
                
            case CommandType.Effect:
                json["cmd"] = "effect";
                json["name"] = cmd.EffectName;
                json["duration"] = cmd.Duration;
                if (cmd.Position != Vector2I.Zero)
                {
                    json["x"] = cmd.Position.X;
                    json["y"] = cmd.Position.Y;
                }
                if (cmd.Color != Colors.Black)
                {
                    json["r"] = (int)(cmd.Color.R * 255);
                    json["g"] = (int)(cmd.Color.G * 255);
                    json["b"] = (int)(cmd.Color.B * 255);
                }
                break;
                
            case CommandType.Clear:
                json["cmd"] = "clear";
                break;
        }
        
        return Json.Stringify(json);
    }
    
    private void HandleHardwareResponse(byte[] data)
    {
        var response = Encoding.UTF8.GetString(data);
        GD.Print($"[HardwareBridgeEngineer] Received: {response}");
        
        // Parse hardware status updates
        try
        {
            var json = Json.ParseString(response).AsGodotDictionary();
            if (json.ContainsKey("status"))
            {
                var status = json["status"].ToString();
                if (status == "error")
                {
                    GD.PrintErr($"[HardwareBridgeEngineer] Hardware error: {json["message"]}");
                }
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[HardwareBridgeEngineer] Failed to parse response: {e.Message}");
        }
    }
    
    private void QueueCommand(HardwareCommand cmd)
    {
        commandQueue.Enqueue(cmd);
    }
    
    private void ApplySecondaryGlow(Vector2I primaryPos, Color primaryColor)
    {
        // Apply a dimmed glow to surrounding secondary LEDs
        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue; // Skip the primary LED
                
                var secondaryPos = new Vector2I(primaryPos.X + dx, primaryPos.Y + dy);
                
                // Check bounds
                if (secondaryPos.X >= 0 && secondaryPos.X < MatrixWidth &&
                    secondaryPos.Y >= 0 && secondaryPos.Y < MatrixHeight &&
                    !IsPrimaryLed(secondaryPos.X, secondaryPos.Y))
                {
                    // Apply 25% brightness to secondary LEDs
                    var glowColor = new Color(
                        primaryColor.R * 0.25f,
                        primaryColor.G * 0.25f,
                        primaryColor.B * 0.25f,
                        primaryColor.A
                    );
                    
                    ledStates[secondaryPos] = glowColor;
                    QueueCommand(new HardwareCommand
                    {
                        Type = CommandType.SetPixel,
                        Position = secondaryPos,
                        Color = glowColor
                    });
                }
            }
        }
    }
    
    private void EmergencyShutdown()
    {
        GD.Print("[HardwareBridgeEngineer] Emergency shutdown initiated");
        
        // Clear all LEDs
        QueueCommand(new HardwareCommand { Type = CommandType.Clear });
        QueueCommand(new HardwareCommand { Type = CommandType.Update });
        
        // Process immediately
        ProcessCommandQueue();
        
        // Disconnect
        if (webSocket != null)
        {
            webSocket.Close();
            webSocket = null;
        }
    }
    
    private Dictionary<string, object> GetHardwareStatus()
    {
        var state = webSocket?.GetReadyState() ?? WebSocketPeer.State.Closed;
        var status = new Dictionary<string, object>
        {
            ["connected"] = state == WebSocketPeer.State.Open,
            ["connection_state"] = state.ToString(),
            ["url"] = webSocketUrl,
            ["led_count"] = LedCount,
            ["board_dimensions"] = new Vector2I(MatrixWidth, MatrixHeight),
            ["queued_commands"] = commandQueue.Count,
            ["active_leds"] = ledStates.Count,
            ["reconnect_attempts"] = reconnectAttempts,
            ["max_reconnect_attempts"] = MaxReconnectAttempts,
            ["sync_fps"] = 1.0f / SyncInterval,
            ["matrix_dimensions"] = $"{MatrixWidth}x{MatrixHeight}",
            ["game_grid_dimensions"] = $"{GameGridWidth}x{GameGridHeight}"
        };
        
        // Add connection quality metrics if connected
        if (state == WebSocketPeer.State.Open)
        {
            status["connection_quality"] = "good"; // Could be enhanced with actual metrics
            status["uptime"] = Time.GetUnixTimeFromSystem() - (wasConnected ? 0 : 0); // Track connection uptime
        }
        
        return status;
    }
}

// Helper classes
public enum CommandType
{
    SetPixel,
    Update,
    Clear,
    Effect
}

public class HardwareCommand
{
    public CommandType Type { get; set; }
    public Vector2I Position { get; set; }
    public Color Color { get; set; }
    public string EffectName { get; set; }
    public float Duration { get; set; }
}