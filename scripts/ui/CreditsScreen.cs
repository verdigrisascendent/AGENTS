using Godot;

/// <summary>
/// Credits Screen - Scrollable credits with Amiga aesthetic
/// </summary>
public partial class CreditsScreen : Control
{
    private ScrollContainer scrollContainer;
    private VBoxContainer creditsContainer;
    private Button backButton;
    
    // Scrolling animation
    private float scrollSpeed = 30.0f; // Pixels per second
    private bool autoScroll = true;
    private float scrollPosition = 0.0f;
    
    // Credits data
    private readonly (string role, string[] names)[] creditEntries = new[]
    {
        ("GAME DESIGN", new[] { "Verdigris Ascendent" }),
        ("PROGRAMMING", new[] { "Claude Code AI", "Human Collaborator" }),
        ("ART DIRECTION", new[] { "Amiga Aesthetic Enforcer" }),
        ("LED INTEGRATION", new[] { "Hardware Bridge Engineer" }),
        ("GAME LOGIC", new[] { "Game State Guardian" }),
        ("AI SYSTEMS", new[] { "Filer AI Module" }),
        ("VISUAL EFFECTS", new[] { "Shader Effects Artist" }),
        ("", new string[] { }), // Spacer
        ("SPECIAL THANKS", new[] { 
            "The Amiga Community",
            "Commodore International",
            "All Retro Gaming Enthusiasts",
            "LED Matrix Manufacturers",
            "The Godot Engine Team"
        }),
        ("", new string[] { }), // Spacer
        ("TECHNOLOGIES", new[] {
            "Godot Engine 4.2",
            "C# / .NET",
            "WebSocket Protocol",
            "SK9822 LED System",
            "FastLED Library"
        }),
        ("", new string[] { }), // Spacer
        ("INSPIRED BY", new[] {
            "Classic Dungeon Crawlers",
            "Amiga Games 1985-1995",
            "Board Game Mechanics",
            "Physical Computing"
        }),
        ("", new string[] { }), // Spacer
        ("", new string[] { }), // Spacer
        ("LIGHTS IN THE DARK", new[] { "© 2024 Verdigris Ascendent" }),
        ("", new string[] { }), // Spacer
        ("", new string[] { "Made with ❤️ and AI" })
    };
    
    public override void _Ready()
    {
        SetupUI();
        PopulateCredits();
        StartScrolling();
    }
    
    private void SetupUI()
    {
        // Background
        var background = new ColorRect();
        background.Color = new Color("#000040");
        background.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        AddChild(background);
        
        // Main container
        var mainContainer = new MarginContainer();
        mainContainer.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        mainContainer.AddThemeConstantOverride("margin_left", 40);
        mainContainer.AddThemeConstantOverride("margin_top", 40);
        mainContainer.AddThemeConstantOverride("margin_right", 40);
        mainContainer.AddThemeConstantOverride("margin_bottom", 40);
        AddChild(mainContainer);
        
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        mainContainer.AddChild(vbox);
        
        // Header
        var header = new HBoxContainer();
        vbox.AddChild(header);
        
        backButton = new Button();
        backButton.Text = "< BACK";
        backButton.CustomMinimumSize = new Vector2(120, 40);
        backButton.Pressed += OnBackPressed;
        StyleButton(backButton);
        header.AddChild(backButton);
        
        var titleLabel = new Label();
        titleLabel.Text = "CREDITS";
        titleLabel.AddThemeFontSizeOverride("font_size", 48);
        titleLabel.AddThemeColorOverride("font_color", Colors.White);
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        header.AddChild(titleLabel);
        
        // Empty space for balance
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(120, 40);
        header.AddChild(spacer);
        
        // Scroll container
        scrollContainer = new ScrollContainer();
        scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scrollContainer.ScrollHorizontal = 0;
        vbox.AddChild(scrollContainer);
        
        // Credits container
        creditsContainer = new VBoxContainer();
        creditsContainer.AddThemeConstantOverride("separation", 30);
        creditsContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scrollContainer.AddChild(creditsContainer);
        
        // Footer info
        var footerLabel = new Label();
        footerLabel.Text = "PRESS SPACE TO PAUSE/RESUME SCROLLING";
        footerLabel.AddThemeColorOverride("font_color", new Color("#666666"));
        footerLabel.AddThemeFontSizeOverride("font_size", 14);
        footerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(footerLabel);
    }
    
    private void StyleButton(Button button)
    {
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = new Color("#0000AA");
        buttonStyle.BorderWidthTop = 2;
        buttonStyle.BorderWidthBottom = 2;
        buttonStyle.BorderWidthLeft = 2;
        buttonStyle.BorderWidthRight = 2;
        buttonStyle.BorderColor = Colors.White;
        button.AddThemeStyleboxOverride("normal", buttonStyle);
        
        var hoverStyle = buttonStyle.Duplicate() as StyleBoxFlat;
        hoverStyle.BgColor = new Color("#0066CC");
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        
        button.AddThemeColorOverride("font_color", Colors.White);
    }
    
    private void PopulateCredits()
    {
        // Add top spacer for scrolling effect
        var topSpacer = new Control();
        topSpacer.CustomMinimumSize = new Vector2(0, 400);
        creditsContainer.AddChild(topSpacer);
        
        foreach (var entry in creditEntries)
        {
            if (string.IsNullOrEmpty(entry.role))
            {
                // Empty line spacer
                var spacer = new Control();
                spacer.CustomMinimumSize = new Vector2(0, 20);
                creditsContainer.AddChild(spacer);
                continue;
            }
            
            // Role/Section header
            var roleLabel = new Label();
            roleLabel.Text = entry.role;
            roleLabel.AddThemeColorOverride("font_color", new Color("#00AAAA"));
            roleLabel.AddThemeFontSizeOverride("font_size", 24);
            roleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            creditsContainer.AddChild(roleLabel);
            
            // Names
            foreach (var name in entry.names)
            {
                var nameLabel = new Label();
                nameLabel.Text = name;
                nameLabel.AddThemeColorOverride("font_color", Colors.White);
                nameLabel.AddThemeFontSizeOverride("font_size", 18);
                nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
                creditsContainer.AddChild(nameLabel);
            }
        }
        
        // Add bottom spacer
        var bottomSpacer = new Control();
        bottomSpacer.CustomMinimumSize = new Vector2(0, 600);
        creditsContainer.AddChild(bottomSpacer);
    }
    
    private void StartScrolling()
    {
        scrollPosition = 0.0f;
        autoScroll = true;
    }
    
    public override void _Process(double delta)
    {
        if (autoScroll)
        {
            scrollPosition += scrollSpeed * (float)delta;
            scrollContainer.ScrollVertical = (int)scrollPosition;
            
            // Check if we've reached the end
            if (scrollContainer.ScrollVertical >= 
                scrollContainer.GetVScrollBar().MaxValue - scrollContainer.Size.Y)
            {
                // Loop back to start
                scrollPosition = 0.0f;
                scrollContainer.ScrollVertical = 0;
            }
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_select")) // Space bar
        {
            autoScroll = !autoScroll;
            GetViewport().SetInputAsHandled();
        }
        else if (@event is InputEventMouseButton mouseEvent)
        {
            // Allow manual scrolling
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp ||
                mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                autoScroll = false;
                scrollPosition = scrollContainer.ScrollVertical;
            }
        }
    }
    
    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/screens/main_menu.tscn");
    }
}