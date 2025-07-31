using Godot;
using System.Collections.Generic;

/// <summary>
/// Loads and manages Amiga-style bitmap fonts
/// </summary>
public partial class AmigaFontLoader : RefCounted
{
    // Topaz font bitmap data (8x8 characters)
    // This would normally be loaded from an actual bitmap font file
    private static readonly Dictionary<char, int[]> TopazGlyphs = new()
    {
        ['A'] = new[] { 0x18, 0x3C, 0x66, 0x7E, 0x66, 0x66, 0x66, 0x00 },
        ['B'] = new[] { 0x7C, 0x66, 0x66, 0x7C, 0x66, 0x66, 0x7C, 0x00 },
        ['C'] = new[] { 0x3C, 0x66, 0x60, 0x60, 0x60, 0x66, 0x3C, 0x00 },
        ['D'] = new[] { 0x78, 0x6C, 0x66, 0x66, 0x66, 0x6C, 0x78, 0x00 },
        ['E'] = new[] { 0x7E, 0x60, 0x60, 0x78, 0x60, 0x60, 0x7E, 0x00 },
        ['F'] = new[] { 0x7E, 0x60, 0x60, 0x78, 0x60, 0x60, 0x60, 0x00 },
        ['G'] = new[] { 0x3C, 0x66, 0x60, 0x6E, 0x66, 0x66, 0x3C, 0x00 },
        ['H'] = new[] { 0x66, 0x66, 0x66, 0x7E, 0x66, 0x66, 0x66, 0x00 },
        ['I'] = new[] { 0x3C, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3C, 0x00 },
        ['J'] = new[] { 0x1E, 0x0C, 0x0C, 0x0C, 0x0C, 0x6C, 0x38, 0x00 },
        ['K'] = new[] { 0x66, 0x6C, 0x78, 0x70, 0x78, 0x6C, 0x66, 0x00 },
        ['L'] = new[] { 0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x7E, 0x00 },
        ['M'] = new[] { 0x63, 0x77, 0x7F, 0x6B, 0x63, 0x63, 0x63, 0x00 },
        ['N'] = new[] { 0x66, 0x76, 0x7E, 0x7E, 0x6E, 0x66, 0x66, 0x00 },
        ['O'] = new[] { 0x3C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x00 },
        ['P'] = new[] { 0x7C, 0x66, 0x66, 0x7C, 0x60, 0x60, 0x60, 0x00 },
        ['Q'] = new[] { 0x3C, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x0E, 0x00 },
        ['R'] = new[] { 0x7C, 0x66, 0x66, 0x7C, 0x78, 0x6C, 0x66, 0x00 },
        ['S'] = new[] { 0x3C, 0x66, 0x60, 0x3C, 0x06, 0x66, 0x3C, 0x00 },
        ['T'] = new[] { 0x7E, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x00 },
        ['U'] = new[] { 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x00 },
        ['V'] = new[] { 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x18, 0x00 },
        ['W'] = new[] { 0x63, 0x63, 0x63, 0x6B, 0x7F, 0x77, 0x63, 0x00 },
        ['X'] = new[] { 0x66, 0x66, 0x3C, 0x18, 0x3C, 0x66, 0x66, 0x00 },
        ['Y'] = new[] { 0x66, 0x66, 0x66, 0x3C, 0x18, 0x18, 0x18, 0x00 },
        ['Z'] = new[] { 0x7E, 0x06, 0x0C, 0x18, 0x30, 0x60, 0x7E, 0x00 },
        [' '] = new[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
        ['0'] = new[] { 0x3C, 0x66, 0x6E, 0x76, 0x66, 0x66, 0x3C, 0x00 },
        ['1'] = new[] { 0x18, 0x18, 0x38, 0x18, 0x18, 0x18, 0x7E, 0x00 },
        ['2'] = new[] { 0x3C, 0x66, 0x06, 0x0C, 0x30, 0x60, 0x7E, 0x00 },
        ['3'] = new[] { 0x3C, 0x66, 0x06, 0x1C, 0x06, 0x66, 0x3C, 0x00 },
        ['4'] = new[] { 0x06, 0x0E, 0x1E, 0x66, 0x7F, 0x06, 0x06, 0x00 },
        ['5'] = new[] { 0x7E, 0x60, 0x7C, 0x06, 0x06, 0x66, 0x3C, 0x00 },
        ['6'] = new[] { 0x3C, 0x66, 0x60, 0x7C, 0x66, 0x66, 0x3C, 0x00 },
        ['7'] = new[] { 0x7E, 0x66, 0x0C, 0x18, 0x18, 0x18, 0x18, 0x00 },
        ['8'] = new[] { 0x3C, 0x66, 0x66, 0x3C, 0x66, 0x66, 0x3C, 0x00 },
        ['9'] = new[] { 0x3C, 0x66, 0x66, 0x3E, 0x06, 0x66, 0x3C, 0x00 },
        ['.'] = new[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x18, 0x00 },
        [','] = new[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x18, 0x30 },
        ['!'] = new[] { 0x18, 0x18, 0x18, 0x18, 0x00, 0x00, 0x18, 0x00 },
        ['?'] = new[] { 0x3C, 0x66, 0x06, 0x0C, 0x18, 0x00, 0x18, 0x00 },
    };
    
    public static BitmapFont CreateTopazFont(int size = 8)
    {
        var font = new BitmapFont();
        var texture = CreateFontTexture();
        
        // Add texture to font
        font.AddTexture(texture);
        
        // Set up font properties
        font.Height = size;
        
        // Add each character
        int charIndex = 0;
        foreach (var kvp in TopazGlyphs)
        {
            var rect = new Rect2(charIndex * 8, 0, 8, 8);
            font.AddChar(kvp.Key, 0, rect);
            charIndex++;
        }
        
        return font;
    }
    
    private static ImageTexture CreateFontTexture()
    {
        // Create image for all characters
        var image = Image.Create(TopazGlyphs.Count * 8, 8, false, Image.Format.Rgba8);
        
        // Fill with transparent
        image.Fill(Colors.Transparent);
        
        // Draw each character
        int xOffset = 0;
        foreach (var kvp in TopazGlyphs)
        {
            DrawCharacter(image, xOffset, kvp.Value);
            xOffset += 8;
        }
        
        // Create texture with nearest neighbor filtering
        var texture = ImageTexture.CreateFromImage(image);
        return texture;
    }
    
    private static void DrawCharacter(Image image, int xOffset, int[] glyphData)
    {
        for (int y = 0; y < 8; y++)
        {
            int row = glyphData[y];
            for (int x = 0; x < 8; x++)
            {
                // Check if bit is set (draw from left to right)
                if ((row & (0x80 >> x)) != 0)
                {
                    image.SetPixel(xOffset + x, y, Colors.White);
                }
            }
        }
    }
    
    public static void CreateTopazFontResource()
    {
        // Load the actual Topaz TTF font
        var topazFont = GD.Load<FontFile>("res://assets/fonts/amiga-topaz.ttf");
        
        if (topazFont != null)
        {
            // Configure font settings for authentic bitmap appearance
            topazFont.Antialiasing = Font.AntialiasingMode.None;
            topazFont.SubpixelPositioning = Font.SubpixelPositioning.Disabled;
            topazFont.Hinting = Font.Hinting.None;
            topazFont.Oversampling = 0.0f;
            topazFont.GenerateMipmaps = false;
            
            GD.Print("[AmigaFontLoader] Configured Topaz font with bitmap settings");
        }
        else
        {
            GD.PrintErr("[AmigaFontLoader] Failed to load Topaz font file");
            
            // Fallback to generated bitmap font
            var font8 = CreateTopazFont(8);
            var font16 = CreateTopazFont(16);
            
            ResourceSaver.Save(font8, "res://assets/fonts/topaz8_fallback.tres");
            ResourceSaver.Save(font16, "res://assets/fonts/topaz16_fallback.tres");
        }
    }
    
    public static void ValidateFontSettings(FontFile font)
    {
        // Ensure font has proper bitmap settings
        if (font.Antialiasing != Font.AntialiasingMode.None)
        {
            GD.PrintErr("[AmigaFontLoader] Warning: Font has antialiasing enabled!");
        }
        
        if (font.SubpixelPositioning != Font.SubpixelPositioning.Disabled)
        {
            GD.PrintErr("[AmigaFontLoader] Warning: Font has subpixel positioning enabled!");
        }
    }
}