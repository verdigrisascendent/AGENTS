using Godot;

/// <summary>
/// Main Menu Screen - Entry point for the game with Amiga aesthetic
/// </summary>
public partial class MainMenuScreen : Control
{
    // UI References
    private Button playButton;
    private Button tutorialButton;
    private Button manualButton;
    private Button multiplayerButton;
    private Button ledButton;
    private Button creditsButton;
    
    // Animation nodes
    private Panel titlePanel;
    private VBoxContainer menuButtons;
    
    // Agents
    private MegaAgent megaAgent;
    private AmigaAestheticEnforcer aestheticEnforcer;
    private ShaderEffectsArtist shaderArtist;
    
    // Animation timers
    private float titleAnimationTime = 0.0f;
    private float menuAnimationTime = 0.0f;
    private bool animationsComplete = false;
    
    public override void _Ready()
    {
        // Initialize agents
        InitializeAgents();
        
        // Get UI references
        playButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButtons/PlayButton");
        tutorialButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButtons/TutorialButton");
        manualButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButtons/ManualButton");
        multiplayerButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButtons/MultiplayerButton");
        ledButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButtons/LEDButton");
        creditsButton = GetNode<Button>("CenterContainer/VBoxContainer/MenuButtons/CreditsButton");
        
        titlePanel = GetNode<Panel>("CenterContainer/VBoxContainer/TitlePanel");
        menuButtons = GetNode<VBoxContainer>("CenterContainer/VBoxContainer/MenuButtons");
        
        // Apply Amiga styling
        ApplyAmigaStyling();
        
        // Apply CRT effects
        ApplyCRTEffects();
        
        // Connect button signals
        ConnectSignals();
        
        // Start animations
        StartAnimations();
        
        GD.Print("[MainMenuScreen] Ready - Amiga aesthetic enforced");
    }
    
    private void InitializeAgents()
    {
        // Get mega agent from autoload or create it
        megaAgent = GetNode<MegaAgent>("/root/MegaAgent");
        
        // Get aesthetic enforcer
        aestheticEnforcer = new AmigaAestheticEnforcer();
        aestheticEnforcer.Initialize(megaAgent);
        
        // Get shader artist
        shaderArtist = new ShaderEffectsArtist();
        shaderArtist.Initialize(megaAgent);
    }
    
    private void ApplyAmigaStyling()
    {
        // Create Amiga-style button theme
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = new Color("#0055AA"); // Workbench blue
        buttonStyle.BorderWidthTop = 2;
        buttonStyle.BorderWidthLeft = 2;
        buttonStyle.BorderWidthBottom = 2;
        buttonStyle.BorderWidthRight = 2;
        buttonStyle.BorderColor = new Color("#AAAAAA"); // Light gray border
        buttonStyle.CornerRadiusTopLeft = 0;
        buttonStyle.CornerRadiusTopRight = 0;
        buttonStyle.CornerRadiusBottomLeft = 0;
        buttonStyle.CornerRadiusBottomRight = 0;
        buttonStyle.AntiAliasing = false;
        
        var buttonHoverStyle = buttonStyle.Duplicate() as StyleBoxFlat;
        buttonHoverStyle.BgColor = new Color("#0066CC"); // Brighter blue on hover
        
        var buttonPressedStyle = buttonStyle.Duplicate() as StyleBoxFlat;
        buttonPressedStyle.BgColor = new Color("#003366"); // Darker blue when pressed
        buttonPressedStyle.BorderColor = new Color("#444444"); // Dark border
        
        // Apply to all buttons
        foreach (Button button in menuButtons.GetChildren())
        {
            button.AddThemeStyleboxOverride("normal", buttonStyle);
            button.AddThemeStyleboxOverride("hover", buttonHoverStyle);
            button.AddThemeStyleboxOverride("pressed", buttonPressedStyle);
            button.AddThemeColorOverride("font_color", Colors.White);
            button.AddThemeColorOverride("font_hover_color", Colors.White);
            button.AddThemeColorOverride("font_pressed_color", new Color("#AAAAAA"));
        }
        
        // Validate colors with aesthetic enforcer
        var validationParams = new Godot.Collections.Dictionary<string, object>
        {
            ["node"] = this
        };
        aestheticEnforcer.Execute("validate_colors", validationParams);
    }
    
