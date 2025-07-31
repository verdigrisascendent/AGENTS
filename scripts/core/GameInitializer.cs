using Godot;

/// <summary>
/// Game Initializer - Autoload script that sets up the agent system and core services
/// </summary>
public partial class GameInitializer : Node
{
    private MegaAgent megaAgent;
    private HardwareBridgeEngineer hardwareBridge;
    
    public override void _Ready()
    {
        GD.Print("[GameInitializer] Starting Lights in the Dark...");
        
        // Set up display settings for pixel-perfect rendering
        ConfigureDisplay();
        
        // Initialize agent system
        InitializeAgents();
        
        // Create font resources
        AmigaFontLoader.CreateTopazFontResource();
        
        // Connect to LED hardware
        ConnectHardware();
        
        GD.Print("[GameInitializer] Initialization complete");
    }
    
    private void ConfigureDisplay()
    {
        // Configure viewport for pixel-perfect scaling
        var viewport = GetViewport();
        viewport.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
        
        // Set window properties
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetSize(new Vector2I(1600, 1280));
        
        // Configure project settings at runtime
        ProjectSettings.SetSetting("rendering/textures/canvas_textures/default_texture_filter", 0);
        
        GD.Print("[GameInitializer] Display configured for retro aesthetics");
    }
    
    private void InitializeAgents()
    {
        // Create and add mega agent as child
        megaAgent = new MegaAgent();
        megaAgent.Name = "MegaAgent";
        AddChild(megaAgent);
        
        // Initialize hardware bridge
        hardwareBridge = new HardwareBridgeEngineer();
        hardwareBridge.Name = "HardwareBridge";
        AddChild(hardwareBridge);
        
        // Register shader effects artist with mega agent
        var shaderArtist = new ShaderEffectsArtist();
        shaderArtist.Initialize(megaAgent);
        
        GD.Print("[GameInitializer] Agent system initialized");
    }
    
    private void ConnectHardware()
    {
        // Attempt to connect to LED board
        var connectParams = new Godot.Collections.Dictionary<string, object>();
        hardwareBridge.Execute("connect", connectParams);
    }
    
    public MegaAgent GetMegaAgent()
    {
        return megaAgent;
    }
    
    public HardwareBridgeEngineer GetHardwareBridge()
    {
        return hardwareBridge;
    }
}