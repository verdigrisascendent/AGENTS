using Godot;

/// <summary>
/// Base class for Amiga-style modal overlays with tile alignment
/// </summary>
public partial class AmigaModal : Control
{
    [Signal]
    public delegate void ModalClosedEventHandler();
    
    // Modal configuration
    protected const int TileSize = 8; // Amiga pixel alignment
    protected bool allowClickOutside = true;
    protected bool showCloseButton = true;
    protected bool animateIn = true;
    
    // UI elements
    protected Panel backgroundDim;
    protected Panel modalPanel;
    protected VBoxContainer contentContainer;
    protected Label titleLabel;
    protected Button closeButton;
    
    // Animation
    private float animationTime = 0.0f;
    private bool isAnimating = false;
    private bool isClosing = false;
    
    // Colors matching Amiga palette
    protected Color backgroundColor = new Color("#000040");
    protected Color borderColor = new Color("#0000AA");
    protected Color titleColor = Colors.White;
    protected Color dimColor = new Color(0, 0, 0, 0.7f);
    
    public override void _Ready()
    {
        // Set to fill screen
        SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        
        CreateModalStructure();
        
        if (animateIn)
        {
            StartOpenAnimation();
        }
    }
    
    protected virtual void CreateModalStructure()
    {
        // Background dim
        backgroundDim = new Panel();
        backgroundDim.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        backgroundDim.SelfModulate = dimColor;
        
        var dimStyle = new StyleBoxFlat();
        dimStyle.BgColor = Colors.Black;
        backgroundDim.AddThemeStyleboxOverride("panel", dimStyle);
        AddChild(backgroundDim);
        
        if (allowClickOutside)
        {
            backgroundDim.GuiInput += OnBackgroundClicked;
        }
        
        // Modal panel (tile-aligned)
        modalPanel = new Panel();
        modalPanel.SetAnchorsAndOffsetsPreset(Control.Preset.Center);
        modalPanel.CustomMinimumSize = GetTileAlignedSize(new Vector2(400, 300));
        
        // Create border style
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = backgroundColor;
        panelStyle.BorderWidthTop = 2;
        panelStyle.BorderWidthBottom = 2;
        panelStyle.BorderWidthLeft = 2;
        panelStyle.BorderWidthRight = 2;
        panelStyle.BorderColor = borderColor;
        modalPanel.AddThemeStyleboxOverride("panel", panelStyle);
        
        AddChild(modalPanel);
        
        // Content container
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        margin.AddThemeConstantOverride("margin_left", TileSize * 2);
        margin.AddThemeConstantOverride("margin_top", TileSize * 2);
        margin.AddThemeConstantOverride("margin_right", TileSize * 2);
        margin.AddThemeConstantOverride("margin_bottom", TileSize * 2);
        modalPanel.AddChild(margin);
        
        contentContainer = new VBoxContainer();
        contentContainer.AddThemeConstantOverride("separation", TileSize);
        margin.AddChild(contentContainer);
        
        // Title bar
        CreateTitleBar();
        
        // Add separator
        var separator = new HSeparator();
        contentContainer.AddChild(separator);
    }
    
    protected virtual void CreateTitleBar()
    {
        var titleBar = new HBoxContainer();
        contentContainer.AddChild(titleBar);
        
        titleLabel = new Label();
        titleLabel.Text = "MODAL";
        titleLabel.AddThemeColorOverride("font_color", titleColor);
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        titleBar.AddChild(titleLabel);
        
        if (showCloseButton)
        {
            closeButton = new Button();
            closeButton.Text = "X";
            closeButton.CustomMinimumSize = new Vector2(TileSize * 4, TileSize * 4);
            closeButton.Pressed += OnClosePressed;
            
            // Style the close button
            var closeStyle = new StyleBoxFlat();
            closeStyle.BgColor = borderColor;
            closeButton.AddThemeStyleboxOverride("normal", closeStyle);
            
            var closeHoverStyle = closeStyle.Duplicate() as StyleBoxFlat;
            closeHoverStyle.BgColor = new Color("#0066CC");
            closeButton.AddThemeStyleboxOverride("hover", closeHoverStyle);
            
            titleBar.AddChild(closeButton);
        }
    }
    
    protected Vector2 GetTileAlignedSize(Vector2 desiredSize)
    {
        // Align to tile grid
        var alignedX = Mathf.Ceil(desiredSize.X / TileSize) * TileSize;
        var alignedY = Mathf.Ceil(desiredSize.Y / TileSize) * TileSize;
        return new Vector2(alignedX, alignedY);
    }
    
    protected Vector2 GetTileAlignedPosition(Vector2 desiredPos)
    {
        // Align position to tile grid
        var alignedX = Mathf.Round(desiredPos.X / TileSize) * TileSize;
        var alignedY = Mathf.Round(desiredPos.Y / TileSize) * TileSize;
        return new Vector2(alignedX, alignedY);
    }
    
    public override void _Process(double delta)
    {
        if (isAnimating)
        {
            AnimateModal((float)delta);
        }
    }
    
    protected virtual void StartOpenAnimation()
    {
        isAnimating = true;
        animationTime = 0.0f;
        isClosing = false;
        
        // Start with modal scaled down and transparent
        modalPanel.Scale = Vector2.One * 0.8f;
        modalPanel.Modulate = new Color(1, 1, 1, 0);
        backgroundDim.Modulate = new Color(1, 1, 1, 0);
    }
    
    protected virtual void StartCloseAnimation()
    {
        isAnimating = true;
        animationTime = 0.0f;
        isClosing = true;
    }
    
    protected virtual void AnimateModal(float delta)
    {
        animationTime += delta * 4.0f; // Fast animation
        
        if (!isClosing)
        {
            // Open animation
            float t = Mathf.Clamp(animationTime, 0.0f, 1.0f);
            float easeT = 1.0f - Mathf.Pow(1.0f - t, 3.0f); // Ease out cubic
            
            modalPanel.Scale = Vector2.One * (0.8f + 0.2f * easeT);
            modalPanel.Modulate = new Color(1, 1, 1, t);
            backgroundDim.Modulate = new Color(1, 1, 1, t * 0.7f);
            
            if (animationTime >= 1.0f)
            {
                isAnimating = false;
            }
        }
        else
        {
            // Close animation
            float t = Mathf.Clamp(animationTime, 0.0f, 1.0f);
            float easeT = Mathf.Pow(t, 3.0f); // Ease in cubic
            
            modalPanel.Scale = Vector2.One * (1.0f - 0.2f * easeT);
            modalPanel.Modulate = new Color(1, 1, 1, 1.0f - t);
            backgroundDim.Modulate = new Color(1, 1, 1, (1.0f - t) * 0.7f);
            
            if (animationTime >= 1.0f)
            {
                EmitSignal(SignalName.ModalClosed);
                QueueFree();
            }
        }
    }
    
    protected virtual void OnBackgroundClicked(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            Close();
        }
    }
    
    protected virtual void OnClosePressed()
    {
        Close();
    }
    
    public virtual void Close()
    {
        if (animateIn)
        {
            StartCloseAnimation();
        }
        else
        {
            EmitSignal(SignalName.ModalClosed);
            QueueFree();
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        // Handle ESC key
        if (@event.IsActionPressed("ui_cancel"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }
}