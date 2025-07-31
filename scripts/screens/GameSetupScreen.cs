using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Game Setup Screen - Player selection and game configuration
/// </summary>
public partial class GameSetupScreen : Control
{
    // Character definitions
    public class CharacterData
    {
        public string Name { get; set; }
        public Color PrimaryColor { get; set; }
        public string Description { get; set; }
        public bool IsSpecial { get; set; }
    }
    
    private readonly Dictionary<string, CharacterData> Characters = new()
    {
        ["Elmer"] = new CharacterData 
        { 
            Name = "Elmer",
            PrimaryColor = new Color("#FF0000"), // Red
            Description = "The bold adventurer",
            IsSpecial = false
        },
        ["Toplop"] = new CharacterData 
        { 
            Name = "Toplop",
            PrimaryColor = new Color("#00FF00"), // Green
            Description = "The cunning rogue",
            IsSpecial = false
        },
        ["Peye"] = new CharacterData 
        { 
            Name = "Peye",
            PrimaryColor = new Color("#FFFF00"), // Yellow
            Description = "The wise scholar",
            IsSpecial = false
        },
        ["Draqthur"] = new CharacterData 
        { 
            Name = "Draqthur",
            PrimaryColor = new Color("#FF00FF"), // Magenta
            Description = "The mystic seer",
            IsSpecial = false
        },
        ["Bluepea"] = new CharacterData 
        { 
            Name = "Bluepea",
            PrimaryColor = new Color("#00FFFF"), // Cyan
            Description = "The mysterious entity",
            IsSpecial = true
        }
    };
    
    // UI References
    private Button backButton;
    private Button decreaseButton;
    private Button increaseButton;
    private Label countLabel;
    private Label bluePeaWarning;
    private VBoxContainer characterList;
    private VBoxContainer aiList;
    private Button cancelButton;
    private Button startButton;
    
    // Game state
    private int playerCount = 4;
    private HashSet<string> selectedCharacters = new() { "Elmer", "Toplop", "Peye", "Draqthur" };
    private Dictionary<string, bool> aiControlled = new();
    private Dictionary<string, CheckBox> characterCheckboxes = new();
    private Dictionary<string, CheckBox> aiCheckboxes = new();
    
    // Agents
    private GameStateGuardian gameStateGuardian;
    private AmigaAestheticEnforcer aestheticEnforcer;
    
    public override void _Ready()
    {
        InitializeAgents();
        GetUIReferences();
        SetupUI();
        ConnectSignals();
        UpdateUI();
        
        GD.Print("[GameSetupScreen] Ready - Player selection interface active");
    }
    
    private void InitializeAgents()
    {
        var megaAgent = GetNode<MegaAgent>("/root/GameInitializer/MegaAgent");
        
        gameStateGuardian = new GameStateGuardian();
        gameStateGuardian.Initialize(megaAgent);
        
        aestheticEnforcer = new AmigaAestheticEnforcer();
        aestheticEnforcer.Initialize(megaAgent);
    }
    
    private void GetUIReferences()
    {
        backButton = GetNode<Button>("Header/BackButton");
        decreaseButton = GetNode<Button>("TabContainer/Players/VBoxContainer/PlayerCountPanel/VBox/HBox/DecreaseButton");
        increaseButton = GetNode<Button>("TabContainer/Players/VBoxContainer/PlayerCountPanel/VBox/HBox/IncreaseButton");
        countLabel = GetNode<Label>("TabContainer/Players/VBoxContainer/PlayerCountPanel/VBox/HBox/CountLabel");
        bluePeaWarning = GetNode<Label>("TabContainer/Players/VBoxContainer/PlayerCountPanel/VBox/BluePeaWarning");
        characterList = GetNode<VBoxContainer>("TabContainer/Players/VBoxContainer/CharacterSelectionPanel/VBox/CharacterList");
        aiList = GetNode<VBoxContainer>("TabContainer/Players/VBoxContainer/AIControlPanel/VBox/AIList");
        cancelButton = GetNode<Button>("Footer/HBoxContainer/CancelButton");
        startButton = GetNode<Button>("Footer/HBoxContainer/StartButton");
    }
    
