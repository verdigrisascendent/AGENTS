using Godot;

/// <summary>
/// Pause Menu - In-game pause overlay with Amiga aesthetic
/// </summary>
public partial class PauseMenu : AmigaModal
{
    [Signal]
    public delegate void ResumeGameEventHandler();
    
    [Signal]
    public delegate void RestartGameEventHandler();
    
    [Signal]
    public delegate void QuitToMenuEventHandler();
    
    // Buttons
    private Button resumeButton;
    private Button settingsButton;
    private Button restartButton;
    private Button quitButton;
    
    // Sub-modals
    private SettingsModal settingsModal;
    
    public override void _Ready()
    {
        // Pause the game
        GetTree().Paused = true;
        ProcessMode = ProcessModeEnum.WhenPaused;
        
        base._Ready();
    }
    
    protected override void CreateModalStructure()
    {
        base.CreateModalStructure();
        
        // Set modal size
        modalPanel.CustomMinimumSize = GetTileAlignedSize(new Vector2(320, 280));
        titleLabel.Text = "GAME PAUSED";
        
        CreatePauseContent();
    }
    
    private void CreatePauseContent()
    {
        // Add some spacing
        var topSpacer = new Control();
        topSpacer.CustomMinimumSize = new Vector2(0, TileSize * 2);
        contentContainer.AddChild(topSpacer);
        
        // Resume button
        resumeButton = CreateMenuButton("RESUME GAME");
        resumeButton.Pressed += OnResumePressed;
        contentContainer.AddChild(resumeButton);
        
        // Settings button
        settingsButton = CreateMenuButton("SETTINGS");
        settingsButton.Pressed += OnSettingsPressed;
        contentContainer.AddChild(settingsButton);
        
        // Restart button
        restartButton = CreateMenuButton("RESTART GAME");
        restartButton.Pressed += OnRestartPressed;
        contentContainer.AddChild(restartButton);
        
        // Quit button
        quitButton = CreateMenuButton("QUIT TO MENU");
        quitButton.Pressed += OnQuitPressed;
        contentContainer.AddChild(quitButton);
        
        // Add bottom spacer
        var bottomSpacer = new Control();
        bottomSpacer.CustomMinimumSize = new Vector2(0, TileSize * 2);
        contentContainer.AddChild(bottomSpacer);
        
        // Game state info
        AddGameStateInfo();
    }
    
    private Button CreateMenuButton(string text)
    {
        var button = new Button();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(0, TileSize * 6);
        
        // Style matching Amiga
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = backgroundColor;
        buttonStyle.BorderWidthTop = 2;
        buttonStyle.BorderWidthBottom = 2;
        buttonStyle.BorderWidthLeft = 2;
        buttonStyle.BorderWidthRight = 2;
        buttonStyle.BorderColor = borderColor;
        button.AddThemeStyleboxOverride("normal", buttonStyle);
        
        var hoverStyle = buttonStyle.Duplicate() as StyleBoxFlat;
        hoverStyle.BgColor = borderColor;
        hoverStyle.BorderColor = Colors.White;
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        
        var pressedStyle = buttonStyle.Duplicate() as StyleBoxFlat;
        pressedStyle.BgColor = new Color("#003366");
        button.AddThemeStyleboxOverride("pressed", pressedStyle);
        
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeFontSizeOverride("font_size", 18);
        
        return button;
    }
    
    private void AddGameStateInfo()
    {
        var separator = new HSeparator();
        contentContainer.AddChild(separator);
        
        // Get game state from main game screen if available
        var gameScreen = GetNode<Node>("/root/MainGameScreen");
        if (gameScreen != null)
        {
            var infoContainer = new VBoxContainer();
            infoContainer.AddThemeConstantOverride("separation", 4);
            contentContainer.AddChild(infoContainer);
            
            // Round info
            var roundInfo = new Label();
            roundInfo.Text = $"ROUND: {gameScreen.Get("currentRound")}";
            roundInfo.AddThemeColorOverride("font_color", new Color("#AAAAAA"));
            roundInfo.AddThemeFontSizeOverride("font_size", 14);
            infoContainer.AddChild(roundInfo);
            
            // Noise level
            var noiseInfo = new Label();
            noiseInfo.Text = $"NOISE LEVEL: {gameScreen.Get("noiseLevel")}";
            noiseInfo.AddThemeColorOverride("font_color", new Color("#AAAAAA"));
            noiseInfo.AddThemeFontSizeOverride("font_size", 14);
            infoContainer.AddChild(noiseInfo);
        }
    }
    
    private void OnResumePressed()
    {
        ResumeGame();
    }
    
    private void OnSettingsPressed()
    {
        // Open settings modal
        settingsModal = new SettingsModal();
        GetParent().AddChild(settingsModal);
    }
    
    private void OnRestartPressed()
    {
        // Show confirmation dialog
        var confirmDialog = new ConfirmationModal();
        confirmDialog.SetMessage("Are you sure you want to restart the game?\nAll progress will be lost!");
        confirmDialog.Confirmed += () => {
            EmitSignal(SignalName.RestartGame);
            ResumeGame();
            QueueFree();
        };
        GetParent().AddChild(confirmDialog);
    }
    
    private void OnQuitPressed()
    {
        // Show confirmation dialog
        var confirmDialog = new ConfirmationModal();
        confirmDialog.SetMessage("Are you sure you want to quit to the main menu?\nAll progress will be lost!");
        confirmDialog.Confirmed += () => {
            EmitSignal(SignalName.QuitToMenu);
            ResumeGame();
            GetTree().ChangeSceneToFile("res://scenes/screens/main_menu.tscn");
        };
        GetParent().AddChild(confirmDialog);
    }
    
    private void ResumeGame()
    {
        GetTree().Paused = false;
        EmitSignal(SignalName.ResumeGame);
        Close();
    }
    
    public override void Close()
    {
        GetTree().Paused = false;
        base.Close();
    }
    
    public override void _ExitTree()
    {
        // Ensure game is unpaused if modal is removed
        GetTree().Paused = false;
        base._ExitTree();
    }
}