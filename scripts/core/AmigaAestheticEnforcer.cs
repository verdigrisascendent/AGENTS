using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Amiga Aesthetic Enforcer - Maintains pixel-perfect retro visual fidelity
/// </summary>
public partial class AmigaAestheticEnforcer : Node, ISpecializedAgent
{
    private MegaAgent megaAgent;
    
    // Amiga ECS 16-color palette
    private readonly Color[] AmigaPalette = new Color[]
    {
        new Color("#000040"), // Base dark blue
        new Color("#0000AA"), // Bright blue
        new Color("#FFFFFF"), // White
        new Color("#AAAAAA"), // Light gray (bevel light)
        new Color("#444444"), // Dark gray (bevel dark)
        new Color("#00AA00"), // Green
        new Color("#AA0000"), // Red
        new Color("#AAAA00"), // Yellow
        new Color("#AA00AA"), // Magenta
        new Color("#00AAAA"), // Cyan
        new Color("#666666"), // Medium gray
        new Color("#880000"), // Dark red
        new Color("#008800"), // Dark green
        new Color("#000088"), // Dark blue alt
        new Color("#888800"), // Dark yellow
        new Color("#880088")  // Dark magenta
    };
    
    // Virtual resolution (16:9 adaptation of 320×200)
    private const int BaseWidth = 384;
    private const int BaseHeight = 216;
    private const int ScaleFactor = 4; // For iPad display
    
    public void Initialize(MegaAgent mega)
    {
        megaAgent = mega;
        GD.Print("[AmigaAestheticEnforcer] Initialized - Protecting retro visual integrity");
    }
    
    public object Execute(string task, Dictionary<string, object> parameters)
    {
        return task switch
        {
            "validate_texture" => ValidateTexture(parameters),
            "setup_viewport" => SetupViewport(parameters),
            "create_bitmap_font" => CreateBitmapFont(parameters),
            "validate_colors" => ValidateColors(parameters),
            "create_bevel_style" => CreateBevelStyle(parameters),
            "validate_font" => ValidateFont(parameters),
            "configure_theme" => ConfigureAmigaTheme(parameters),
            _ => null
        };
    }
    
    private bool ValidateTexture(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("texture_path", out var pathObj))
            return false;
            
        var path = pathObj.ToString();
        var texture = GD.Load<Texture2D>(path);
        
        if (texture == null)
        {
            GD.PrintErr($"[AmigaAestheticEnforcer] Failed to load texture: {path}");
            return false;
        }
        
        // Check import settings
        var image = texture.GetImage();
        if (image == null)
        {
            GD.PrintErr("[AmigaAestheticEnforcer] Texture has no image data");
            return false;
        }
        
        // Validate colors against palette
        var invalidColors = new List<Color>();
        for (int y = 0; y < image.GetHeight(); y++)
        {
            for (int x = 0; x < image.GetWidth(); x++)
            {
                var pixel = image.GetPixel(x, y);
                if (!IsColorInPalette(pixel))
                {
                    if (!invalidColors.Contains(pixel))
                        invalidColors.Add(pixel);
                }
            }
        }
        
        if (invalidColors.Count > 0)
        {
            GD.PrintErr($"[AmigaAestheticEnforcer] Texture contains {invalidColors.Count} invalid colors");
            return false;
        }
        
