using Godot;

/// <summary>
/// Settings Modal - Tile-aligned settings popup with Amiga aesthetic
/// </summary>
public partial class SettingsModal : AmigaModal
{
    // Settings controls
    private HSlider sfxVolumeSlider;
    private Label sfxVolumeLabel;
    private HSlider musicVolumeSlider;
    private Label musicVolumeLabel;
    private CheckBox fullscreenCheck;
    private CheckBox vsyncCheck;
    private CheckBox crtEffectsCheck;
    private CheckBox scanlinesCheck;
    private OptionButton difficultyOption;
    private Button applyButton;
    private Button defaultsButton;
    
    // Current settings
    private float sfxVolume = 0.8f;
    private float musicVolume = 0.6f;
    private bool fullscreen = false;
    private bool vsync = true;
    private bool crtEffects = true;
    private bool scanlines = true;
    private int difficulty = 1; // Normal
    
    public override void _Ready()
    {
        base._Ready();
        LoadSettings();
    }
    
    protected override void CreateModalStructure()
    {
        base.CreateModalStructure();
        
        // Set modal size
        modalPanel.CustomMinimumSize = GetTileAlignedSize(new Vector2(480, 400));
        titleLabel.Text = "SETTINGS";
        
        CreateSettingsContent();
    }
    
    private void CreateSettingsContent()
    {
        // Audio Section
        var audioLabel = CreateSectionLabel("AUDIO");
        contentContainer.AddChild(audioLabel);
        
        // SFX Volume
        var sfxContainer = new HBoxContainer();
        sfxContainer.AddThemeConstantOverride("separation", TileSize);
        contentContainer.AddChild(sfxContainer);
        
        var sfxLabel = new Label();
        sfxLabel.Text = "SFX VOLUME:";
        sfxLabel.CustomMinimumSize = new Vector2(TileSize * 16, 0);
        sfxLabel.AddThemeColorOverride("font_color", Colors.White);
        sfxContainer.AddChild(sfxLabel);
        
        sfxVolumeSlider = new HSlider();
        sfxVolumeSlider.MinValue = 0.0;
        sfxVolumeSlider.MaxValue = 1.0;
        sfxVolumeSlider.Step = 0.1;
        sfxVolumeSlider.Value = sfxVolume;
        sfxVolumeSlider.CustomMinimumSize = new Vector2(TileSize * 20, 0);
        sfxVolumeSlider.ValueChanged += OnSfxVolumeChanged;
        sfxContainer.AddChild(sfxVolumeSlider);
        
        sfxVolumeLabel = new Label();
        sfxVolumeLabel.Text = $"{(int)(sfxVolume * 100)}%";
        sfxVolumeLabel.CustomMinimumSize = new Vector2(TileSize * 6, 0);
        sfxVolumeLabel.AddThemeColorOverride("font_color", new Color("#AAAAAA"));
        sfxContainer.AddChild(sfxVolumeLabel);
        
        // Music Volume
        var musicContainer = new HBoxContainer();
        musicContainer.AddThemeConstantOverride("separation", TileSize);
        contentContainer.AddChild(musicContainer);
        
        var musicLabel = new Label();
        musicLabel.Text = "MUSIC VOLUME:";
        musicLabel.CustomMinimumSize = new Vector2(TileSize * 16, 0);
        musicLabel.AddThemeColorOverride("font_color", Colors.White);
        musicContainer.AddChild(musicLabel);
        
        musicVolumeSlider = new HSlider();
        musicVolumeSlider.MinValue = 0.0;
        musicVolumeSlider.MaxValue = 1.0;
        musicVolumeSlider.Step = 0.1;
        musicVolumeSlider.Value = musicVolume;
        musicVolumeSlider.CustomMinimumSize = new Vector2(TileSize * 20, 0);
        musicVolumeSlider.ValueChanged += OnMusicVolumeChanged;
        musicContainer.AddChild(musicVolumeSlider);
        
        musicVolumeLabel = new Label();
        musicVolumeLabel.Text = $"{(int)(musicVolume * 100)}%";
        musicVolumeLabel.CustomMinimumSize = new Vector2(TileSize * 6, 0);
        musicVolumeLabel.AddThemeColorOverride("font_color", new Color("#AAAAAA"));
        musicContainer.AddChild(musicVolumeLabel);
        
        // Add separator
        contentContainer.AddChild(new HSeparator());
        
        // Video Section
        var videoLabel = CreateSectionLabel("VIDEO");
        contentContainer.AddChild(videoLabel);
        
        // Fullscreen
        fullscreenCheck = new CheckBox();
        fullscreenCheck.Text = "FULLSCREEN";
        fullscreenCheck.ButtonPressed = fullscreen;
        fullscreenCheck.AddThemeColorOverride("font_color", Colors.White);
        contentContainer.AddChild(fullscreenCheck);
        
        // VSync
        vsyncCheck = new CheckBox();
        vsyncCheck.Text = "VSYNC";
        vsyncCheck.ButtonPressed = vsync;
        vsyncCheck.AddThemeColorOverride("font_color", Colors.White);
        contentContainer.AddChild(vsyncCheck);
        
        // CRT Effects
        crtEffectsCheck = new CheckBox();
        crtEffectsCheck.Text = "CRT EFFECTS";
        crtEffectsCheck.ButtonPressed = crtEffects;
        crtEffectsCheck.AddThemeColorOverride("font_color", Colors.White);
        contentContainer.AddChild(crtEffectsCheck);
        
        // Scanlines
        scanlinesCheck = new CheckBox();
        scanlinesCheck.Text = "SCANLINES";
        scanlinesCheck.ButtonPressed = scanlines;
        scanlinesCheck.AddThemeColorOverride("font_color", Colors.White);
        contentContainer.AddChild(scanlinesCheck);
        
        // Add separator
        contentContainer.AddChild(new HSeparator());
        
        // Gameplay Section
        var gameplayLabel = CreateSectionLabel("GAMEPLAY");
        contentContainer.AddChild(gameplayLabel);
        
        // Difficulty
        var difficultyContainer = new HBoxContainer();
        difficultyContainer.AddThemeConstantOverride("separation", TileSize);
        contentContainer.AddChild(difficultyContainer);
        
        var diffLabel = new Label();
        diffLabel.Text = "DIFFICULTY:";
        diffLabel.CustomMinimumSize = new Vector2(TileSize * 16, 0);
        diffLabel.AddThemeColorOverride("font_color", Colors.White);
        difficultyContainer.AddChild(diffLabel);
        
        difficultyOption = new OptionButton();
        difficultyOption.AddItem("EASY");
        difficultyOption.AddItem("NORMAL");
        difficultyOption.AddItem("HARD");
        difficultyOption.AddItem("NIGHTMARE");
        difficultyOption.Selected = difficulty;
        difficultyOption.CustomMinimumSize = new Vector2(TileSize * 20, 0);
        difficultyContainer.AddChild(difficultyOption);
        
        // Add spacer
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, TileSize * 2);
        contentContainer.AddChild(spacer);
        
