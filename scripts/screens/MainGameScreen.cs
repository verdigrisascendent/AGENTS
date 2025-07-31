using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Main Game Screen - Core gameplay with grid, HUD, and game state management
/// </summary>
public partial class MainGameScreen : Control
{
    // UI References
    private GameGrid gameGrid;
    private Label roundLabel;
    private Label turnLabel;
    private VBoxContainer playerList;
    private VBoxContainer actionButtons;
    private RichTextLabel messageLog;
    private ProgressBar noiseBar;
    private Label aidronStatus;
    private Label exitStatus;
    private Panel collapsePanel;
    private Label collapseLabel;
    
    // Action buttons
    private Button moveButton;
    private Button signalButton;
    private Button illuminateButton;
    private Button memoryTokenButton;
    private Button endTurnButton;
    
    // Game state
    private int currentRound = 1;
    private int noiseLevel = 0;
    private bool collapseMode = false;
    private int collapseRoundsRemaining = 0;
    private List<PlayerInfo> players = new();
    private List<Vector2I> filerPositions = new();
    private int currentPlayerIndex = 0;
    private string selectedAction = "";
    private bool aidronFound = false;
    private Vector2I? aidronPosition = null;
    private Vector2I? exitPosition = null;
    private bool exitReached = false;
    private int totalTokensUsed = 0;
    private HashSet<Vector2I> exploredCells = new();
    
    // Action economy tracking
    private bool hasUsedIlluminate = false;
    private bool hasUsedOtherAction = false;
    private int movesRemaining = 1;
    
    // Agents
    private GameStateGuardian gameStateGuardian;
    private HardwareBridgeEngineer hardwareBridge;
    private FilerAI filerAI;
    
    // Game systems
    private CollapseManager collapseManager;
    private MemorySparkSystem memorySparkSystem;
    private RulesVerifierAgent rulesVerifier;
    private FilingSystem filingSystem;
    private RandomNumberGenerator rng = new();
    
    public override void _Ready()
    {
        GetUIReferences();
        InitializeAgents();
        LoadGameConfiguration();
        SetupGame();
        ConnectSignals();
        
        StartGame();
    }
    
    public override void _Input(InputEvent @event)
    {
        // Handle pause
        if (@event.IsActionPressed("ui_cancel"))
        {
            ShowPauseMenu();
            GetViewport().SetInputAsHandled();
        }
    }
    
    private void GetUIReferences()
    {
        // Get grid
        gameGrid = GetNode<GameGrid>("GameContainer/CenterContainer/GridViewport/SubViewport/GameGrid");
        
        // Left panel
        roundLabel = GetNode<Label>("GameContainer/LeftPanel/VBoxContainer/TurnInfo/RoundLabel");
        turnLabel = GetNode<Label>("GameContainer/LeftPanel/VBoxContainer/TurnInfo/TurnLabel");
        playerList = GetNode<VBoxContainer>("GameContainer/LeftPanel/VBoxContainer/PlayerList");
        actionButtons = GetNode<VBoxContainer>("GameContainer/LeftPanel/VBoxContainer/ActionButtons");
        noiseBar = GetNode<ProgressBar>("GameContainer/LeftPanel/VBoxContainer/NoiseLevel/NoiseBar");
        
        // Action buttons
        moveButton = GetNode<Button>("GameContainer/LeftPanel/VBoxContainer/ActionButtons/MoveButton");
        signalButton = GetNode<Button>("GameContainer/LeftPanel/VBoxContainer/ActionButtons/SignalButton");
        illuminateButton = GetNode<Button>("GameContainer/LeftPanel/VBoxContainer/ActionButtons/IlluminateButton");
        memoryTokenButton = GetNodeOrNull<Button>("GameContainer/LeftPanel/VBoxContainer/ActionButtons/MemoryTokenButton");
        endTurnButton = GetNode<Button>("GameContainer/LeftPanel/VBoxContainer/ActionButtons/EndTurnButton");
        
        // Right panel
        messageLog = GetNode<RichTextLabel>("GameContainer/RightPanel/VBoxContainer/MessageScroll/MessageLog");
        aidronStatus = GetNode<Label>("GameContainer/RightPanel/VBoxContainer/SpecialInfo/AidronStatus");
        exitStatus = GetNode<Label>("GameContainer/RightPanel/VBoxContainer/SpecialInfo/ExitStatus");
        
        // Collapse panel
        collapsePanel = GetNode<Panel>("CollapsePanel");
        collapseLabel = GetNode<Label>("CollapsePanel/CollapseLabel");
    }
    
