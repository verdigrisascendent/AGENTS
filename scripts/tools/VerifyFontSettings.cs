using Godot;

/// <summary>
/// Tool script to verify and fix font import settings
/// </summary>
[Tool]
public partial class VerifyFontSettings : EditorScript
{
    public override void _Run()
    {
        GD.Print("=== Verifying Font Import Settings ===");
        
        var fontPaths = new string[]
        {
            "res://assets/fonts/amiga-topaz.ttf",
            "res://assets/fonts/topaz8.tres",
            "res://assets/fonts/topaz16.tres"
        };
        
        foreach (var path in fontPaths)
        {
            if (ResourceLoader.Exists(path))
            {
                var resource = GD.Load(path);
                
                if (resource is FontFile font)
                {
                    GD.Print($"\nChecking: {path}");
                    GD.Print($"  Antialiasing: {font.Antialiasing} (should be None)");
                    GD.Print($"  Subpixel Positioning: {font.SubpixelPositioning} (should be Disabled)");
                    GD.Print($"  Hinting: {font.Hinting} (should be None)");
                    GD.Print($"  Generate Mipmaps: {font.GenerateMipmaps} (should be false)");
                    
                    // Fix settings if needed
                    bool needsSave = false;
                    
                    if (font.Antialiasing != Font.AntialiasingMode.None)
                    {
                        font.Antialiasing = Font.AntialiasingMode.None;
                        needsSave = true;
                    }
                    
                    if (font.SubpixelPositioning != Font.SubpixelPositioning.Disabled)
                    {
                        font.SubpixelPositioning = Font.SubpixelPositioning.Disabled;
                        needsSave = true;
                    }
                    
                    if (font.Hinting != Font.Hinting.None)
                    {
                        font.Hinting = Font.Hinting.None;
                        needsSave = true;
                    }
                    
                    if (font.GenerateMipmaps)
                    {
                        font.GenerateMipmaps = false;
                        needsSave = true;
                    }
                    
                    if (needsSave)
                    {
                        ResourceSaver.Save(font, path);
                        GD.Print($"  ✓ Fixed and saved font settings");
                    }
                    else
                    {
                        GD.Print($"  ✓ Font settings are correct");
                    }
                }
            }
            else
            {
                GD.PrintErr($"Font not found: {path}");
            }
        }
        
        GD.Print("\n=== Font Verification Complete ===");
    }
}