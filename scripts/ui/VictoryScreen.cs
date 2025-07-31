using Godot;

/// <summary>
/// Victory Screen - Celebration screen when players win with Amiga aesthetic
/// </summary>
public partial class VictoryScreen : AmigaModal
{
    private Label congratsLabel;
    private VBoxContainer statsContainer;
    private Button continueButton;
    private Button menuButton;
    
    // Animation
    private float animationTime = 0.0f;
    private float rainbowTime = 0.0f;
    private Godot.Collections.Array<ColorRect> starParticles = new();
    
    // LED celebration
    private HardwareBridgeEngineer hardwareBridge;
    
    protected override void CreateModalStructure()
    {
        // Fullscreen celebration
        allowClickOutside = false;
        showCloseButton = false;
        animateIn = false; // Custom animation
        
        base.CreateModalStructure();
        
        // Set modal to cover more of screen
        modalPanel.CustomMinimumSize = GetTileAlignedSize(new Vector2(640, 480));
        titleLabel.Text = "VICTORY!";
        titleLabel.AddThemeFontSizeOverride("font_size", 48);
        
        CreateVictoryContent();
        
        // Get hardware bridge for LED celebration
        hardwareBridge = GetNode<HardwareBridgeEngineer>("/root/GameInitializer/HardwareBridge");
        
        // Start celebration
        StartVictoryAnimation();
    }
    
    private void CreateVictoryContent()
    {
        // Congratulations message
        congratsLabel = new Label();
        congratsLabel.Text = "CONGRATULATIONS!\nYOU HAVE ESCAPED THE VAULT!";
        congratsLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        congratsLabel.AddThemeFontSizeOverride("font_size", 28);
        congratsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        congratsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentContainer.AddChild(congratsLabel);
        
        // Add spacing
        var spacer1 = new Control();
        spacer1.CustomMinimumSize = new Vector2(0, TileSize * 4);
        contentContainer.AddChild(spacer1);
        
        // Stats section
        var statsLabel = new Label();
        statsLabel.Text = "MISSION COMPLETE";
        statsLabel.AddThemeColorOverride("font_color", Colors.White);
        statsLabel.AddThemeFontSizeOverride("font_size", 24);
        statsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentContainer.AddChild(statsLabel);
        
        var separator = new HSeparator();
        contentContainer.AddChild(separator);
        
        statsContainer = new VBoxContainer();
        statsContainer.AddThemeConstantOverride("separation", 6);
        contentContainer.AddChild(statsContainer);
        
        // Add spacing
        var spacer2 = new Control();
        spacer2.CustomMinimumSize = new Vector2(0, TileSize * 4);
        spacer2.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        contentContainer.AddChild(spacer2);
        
        // Button row
        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", TileSize * 3);
        buttonRow.Alignment = BoxContainer.AlignmentMode.Center;
        contentContainer.AddChild(buttonRow);
        
        continueButton = new Button();
        continueButton.Text = "NEW GAME +";
        continueButton.CustomMinimumSize = new Vector2(TileSize * 20, TileSize * 6);
        continueButton.Pressed += OnContinuePressed;
        continueButton.Visible = false; // Show after animation
        StyleButton(continueButton, true);
        buttonRow.AddChild(continueButton);
        
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
        buttonStyle.BgColor = isPrimary ? Colors.Green : backgroundColor;
        buttonStyle.BorderWidthTop = 2;
        buttonStyle.BorderWidthBottom = 2;
        buttonStyle.BorderWidthLeft = 2;
        buttonStyle.BorderWidthRight = 2;
        buttonStyle.BorderColor = isPrimary ? Colors.White : Colors.Green;
        button.AddThemeStyleboxOverride("normal", buttonStyle);
        
        var hoverStyle = buttonStyle.Duplicate() as StyleBoxFlat;
        hoverStyle.BgColor = isPrimary ? new Color("#00CC00") : new Color("#006600");
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        
        button.AddThemeColorOverride("font_color", Colors.White);
    }
    