    private void InitializeAgents()
    {
        var megaAgent = GetNode<MegaAgent>("/root/GameInitializer/MegaAgent");
        
        gameStateGuardian = new GameStateGuardian();
        gameStateGuardian.Initialize(megaAgent);
        
        hardwareBridge = GetNode<HardwareBridgeEngineer>("/root/GameInitializer/HardwareBridge");
        
        filerAI = new FilerAI();
        filerAI.Initialize(gameStateGuardian);
        
        // Initialize game systems
        collapseManager = new CollapseManager();
        AddChild(collapseManager);
        collapseManager.CollapseStarted += OnCollapseStarted;
        collapseManager.CollapseEnded += OnCollapseEnded;
        collapseManager.CollapseEventTriggered += OnCollapseEventTriggered;
        
        memorySparkSystem = new MemorySparkSystem();
        AddChild(memorySparkSystem);
        memorySparkSystem.SetGameGrid(gameGrid);
        
        filingSystem = new FilingSystem();
        AddChild(filingSystem);
        filingSystem.PlayerFiled += OnPlayerFiled;
        filingSystem.PlayerUnfiled += OnPlayerUnfiled;
        
        rulesVerifier = new RulesVerifierAgent();
        rulesVerifier.Initialize(megaAgent);
    }
    
    private void LoadGameConfiguration()
    {
        // Load from game initializer
        var gameInit = GetNode("/root/GameInitializer");
        var config = gameInit.Get("game_config").AsGodotDictionary();
        
        if (config != null)
        {
            var playerArray = config["players"].AsGodotArray();
            foreach (var playerData in playerArray)
            {
                var player = playerData.AsGodotDictionary();
                players.Add(new PlayerInfo
                {
                    Name = player["name"].AsString(),
                    Color = player["color"].AsColor(),
                    IsAI = player["is_ai"].AsBool(),
                    Position = player["position"].AsVector2I(),
                    Tokens = player["tokens"].AsInt32()
                });
            }
        }
        else
        {
            // Default test configuration
            players.Add(new PlayerInfo { Name = "Elmer", Color = Colors.Red, Position = new Vector2I(0, 0), Tokens = 2 });
            players.Add(new PlayerInfo { Name = "Toplop", Color = Colors.Green, Position = new Vector2I(7, 5), Tokens = 2 });
        }
    }
    
    private void SetupGame()
    {
        // Initialize player list UI
        foreach (var player in players)
        {
            var playerPanel = new HBoxContainer();
            playerPanel.AddThemeConstantOverride("separation", 8);
            
            var colorRect = new ColorRect();
            colorRect.CustomMinimumSize = new Vector2(16, 16);
            colorRect.Color = player.Color;
            
            var nameLabel = new Label();
            nameLabel.Text = player.Name.ToUpper();
            nameLabel.AddThemeColorOverride("font_color", Colors.White);
            
            var tokenLabel = new Label();
            tokenLabel.Text = $"Tokens: {player.Tokens}";
            tokenLabel.AddThemeColorOverride("font_color", new Color("#AAAAAA"));
            tokenLabel.Name = player.Name + "Tokens";
            
            playerPanel.AddChild(colorRect);
            playerPanel.AddChild(nameLabel);
            playerPanel.AddChild(tokenLabel);
            
            playerList.AddChild(playerPanel);
        }
        
        // Place special locations randomly (for now)
        var random = new RandomNumberGenerator();
        random.Randomize();
        
        var aidronPos = new Vector2I(random.RandiRange(0, 7), random.RandiRange(0, 5));
        var exitPos = new Vector2I(random.RandiRange(0, 7), random.RandiRange(0, 5));
        
        // Ensure they're not on the same spot
        while (exitPos == aidronPos)
        {
            exitPos = new Vector2I(random.RandiRange(0, 7), random.RandiRange(0, 5));
        }
        
        gameGrid.SetSpecialLocation("Aidron", aidronPos);
        gameGrid.SetSpecialLocation("Exit", exitPos);
        
        // Place filers
        filerPositions = new List<Vector2I>
        {
            new Vector2I(3, 2),
            new Vector2I(5, 3)
        };
        gameGrid.SetFilerPositions(filerPositions);
    }
    
