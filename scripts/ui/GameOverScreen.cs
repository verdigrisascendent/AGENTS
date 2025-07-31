using Godot;

/// <summary>
/// Game Over Screen - Displays when players lose with Amiga aesthetic
/// </summary>
public partial class GameOverScreen : AmigaModal
{
    private Label reasonLabel;
    private VBoxContainer statsContainer;
    private Button retryButton;
    private Button menuButton;
    
    // Animation
    private float textAnimationTime = 0.0f;
    private string fullReasonText = "";
    private int revealedChars = 0;
    
    protected override void CreateModalStructure()
    {
        // Make it fullscreen for dramatic effect
        allowClickOutside = false;
        showCloseButton = false;
        animateIn = false; // Custom animation
        
        base.CreateModalStructure();
        
        // Set modal to cover more of screen
        modalPanel.CustomMinimumSize = GetTileAlignedSize(new Vector2(600, 400));
        titleLabel.Text = "GAME OVER";
        titleLabel.AddThemeColorOverride("font_color", Colors.Red);
        titleLabel.AddThemeFontSizeOverride("font_size", 48);
        
        CreateGameOverContent();
        
        // Start custom animations
        StartGameOverAnimation();
    }
    
    private void CreateGameOverContent()
    {
        // Reason for game over
        reasonLabel = new Label();
        reasonLabel.Text = "";
        reasonLabel.AddThemeColorOverride("font_color", new Color("#FF6666"));
        reasonLabel.AddThemeFontSizeOverride("font_size", 24);
        reasonLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        contentContainer.AddChild(reasonLabel);
        
        // Add spacing
        var spacer1 = new Control();
        spacer1.CustomMinimumSize = new Vector2(0, TileSize * 3);
        contentContainer.AddChild(spacer1);
        
        // Stats section
        var statsLabel = new Label();
        statsLabel.Text = "FINAL STATISTICS";
        statsLabel.AddThemeColorOverride("font_color", new Color("#AAAAAA"));
        statsLabel.AddThemeFontSizeOverride("font_size", 20);
        contentContainer.AddChild(statsLabel);
        
        var separator = new HSeparator();
        contentContainer.AddChild(separator);
        
        statsContainer = new VBoxContainer();
        statsContainer.AddThemeConstantOverride("separation", 4);
        contentContainer.AddChild(statsContainer);
        
        // Add spacing
        var spacer2 = new Control();
        spacer2.CustomMinimumSize = new Vector2(0, TileSize * 3);
        spacer2.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        contentContainer.AddChild(spacer2);
        
        // Button row
        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", TileSize * 2);
        buttonRow.Alignment = BoxContainer.AlignmentMode.Center;
        contentContainer.AddChild(buttonRow);
        
        retryButton = new Button();
        retryButton.Text = "TRY AGAIN";
        retryButton.CustomMinimumSize = new Vector2(TileSize * 20, TileSize * 6);
        retryButton.Pressed += OnRetryPressed;
        retryButton.Visible = false; // Show after animation
        StyleButton(retryButton, true);
        buttonRow.AddChild(retryButton);
        
        menuButton = new Button();
        menuButton.Text = "MAIN MENU";
        menuButton.CustomMinimumSize = new Vector2(TileSize * 20, TileSize * 6);
        menuButton.Pressed += OnMenuPressed;
        menuButton.Visible = false; // Show after animation
        StyleButton(menuButton, false);
        buttonRow.AddChild(menuButton);
    }
    
    private void StyleButton(Button button, bool isPrimary)
    {
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = isPrimary ? new Color("#AA0000") : backgroundColor;
        buttonStyle.BorderWidthTop = 2;
        buttonStyle.BorderWidthBottom = 2;
        buttonStyle.BorderWidthLeft = 2;
        buttonStyle.BorderWidthRight = 2;
        buttonStyle.BorderColor = isPrimary ? Colors.White : new Color("#AA0000");
        button.AddThemeStyleboxOverride("normal", buttonStyle);
        
        var hoverStyle = buttonStyle.Duplicate() as StyleBoxFlat;
        hoverStyle.BgColor = isPrimary ? new Color("#CC0000") : new Color("#660000");
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        
        button.AddThemeColorOverride("font_color", Colors.White);
    }
    
