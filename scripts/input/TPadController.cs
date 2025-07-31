using Godot;
using System.Collections.Generic;

/// <summary>
/// T-Pad Controller - Handles T-shaped directional input for game control
/// </summary>
public partial class TPadController : Control
{
    [Signal]
    public delegate void DirectionPressedEventHandler(Vector2I direction);
    
    [Signal]
    public delegate void ActionPressedEventHandler();
    
    // T-pad configuration
    private const float ButtonSize = 80.0f;
    private const float ButtonSpacing = 10.0f;
    
    // Button references
    private TextureButton upButton;
    private TextureButton downButton;
    private TextureButton leftButton;
    private TextureButton rightButton;
    private TextureButton centerButton;
    
    // Visual feedback
    private Dictionary<TextureButton, float> buttonPressTimers = new();
    private Color normalColor = new Color("#0000AA");
    private Color pressedColor = new Color("#00AAAA");
    private Color hoverColor = new Color("#0066CC");
    
    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(ButtonSize * 3 + ButtonSpacing * 2, ButtonSize * 3 + ButtonSpacing * 2);
        
        CreateTPadButtons();
        ConnectSignals();
        
        // Configure for touch
        if (OS.HasFeature("mobile") || OS.HasFeature("web_ios") || OS.HasFeature("web_android"))
        {
            ConfigureForTouch();
        }
    }
    
    private void CreateTPadButtons()
    {
        // Create button container
        var container = new Control();
        container.SetAnchorsAndOffsetsPreset(Control.Preset.Center);
        AddChild(container);
        
        // Up button
        upButton = CreateButton("UP");
        upButton.Position = new Vector2(ButtonSize + ButtonSpacing, 0);
        container.AddChild(upButton);
        
        // Left button
        leftButton = CreateButton("LEFT");
        leftButton.Position = new Vector2(0, ButtonSize + ButtonSpacing);
        container.AddChild(leftButton);
        
        // Center button (action)
        centerButton = CreateButton("ACTION");
        centerButton.Position = new Vector2(ButtonSize + ButtonSpacing, ButtonSize + ButtonSpacing);
        container.AddChild(centerButton);
        
        // Right button
        rightButton = CreateButton("RIGHT");
        rightButton.Position = new Vector2((ButtonSize + ButtonSpacing) * 2, ButtonSize + ButtonSpacing);
        container.AddChild(rightButton);
        
        // Down button
        downButton = CreateButton("DOWN");
        downButton.Position = new Vector2(ButtonSize + ButtonSpacing, (ButtonSize + ButtonSpacing) * 2);
        container.AddChild(downButton);
    }
    
    private TextureButton CreateButton(string label)
    {
        var button = new TextureButton();
        button.CustomMinimumSize = new Vector2(ButtonSize, ButtonSize);
        button.ExpandMode = TextureButton.ExpandModeEnum.FitWidthProportional;
        button.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
        
        // Create button texture (placeholder)
        var normalTexture = CreateButtonTexture(normalColor);
        var pressedTexture = CreateButtonTexture(pressedColor);
        var hoverTexture = CreateButtonTexture(hoverColor);
        
        button.TextureNormal = normalTexture;
        button.TexturePressed = pressedTexture;
        button.TextureHover = hoverTexture;
        
        // Add label
        var labelNode = new Label();
        labelNode.Text = label;
        labelNode.AddThemeColorOverride("font_color", Colors.White);
        labelNode.SetAnchorsAndOffsetsPreset(Control.Preset.Center);
        button.AddChild(labelNode);
        
        buttonPressTimers[button] = 0.0f;
        
        return button;
    }
    
    private ImageTexture CreateButtonTexture(Color color)
    {
        // Create a simple colored square texture
        var image = Image.Create((int)ButtonSize, (int)ButtonSize, false, Image.Format.Rgb8);
        image.Fill(color);
        
        // Add border
        for (int x = 0; x < ButtonSize; x++)
        {
            for (int y = 0; y < ButtonSize; y++)
            {
                if (x < 2 || x >= ButtonSize - 2 || y < 2 || y >= ButtonSize - 2)
                {
                    image.SetPixel(x, y, Colors.White);
                }
            }
        }
        
        return ImageTexture.CreateFromImage(image);
    }
    
    private void ConnectSignals()
    {
        upButton.Pressed += () => OnDirectionPressed(Vector2I.Up);
        downButton.Pressed += () => OnDirectionPressed(Vector2I.Down);
        leftButton.Pressed += () => OnDirectionPressed(Vector2I.Left);
        rightButton.Pressed += () => OnDirectionPressed(Vector2I.Right);
        centerButton.Pressed += () => EmitSignal(SignalName.ActionPressed);
        
        // Visual feedback
        upButton.ButtonDown += () => buttonPressTimers[upButton] = 0.2f;
        downButton.ButtonDown += () => buttonPressTimers[downButton] = 0.2f;
        leftButton.ButtonDown += () => buttonPressTimers[leftButton] = 0.2f;
        rightButton.ButtonDown += () => buttonPressTimers[rightButton] = 0.2f;
        centerButton.ButtonDown += () => buttonPressTimers[centerButton] = 0.2f;
    }
    
    private void OnDirectionPressed(Vector2I direction)
    {
        EmitSignal(SignalName.DirectionPressed, direction);
        
        // Haptic feedback on mobile
        if (OS.HasFeature("mobile"))
        {
            Input.VibrateHandheld(50);
        }
    }
    
    public override void _Process(double delta)
    {
        // Update button press animations
        var keys = new List<TextureButton>(buttonPressTimers.Keys);
        foreach (var button in keys)
        {
            if (buttonPressTimers[button] > 0)
            {
                buttonPressTimers[button] -= (float)delta;
                
                // Pulse effect
                var scale = 1.0f + Mathf.Sin(buttonPressTimers[button] * 20.0f) * 0.1f;
                button.Scale = Vector2.One * scale;
                
                if (buttonPressTimers[button] <= 0)
                {
                    button.Scale = Vector2.One;
                }
            }
        }
    }
    
    private void ConfigureForTouch()
    {
        // Increase button sizes for touch
        var touchScale = 1.2f;
        Scale = Vector2.One * touchScale;
        
        // Add touch areas around buttons
        foreach (var button in new[] { upButton, downButton, leftButton, rightButton, centerButton })
        {
            var touchArea = new Control();
            touchArea.CustomMinimumSize = button.CustomMinimumSize * 1.3f;
            touchArea.Position = button.Position - (touchArea.CustomMinimumSize - button.CustomMinimumSize) / 2;
            touchArea.GuiInput += (InputEvent @event) =>
            {
                if (@event is InputEventScreenTouch touchEvent && touchEvent.Pressed)
                {
                    button.EmitSignal(TextureButton.SignalName.Pressed);
                    button.EmitSignal(TextureButton.SignalName.ButtonDown);
                }
            };
            
            button.GetParent().AddChild(touchArea);
            button.GetParent().MoveChild(touchArea, 0); // Behind button
        }
    }
    
    public void SetEnabled(bool enabled)
    {
        upButton.Disabled = !enabled;
        downButton.Disabled = !enabled;
        leftButton.Disabled = !enabled;
        rightButton.Disabled = !enabled;
        centerButton.Disabled = !enabled;
        
        Modulate = enabled ? Colors.White : new Color(1, 1, 1, 0.5f);
    }
    
    // Alternative input methods
    public override void _Input(InputEvent @event)
    {
        if (!IsVisibleInTree()) return;
        
        // Keyboard shortcuts
        if (@event.IsActionPressed("ui_up"))
            OnDirectionPressed(Vector2I.Up);
        else if (@event.IsActionPressed("ui_down"))
            OnDirectionPressed(Vector2I.Down);
        else if (@event.IsActionPressed("ui_left"))
            OnDirectionPressed(Vector2I.Left);
        else if (@event.IsActionPressed("ui_right"))
            OnDirectionPressed(Vector2I.Right);
        else if (@event.IsActionPressed("ui_accept"))
            EmitSignal(SignalName.ActionPressed);
    }
}