    private void SetupUI()
    {
        // Initialize AI control states
        foreach (var character in Characters.Keys)
        {
            aiControlled[character] = false;
        }
        
        // Create character selection checkboxes
        foreach (var kvp in Characters)
        {
            if (kvp.Key == "Bluepea") continue; // Bluepea is special
            
            var container = new HBoxContainer();
            container.AddThemeConstantOverride("separation", 16);
            
            var checkbox = new CheckBox();
            checkbox.Name = kvp.Key + "Check";
            checkbox.ButtonPressed = selectedCharacters.Contains(kvp.Key);
            checkbox.Toggled += (bool pressed) => OnCharacterToggled(kvp.Key, pressed);
            characterCheckboxes[kvp.Key] = checkbox;
            
            var colorRect = new ColorRect();
            colorRect.CustomMinimumSize = new Vector2(32, 32);
            colorRect.Color = kvp.Value.PrimaryColor;
            
            var label = new Label();
            label.Text = kvp.Key.ToUpper();
            label.AddThemeColorOverride("font_color", Colors.White);
            label.AddThemeFontSizeOverride("font_size", 20);
            
            container.AddChild(checkbox);
            container.AddChild(colorRect);
            container.AddChild(label);
            
            characterList.AddChild(container);
        }
        
        ApplyAmigaStyling();
    }
    
    private void ApplyAmigaStyling()
    {
        // Style buttons with Amiga aesthetic
        var buttonStyle = new StyleBoxFlat();
        buttonStyle.BgColor = new Color("#0055AA");
        buttonStyle.SetBorderWidthAll(2);
        buttonStyle.BorderColor = new Color("#AAAAAA");
        buttonStyle.SetCornerRadiusAll(0);
        buttonStyle.AntiAliasing = false;
        
        foreach (Button button in new[] { backButton, decreaseButton, increaseButton, cancelButton })
        {
            button.AddThemeStyleboxOverride("normal", buttonStyle);
            button.AddThemeStyleboxOverride("hover", CreateHoverStyle(buttonStyle));
            button.AddThemeStyleboxOverride("pressed", CreatePressedStyle(buttonStyle));
        }
        
        // Special styling for start button
        var startStyle = buttonStyle.Duplicate() as StyleBoxFlat;
        startStyle.BgColor = new Color("#00AA00");
        startButton.AddThemeStyleboxOverride("normal", startStyle);
        startButton.AddThemeStyleboxOverride("hover", CreateHoverStyle(startStyle));
        startButton.AddThemeStyleboxOverride("pressed", CreatePressedStyle(startStyle));
    }
    
    private StyleBoxFlat CreateHoverStyle(StyleBoxFlat baseStyle)
    {
        var hoverStyle = baseStyle.Duplicate() as StyleBoxFlat;
        hoverStyle.BgColor = hoverStyle.BgColor.Lightened(0.2f);
        return hoverStyle;
    }
    
    private StyleBoxFlat CreatePressedStyle(StyleBoxFlat baseStyle)
    {
        var pressedStyle = baseStyle.Duplicate() as StyleBoxFlat;
        pressedStyle.BgColor = pressedStyle.BgColor.Darkened(0.2f);
        pressedStyle.BorderColor = new Color("#444444");
        return pressedStyle;
    }
    
    private void ConnectSignals()
    {
        backButton.Pressed += OnBackPressed;
        decreaseButton.Pressed += OnDecreasePressed;
        increaseButton.Pressed += OnIncreasePressed;
        cancelButton.Pressed += OnBackPressed;
        startButton.Pressed += OnStartPressed;
    }
    
    private void OnBackPressed()
    {
        GD.Print("[GameSetupScreen] Returning to main menu");
        GetTree().ChangeSceneToFile("res://scenes/screens/main_menu.tscn");
    }
    
    private void OnDecreasePressed()
    {
        if (playerCount > 1)
        {
            playerCount--;
            UpdatePlayerCount();
        }
    }
    
    private void OnIncreasePressed()
    {
        if (playerCount < 5)
        {
            playerCount++;
            UpdatePlayerCount();
        }
    }
    
