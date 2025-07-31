using Godot;
using System.Collections.Generic;

/// <summary>
/// Shader Effects Artist - Creates authentic retro CRT effects and transitions
/// </summary>
public partial class ShaderEffectsArtist : Node, ISpecializedAgent
{
    private MegaAgent megaAgent;
    private Dictionary<string, ShaderMaterial> activeShaders = new();
    
    public void Initialize(MegaAgent mega)
    {
        megaAgent = mega;
        GD.Print("[ShaderEffectsArtist] Initialized - Creating retro visual effects");
    }
    
    public object Execute(string task, Dictionary<string, object> parameters)
    {
        return task switch
        {
            "create_scanline_shader" => CreateScanlineShader(parameters),
            "create_phosphor_glow" => CreatePhosphorGlowShader(parameters),
            "create_boot_sequence" => CreateBootSequenceShader(parameters),
            "apply_crt_effect" => ApplyCRTEffect(parameters),
            "trigger_collapse_effect" => TriggerCollapseEffect(parameters),
            _ => null
        };
    }
    
    private ShaderMaterial CreateScanlineShader(Dictionary<string, object> parameters)
    {
        var shader = new Shader();
        shader.Code = @"
shader_type canvas_item;

uniform float scanline_count : hint_range(50.0, 500.0, 1.0) = 220.0;
uniform float scanline_intensity : hint_range(0.0, 1.0, 0.01) = 0.15;
uniform float scanline_speed : hint_range(0.0, 10.0, 0.1) = 2.0;
uniform float brightness : hint_range(0.5, 1.5, 0.01) = 0.95;

void fragment() {
    vec4 color = texture(TEXTURE, UV);
    
    // Calculate scanline
    float scanline = sin(UV.y * scanline_count + TIME * scanline_speed);
    scanline = (scanline + 1.0) * 0.5; // normalize to 0-1
    scanline = mix(1.0 - scanline_intensity, 1.0, scanline);
    
    // Apply scanline and brightness adjustment
    color.rgb *= scanline * brightness;
    
    // Subtle vignette for CRT feel
    vec2 center = UV - vec2(0.5);
    float vignette = 1.0 - dot(center, center) * 0.3;
    color.rgb *= vignette;
    
    COLOR = color;
}
";
        
        var material = new ShaderMaterial();
        material.Shader = shader;
        material.SetShaderParameter("scanline_count", 220.0);
        material.SetShaderParameter("scanline_intensity", 0.15);
        material.SetShaderParameter("scanline_speed", 2.0);
        material.SetShaderParameter("brightness", 0.95);
        
        activeShaders["scanline"] = material;
        GD.Print("[ShaderEffectsArtist] Created scanline shader");
        return material;
    }
    
    private ShaderMaterial CreatePhosphorGlowShader(Dictionary<string, object> parameters)
    {
        var shader = new Shader();
        shader.Code = @"
shader_type canvas_item;

uniform float glow_strength : hint_range(0.0, 2.0, 0.01) = 0.3;
uniform float phosphor_decay : hint_range(0.0, 0.99, 0.01) = 0.85;
uniform vec3 phosphor_color = vec3(0.1, 1.0, 0.1); // Green phosphor
uniform sampler2D previous_frame;

void fragment() {
    vec4 current = texture(TEXTURE, UV);
    vec4 previous = texture(previous_frame, UV);
    
    // Phosphor persistence effect
    vec3 glow = previous.rgb * phosphor_decay;
    
    // Add green tint to bright areas
    float brightness = dot(current.rgb, vec3(0.299, 0.587, 0.114));
    vec3 phosphor_tint = phosphor_color * brightness * glow_strength;
    
    // Combine current frame with phosphor glow
    current.rgb = max(current.rgb, glow);
    current.rgb += phosphor_tint;
    
    // Bloom effect for bright pixels
    if (brightness > 0.7) {
        current.rgb += vec3(0.05, 0.1, 0.05) * (brightness - 0.7);
    }
    
    COLOR = current;
}
";
        
        var material = new ShaderMaterial();
        material.Shader = shader;
        material.SetShaderParameter("glow_strength", 0.3);
        material.SetShaderParameter("phosphor_decay", 0.85);
        material.SetShaderParameter("phosphor_color", new Vector3(0.1f, 1.0f, 0.1f));
        
        activeShaders["phosphor_glow"] = material;
        GD.Print("[ShaderEffectsArtist] Created phosphor glow shader");
        return material;
    }
    