    private void ConnectSignals()
    {
        // Grid signals
        gameGrid.CellClicked += OnCellClicked;
        gameGrid.SpecialLocationFound += OnSpecialLocationFound;
        
        // Action buttons
        moveButton.Pressed += () => SelectAction("MOVE");
        signalButton.Pressed += () => SelectAction("SIGNAL");
        illuminateButton.Pressed += () => SelectAction("ILLUMINATE");
        if (memoryTokenButton != null)
            memoryTokenButton.Pressed += () => SelectAction("MEMORY_TOKEN");
        endTurnButton.Pressed += OnEndTurnPressed;
    }
    
    private void StartGame()
    {
        // Initialize player positions
        foreach (var player in players)
        {
            gameGrid.SetPlayerPosition(player.Name, player.Position);
        }
        
        // Light starting positions
        foreach (var player in players)
        {
            gameGrid.SetCellLight(player.Position, true);
        }
        
        // Start ambient void breathing effect
        var voidParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "void_breathing"
        };
        hardwareBridge.Execute("trigger_effect", voidParams);
        
        // Start first turn
        StartPlayerTurn();
        
        AddMessage("Game started!", MessageType.System);
        AddMessage("The vault is dark and dangerous...", MessageType.System);
    }
    
    private void StartPlayerTurn()
    {
        var currentPlayer = players[currentPlayerIndex];
        
        roundLabel.Text = $"ROUND {currentRound}";
        turnLabel.Text = $"{currentPlayer.Name.ToUpper()}'S TURN";
        turnLabel.AddThemeColorOverride("font_color", currentPlayer.Color);
        
        gameGrid.SetCurrentPlayer(currentPlayer.Name);
        
        // Reset action economy
        hasUsedIlluminate = false;
        hasUsedOtherAction = false;
        movesRemaining = collapseMode ? 2 : 1;  // 2 moves during collapse
        selectedAction = "";
        UpdateActionButtons();
        
        AddMessage($"{currentPlayer.Name}'s turn begins.", MessageType.System);
        if (collapseMode)
        {
            AddMessage($"Collapse mode: {movesRemaining} moves available!", MessageType.Warning);
        }
        
        // Handle AI players
        if (currentPlayer.IsAI)
        {
            ProcessAITurn();
        }
    }
    
    private void SelectAction(string action)
    {
        selectedAction = action;
        UpdateActionButtons();
        
        AddMessage($"Selected {action}. Click a cell to perform action.", MessageType.Action);
        
        // For SIGNAL, execute immediately (no target needed)
        if (action == "SIGNAL")
        {
            ExecuteSignal();
        }
    }
    
    private void UpdateActionButtons()
    {
        var currentPlayer = players[currentPlayerIndex];
        
        // Update button states based on action economy
        moveButton.Disabled = selectedAction == "MOVE" || movesRemaining <= 0;
        signalButton.Disabled = selectedAction == "SIGNAL" || hasUsedOtherAction;
        illuminateButton.Disabled = selectedAction == "ILLUMINATE" || currentPlayer.Tokens <= 0 || hasUsedIlluminate;
        if (memoryTokenButton != null)
            memoryTokenButton.Disabled = selectedAction == "MEMORY_TOKEN" || currentPlayer.Tokens <= 0 || hasUsedOtherAction || collapseMode;
        
        // Update button text to show status
        moveButton.Text = $"MOVE ({movesRemaining} left)";
        illuminateButton.Text = $"ILLUMINATE ({currentPlayer.Tokens})";
        signalButton.Text = hasUsedOtherAction ? "SIGNAL (Used)" : "SIGNAL";
        if (memoryTokenButton != null)
            memoryTokenButton.Text = $"MEMORY TOKEN ({currentPlayer.Tokens})";
        
        // Enable end turn if any action has been taken
        endTurnButton.Disabled = !hasUsedIlluminate && !hasUsedOtherAction && movesRemaining == (collapseMode ? 2 : 1);
    }
    
    private void OnCellClicked(Vector2I position)
    {
        if (string.IsNullOrEmpty(selectedAction))
        {
            AddMessage("Select an action first!", MessageType.Warning);
            return;
        }
        
        var currentPlayer = players[currentPlayerIndex];
        
        switch (selectedAction)
        {
            case "MOVE":
                ExecuteMove(position);
                break;
            case "ILLUMINATE":
                ExecuteIlluminate(position);
                break;
            case "MEMORY_TOKEN":
                ExecuteMemoryToken(position);
                break;
        }
    }
    
    private void ExecuteMove(Vector2I targetPos)
    {
        var currentPlayer = players[currentPlayerIndex];
        
        // Validate move with game state guardian
        var moveParams = new Godot.Collections.Dictionary<string, object>
        {
            ["player_id"] = currentPlayer.Name,
            ["action"] = "MOVE",
            ["target_position"] = targetPos
        };
        
        var validation = gameStateGuardian.Execute("validate_action", moveParams) as ValidationResult;
        
        if (validation != null && validation.IsValid)
        {
            // Apply the move
            gameGrid.SetPlayerPosition(currentPlayer.Name, targetPos);
            currentPlayer.Position = targetPos;
            
            // Track explored cells
            exploredCells.Add(targetPos);
            
            AddMessage($"{currentPlayer.Name} moved to {(char)('A' + targetPos.X)}{targetPos.Y + 1}", MessageType.Action);
            
            // During collapse, check for spark chance
            if (collapseMode && rng.Randf() < 0.75f)
            {
                memorySparkSystem.CreateLightSpark(targetPos);
                AddMessage("A spark of light appears!", MessageType.Success);
            }
            
            // Trigger player heartbeat LED effect
            var heartbeatParams = new Godot.Collections.Dictionary<string, object>
            {
                ["effect"] = "player_heartbeat",
                ["position"] = targetPos,
                ["color"] = currentPlayer.Color
            };
            hardwareBridge.Execute("trigger_effect", heartbeatParams);
            
            // Update action economy
            movesRemaining--;
            selectedAction = "";
            UpdateActionButtons();
        }
        else
        {
            AddMessage(validation?.Message ?? "Invalid move!", MessageType.Error);
        }
    }
    
    private void ExecuteSignal()
    {
        var currentPlayer = players[currentPlayerIndex];
        
        // Increase noise
        noiseLevel += 2;
        UpdateNoiseLevel();
        
        AddMessage($"{currentPlayer.Name} signals! (+2 Noise)", MessageType.Action);
        
        // Light up adjacent cells
        var playerPos = currentPlayer.Position;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var checkPos = playerPos + new Vector2I(dx, dy);
                if (checkPos.X >= 0 && checkPos.X < 8 && checkPos.Y >= 0 && checkPos.Y < 6)
                {
                    gameGrid.SetCellLight(checkPos, true);
                }
            }
        }
        
        // Trigger LED effect
        var effectParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "signal_pulse",
            ["position"] = playerPos,
            ["color"] = currentPlayer.Color
        };
        hardwareBridge.Execute("trigger_effect", effectParams);
        
        // Update action economy
        hasUsedOtherAction = true;
        selectedAction = "";
        UpdateActionButtons();
    }
    
    private void ExecuteIlluminate(Vector2I targetPos)
    {
        var currentPlayer = players[currentPlayerIndex];
        
        if (currentPlayer.Tokens <= 0)
        {
            AddMessage("No tokens available!", MessageType.Error);
            return;
        }
        
        // Validate illuminate
        var illuminateParams = new Godot.Collections.Dictionary<string, object>
        {
            ["player_id"] = currentPlayer.Name,
            ["action"] = "ILLUMINATE",
            ["target_position"] = targetPos
        };
        
        var validation = gameStateGuardian.Execute("validate_action", illuminateParams) as ValidationResult;
        
        if (validation != null && validation.IsValid)
        {
            // Spend token
            currentPlayer.Tokens--;
            totalTokensUsed++;
            UpdatePlayerTokenDisplay(currentPlayer.Name, currentPlayer.Tokens);
            
            // Light the cell
            gameGrid.SetCellLight(targetPos, true);
            
            AddMessage($"{currentPlayer.Name} illuminates {(char)('A' + targetPos.X)}{targetPos.Y + 1}", MessageType.Action);
            
            // Trigger illuminate bloom LED effect
            var bloomParams = new Godot.Collections.Dictionary<string, object>
            {
                ["effect"] = "illuminate_bloom",
                ["position"] = targetPos
            };
            hardwareBridge.Execute("trigger_effect", bloomParams);
            
            // Update action economy
            hasUsedIlluminate = true;
            selectedAction = "";
            UpdateActionButtons();
        }
        else
        {
            AddMessage(validation?.Message ?? "Cannot illuminate there!", MessageType.Error);
        }
    }
    
    private void ExecuteMemoryToken(Vector2I targetPos)
    {
        var currentPlayer = players[currentPlayerIndex];
        
        if (currentPlayer.Tokens <= 0)
        {
            AddMessage("No tokens available!", MessageType.Error);
            return;
        }
        
        if (collapseMode)
        {
            AddMessage("Cannot use Memory Tokens during collapse!", MessageType.Error);
            return;
        }
        
        // Spend token to create permanent memory spark
        currentPlayer.Tokens--;
        totalTokensUsed++;
        UpdatePlayerTokenDisplay(currentPlayer.Name, currentPlayer.Tokens);
        
        // Create permanent memory spark
        memorySparkSystem.CreateMemorySparkFromToken(targetPos, currentPlayer.Name, collapseMode);
        
        AddMessage($"{currentPlayer.Name} creates a Memory Spark at {(char)('A' + targetPos.X)}{targetPos.Y + 1}", MessageType.Success);
        
        // Trigger memory spark animation
        var memoryParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "memory_spark_creation",
            ["position"] = targetPos
        };
        hardwareBridge.Execute("trigger_effect", memoryParams);
        
        // Update action economy
        hasUsedOtherAction = true;
        selectedAction = "";
        UpdateActionButtons();
    }
    
    private void OnEndTurnPressed()
    {
        EndPlayerTurn();
    }
    
    private void EndPlayerTurn()
    {
        // Move to next player
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        
        // Check if round is complete
        if (currentPlayerIndex == 0)
        {
            AdvanceRound();
        }
        else
        {
            StartPlayerTurn();
        }
    }
    
    private void AdvanceRound()
    {
        currentRound++;
        
        // Filer phase
        ProcessFilerPhase();
        
        // Process memory sparks
        memorySparkSystem.ProcessRound();
        
        // Check collapse
        if (collapseMode)
        {
            collapseManager.ProcessRound();
            collapseRoundsRemaining = collapseManager.RoundsRemaining;
            
            if (collapseRoundsRemaining <= 0)
            {
                GameOver("The vault has collapsed!");
                return;
            }
            
            UpdateCollapseDisplay();
        }
        
        StartPlayerTurn();
    }
    
    private void ProcessFilerPhase()
    {
        AddMessage("Filers prowl in the darkness...", MessageType.Danger);
        
        // Build filer state
        var filerState = new FilerState
        {
            FilerPositions = filerPositions,
            NoiseLevel = noiseLevel,
            Round = currentRound
        };
        
        // Build player positions
        foreach (var player in players)
        {
            filerState.PlayerPositions[player.Name] = player.Position;
        }
        
        // Build lit cells
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 6; y++)
            {
                var pos = new Vector2I(x, y);
                // Check if cell is lit (would need to track this properly)
                // For now, assume cells near players are lit
                foreach (var player in players)
                {
                    if (player.Position.DistanceTo(pos) <= 1)
                    {
                        filerState.LitCells.Add(pos);
                    }
                }
            }
        }
        
        // Get filer moves
        var filerMoves = filerAI.ProcessFilerTurn(filerState);
        
        // Apply moves
        var newFilerPositions = new List<Vector2I>(filerPositions);
        
        foreach (var move in filerMoves)
        {
            switch (move.Action)
            {
                case FilerAction.Move:
                    if (move.FilerId < newFilerPositions.Count)
                    {
                        newFilerPositions[move.FilerId] = move.ToPosition;
                        AddMessage($"Filer moves to {(char)('A' + move.ToPosition.X)}{move.ToPosition.Y + 1}", MessageType.Danger);
                    }
                    break;
                    
                case FilerAction.File:
                    AddMessage($"Filer attempts to file {move.TargetPlayer}!", MessageType.Danger);
                    // Handle filing logic
                    FilePlayer(move.TargetPlayer);
                    break;
                    
                case FilerAction.Spawn:
                    newFilerPositions.Add(move.ToPosition);
                    AddMessage("A third Filer emerges from the darkness!", MessageType.Danger);
                    break;
            }
        }
        
        // Update grid
        filerPositions = newFilerPositions;
        gameGrid.SetFilerPositions(filerPositions);
    }
    
    private void FilePlayer(string playerName)
    {
        var player = players.FirstOrDefault(p => p.Name == playerName);
        if (player != null)
        {
            // Check if player is in darkness
            var isInDarkness = !gameGrid.IsCellLit(player.Position);
            
            // Get filer position (assumed to be adjacent)
            Vector2I filerPos = player.Position; // This should come from filer AI
            foreach (var fPos in filerPositions)
            {
                var diff = player.Position - fPos;
                if (Mathf.Abs(diff.X) <= 1 && Mathf.Abs(diff.Y) <= 1)
                {
                    filerPos = fPos;
                    break;
                }
            }
            
            // Attempt filing
            if (filingSystem.AttemptFiling(playerName, player.Position, filerPos, isInDarkness))
            {
                players.Remove(player);
                gameGrid.RemovePlayer(playerName);
                
                // Check for game over
                if (players.Count == 0)
                {
                    GameOver("All players have been filed!");
                }
            }
        }
    }
    
    private void OnPlayerFiled(string playerName, Vector2I position)
    {
        AddMessage($"{playerName} has been FILED!", MessageType.Danger);
        
        // Trigger SOS pattern periodically
        var timer = GetTree().CreateTimer(5.0);
        timer.Timeout += () => {
            if (filingSystem.IsPlayerFiled(playerName))
            {
                filingSystem.TriggerFiledSOS(playerName);
            }
        };
    }
    
    private void OnPlayerUnfiled(string playerName)
    {
        AddMessage($"{playerName} has been UNFILED and returns!", MessageType.Success);
    }
    
    private void ProcessAITurn()
    {
        // Simple AI for now - just end turn
        var timer = GetTree().CreateTimer(1.0);
        timer.Timeout += EndPlayerTurn;
    }
    
    private void UpdateNoiseLevel()
    {
        var previousLevel = (int)noiseBar.Value;
        noiseBar.Value = noiseLevel;
        
        // Update bar color based on level
        var barStyle = new StyleBoxFlat();
        barStyle.BgColor = noiseLevel switch
        {
            <= 4 => Colors.Green,
            <= 7 => Colors.Yellow,
            <= 12 => Colors.Orange,
            _ => Colors.Red
        };
        
        noiseBar.AddThemeStyleboxOverride("fill", barStyle);
        
        // Trigger fear ripple if noise increased past a threshold
        if (noiseLevel > previousLevel && (noiseLevel == 5 || noiseLevel == 8 || noiseLevel == 13))
        {
            // Get center of all player positions for ripple origin
            if (players.Count > 0)
            {
                var centerX = players.Average(p => p.Position.X);
                var centerY = players.Average(p => p.Position.Y);
                var fearParams = new Godot.Collections.Dictionary<string, object>
                {
                    ["effect"] = "fear_ripple",
                    ["position"] = new Vector2I((int)centerX, (int)centerY)
                };
                hardwareBridge.Execute("trigger_effect", fearParams);
            }
        }
    }
    
    private void UpdatePlayerTokenDisplay(string playerName, int tokens)
    {
        var tokenLabel = playerList.GetNode<Label>(playerName + "Tokens");
        if (tokenLabel != null)
        {
            tokenLabel.Text = $"Tokens: {tokens}";
        }
    }
    
    private void OnSpecialLocationFound(string type, Vector2I position)
    {
        switch (type)
        {
            case "Aidron":
                aidronFound = true;
                aidronPosition = position;
                aidronStatus.Text = $"AIDRON: {(char)('A' + position.X)}{position.Y + 1}";
                aidronStatus.AddThemeColorOverride("font_color", Colors.Green);
                
                if (collapseMode)
                {
                    // Emergency Protocol: Instant activation during collapse
                    AddMessage("AIDRON EMERGENCY PROTOCOL ACTIVATED!", MessageType.Success);
                    ActivateAidronProtocol(position);
                }
                else
                {
                    AddMessage("Aidron discovered! Will activate if collapse begins!", MessageType.Success);
                    gameGrid.SetCellLight(position, true, true);
                }
                break;
                
            case "Exit":
                exitPosition = position;
                exitStatus.Text = $"EXIT: {(char)('A' + position.X)}{position.Y + 1}";
                exitStatus.AddThemeColorOverride("font_color", Colors.Magenta);
                AddMessage("Exit found! Escape is possible!", MessageType.Success);
                
                // Check if player can win
                if (aidronFound)
                {
                    CheckVictoryCondition(position);
                }
                break;
        }
    }
    
    private void TriggerCollapse()
    {
        collapseMode = true;
        collapseManager.StartCollapse();
        collapseRoundsRemaining = collapseManager.RoundsRemaining;
        
        collapsePanel.Visible = true;
        UpdateCollapseDisplay();
        
        // Check if Aidron was already found
        if (aidronFound && aidronPosition.HasValue)
        {
            AddMessage("AIDRON EMERGENCY PROTOCOL ACTIVATING!", MessageType.Success);
            ActivateAidronProtocol(aidronPosition.Value);
        }
        
        // Trigger hardware effect
        var collapseParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "collapse_pulse"
        };
        hardwareBridge.Execute("trigger_effect", collapseParams);
        
        AddMessage("THE VAULT IS COLLAPSING! 3 ROUNDS TO ESCAPE!", MessageType.Collapse);
    }
    
    private void OnCollapseStarted(int rounds)
    {
        collapseMode = true;
        collapseRoundsRemaining = rounds;
        UpdateCollapseDisplay();
    }
    
    private void OnCollapseEnded()
    {
        GameOver("The vault has collapsed!");
    }
    
    private void UpdateCollapseDisplay()
    {
        collapseLabel.Text = $"VAULT COLLAPSE! {collapseRoundsRemaining} ROUNDS REMAINING!";
    }
    
    private void OnCollapseEventTriggered(CollapseEventResult result)
    {
        AddMessage(result.Message, MessageType.Collapse);
        
        // Handle different event types
        switch (result.EventType)
        {
            case CollapseEvents.EventType.LightCascade:
                // Extinguish lights
                foreach (var tile in result.ExtinguishedLights)
                {
                    gameGrid.SetCellLight(tile, false);
                }
                break;
                
            case CollapseEvents.EventType.DebrisPath:
                // Block tiles (would need to add blocked tile tracking to GameGrid)
                foreach (var tile in result.BlockedTiles)
                {
                    AddMessage($"Debris blocks {(char)('A' + tile.X)}{tile.Y + 1}!", MessageType.Warning);
                }
                break;
                
            case CollapseEvents.EventType.MemoryEcho:
                // Show filed players briefly
                if (result.ShowFiledPlayers)
                {
                    AddMessage("You see ghostly echoes of filed players!", MessageType.System);
                    // Would need to implement visual effect
                }
                break;
                
            case CollapseEvents.EventType.TimeSlip:
                // Already handled in CollapseManager
                UpdateCollapseDisplay();
                break;
                
            case CollapseEvents.EventType.ShatteredPath:
                // Collapse tiles permanently
                foreach (var tile in result.CollapsedTiles)
                {
                    // Would need to add collapsed tile tracking to GameGrid
                    AddMessage($"Floor collapses at {(char)('A' + tile.X)}{tile.Y + 1}!", MessageType.Danger);
                }
                break;
        }
        
        // Trigger LED effect
        if (!string.IsNullOrEmpty(result.Effect))
        {
            var effectParams = new Godot.Collections.Dictionary<string, object>
            {
                ["effect"] = result.Effect,
                ["duration"] = result.Duration
            };
            hardwareBridge.Execute("trigger_effect", effectParams);
        }
    }
    
    private void GameOver(string reason)
    {
        AddMessage($"GAME OVER: {reason}", MessageType.System);
        
        // Disable all actions
        foreach (Button button in actionButtons.GetChildren())
        {
            button.Disabled = true;
        }
        
        // Show game over screen after delay
        var timer = GetTree().CreateTimer(2.0);
        timer.Timeout += () => {
            var gameOverScreen = new GameOverScreen();
            gameOverScreen.SetGameOverReason(reason);
            
            // Collect game stats
            var stats = new Godot.Collections.Dictionary
            {
                ["rounds"] = currentRound,
                ["players_filed"] = 4 - players.Count, // Assuming started with 4
                ["cells_explored"] = GetExploredCellCount(),
                ["tokens_used"] = GetUsedTokenCount(),
                ["total_noise"] = noiseLevel,
                ["aidron_found"] = IsAidronFound()
            };
            gameOverScreen.SetGameStats(stats);
            
            GetTree().Root.AddChild(gameOverScreen);
        };
    }
    
    private void AddMessage(string text, MessageType type)
    {
        var color = type switch
        {
            MessageType.System => "[color=#AAAAAA]",
            MessageType.Action => "[color=#FFFFFF]",
            MessageType.Warning => "[color=#FFFF00]",
            MessageType.Error => "[color=#FF0000]",
            MessageType.Success => "[color=#00FF00]",
            MessageType.Danger => "[color=#FF6600]",
            MessageType.Collapse => "[color=#FF00FF]",
            _ => "[color=#FFFFFF]"
        };
        
        var timestamp = $"[color=#666666][{currentRound}:{currentPlayerIndex + 1}][/color] ";
        messageLog.AppendText($"{timestamp}{color}{text}[/color]\n");
    }
    
    private void ShowPauseMenu()
    {
        var pauseMenu = new PauseMenu();
        pauseMenu.ResumeGame += () => AddMessage("Game resumed", MessageType.System);
        pauseMenu.RestartGame += RestartGame;
        pauseMenu.QuitToMenu += () => GetTree().ChangeSceneToFile("res://scenes/screens/main_menu.tscn");
        GetTree().Root.AddChild(pauseMenu);
    }
    
    private void RestartGame()
    {
        GetTree().ReloadCurrentScene();
    }
    
    private void CheckVictoryCondition(Vector2I exitPosition)
    {
        // Check if any player is on the exit
        foreach (var player in players)
        {
            if (player.Position == exitPosition)
            {
                Victory();
                return;
            }
        }
    }
    
    private void ActivateAidronProtocol(Vector2I aidronPos)
    {
        if (!exitPosition.HasValue)
        {
            AddMessage("WARNING: Exit not found! Aidron cannot plot course!", MessageType.Error);
            return;
        }
        
        var exitPos = exitPosition.Value;
        
        // Calculate path from Aidron to Exit
        var path = CalculateAidronPath(aidronPos, exitPos);
        
        // Create 3-wide corridor of permanent light
        foreach (var point in path)
        {
            // Light center point
            gameGrid.SetCellLight(point, true, true);
            
            // Light adjacent tiles for 3-wide corridor
            var adjacent = new Vector2I[]
            {
                point + Vector2I.Up,
                point + Vector2I.Down,
                point + Vector2I.Left,
                point + Vector2I.Right,
                point + new Vector2I(-1, -1),
                point + new Vector2I(1, -1),
                point + new Vector2I(-1, 1),
                point + new Vector2I(1, 1)
            };
            
            foreach (var adj in adjacent)
            {
                if (adj.X >= 0 && adj.X < 8 && adj.Y >= 0 && adj.Y < 6)
                {
                    gameGrid.SetCellLight(adj, true, true);
                }
            }
        }
        
        AddMessage($"Aidron creates a 3-wide corridor of light to the exit!", MessageType.Success);
        
        // Trigger Aidron protocol animation sequence
        var protocolParams = new Godot.Collections.Dictionary<string, object>
        {
            ["effect"] = "aidron_protocol",
            ["aidron_pos"] = aidronPos,
            ["exit_pos"] = exitPos,
            ["path"] = path
        };
        hardwareBridge.Execute("trigger_effect", protocolParams);
    }
    
    private List<Vector2I> CalculateAidronPath(Vector2I start, Vector2I end)
    {
        // Simple pathfinding - straight line with Manhattan distance
        var path = new List<Vector2I>();
        var current = start;
        
        while (current != end)
        {
            path.Add(current);
            
            // Move towards exit
            if (current.X < end.X)
                current.X++;
            else if (current.X > end.X)
                current.X--;
            else if (current.Y < end.Y)
                current.Y++;
            else if (current.Y > end.Y)
                current.Y--;
        }
        
        path.Add(end);
        return path;
    }
    
    private void Victory()
    {
        AddMessage("VICTORY! You have escaped the vault!", MessageType.Success);
        
        // Disable all actions
        foreach (Button button in actionButtons.GetChildren())
        {
            button.Disabled = true;
        }
        
        // Show victory screen after delay
        var timer = GetTree().CreateTimer(2.0);
        timer.Timeout += () => {
            var victoryScreen = new VictoryScreen();
            
            // Collect victory stats
            var stats = new Godot.Collections.Dictionary
            {
                ["rounds"] = currentRound,
                ["survivors"] = players.Count,
                ["total_players"] = 4, // Assuming started with 4
                ["tokens_left"] = players.Sum(p => p.Tokens),
                ["peak_noise"] = noiseLevel,
                ["aidron_found"] = aidronFound
            };
            victoryScreen.SetVictoryStats(stats);
            
            GetTree().Root.AddChild(victoryScreen);
        };
    }
    
    private int GetExploredCellCount()
    {
        return exploredCells.Count;
    }
    
    private int GetUsedTokenCount()
    {
        return totalTokensUsed;
    }
    
    private bool IsAidronFound()
    {
        return aidronFound;
    }
}

// Helper classes
public class PlayerInfo
{
    public string Name { get; set; }
    public Color Color { get; set; }
    public bool IsAI { get; set; }
    public Vector2I Position { get; set; }
    public int Tokens { get; set; }
}

public enum MessageType
{
    System,
    Action,
    Warning,
    Error,
    Success,
    Danger,
    Collapse
}