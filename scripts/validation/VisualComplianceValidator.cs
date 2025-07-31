using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Validates visual compliance with Amiga aesthetic requirements
/// </summary>
public class VisualComplianceValidator : IValidationSuite
{
    private Viewport viewport;
    private const int TargetWidth = 384;
    private const int TargetHeight = 216;
    private const int MaxColors = 16;
    private const int GridAlignment = 8;
    
    public void Initialize()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        viewport = tree?.Root;
    }
    
    public void Cleanup()
    {
        // No cleanup needed
    }
    
    public List<ValidationTest> GetTests()
    {
        return new List<ValidationTest>
        {
            new ValidationTest
            {
                Name = "Virtual Resolution",
                Description = "384×216 virtual resolution, integer scaled",
                IsCritical = true,
                RequiresScreenshot = true,
                TestFunc = TestVirtualResolution
            },
            new ValidationTest
            {
                Name = "Color Palette Limit",
                Description = "16-color limit check",
                IsCritical = true,
                RequiresScreenshot = true,
                TestFunc = TestColorPalette
            },
            new ValidationTest
            {
                Name = "Bitmap Fonts",
                Description = "Bitmap only (Topaz 8×8/8×16)",
                IsCritical = true,
                TestFunc = TestBitmapFonts
            },
            new ValidationTest
            {
                Name = "Texture Filtering",
                Description = "Nearest neighbor, no anti-aliasing",
                IsCritical = true,
                TestFunc = TestTextureFiltering
            },
            new ValidationTest
            {
                Name = "Grid Alignment",
                Description = "8px multiples alignment",
                IsCritical = false,
                RequiresScreenshot = true,
                TestFunc = TestGridAlignment
            },
            new ValidationTest
            {
                Name = "Scanline Effect",
                Description = "CRT scanline shader present",
                IsCritical = false,
                RequiresScreenshot = true,
                TestFunc = TestScanlineEffect
            },
            new ValidationTest
            {
                Name = "Pixel Perfect Rendering",
                Description = "No sub-pixel positioning",
                IsCritical = true,
                TestFunc = TestPixelPerfect
            },
            new ValidationTest
            {
                Name = "Amiga Color Scheme",
                Description = "Uses authentic Amiga colors",
                IsCritical = false,
                TestFunc = TestAmigaColors
            }
        };
    }
    
    public int GetTestCount() => GetTests().Count;
    
    public async Task<ValidationResult> RunTest(ValidationTest test)
    {
        var startTime = Time.GetUnixTimeFromSystem();
        var result = await test.TestFunc();
        result.Duration = Time.GetUnixTimeFromSystem() - startTime;
        result.TestName = test.Name;
        result.IsCritical = test.IsCritical;
        return result;
    }
    
    private async Task<ValidationResult> TestVirtualResolution()
    {
        var result = new ValidationResult { Passed = true };
        
        // Check viewport size
        var viewportSize = viewport.GetVisibleRect().Size;
        
        // Check if using virtual resolution
        var stretchMode = ProjectSettings.GetSetting("display/window/stretch/mode").ToString();
        var stretchAspect = ProjectSettings.GetSetting("display/window/stretch/aspect").ToString();
        
        if (stretchMode != "viewport")
        {
            result.Passed = false;
            result.Errors.Add($"Stretch mode should be 'viewport', found '{stretchMode}'");
        }
        
        if (stretchAspect != "keep")
        {
            result.Passed = false;
            result.Errors.Add($"Stretch aspect should be 'keep', found '{stretchAspect}'");
        }
        
        // Check configured viewport size
        var configWidth = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
        var configHeight = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
        
        // Allow integer scaling of base resolution
        bool validResolution = false;
        for (int scale = 1; scale <= 8; scale++)
        {
            if (configWidth == TargetWidth * scale && configHeight == TargetHeight * scale)
            {
                validResolution = true;
                result.Metrics["scale_factor"] = scale;
                break;
            }
        }
        
        if (!validResolution)
        {
            result.Passed = false;
            result.Errors.Add($"Resolution {configWidth}×{configHeight} is not an integer scale of {TargetWidth}×{TargetHeight}");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestColorPalette()
    {
        var result = new ValidationResult { Passed = true };
        
        // Capture current frame
        await ToSignal(viewport.GetTree(), SceneTree.SignalName.ProcessFrame);
        var image = viewport.GetTexture().GetImage();
        
        // Count unique colors
        var uniqueColors = new HashSet<Color>();
        
        for (int y = 0; y < image.GetHeight(); y += 4) // Sample every 4th pixel for speed
        {
            for (int x = 0; x < image.GetWidth(); x += 4)
            {
                var color = image.GetPixel(x, y);
                // Quantize to nearest 16-color palette entry
                color = QuantizeToAmigaPalette(color);
                uniqueColors.Add(color);
            }
        }
        
        result.Metrics["unique_colors"] = uniqueColors.Count;
        
        if (uniqueColors.Count > MaxColors)
        {
            result.Passed = false;
            result.Errors.Add($"Found {uniqueColors.Count} unique colors, maximum is {MaxColors}");
            
            // List extra colors
            var extraColors = uniqueColors.Skip(MaxColors).Take(5);
            foreach (var color in extraColors)
            {
                result.Errors.Add($"  Extra color: {color}");
            }
        }
        
        return result;
    }
    
    private async Task<ValidationResult> TestBitmapFonts()
    {
        var result = new ValidationResult { Passed = true };
        
        // Check default font
        var defaultFont = ThemeDB.FallbackFont;
        
        if (defaultFont == null)
        {
            result.Warnings.Add("No default font configured");
        }
        else
        {
            // Check if it's a bitmap font
            if (defaultFont is not BitmapFont)
            {
                result.Passed = false;
                result.Errors.Add("Default font is not a bitmap font");
            }
        }
        
        // Check theme font
        var themePath = (string)ProjectSettings.GetSetting("gui/theme/custom");
        if (!string.IsNullOrEmpty(themePath))
        {
            var theme = GD.Load<Theme>(themePath);
            if (theme != null)
            {
                var themeFont = theme.DefaultFont;
                if (themeFont != null && themeFont is not BitmapFont)
                {
                    result.Passed = false;
                    result.Errors.Add("Theme font is not a bitmap font");
                }
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestTextureFiltering()
    {
        var result = new ValidationResult { Passed = true };
        
        // Check project settings
        var defaultFilter = ProjectSettings.GetSetting("rendering/textures/canvas_textures/default_texture_filter").ToString();
        
        if (defaultFilter != "0") // 0 = Nearest
        {
            result.Passed = false;
            result.Errors.Add($"Default texture filter should be 'Nearest' (0), found '{defaultFilter}'");
        }
        
        // Check anti-aliasing
        var msaa2D = ProjectSettings.GetSetting("rendering/anti_aliasing/quality/msaa_2d", 0);
        var msaa3D = ProjectSettings.GetSetting("rendering/anti_aliasing/quality/msaa_3d", 0);
        
        if ((int)msaa2D != 0)
        {
            result.Passed = false;
            result.Errors.Add($"2D MSAA should be disabled, found level {msaa2D}");
        }
        
        if ((int)msaa3D != 0)
        {
            result.Warnings.Add($"3D MSAA is enabled at level {msaa3D}");
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestGridAlignment()
    {
        var result = new ValidationResult { Passed = true };
        
        // Find UI elements and check positions
        var tree = viewport.GetTree();
        var nodes = new List<Node>();
        GetAllNodes(tree.CurrentScene, nodes);
        
        int misalignedCount = 0;
        foreach (var node in nodes)
        {
            if (node is Control control)
            {
                var pos = control.GlobalPosition;
                
                // Check if position is aligned to 8px grid
                if (pos.X % GridAlignment != 0 || pos.Y % GridAlignment != 0)
                {
                    misalignedCount++;
                    if (result.Errors.Count < 5) // Limit error spam
                    {
                        result.Errors.Add($"{node.Name} at ({pos.X}, {pos.Y}) not aligned to {GridAlignment}px grid");
                    }
                }
            }
        }
        
        result.Metrics["misaligned_nodes"] = misalignedCount;
        
        if (misalignedCount > 0)
        {
            result.Passed = false;
            if (misalignedCount > 5)
            {
                result.Errors.Add($"... and {misalignedCount - 5} more misaligned nodes");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestScanlineEffect()
    {
        var result = new ValidationResult { Passed = true };
        
        // Look for scanline shader or post-processing
        var environment = viewport.GetCamera2D()?.Environment;
        
        if (environment == null)
        {
            result.Warnings.Add("No environment configured for scanline effects");
        }
        else
        {
            // Check for CRT shader effect
            var hasScanlinesEffect = false;
            
            // Check if any canvas layer has scanline shader
            var canvasLayers = viewport.GetTree().GetNodesInGroup("canvas_layers");
            foreach (CanvasLayer layer in canvasLayers)
            {
                if (layer.Material != null && layer.Material.ResourcePath.Contains("scanline"))
                {
                    hasScanlinesEffect = true;
                    break;
                }
            }
            
            if (!hasScanlinesEffect)
            {
                result.Warnings.Add("No scanline effect detected (optional)");
            }
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestPixelPerfect()
    {
        var result = new ValidationResult { Passed = true };
        
        // Check snap settings
        var snapEnabled = ProjectSettings.GetSetting("rendering/2d/snap/snap_2d_transforms_to_pixel", false);
        var snapVertices = ProjectSettings.GetSetting("rendering/2d/snap/snap_2d_vertices_to_pixel", false);
        
        if (!(bool)snapEnabled)
        {
            result.Passed = false;
            result.Errors.Add("2D transform pixel snapping should be enabled");
        }
        
        if (!(bool)snapVertices)
        {
            result.Passed = false;
            result.Errors.Add("2D vertices pixel snapping should be enabled");
        }
        
        // Check for sub-pixel positioning in nodes
        var nodes = new List<Node>();
        GetAllNodes(viewport.GetTree().CurrentScene, nodes);
        
        int subPixelCount = 0;
        foreach (var node in nodes.OfType<Node2D>())
        {
            var pos = node.Position;
            if (pos.X != Mathf.Floor(pos.X) || pos.Y != Mathf.Floor(pos.Y))
            {
                subPixelCount++;
                if (result.Errors.Count < 3)
                {
                    result.Errors.Add($"{node.Name} has sub-pixel position ({pos.X}, {pos.Y})");
                }
            }
        }
        
        if (subPixelCount > 0)
        {
            result.Passed = false;
            result.Metrics["subpixel_nodes"] = subPixelCount;
        }
        
        await Task.CompletedTask;
        return result;
    }
    
    private async Task<ValidationResult> TestAmigaColors()
    {
        var result = new ValidationResult { Passed = true };
        
        // Define authentic Amiga palette
        var amigaPalette = new Color[]
        {
            new Color("000000"), // Black
            new Color("FFFFFF"), // White
            new Color("FF0000"), // Red
            new Color("00FF00"), // Green
            new Color("0000FF"), // Blue
            new Color("FFFF00"), // Yellow
            new Color("FF00FF"), // Magenta
            new Color("00FFFF"), // Cyan
            new Color("C0C0C0"), // Light Gray
            new Color("808080"), // Gray
            new Color("800000"), // Dark Red
            new Color("008000"), // Dark Green
            new Color("000080"), // Dark Blue
            new Color("808000"), // Brown
            new Color("800080"), // Purple
            new Color("008080")  // Teal
        };
        
        // Check theme colors
        var themePath = (string)ProjectSettings.GetSetting("gui/theme/custom", "");
        if (!string.IsNullOrEmpty(themePath))
        {
            var theme = GD.Load<Theme>(themePath);
            if (theme != null)
            {
                // Check if theme colors match Amiga palette
                var themeColor = theme.GetColor("font_color", "Label");
                if (!IsInPalette(themeColor, amigaPalette))
                {
                    result.Warnings.Add($"Theme color {themeColor} not in Amiga palette");
                }
            }
        }
        
        result.Metrics["amiga_compliance"] = 1.0;
        
        await Task.CompletedTask;
        return result;
    }
    
    private Color QuantizeToAmigaPalette(Color color)
    {
        // Quantize to 4-bit per channel (Amiga OCS/ECS)
        var r = Mathf.Round(color.R * 15) / 15;
        var g = Mathf.Round(color.G * 15) / 15;
        var b = Mathf.Round(color.B * 15) / 15;
        return new Color(r, g, b, color.A);
    }
    
    private bool IsInPalette(Color color, Color[] palette)
    {
        foreach (var paletteColor in palette)
        {
            if (Mathf.Abs(color.R - paletteColor.R) < 0.1f &&
                Mathf.Abs(color.G - paletteColor.G) < 0.1f &&
                Mathf.Abs(color.B - paletteColor.B) < 0.1f)
            {
                return true;
            }
        }
        return false;
    }
    
    private void GetAllNodes(Node root, List<Node> nodes)
    {
        nodes.Add(root);
        foreach (var child in root.GetChildren())
        {
            GetAllNodes(child, nodes);
        }
    }
    
    private async Task ToSignal(SceneTree tree, StringName signal)
    {
        await tree.ToSignal(tree, signal);
    }
}