    private void UpdatePlayerCount()
    {
        countLabel.Text = playerCount.ToString();
        
        // Show/hide Bluepea warning
        bluePeaWarning.Visible = playerCount == 5;
        
        // Adjust selected characters
        if (playerCount == 5)
        {
            // Force include Bluepea
            selectedCharacters.Add("Bluepea");
        }
        else
        {
            selectedCharacters.Remove("Bluepea");
            
            // Ensure we have enough characters selected
            var availableChars = Characters.Keys.Where(c => c != "Bluepea").ToList();
            while (selectedCharacters.Count < playerCount && selectedCharacters.Count < availableChars.Count)
            {
                var nextChar = availableChars.FirstOrDefault(c => !selectedCharacters.Contains(c));
                if (nextChar != null)
                    selectedCharacters.Add(nextChar);
            }
            
            // Remove excess characters
            while (selectedCharacters.Count > playerCount)
            {
                var lastChar = selectedCharacters.Last();
                selectedCharacters.Remove(lastChar);
            }
        }
        
        UpdateUI();
    }
    
    private void OnCharacterToggled(string character, bool pressed)
    {
        if (pressed)
        {
            if (selectedCharacters.Count < playerCount)
            {
                selectedCharacters.Add(character);
            }
            else
            {
                // Can't select more than player count
                characterCheckboxes[character].SetPressedNoSignal(false);
            }
        }
        else
        {
            if (selectedCharacters.Count > 1)
            {
                selectedCharacters.Remove(character);
            }
            else
            {
                // Must have at least one character
                characterCheckboxes[character].SetPressedNoSignal(true);
            }
        }
        
        UpdateUI();
    }
    
    private void UpdateUI()
    {
        // Update character checkboxes
        foreach (var kvp in characterCheckboxes)
        {
            kvp.Value.SetPressedNoSignal(selectedCharacters.Contains(kvp.Key));
            kvp.Value.Disabled = playerCount == 5 && kvp.Key != "Bluepea";
        }
        
        // Update AI control list
        foreach (Node child in aiList.GetChildren())
        {
            child.QueueFree();
        }
        
        foreach (var character in selectedCharacters.OrderBy(c => c))
        {
            var container = new HBoxContainer();
            container.AddThemeConstantOverride("separation", 16);
            
            var colorRect = new ColorRect();
            colorRect.CustomMinimumSize = new Vector2(24, 24);
            colorRect.Color = Characters[character].PrimaryColor;
            
            var label = new Label();
            label.Text = character.ToUpper();
            label.CustomMinimumSize = new Vector2(120, 0);
            label.AddThemeColorOverride("font_color", Colors.White);
            label.AddThemeFontSizeOverride("font_size", 18);
            
            var aiCheckbox = new CheckBox();
            aiCheckbox.Text = "AI";
            aiCheckbox.ButtonPressed = aiControlled[character];
            aiCheckbox.Toggled += (bool pressed) => { aiControlled[character] = pressed; };
            aiCheckboxes[character] = aiCheckbox;
            
            container.AddChild(colorRect);
            container.AddChild(label);
            container.AddChild(aiCheckbox);
            
            aiList.AddChild(container);
        }
        
        // Update button states
        decreaseButton.Disabled = playerCount <= 1;
        increaseButton.Disabled = playerCount >= 5;
        startButton.Disabled = selectedCharacters.Count != playerCount;
    }
    
    private void OnStartPressed()
    {
        GD.Print("[GameSetupScreen] Starting game with configuration:");
        GD.Print($"  Players: {playerCount}");
        GD.Print($"  Characters: {string.Join(", ", selectedCharacters)}");
        
        // Create player data for game state
        var players = new List<Godot.Collections.Dictionary<string, Variant>>();
        foreach (var character in selectedCharacters)
        {
            players.Add(new Godot.Collections.Dictionary<string, Variant>
            {
                ["name"] = character,
                ["color"] = Characters[character].PrimaryColor,
                ["is_ai"] = aiControlled[character],
                ["position"] = new Vector2I(-1, -1), // Will be set by game
                ["tokens"] = 2
            });
        }
        
        // Store game configuration
        var gameConfig = new Godot.Collections.Dictionary<string, Variant>
        {
            ["players"] = players,
            ["player_count"] = playerCount,
            ["enable_bluepea"] = selectedCharacters.Contains("Bluepea"),
            ["speed_mode"] = "casual"
        };
        
        // Save to global game state
        GetNode("/root/GameInitializer").Set("game_config", gameConfig);
        
        // Transition to main game screen
        GetTree().ChangeSceneToFile("res://scenes/screens/main_game.tscn");
    }
}