    public void SetGameOverReason(string reason)
    {
        fullReasonText = reason.ToUpper();
    }
    
    public void SetGameStats(Godot.Collections.Dictionary stats)
    {
        // Clear existing stats
        foreach (Node child in statsContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        // Add stat rows
        AddStatRow("ROUNDS SURVIVED:", stats.Get("rounds", 0).ToString());
        AddStatRow("PLAYERS LOST:", stats.Get("players_filed", 0).ToString());
        AddStatRow("CELLS EXPLORED:", stats.Get("cells_explored", 0).ToString());
        AddStatRow("TOKENS USED:", stats.Get("tokens_used", 0).ToString());
        AddStatRow("NOISE GENERATED:", stats.Get("total_noise", 0).ToString());
        
        if (stats.ContainsKey("aidron_found") && (bool)stats["aidron_found"])
        {
            AddStatRow("AIDRON:", "FOUND", Colors.Green);
        }
        else
        {
            AddStatRow("AIDRON:", "NOT FOUND", Colors.Red);
        }
    }
    
    private void AddStatRow(string label, string value, Color? valueColor = null)
    {
        var row = new HBoxContainer();
        statsContainer.AddChild(row);
        
        var labelNode = new Label();
        labelNode.Text = label;
        labelNode.AddThemeColorOverride("font_color", new Color("#AAAAAA"));
        labelNode.CustomMinimumSize = new Vector2(TileSize * 25, 0);
        row.AddChild(labelNode);
        
        var valueNode = new Label();
        valueNode.Text = value;
        valueNode.AddThemeColorOverride("font_color", valueColor ?? Colors.White);
        row.AddChild(valueNode);
    }
    
    private void StartGameOverAnimation()
    {
        // Red flash effect
        var flash = new ColorRect();
        flash.Color = new Color(1, 0, 0, 0.5f);
        flash.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        flash.MouseFilter = MouseFilterEnum.Ignore;
        GetParent().AddChild(flash);
        
        // Animate flash
        var tween = CreateTween();
        tween.TweenProperty(flash, "modulate:a", 0.0f, 0.5f);
        tween.TweenCallback(Callable.From(() => flash.QueueFree()));
        
        // Start text reveal
        textAnimationTime = 0.0f;
        revealedChars = 0;
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // Typewriter effect for reason text
        if (revealedChars < fullReasonText.Length)
        {
            textAnimationTime += (float)delta;
            
            if (textAnimationTime >= 0.05f) // 20 chars per second
            {
                textAnimationTime = 0.0f;
                revealedChars++;
                reasonLabel.Text = fullReasonText.Substring(0, revealedChars);
                
                // Play typing sound
                PlayTypeSound();
            }
        }
        else if (!retryButton.Visible)
        {
            // Show buttons after text is revealed
            retryButton.Visible = true;
            menuButton.Visible = true;
            
            // Animate buttons in
            retryButton.Modulate = new Color(1, 1, 1, 0);
            menuButton.Modulate = new Color(1, 1, 1, 0);
            
            var tween = CreateTween();
            tween.SetParallel();
            tween.TweenProperty(retryButton, "modulate:a", 1.0f, 0.3f);
            tween.TweenProperty(menuButton, "modulate:a", 1.0f, 0.3f);
        }
    }
    
    private void PlayTypeSound()
    {
        // Play a typewriter sound effect
        var audioPlayer = new AudioStreamPlayer();
        audioPlayer.Bus = "SFX";
        audioPlayer.VolumeDb = -10;
        // audioPlayer.Stream = preload("res://audio/type.ogg");
        AddChild(audioPlayer);
        audioPlayer.Play();
        audioPlayer.Finished += () => audioPlayer.QueueFree();
    }
    
    private void OnRetryPressed()
    {
        // Restart the game
        GetTree().ReloadCurrentScene();
    }
    
    private void OnMenuPressed()
    {
        // Return to main menu
        GetTree().ChangeSceneToFile("res://scenes/screens/main_menu.tscn");
    }
}