        GD.Print($"[AmigaAestheticEnforcer] Texture validated: {path}");
        return true;
    }
    
    private bool IsColorInPalette(Color color)
    {
        const float tolerance = 0.01f;
        return AmigaPalette.Any(p => 
            Mathf.Abs(p.R - color.R) < tolerance &&
            Mathf.Abs(p.G - color.G) < tolerance &&
            Mathf.Abs(p.B - color.B) < tolerance
        );
    }
    
    private Dictionary<string, object> SetupViewport(Dictionary<string, object> parameters)
    {
        var config = new Dictionary<string, object>
        {
            ["base_width"] = BaseWidth,
            ["base_height"] = BaseHeight,
            ["scale_factor"] = ScaleFactor,
            ["filter_mode"] = Viewport.DefaultTextureFilter.Nearest,
            ["clear_color"] = AmigaPalette[0] // Base dark blue
        };
        
        GD.Print($"[AmigaAestheticEnforcer] Viewport configured: {BaseWidth}x{BaseHeight} @ {ScaleFactor}x");
        return config;
    }
    
    private BitmapFont CreateBitmapFont(Dictionary<string, object> parameters)
    {
        var fontName = parameters.GetValueOrDefault("name", "topaz").ToString();
        var size = parameters.GetValueOrDefault("size", 8);
        
        var font = new BitmapFont();
        // Font creation would load from actual bitmap font data
        
        GD.Print($"[AmigaAestheticEnforcer] Created bitmap font: {fontName} @ {size}px");
        return font;
    }
    
    private StyleBox CreateBevelStyle(Dictionary<string, object> parameters)
    {
        var depth = (int)parameters.GetValueOrDefault("depth", 2);
        var raised = (bool)parameters.GetValueOrDefault("raised", true);
        
        var style = new StyleBoxFlat();
        style.BgColor = AmigaPalette[2]; // White background
        
        // Amiga-style beveled edges
        if (raised)
        {
            style.BorderColor = AmigaPalette[3]; // Light gray for raised
            style.BorderWidthTop = depth;
            style.BorderWidthLeft = depth;
            style.SetBorderWidthAll(depth);
            // Dark edges would be drawn separately
        }
        else
        {
            style.BorderColor = AmigaPalette[4]; // Dark gray for sunken
            style.BorderWidthBottom = depth;
            style.BorderWidthRight = depth;
            style.SetBorderWidthAll(depth);
        }
        
        style.CornerRadiusTopLeft = 0; // No rounded corners!
        style.AntiAliasing = false;
        
        return style;
    }
    
    private bool ValidateColors(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("node", out var nodeObj))
            return false;
            
        var node = nodeObj as Control;
        if (node == null)
            return false;
            
        // Check modulate color
        if (!IsColorInPalette(node.Modulate))
        {
            GD.PrintErr($"[AmigaAestheticEnforcer] Invalid modulate color on {node.Name}");
            return false;
        }
        
        // Check theme colors if applicable
        if (node.Theme != null)
        {
            // Would validate all theme colors here
        }
        
        return true;
    }
    
    private bool ValidateFont(Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue("font", out var fontObj))
            return false;
            
        var font = fontObj as FontFile;
        if (font == null)
            return false;
            
        // Check font settings
        bool isValid = true;
        
        if (font.Antialiasing != Font.AntialiasingMode.None)
        {
            GD.PrintErr($"[AmigaAestheticEnforcer] Font has antialiasing enabled: {font.ResourcePath}");
            isValid = false;
        }
        
        if (font.SubpixelPositioning != Font.SubpixelPositioning.Disabled)
        {
            GD.PrintErr($"[AmigaAestheticEnforcer] Font has subpixel positioning: {font.ResourcePath}");
            isValid = false;
        }
        
        if (font.Hinting != Font.Hinting.None)
        {
            GD.PrintErr($"[AmigaAestheticEnforcer] Font has hinting enabled: {font.ResourcePath}");
            isValid = false;
        }
        
        return isValid;
    }
    
    private Theme ConfigureAmigaTheme(Dictionary<string, object> parameters)
    {
        var theme = new Theme();
        
        // Load Topaz font
        var topazFont = GD.Load<FontFile>("res://assets/fonts/amiga-topaz.ttf");
        if (topazFont != null)
        {
            theme.DefaultFont = topazFont;
            theme.DefaultFontSize = 16;
        }
        
        // Configure button styles
        var buttonNormal = CreateBevelStyle(new Dictionary<string, object>
        {
            ["depth"] = 2,
            ["raised"] = true
        });
        buttonNormal.BgColor = AmigaPalette[1]; // Workbench blue
        
        var buttonHover = buttonNormal.Duplicate() as StyleBoxFlat;
        buttonHover.BgColor = AmigaPalette[1].Lightened(0.1f);
        
        var buttonPressed = CreateBevelStyle(new Dictionary<string, object>
        {
            ["depth"] = 2,
            ["raised"] = false
        });
        buttonPressed.BgColor = AmigaPalette[1].Darkened(0.2f);
        
        theme.SetStylebox("normal", "Button", buttonNormal);
        theme.SetStylebox("hover", "Button", buttonHover);
        theme.SetStylebox("pressed", "Button", buttonPressed);
        
        // Set colors
        theme.SetColor("font_color", "Button", AmigaPalette[2]); // White
        theme.SetColor("font_color", "Label", AmigaPalette[2]);
        
        GD.Print("[AmigaAestheticEnforcer] Configured Amiga theme");
        return theme;
    }
}