    private void ApplyCRTEffects()
    {
        // Create a shader layer for the entire screen
        var shaderLayer = new ColorRect();
        shaderLayer.Name = "CRTShaderLayer";
        shaderLayer.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        shaderLayer.Color = Colors.White;
        shaderLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        // Apply CRT shader
        var crtParams = new Godot.Collections.Dictionary<string, object>
        {
            ["target"] = shaderLayer
        };
        shaderArtist.Execute("apply_crt_effect", crtParams);
        
        // Add as topmost layer
        AddChild(shaderLayer);
        MoveChild(shaderLayer, GetChildCount() - 1);
        
        GD.Print("[MainMenuScreen] CRT effects applied");
    }
    
    private void ConnectSignals()
    {
        playButton.Pressed += OnPlayPressed;
        tutorialButton.Pressed += OnTutorialPressed;
        manualButton.Pressed += OnManualPressed;
        multiplayerButton.Pressed += OnMultiplayerPressed;
        ledButton.Pressed += OnLEDPressed;
        creditsButton.Pressed += OnCreditsPressed;
    }
    
    private void StartAnimations()
    {
        // Initially hide elements
        titlePanel.Modulate = new Color(1, 1, 1, 0);
        menuButtons.Modulate = new Color(1, 1, 1, 0);
        
        // Disable menu buttons until animation completes
        foreach (Button button in menuButtons.GetChildren())
        {
            button.Disabled = true;
        }
    }
    
    public override void _Process(double delta)
    {
        if (!animationsComplete)
        {
            AnimateUI((float)delta);
        }
    }
    
    private void AnimateUI(float delta)
    {
        // Title animation (elastic effect)
        if (titleAnimationTime < 2.0f)
        {
            titleAnimationTime += delta;
            float t = Mathf.Clamp(titleAnimationTime / 2.0f, 0.0f, 1.0f);
            
            // Elastic out curve
            float elasticT = 1.0f - Mathf.Pow(2, -10 * t) * Mathf.Cos(t * Mathf.Pi * 2);
            
            titlePanel.Modulate = new Color(1, 1, 1, t);
            titlePanel.Scale = Vector2.One * (0.8f + 0.2f * elasticT);
        }
        
        // Menu animation (fade in with slide)
        if (titleAnimationTime > 1.0f && menuAnimationTime < 0.8f)
        {
            menuAnimationTime += delta;
            float t = Mathf.Clamp(menuAnimationTime / 0.8f, 0.0f, 1.0f);
            
            // Ease out cubic
            float easeT = 1.0f - Mathf.Pow(1.0f - t, 3.0f);
            
            menuButtons.Modulate = new Color(1, 1, 1, t);
            menuButtons.Position = new Vector2(0, 20 * (1.0f - easeT));
            
            // Enable buttons when animation completes
            if (t >= 1.0f)
            {
                foreach (Button button in menuButtons.GetChildren())
                {
                    button.Disabled = false;
                }
                animationsComplete = true;
            }
        }
    }
    
    private void OnPlayPressed()
    {
        GD.Print("[MainMenuScreen] Play pressed");
        PlayClickSound();
        
        // Navigate to game setup screen
        GetTree().ChangeSceneToFile("res://scenes/screens/game_setup.tscn");
    }
    
    private void OnTutorialPressed()
    {
        GD.Print("[MainMenuScreen] Tutorial pressed");
        PlayClickSound();
        
        // Navigate to tutorial screen
        GetTree().ChangeSceneToFile("res://scenes/screens/tutorial_screen.tscn");
    }
    
    private void OnManualPressed()
    {
        GD.Print("[MainMenuScreen] Manual pressed");
        PlayClickSound();
        
        // Navigate to manual screen
        GetTree().ChangeSceneToFile("res://scenes/screens/manual.tscn");
    }
    
    private void OnMultiplayerPressed()
    {
        GD.Print("[MainMenuScreen] Multiplayer pressed");
        PlayClickSound();
        
        // Navigate to multiplayer lobby
        GetTree().ChangeSceneToFile("res://scenes/screens/multiplayer_lobby.tscn");
    }
    
    private void OnLEDPressed()
    {
        GD.Print("[MainMenuScreen] LED Board pressed");
        PlayClickSound();
        
        // Navigate to LED board test screen
        GetTree().ChangeSceneToFile("res://scenes/screens/led_test.tscn");
    }
    
    private void OnCreditsPressed()
    {
        GD.Print("[MainMenuScreen] Credits pressed");
        PlayClickSound();
        
        // Navigate to credits screen
        GetTree().ChangeSceneToFile("res://scenes/screens/credits_screen.tscn");
    }
    
    private void PlayClickSound()
    {
        // Play authentic Amiga click sound
        // Would be implemented with actual audio system
    }
}