    private ShaderMaterial CreateBootSequenceShader(Dictionary<string, object> parameters)
    {
        var shader = new Shader();
        shader.Code = @"
shader_type canvas_item;

uniform float boot_progress : hint_range(0.0, 1.0, 0.01) = 0.0;
uniform float scan_position : hint_range(0.0, 1.0, 0.01) = 0.0;
uniform vec4 scan_color : source_color = vec4(0.0, 0.8, 0.8, 1.0);
uniform float noise_amount : hint_range(0.0, 1.0, 0.01) = 0.3;

// Simple noise function
float random(vec2 st) {
    return fract(sin(dot(st.xy, vec2(12.9898, 78.233))) * 43758.5453123);
}

void fragment() {
    vec4 color = texture(TEXTURE, UV);
    
    // Boot sequence stages
    if (boot_progress < 0.3) {
        // Stage 1: Black with noise
        float noise = random(UV + TIME) * noise_amount;
        COLOR = vec4(vec3(noise * 0.1), 1.0);
    }
    else if (boot_progress < 0.6) {
        // Stage 2: Scanline wipe from top
        float wipe_pos = (boot_progress - 0.3) / 0.3;
        if (UV.y < wipe_pos) {
            // Add scanning line effect
            float line_dist = abs(UV.y - wipe_pos);
            if (line_dist < 0.02) {
                color = mix(color, scan_color, 1.0 - line_dist / 0.02);
            }
            COLOR = color;
        } else {
            COLOR = vec4(0.0, 0.0, 0.0, 1.0);
        }
    }
    else {
        // Stage 3: Fade in content
        float fade = (boot_progress - 0.6) / 0.4;
        color.a *= fade;
        COLOR = color;
    }
}
";
        
        var material = new ShaderMaterial();
        material.Shader = shader;
        material.SetShaderParameter("boot_progress", 0.0);
        material.SetShaderParameter("scan_position", 0.0);
        material.SetShaderParameter("scan_color", new Color(0.0f, 0.8f, 0.8f, 1.0f));
        material.SetShaderParameter("noise_amount", 0.3);
        
        activeShaders["boot_sequence"] = material;
        GD.Print("[ShaderEffectsArtist] Created boot sequence shader");
        return material;
    }
    
    private Dictionary<string, object> ApplyCRTEffect(Dictionary<string, object> parameters)
    {
        var targetNode = parameters["target"] as CanvasItem;
        if (targetNode == null)
            return new Dictionary<string, object> { ["success"] = false };
        
        // Combine scanline and phosphor effects
        var crtShader = new Shader();
        crtShader.Code = @"
shader_type canvas_item;

// Scanline parameters
uniform float scanline_count : hint_range(50.0, 500.0, 1.0) = 220.0;
uniform float scanline_intensity : hint_range(0.0, 1.0, 0.01) = 0.15;
uniform float scanline_speed : hint_range(0.0, 10.0, 0.1) = 2.0;

// CRT curvature
uniform float curvature : hint_range(0.0, 0.5, 0.01) = 0.02;

// Phosphor glow
uniform float glow_strength : hint_range(0.0, 2.0, 0.01) = 0.2;
uniform vec3 phosphor_tint = vec3(0.05, 0.1, 0.05);

// Overall adjustments
uniform float brightness : hint_range(0.5, 1.5, 0.01) = 0.92;
uniform float contrast : hint_range(0.5, 2.0, 0.01) = 1.1;

vec2 curve_uv(vec2 uv) {
    uv = uv * 2.0 - 1.0;
    vec2 offset = abs(uv.yx) / vec2(6.0, 4.0);
    uv = uv + uv * offset * offset * curvature;
    uv = uv * 0.5 + 0.5;
    return uv;
}

void fragment() {
    vec2 curved_uv = curve_uv(UV);
    
    // Sample with curved coordinates
    if (curved_uv.x < 0.0 || curved_uv.x > 1.0 || curved_uv.y < 0.0 || curved_uv.y > 1.0) {
        COLOR = vec4(0.0, 0.0, 0.0, 1.0);
        return;
    }
    
    vec4 color = texture(TEXTURE, curved_uv);
    
    // Scanlines
    float scanline = sin(curved_uv.y * scanline_count + TIME * scanline_speed);
    scanline = (scanline + 1.0) * 0.5;
    scanline = mix(1.0 - scanline_intensity, 1.0, scanline);
    
    // Apply scanline
    color.rgb *= scanline;
    
    // Phosphor glow on bright areas
    float luma = dot(color.rgb, vec3(0.299, 0.587, 0.114));
    if (luma > 0.6) {
        color.rgb += phosphor_tint * (luma - 0.6) * glow_strength;
    }
    
    // Vignette
    vec2 center = curved_uv - vec2(0.5);
    float vignette = 1.0 - dot(center, center) * 0.4;
    color.rgb *= vignette;
    
    // Brightness and contrast
    color.rgb = (color.rgb - 0.5) * contrast + 0.5;
    color.rgb *= brightness;
    
    COLOR = color;
}
";
        
        var material = new ShaderMaterial();
        material.Shader = crtShader;
        material.SetShaderParameter("scanline_count", 220.0);
        material.SetShaderParameter("scanline_intensity", 0.15);
        material.SetShaderParameter("curvature", 0.02);
        material.SetShaderParameter("brightness", 0.92);
        
        targetNode.Material = material;
        activeShaders["crt_combined"] = material;
        
        GD.Print("[ShaderEffectsArtist] Applied CRT effect to target node");
        return new Dictionary<string, object> { ["success"] = true, ["material"] = material };
    }
    
    private void TriggerCollapseEffect(Dictionary<string, object> parameters)
    {
        // Find all active CRT shaders and modify them for collapse
        foreach (var kvp in activeShaders)
        {
            var material = kvp.Value;
            
            // Add red tint and increase noise
            material.SetShaderParameter("phosphor_tint", new Vector3(0.3f, 0.05f, 0.05f));
            material.SetShaderParameter("scanline_intensity", 0.25);
            material.SetShaderParameter("scanline_speed", 8.0);
            material.SetShaderParameter("brightness", 0.85);
        }
        
        GD.Print("[ShaderEffectsArtist] Triggered collapse effect on all shaders");
    }
}