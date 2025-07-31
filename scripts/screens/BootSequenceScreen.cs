using Godot;
using System.Threading.Tasks;

/// <summary>
/// Boot Sequence Screen - Authentic Amiga-style boot with scanline effects
/// </summary>
public partial class BootSequenceScreen : Control
{
    // UI References
    private ColorRect shaderLayer;
    private Label loadingText;
    private VBoxContainer memoryInfo;
    private Control logoContainer;
    private Panel logoPanel;
    private Timer animationTimer;
    
    // Shader and effects
    private ShaderEffectsArtist shaderArtist;
    private AmigaAestheticEnforcer aestheticEnforcer;
    private ShaderMaterial bootShader;
    
    // Animation state
    private float bootProgress = 0.0f;
    private float elapsedTime = 0.0f;
    private bool sequenceComplete = false;
    
    // Boot sequence timings
    private const float BlackDuration = 0.5f;
    private const float LoadingDuration = 1.5f;
    private const float ScanlineDuration = 1.0f;
    private const float LogoFadeDuration = 1.0f;
    private const float TotalDuration = BlackDuration + LoadingDuration + ScanlineDuration + LogoFadeDuration;
    
    public override void _Ready()
    {
        InitializeAgents();
        GetReferences();
        SetupBootSequence();
        
        // Connect timer for animation updates
        animationTimer.Timeout += OnAnimationTick;
        
        GD.Print("[BootSequence] Starting Amiga-style boot sequence");
    }
    
    private void InitializeAgents()
    {
        var megaAgent = GetNode<MegaAgent>("/root/GameInitializer/MegaAgent");
        
        shaderArtist = new ShaderEffectsArtist();
        shaderArtist.Initialize(megaAgent);
        
        aestheticEnforcer = new AmigaAestheticEnforcer();
        aestheticEnforcer.Initialize(megaAgent);
    }
    
    private void GetReferences()
    {
        shaderLayer = GetNode<ColorRect>("ShaderLayer");
        loadingText = GetNode<Label>("LoadingContainer/LoadingText");
        memoryInfo = GetNode<VBoxContainer>("MemoryInfo");
        logoContainer = GetNode<Control>("LogoContainer");
        logoPanel = GetNode<Panel>("LogoContainer/LogoPanel");
        animationTimer = GetNode<Timer>("AnimationTimer");
    }
    
    private void SetupBootSequence()
    {
        // Create boot sequence shader
        var shaderParams = new Godot.Collections.Dictionary<string, object>();
        bootShader = shaderArtist.Execute("create_boot_sequence", shaderParams) as ShaderMaterial;
        shaderLayer.Material = bootShader;
        
        // Initially hide elements
        loadingText.Visible = false;
        memoryInfo.Visible = false;
        logoContainer.Visible = false;
        
        // Apply CRT effect to the whole screen
        var crtParams = new Godot.Collections.Dictionary<string, object>
        {
            ["target"] = shaderLayer
        };
        shaderArtist.Execute("apply_crt_effect", crtParams);
    }
    
    private void OnAnimationTick()
    {
        if (sequenceComplete) return;
        
        elapsedTime += (float)animationTimer.WaitTime;
        bootProgress = Mathf.Clamp(elapsedTime / TotalDuration, 0.0f, 1.0f);
        
        UpdateBootSequence();
        
        if (bootProgress >= 1.0f && !sequenceComplete)
        {
            sequenceComplete = true;
            TransitionToMainMenu();
        }
    }
    
    private void UpdateBootSequence()
    {
        // Update shader progress
        bootShader.SetShaderParameter("boot_progress", bootProgress);
        
        // Stage 1: Black screen (0.0 - 0.125)
        if (elapsedTime < BlackDuration)
        {
            // Just black with subtle noise
        }
        // Stage 2: Loading text (0.125 - 0.5)
        else if (elapsedTime < BlackDuration + LoadingDuration)
        {
            if (!loadingText.Visible)
            {
                loadingText.Visible = true;
                FlickerLoadingText();
            }
            
            // Show memory info progressively
            if (!memoryInfo.Visible && elapsedTime > BlackDuration + 0.5f)
            {
                memoryInfo.Visible = true;
                AnimateMemoryInfo();
            }
        }
        // Stage 3: Scanline wipe (0.5 - 0.75)
        else if (elapsedTime < BlackDuration + LoadingDuration + ScanlineDuration)
        {
            loadingText.Visible = false;
            memoryInfo.Visible = false;
            
            // The shader handles the scanline wipe effect
        }
        // Stage 4: Logo fade in (0.75 - 1.0)
        else
        {
            if (!logoContainer.Visible)
            {
                logoContainer.Visible = true;
            }
            
            float fadeProg = (elapsedTime - (BlackDuration + LoadingDuration + ScanlineDuration)) / LogoFadeDuration;
            logoPanel.Modulate = new Color(1, 1, 1, fadeProg);
        }
    }
    
    private async void FlickerLoadingText()
    {
        // Simulate old CRT text flicker
        for (int i = 0; i < 3; i++)
        {
            loadingText.Modulate = new Color(1, 1, 1, 0.3f);
            await ToSignal(GetTree().CreateTimer(0.05), "timeout");
            loadingText.Modulate = new Color(1, 1, 1, 1.0f);
            await ToSignal(GetTree().CreateTimer(0.1), "timeout");
        }
    }
    
    private async void AnimateMemoryInfo()
    {
        var lines = memoryInfo.GetChildren();
        foreach (Label line in lines)
        {
            line.Visible = false;
        }
        
        // Show lines one by one with typing effect
        foreach (Label line in lines)
        {
            await TypewriterEffect(line);
            await ToSignal(GetTree().CreateTimer(0.2), "timeout");
        }
    }
    
    private async Task TypewriterEffect(Label label)
    {
        var fullText = label.Text;
        label.Text = "";
        label.Visible = true;
        
        for (int i = 0; i <= fullText.Length; i++)
        {
            label.Text = fullText.Substring(0, i);
            
            // Add cursor blink at end
            if (i < fullText.Length)
            {
                label.Text += "_";
            }
            
            await ToSignal(GetTree().CreateTimer(0.03), "timeout");
        }
    }
    
    private void TransitionToMainMenu()
    {
        GD.Print("[BootSequence] Boot complete, transitioning to main menu");
        
        // Quick flash effect
        var flash = new ColorRect();
        flash.Color = Colors.White;
        flash.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        AddChild(flash);
        
        // Fade out flash
        var tween = CreateTween();
        tween.TweenProperty(flash, "modulate:a", 0.0f, 0.3f);
        tween.TweenCallback(Callable.From(() =>
        {
            GetTree().ChangeSceneToFile("res://scenes/screens/main_menu.tscn");
        }));
    }
}