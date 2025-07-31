using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Save Game Manager - Handles game state persistence
/// </summary>
public partial class SaveGameManager : Node
{
    private const string SavePath = "user://savegame.dat";
    private const string AutoSavePath = "user://autosave.dat";
    private const int SaveVersion = 1;
    
    public static SaveGameManager Instance { get; private set; }
    
    public override void _Ready()
    {
        Instance = this;
    }
    
    public bool SaveGame(GameSaveData saveData, bool isAutoSave = false)
    {
        var path = isAutoSave ? AutoSavePath : SavePath;
        
        try
        {
            var saveDict = new Godot.Collections.Dictionary
            {
                ["version"] = SaveVersion,
                ["timestamp"] = Time.GetUnixTimeFromSystem(),
                ["game_data"] = SerializeGameData(saveData)
            };
            
            var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"[SaveGameManager] Failed to open save file: {path}");
                return false;
            }
            
            file.StoreVar(saveDict);
            file.Close();
            
            GD.Print($"[SaveGameManager] Game saved to {path}");
            return true;
        }
        catch (Exception e)
        {
            GD.PrintErr($"[SaveGameManager] Save failed: {e.Message}");
            return false;
        }
    }
    
    public GameSaveData LoadGame(bool isAutoSave = false)
    {
        var path = isAutoSave ? AutoSavePath : SavePath;
        
        try
        {
            if (!FileAccess.FileExists(path))
            {
                GD.Print($"[SaveGameManager] No save file found at {path}");
                return null;
            }
            
            var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"[SaveGameManager] Failed to open save file: {path}");
                return null;
            }
            
            var saveDict = file.GetVar().AsGodotDictionary();
            file.Close();
            
            // Check version compatibility
            var version = saveDict.Get("version", 0).AsInt32();
            if (version != SaveVersion)
            {
                GD.PrintErr($"[SaveGameManager] Save version mismatch: {version} != {SaveVersion}");
                return null;
            }
            
            var gameData = saveDict["game_data"].AsGodotDictionary();
            return DeserializeGameData(gameData);
        }
        catch (Exception e)
        {
            GD.PrintErr($"[SaveGameManager] Load failed: {e.Message}");
            return null;
        }
    }
    
    private Godot.Collections.Dictionary SerializeGameData(GameSaveData data)
    {
        var dict = new Godot.Collections.Dictionary
        {
            ["round"] = data.CurrentRound,
            ["current_player_index"] = data.CurrentPlayerIndex,
            ["noise_level"] = data.NoiseLevel,
            ["is_collapsing"] = data.IsCollapsing,
            ["collapse_rounds"] = data.CollapseRoundsRemaining,
            ["aidron_found"] = data.AidronFound,
            ["players"] = SerializePlayers(data.Players),
            ["filers"] = SerializeFilers(data.FilerPositions),
            ["lit_cells"] = SerializeLitCells(data.LitCells),
            ["special_locations"] = SerializeSpecialLocations(data.SpecialLocations)
        };
        
        return dict;
    }
    
    private GameSaveData DeserializeGameData(Godot.Collections.Dictionary dict)
    {
        var data = new GameSaveData
        {
            CurrentRound = dict.Get("round", 1).AsInt32(),
            CurrentPlayerIndex = dict.Get("current_player_index", 0).AsInt32(),
            NoiseLevel = dict.Get("noise_level", 0).AsInt32(),
            IsCollapsing = dict.Get("is_collapsing", false).AsBool(),
            CollapseRoundsRemaining = dict.Get("collapse_rounds", 0).AsInt32(),
            AidronFound = dict.Get("aidron_found", false).AsBool()
        };
        
        data.Players = DeserializePlayers(dict["players"].AsGodotArray());
        data.FilerPositions = DeserializeFilers(dict["filers"].AsGodotArray());
        data.LitCells = DeserializeLitCells(dict["lit_cells"].AsGodotArray());
        data.SpecialLocations = DeserializeSpecialLocations(dict["special_locations"].AsGodotDictionary());
        
        return data;
    }
    
    private Godot.Collections.Array SerializePlayers(List<PlayerSaveInfo> players)
    {
        var array = new Godot.Collections.Array();
        
        foreach (var player in players)
        {
            array.Add(new Godot.Collections.Dictionary
            {
                ["name"] = player.Name,
                ["position_x"] = player.Position.X,
                ["position_y"] = player.Position.Y,
                ["tokens"] = player.Tokens,
                ["color_r"] = player.Color.R,
                ["color_g"] = player.Color.G,
                ["color_b"] = player.Color.B,
                ["is_filed"] = player.IsFiled
            });
        }
        
        return array;
    }
    
    private List<PlayerSaveInfo> DeserializePlayers(Godot.Collections.Array array)
    {
        var players = new List<PlayerSaveInfo>();
        
        foreach (var item in array)
        {
            var dict = item.AsGodotDictionary();
            players.Add(new PlayerSaveInfo
            {
                Name = dict["name"].AsString(),
                Position = new Vector2I(dict["position_x"].AsInt32(), dict["position_y"].AsInt32()),
                Tokens = dict["tokens"].AsInt32(),
                Color = new Color(dict["color_r"].AsSingle(), dict["color_g"].AsSingle(), dict["color_b"].AsSingle()),
                IsFiled = dict["is_filed"].AsBool()
            });
        }
        
        return players;
    }
    
    private Godot.Collections.Array SerializeFilers(List<Vector2I> filers)
    {
        var array = new Godot.Collections.Array();
        
        foreach (var pos in filers)
        {
            array.Add(new Godot.Collections.Dictionary
            {
                ["x"] = pos.X,
                ["y"] = pos.Y
            });
        }
        
        return array;
    }
    
    private List<Vector2I> DeserializeFilers(Godot.Collections.Array array)
    {
        var filers = new List<Vector2I>();
        
        foreach (var item in array)
        {
            var dict = item.AsGodotDictionary();
            filers.Add(new Vector2I(dict["x"].AsInt32(), dict["y"].AsInt32()));
        }
        
        return filers;
    }
    
    private Godot.Collections.Array SerializeLitCells(HashSet<Vector2I> litCells)
    {
        var array = new Godot.Collections.Array();
        
        foreach (var pos in litCells)
        {
            array.Add(new Godot.Collections.Dictionary
            {
                ["x"] = pos.X,
                ["y"] = pos.Y
            });
        }
        
        return array;
    }
    
    private HashSet<Vector2I> DeserializeLitCells(Godot.Collections.Array array)
    {
        var litCells = new HashSet<Vector2I>();
        
        foreach (var item in array)
        {
            var dict = item.AsGodotDictionary();
            litCells.Add(new Vector2I(dict["x"].AsInt32(), dict["y"].AsInt32()));
        }
        
        return litCells;
    }
    
    private Godot.Collections.Dictionary SerializeSpecialLocations(Dictionary<string, Vector2I> locations)
    {
        var dict = new Godot.Collections.Dictionary();
        
        foreach (var kvp in locations)
        {
            dict[kvp.Key] = new Godot.Collections.Dictionary
            {
                ["x"] = kvp.Value.X,
                ["y"] = kvp.Value.Y
            };
        }
        
        return dict;
    }
    
    private Dictionary<string, Vector2I> DeserializeSpecialLocations(Godot.Collections.Dictionary dict)
    {
        var locations = new Dictionary<string, Vector2I>();
        
        foreach (var key in dict.Keys)
        {
            var posDict = dict[key].AsGodotDictionary();
            locations[key.AsString()] = new Vector2I(posDict["x"].AsInt32(), posDict["y"].AsInt32());
        }
        
        return locations;
    }
    
    public bool HasSaveGame(bool checkAutoSave = false)
    {
        var path = checkAutoSave ? AutoSavePath : SavePath;
        return FileAccess.FileExists(path);
    }
    
    public void DeleteSaveGame(bool deleteAutoSave = false)
    {
        var path = deleteAutoSave ? AutoSavePath : SavePath;
        
        if (FileAccess.FileExists(path))
        {
            DirAccess.RemoveAbsolute(path);
            GD.Print($"[SaveGameManager] Deleted save at {path}");
        }
    }
}

// Save data structures
public class GameSaveData
{
    public int CurrentRound { get; set; }
    public int CurrentPlayerIndex { get; set; }
    public int NoiseLevel { get; set; }
    public bool IsCollapsing { get; set; }
    public int CollapseRoundsRemaining { get; set; }
    public bool AidronFound { get; set; }
    public List<PlayerSaveInfo> Players { get; set; } = new();
    public List<Vector2I> FilerPositions { get; set; } = new();
    public HashSet<Vector2I> LitCells { get; set; } = new();
    public Dictionary<string, Vector2I> SpecialLocations { get; set; } = new();
}

public class PlayerSaveInfo
{
    public string Name { get; set; }
    public Vector2I Position { get; set; }
    public int Tokens { get; set; }
    public Color Color { get; set; }
    public bool IsFiled { get; set; }
}