        // Button row
        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", TileSize);
        buttonRow.Alignment = BoxContainer.AlignmentMode.Center;
        contentContainer.AddChild(buttonRow);
        
        defaultsButton = new Button();
        defaultsButton.Text = "DEFAULTS";
        defaultsButton.CustomMinimumSize = new Vector2(TileSize * 16, TileSize * 5);
        defaultsButton.Pressed += OnDefaultsPressed;
        StyleButton(defaultsButton);
        buttonRow.AddChild(defaultsButton);
        
        applyButton = new Button();
        applyButton.Text = "APPLY";
        applyButton.CustomMinimumSize = new Vector2(TileSize * 16, TileSize * 5);
        applyButton.Pressed += OnApplyPressed;
        StyleButton(applyButton);
        buttonRow.AddChild(applyButton);
    }
    
    private Label CreateSectionLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", new Color("#00AAAA"));
        label.AddThemeFontSizeOverride("font_size", 20);
        return label;
    }
    
    private void StyleButton(Button button)
    {
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = borderColor;
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
    
    private void OnSfxVolumeChanged(double value)
    {
        sfxVolume = (float)value;
        sfxVolumeLabel.Text = $"{(int)(sfxVolume * 100)}%";
    }
    
    private void OnMusicVolumeChanged(double value)
    {
        musicVolume = (float)value;
        musicVolumeLabel.Text = $"{(int)(musicVolume * 100)}%";
    }
    
    private void OnDefaultsPressed()
    {
        // Reset to defaults
        sfxVolume = 0.8f;
        musicVolume = 0.6f;
        fullscreen = false;
        vsync = true;
        crtEffects = true;
        scanlines = true;
        difficulty = 1;
        
        // Update UI
        sfxVolumeSlider.Value = sfxVolume;
        musicVolumeSlider.Value = musicVolume;
        fullscreenCheck.ButtonPressed = fullscreen;
        vsyncCheck.ButtonPressed = vsync;
        crtEffectsCheck.ButtonPressed = crtEffects;
        scanlinesCheck.ButtonPressed = scanlines;
        difficultyOption.Selected = difficulty;
    }
    
    private void OnApplyPressed()
    {
        SaveSettings();
        ApplySettings();
        Close();
    }
    
    private void LoadSettings()
    {
        // Load from ProjectSettings or ConfigFile
        var config = new ConfigFile();
        var error = config.Load("user://settings.cfg");
        
        if (error == Error.Ok)
        {
            sfxVolume = (float)config.GetValue("audio", "sfx_volume", 0.8);
            musicVolume = (float)config.GetValue("audio", "music_volume", 0.6);
            fullscreen = (bool)config.GetValue("video", "fullscreen", false);
            vsync = (bool)config.GetValue("video", "vsync", true);
            crtEffects = (bool)config.GetValue("video", "crt_effects", true);
            scanlines = (bool)config.GetValue("video", "scanlines", true);
            difficulty = (int)config.GetValue("gameplay", "difficulty", 1);
        }
    }
    
    private void SaveSettings()
    {
        var config = new ConfigFile();
        
        config.SetValue("audio", "sfx_volume", sfxVolume);
        config.SetValue("audio", "music_volume", musicVolume);
        config.SetValue("video", "fullscreen", fullscreen);
        config.SetValue("video", "vsync", vsync);
        config.SetValue("video", "crt_effects", crtEffects);
        config.SetValue("video", "scanlines", scanlines);
        config.SetValue("gameplay", "difficulty", difficulty);
        
        config.Save("user://settings.cfg");
    }
    
    private void ApplySettings()
    {
        // Apply audio settings
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("SFX"), 
            Mathf.LinearToDb(sfxVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Music"), 
            Mathf.LinearToDb(musicVolume));
        
        // Apply video settings
        if (fullscreen != ((Window)GetWindow()).Mode == Window.ModeEnum.Fullscreen)
        {
            if (fullscreen)
            {
                GetWindow().Mode = Window.ModeEnum.Fullscreen;
            }
            else
            {
                GetWindow().Mode = Window.ModeEnum.Windowed;
            }
        }
        
        // VSync
        if (vsync)
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Enabled);
        }
        else
        {
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        }
        
        // Apply CRT effects (would need to communicate with shader system)
        // This would typically be done through a signal or singleton
        GetNode<Node>("/root/GameSettings")?.Call("set_crt_effects", crtEffects);
        GetNode<Node>("/root/GameSettings")?.Call("set_scanlines", scanlines);
    }
}