    public void SetVictoryStats(Godot.Collections.Dictionary stats)
    {
        // Clear existing stats
        foreach (Node child in statsContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        // Add stat rows with celebratory colors
        AddStatRow("ROUNDS COMPLETED:", stats.Get("rounds", 0).ToString(), Colors.Green);
        AddStatRow("SURVIVORS:", $"{stats.Get("survivors", 0)}/{stats.Get("total_players", 4)}", Colors.Cyan);
        AddStatRow("TOKENS REMAINING:", stats.Get("tokens_left", 0).ToString(), Colors.Yellow);
        AddStatRow("PEAK NOISE LEVEL:", stats.Get("peak_noise", 0).ToString(), Colors.Orange);
        
        // Calculate score
        int baseScore = 1000;
        int survivorBonus = (int)stats.Get("survivors", 0) * 250;
        int tokenBonus = (int)stats.Get("tokens_left", 0) * 100;
        int speedBonus = Mathf.Max(0, 500 - (int)stats.Get("rounds", 0) * 50);
        int totalScore = baseScore + survivorBonus + tokenBonus + speedBonus;
        
        AddStatRow("", "", Colors.White); // Spacing
        AddStatRow("BASE SCORE:", baseScore.ToString(), Colors.White);
        AddStatRow("SURVIVOR BONUS:", $"+{survivorBonus}", Colors.Green);
        AddStatRow("TOKEN BONUS:", $"+{tokenBonus}", Colors.Yellow);
        AddStatRow("SPEED BONUS:", $"+{speedBonus}", Colors.Cyan);
        AddStatRow("", "", Colors.White); // Spacing
        AddStatRow("TOTAL SCORE:", totalScore.ToString(), Colors.Magenta);
    }
    
    private void AddStatRow(string label, string value, Color valueColor)
    {
        if (string.IsNullOrEmpty(label) && string.IsNullOrEmpty(value))
        {
            // Empty row for spacing
            var spacer = new Control();
            spacer.CustomMinimumSize = new Vector2(0, TileSize);
            statsContainer.AddChild(spacer);
            return;
        }
        
        var row = new HBoxContainer();
        row.Alignment = BoxContainer.AlignmentMode.Center;
        statsContainer.AddChild(row);
        
        var labelNode = new Label();
        labelNode.Text = label;
        labelNode.AddThemeColorOverride("font_color", new Color("#AAAAAA"));
        labelNode.CustomMinimumSize = new Vector2(TileSize * 20, 0);
        labelNode.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(labelNode);
        
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(TileSize * 2, 0);
        row.AddChild(spacer);
        
        var valueNode = new Label();
        valueNode.Text = value;
        valueNode.AddThemeColorOverride("font_color", valueColor);
        valueNode.CustomMinimumSize = new Vector2(TileSize * 10, 0);
        valueNode.HorizontalAlignment = HorizontalAlignment.Left;
        row.AddChild(valueNode);
    }
    
    private void StartVictoryAnimation()
    {
        // Trigger LED victory effect
        if (hardwareBridge != null)
        {
            var effectParams = new Godot.Collections.Dictionary<string, object>
            {
                ["effect"] = "victory"
            };
            hardwareBridge.Execute("trigger_effect", effectParams);
        }
        
        // Create star particles
        CreateStarParticles();
        
        // Animate modal in with celebration
        modalPanel.Scale = Vector2.One * 0.5f;
        modalPanel.Modulate = new Color(1, 1, 1, 0);
        
        var tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(modalPanel, "scale", Vector2.One, 0.8f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Elastic);
        tween.TweenProperty(modalPanel, "modulate:a", 1.0f, 0.5f);
        
        // Show buttons after delay
        tween.Chain();
        tween.TweenInterval(2.0f);
        tween.TweenCallback(Callable.From(() => {
            continueButton.Visible = true;
            menuButton.Visible = true;
            AnimateButtonsIn();
        }));
        
        // Play victory sound
        PlayVictorySound();
    }
    
    private void CreateStarParticles()
    {
        var random = new RandomNumberGenerator();
        random.Randomize();
        
        // Create 50 star particles
        for (int i = 0; i < 50; i++)
        {
            var star = new ColorRect();
            star.Color = new Color(
                random.Randf(),
                random.Randf(),
                random.Randf()
            );
            star.CustomMinimumSize = Vector2.One * random.RandfRange(2, 6);
            star.Position = new Vector2(
                random.RandfRange(0, Size.X),
                random.RandfRange(-100, -50)
            );
            star.MouseFilter = MouseFilterEnum.Ignore;
            
            AddChild(star);
            MoveChild(star, 0); // Behind modal
            starParticles.Add(star);
            
            // Animate falling
            var tween = CreateTween();
            tween.SetLoops();
            var fallTime = random.RandfRange(3.0f, 6.0f);
            tween.TweenProperty(star, "position:y", Size.Y + 100, fallTime);
            tween.TweenCallback(Callable.From(() => {
                star.Position = new Vector2(
                    random.RandfRange(0, Size.X),
                    random.RandfRange(-100, -50)
                );
            }));
        }
    }
    
    private void AnimateButtonsIn()
    {
        continueButton.Scale = Vector2.One * 0.8f;
        menuButton.Scale = Vector2.One * 0.8f;
        
        var tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(continueButton, "scale", Vector2.One, 0.3f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        tween.TweenProperty(menuButton, "scale", Vector2.One, 0.3f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);
        
        // Rainbow effect on title
        rainbowTime += (float)delta * 2.0f;
        var hue = Mathf.PosMod(rainbowTime, 1.0f);
        titleLabel.AddThemeColorOverride("font_color", Color.FromHsv(hue, 0.8f, 1.0f));
        
        // Pulse congratulations text
        animationTime += (float)delta;
        var scale = 1.0f + Mathf.Sin(animationTime * 3.0f) * 0.05f;
        congratsLabel.Scale = Vector2.One * scale;
    }
    
    private void PlayVictorySound()
    {
        // Play victory fanfare
        var audioPlayer = new AudioStreamPlayer();
        audioPlayer.Bus = "Music";
        // audioPlayer.Stream = preload("res://audio/victory.ogg");
        AddChild(audioPlayer);
        audioPlayer.Play();
        audioPlayer.Finished += () => audioPlayer.QueueFree();
    }
    
    private void OnContinuePressed()
    {
        // Start new game with harder difficulty
        GetTree().ChangeSceneToFile("res://scenes/screens/game_setup.tscn");
    }
    
    private void OnMenuPressed()
    {
        // Return to main menu
        GetTree().ChangeSceneToFile("res://scenes/screens/main_menu.tscn");
    }
    
    public override void _ExitTree()
    {
        // Clean up particles
        foreach (ColorRect star in starParticles)
        {
            star?.QueueFree();
        }
        starParticles.Clear();
        
        base._ExitTree();
    }
}