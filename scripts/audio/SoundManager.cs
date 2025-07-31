using Godot;
using System.Collections.Generic;

/// <summary>
/// Sound Manager - Handles all game audio with Amiga-style sounds
/// </summary>
public partial class SoundManager : Node
{
    private static SoundManager instance;
    
    // Audio buses
    private const string SFXBus = "SFX";
    private const string MusicBus = "Music";
    private const string AmbienceBus = "Ambience";
    
    // Sound pools for variety
    private Dictionary<string, AudioStream[]> soundPools = new();
    private Dictionary<string, AudioStreamPlayer> loopingSounds = new();
    
    // Volume settings
    private float masterVolume = 1.0f;
    private float sfxVolume = 0.8f;
    private float musicVolume = 0.6f;
    
    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new SoundManager();
            }
            return instance;
        }
    }
    
    public override void _Ready()
    {
        instance = this;
        LoadSoundSettings();
        InitializeSoundPools();
    }
    
    private void LoadSoundSettings()
    {
        var config = new ConfigFile();
        var error = config.Load("user://settings.cfg");
        
        if (error == Error.Ok)
        {
            sfxVolume = (float)config.GetValue("audio", "sfx_volume", 0.8);
            musicVolume = (float)config.GetValue("audio", "music_volume", 0.6);
            
            ApplyVolumeSettings();
        }
    }
    
    private void ApplyVolumeSettings()
    {
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(SFXBus), 
            Mathf.LinearToDb(sfxVolume * masterVolume));
        AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex(MusicBus), 
            Mathf.LinearToDb(musicVolume * masterVolume));
    }
    
    private void InitializeSoundPools()
    {
        // Initialize sound categories
        // In a real implementation, these would be loaded from resources
        soundPools["click"] = new AudioStream[] { };
        soundPools["move"] = new AudioStream[] { };
        soundPools["light"] = new AudioStream[] { };
        soundPools["filed"] = new AudioStream[] { };
        soundPools["collapse"] = new AudioStream[] { };
        soundPools["victory"] = new AudioStream[] { };
        soundPools["defeat"] = new AudioStream[] { };
    }
    
    public void PlaySound(string soundName, float volumeDb = 0.0f, float pitch = 1.0f)
    {
        var player = new AudioStreamPlayer();
        player.Bus = SFXBus;
        player.VolumeDb = volumeDb;
        player.PitchScale = pitch;
        
        // Get sound from pool or create placeholder
        if (soundPools.ContainsKey(soundName) && soundPools[soundName].Length > 0)
        {
            var sounds = soundPools[soundName];
            var randomIndex = GD.RandRange(0, sounds.Length - 1);
            player.Stream = sounds[randomIndex];
        }
        else
        {
            // Placeholder - in real implementation would load actual sound
            GD.Print($"[SoundManager] Playing sound: {soundName}");
        }
        
        AddChild(player);
        player.Play();
        player.Finished += () => player.QueueFree();
    }
    
    public void PlayUISound(string soundName)
    {
        // UI sounds are typically quieter
        PlaySound(soundName, -6.0f);
    }
    
    public void PlayMovementSound(string terrainType = "default")
    {
        // Different sounds for different terrain
        PlaySound($"move_{terrainType}", -3.0f);
    }
    
    public void PlayLightSound(bool isOn)
    {
        if (isOn)
        {
            PlaySound("light_on", -3.0f, 1.2f);
        }
        else
        {
            PlaySound("light_off", -3.0f, 0.8f);
        }
    }
    
    public void PlayFilerSound(string action)
    {
        switch (action)
        {
            case "prowl":
                PlaySound("filer_prowl", -6.0f, 0.7f);
                break;
            case "alert":
                PlaySound("filer_alert", 0.0f, 1.0f);
                break;
            case "file":
                PlaySound("filer_file", 3.0f, 0.9f);
                break;
        }
    }
    
    public void PlayCollapseSound(string phase)
    {
        switch (phase)
        {
            case "start":
                PlaySound("collapse_alarm", 0.0f);
                StartCollapseAmbience();
                break;
            case "rumble":
                PlaySound("collapse_rumble", -3.0f);
                break;
            case "debris":
                PlaySound("collapse_debris", 0.0f);
                break;
            case "final":
                PlaySound("collapse_final", 6.0f);
                StopCollapseAmbience();
                break;
        }
    }
    
    private void StartCollapseAmbience()
    {
        if (loopingSounds.ContainsKey("collapse_ambience"))
            return;
            
        var ambience = new AudioStreamPlayer();
        ambience.Bus = AmbienceBus;
        ambience.VolumeDb = -12.0f;
        // ambience.Stream = preload("res://audio/ambience/collapse_loop.ogg");
        
        AddChild(ambience);
        ambience.Play();
        
        loopingSounds["collapse_ambience"] = ambience;
    }
    
    private void StopCollapseAmbience()
    {
        if (loopingSounds.TryGetValue("collapse_ambience", out var ambience))
        {
            var tween = CreateTween();
            tween.TweenProperty(ambience, "volume_db", -80.0f, 2.0f);
            tween.TweenCallback(Callable.From(() => {
                ambience.Stop();
                ambience.QueueFree();
                loopingSounds.Remove("collapse_ambience");
            }));
        }
    }
    
    public void PlayMemorySparkSound()
    {
        PlaySound("memory_spark", -3.0f, GD.Randf() * 0.4f + 0.8f);
    }
    
    public void PlayVictoryFanfare()
    {
        StopAllLoopingSounds();
        PlaySound("victory_fanfare", 0.0f);
    }
    
    public void PlayDefeatSound()
    {
        StopAllLoopingSounds();
        PlaySound("defeat_sound", -3.0f);
    }
    
    private void StopAllLoopingSounds()
    {
        foreach (var sound in loopingSounds.Values)
        {
            sound.Stop();
            sound.QueueFree();
        }
        loopingSounds.Clear();
    }
    
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp(volume, 0.0f, 1.0f);
        ApplyVolumeSettings();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp(volume, 0.0f, 1.0f);
        ApplyVolumeSettings();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp(volume, 0.0f, 1.0f);
        ApplyVolumeSettings();
    }
}