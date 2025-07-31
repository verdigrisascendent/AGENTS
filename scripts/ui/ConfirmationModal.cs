using Godot;

/// <summary>
/// Confirmation Modal - Simple yes/no dialog with Amiga aesthetic
/// </summary>
public partial class ConfirmationModal : AmigaModal
{
    [Signal]
    public delegate void ConfirmedEventHandler();
    
    [Signal]
    public delegate void CancelledEventHandler();
    
    private RichTextLabel messageLabel;
    private Button confirmButton;
    private Button cancelButton;
    
    protected override void CreateModalStructure()
    {
        base.CreateModalStructure();
        
        // Set modal size
        modalPanel.CustomMinimumSize = GetTileAlignedSize(new Vector2(400, 200));
        titleLabel.Text = "CONFIRM";
        showCloseButton = false; // Force user to choose
        allowClickOutside = false;
        
        CreateConfirmContent();
    }
    
    private void CreateConfirmContent()
    {
        // Message area
        messageLabel = new RichTextLabel();
        messageLabel.BbcodeEnabled = true;
        messageLabel.FitContent = true;
        messageLabel.CustomMinimumSize = new Vector2(0, TileSize * 8);
        messageLabel.AddThemeColorOverride("default_color", Colors.White);
        contentContainer.AddChild(messageLabel);
        
        // Add spacer
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, TileSize * 2);
        spacer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        contentContainer.AddChild(spacer);
        
        // Button row
        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", TileSize * 2);
        buttonRow.Alignment = BoxContainer.AlignmentMode.Center;
        contentContainer.AddChild(buttonRow);
        
        // Cancel button
        cancelButton = new Button();
        cancelButton.Text = "CANCEL";
        cancelButton.CustomMinimumSize = new Vector2(TileSize * 16, TileSize * 5);
        cancelButton.Pressed += OnCancelPressed;
        StyleButton(cancelButton, false);
        buttonRow.AddChild(cancelButton);
        
        // Confirm button
        confirmButton = new Button();
        confirmButton.Text = "CONFIRM";
        confirmButton.CustomMinimumSize = new Vector2(TileSize * 16, TileSize * 5);
        confirmButton.Pressed += OnConfirmPressed;
        StyleButton(confirmButton, true);
        buttonRow.AddChild(confirmButton);
    }
    
    private void StyleButton(Button button, bool isPrimary)
    {
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = isPrimary ? borderColor : backgroundColor;
        buttonStyle.BorderWidthTop = 2;
        buttonStyle.BorderWidthBottom = 2;
        buttonStyle.BorderWidthLeft = 2;
        buttonStyle.BorderWidthRight = 2;
        buttonStyle.BorderColor = isPrimary ? Colors.White : borderColor;
        button.AddThemeStyleboxOverride("normal", buttonStyle);
        
        var hoverStyle = buttonStyle.Duplicate() as StyleBoxFlat;
        hoverStyle.BgColor = isPrimary ? new Color("#0066CC") : borderColor;
        button.AddThemeStyleboxOverride("hover", hoverStyle);
        
        button.AddThemeColorOverride("font_color", Colors.White);
    }
    
    public void SetMessage(string message)
    {
        messageLabel.Text = message;
    }
    
    public void SetTitle(string title)
    {
        titleLabel.Text = title.ToUpper();
    }
    
    public void SetConfirmText(string text)
    {
        confirmButton.Text = text.ToUpper();
    }
    
    public void SetCancelText(string text)
    {
        cancelButton.Text = text.ToUpper();
    }
    
    private void OnConfirmPressed()
    {
        EmitSignal(SignalName.Confirmed);
        Close();
    }
    
    private void OnCancelPressed()
    {
        EmitSignal(SignalName.Cancelled);
        Close();
    }
}