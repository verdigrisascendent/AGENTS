using Godot;
using System.Collections.Generic;

/// <summary>
/// Tutorial Screen - Interactive tutorial system for teaching game mechanics
/// </summary>
public partial class TutorialScreen : Control
{
    // UI References
    private Button backButton;
    private Label titleLabel;
    private Label stepIndicator;
    private RichTextLabel instructionText;
    private RichTextLabel hintText;
    private RichTextLabel ruleText;
    private Control tutorialGrid;
    private Button previousButton;
    private Button practiceModeButton;
    private Button nextButton;
    private Button skipButton;
    
    // Tutorial state
    private int currentStep = 0;
    private List<TutorialStep> tutorialSteps;
    private TutorialDemoGrid demoGrid;
    private bool practiceMode = false;
    
    // Agents
    private AmigaAestheticEnforcer aestheticEnforcer;
    
    public override void _Ready()
    {
        GetUIReferences();
        InitializeAgents();
        CreateTutorialSteps();
        ConnectSignals();
        
        // Create demo grid
        CreateDemoGrid();
        
        // Start with first step
        ShowStep(0);
    }
    
    private void GetUIReferences()
    {
        // Header
        backButton = GetNode<Button>("MainContainer/VBoxContainer/Header/BackButton");
        titleLabel = GetNode<Label>("MainContainer/VBoxContainer/Header/TitleLabel");
        stepIndicator = GetNode<Label>("MainContainer/VBoxContainer/Header/StepIndicator");
        
        // Content
        instructionText = GetNode<RichTextLabel>("MainContainer/VBoxContainer/ContentArea/LeftPanel/InstructionPanel/MarginContainer/ScrollContainer/InstructionText");
        hintText = GetNode<RichTextLabel>("MainContainer/VBoxContainer/ContentArea/LeftPanel/ControlHints/MarginContainer/HintText");
        ruleText = GetNode<RichTextLabel>("MainContainer/VBoxContainer/ContentArea/RightPanel/RulePanel/MarginContainer/RuleText");
        tutorialGrid = GetNode<Control>("MainContainer/VBoxContainer/ContentArea/RightPanel/DemoViewport/SubViewport/TutorialGrid");
        
        // Navigation
        previousButton = GetNode<Button>("MainContainer/VBoxContainer/NavigationButtons/PreviousButton");
        practiceModeButton = GetNode<Button>("MainContainer/VBoxContainer/NavigationButtons/PracticeModeButton");
        nextButton = GetNode<Button>("MainContainer/VBoxContainer/NavigationButtons/NextButton");
        skipButton = GetNode<Button>("MainContainer/VBoxContainer/NavigationButtons/SkipButton");
    }
    
    private void InitializeAgents()
    {
        var megaAgent = GetNode<MegaAgent>("/root/GameInitializer/MegaAgent");
        aestheticEnforcer = new AmigaAestheticEnforcer();
        aestheticEnforcer.Initialize(megaAgent);
    }
    
    private void CreateTutorialSteps()
    {
        tutorialSteps = new List<TutorialStep>
        {
            new TutorialStep
            {
                Title = "WELCOME TO THE VAULT",
                Instruction = "[b]WELCOME TO LIGHTS IN THE DARK[/b]\n\nYou are trapped in a vault with your companions. The darkness is alive and dangerous - Filers hunt those who stray from the light.\n\n[color=#FFFF00]OBJECTIVE:[/color]\nFind the AIDRON to activate permanent light, then escape through the EXIT before the vault collapses!",
                Hint = "This is the game overview. Click NEXT to begin learning the mechanics.",
                Rules = "[b]WINNING CONDITIONS:[/b]\n• Find and activate the AIDRON (permanent light source)\n• Reach the EXIT with at least one player\n• Escape before the vault collapses\n\n[b]LOSING CONDITIONS:[/b]\n• All players get filed\n• Vault collapses before escape",
                DemoSetup = SetupOverviewDemo
            },
            
            new TutorialStep
            {
                Title = "THE GAME GRID",
                Instruction = "[b]THE PLAYING FIELD[/b]\n\nThe vault is represented by an 8×6 grid. Each cell can be:\n\n[color=#FFFFFF]• LIT[/color] - Safe to move through\n[color=#666666]• DARK[/color] - Dangerous! Filers can catch you here\n[color=#00FF00]• AIDRON[/color] - Activates permanent light\n[color=#FF00FF]• EXIT[/color] - Your escape route",
                Hint = "The demo shows a typical game grid. Notice how some cells are lit (white) while others remain dark.",
                Rules = "[b]GRID BASICS:[/b]\n• 8 columns (A-H) × 6 rows (1-6)\n• Players start at opposite corners\n• Special locations are hidden until discovered\n• Light fades over time unless permanent",
                DemoSetup = SetupGridDemo
            },
            
            new TutorialStep
            {
                Title = "PLAYER MOVEMENT",
                Instruction = "[b]HOW TO MOVE[/b]\n\nPlayers can move to adjacent cells (including diagonals) but with restrictions:\n\n• Can move freely between lit cells\n• Can move ONE cell into darkness\n• CANNOT move through multiple dark cells\n• CANNOT move onto cells with Filers",
                Hint = "Click on a valid cell to move there. Valid moves are highlighted when you hover.",
                Rules = "[b]MOVEMENT RULES:[/b]\n• 8-directional movement (including diagonals)\n• One move per turn\n• Must be adjacent to current position\n• Light restriction applies",
                DemoSetup = SetupMovementDemo,
                Interactive = true
            },
            
            new TutorialStep
            {
                Title = "ACTIONS: SIGNAL",
                Instruction = "[b]THE SIGNAL ACTION[/b]\n\nWhen you can't see your companions, use SIGNAL to:\n\n• Light up all 8 adjacent cells\n• Help other players see\n• BUT: Increases noise by +2\n\nBe careful - noise attracts Filers!",
                Hint = "Press the SIGNAL button to light your surroundings. Watch the noise meter increase!",
                Rules = "[b]SIGNAL MECHANICS:[/b]\n• No token cost\n• Lights 3×3 area around player\n• Light is temporary (fades after turns)\n• +2 noise penalty\n• Can save trapped players",
                DemoSetup = SetupSignalDemo,
                Interactive = true
            },
            
            new TutorialStep
            {
                Title = "ACTIONS: ILLUMINATE",
                Instruction = "[b]THE ILLUMINATE ACTION[/b]\n\nSpend tokens to create permanent light:\n\n• Costs 1 token\n• Creates PERMANENT light\n• Target any adjacent cell\n• Strategic resource - use wisely!",
                Hint = "Click ILLUMINATE, then click an adjacent cell. Notice your token count decreases.",
                Rules = "[b]ILLUMINATE STRATEGY:[/b]\n• Limited tokens (usually 2 per player)\n• Permanent light never fades\n• Create safe paths\n• Light key intersections\n• Save some for emergencies",
                DemoSetup = SetupIlluminateDemo,
                Interactive = true
            },
            
            new TutorialStep
            {
                Title = "THE FILERS",
                Instruction = "[b]DANGER: FILERS[/b]\n\nFilers are vault guardians that hunt in darkness:\n\n[color=#00FF00]• DORMANT (0-4 noise):[/color] Patrol edges\n[color=#FFFF00]• ALERT (5-7 noise):[/color] Investigate sounds\n[color=#FF6600]• HUNTING (8-12 noise):[/color] Actively pursue\n[color=#FF0000]• CRISIS (13+ noise):[/color] Third Filer spawns!",
                Hint = "Filers are shown as red 'F' markers. They move after all players have acted.",
                Rules = "[b]FILER BEHAVIOR:[/b]\n• Move after player turns\n• Can only file players in darkness\n• Cannot enter lit cells\n• Smarter at higher noise levels\n• Filing = permanent removal",
                DemoSetup = SetupFilerDemo
            },
            
            new TutorialStep
            {
                Title = "NOISE MANAGEMENT",
                Instruction = "[b]THE NOISE SYSTEM[/b]\n\nNoise attracts Filers and changes their behavior:\n\n• Moving = No noise\n• Signal = +2 noise\n• Filed player = Reset to 0\n\nKeep noise low to survive!",
                Hint = "The noise bar shows current level and changes color as danger increases.",
                Rules = "[b]NOISE THRESHOLDS:[/b]\n• 0-4: Safe (green)\n• 5-7: Caution (yellow)\n• 8-12: Danger (orange)\n• 13+: CRISIS (red)\n\nCoordinate to manage noise!",
                DemoSetup = SetupNoiseDemo
            },
            
            new TutorialStep
            {
                Title = "SPECIAL LOCATIONS",
                Instruction = "[b]AIDRON & EXIT[/b]\n\nTwo hidden locations are key to victory:\n\n[color=#00FF00]AIDRON:[/color]\n• Activates when found\n• Provides permanent 3×3 light\n• Essential for safe escape\n\n[color=#FF00FF]EXIT:[/color]\n• Only usable after finding Aidron\n• Reach it to win!",
                Hint = "Special locations appear when you move onto them. The Aidron must be found first!",
                Rules = "[b]DISCOVERY RULES:[/b]\n• Hidden until stepped on\n• Any player can discover\n• Aidron activates immediately\n• Exit requires Aidron first\n• Locations are randomized",
                DemoSetup = SetupSpecialDemo
            },
            
            new TutorialStep
            {
                Title = "COLLAPSE MODE",
                Instruction = "[b]VAULT COLLAPSE[/b]\n\nSometimes the vault becomes unstable:\n\n[color=#FF00FF]• 3 rounds to escape\n• All lights flicker\n• Debris effects\n• Reality glitches[/color]\n\nWhen collapse begins, RUN FOR THE EXIT!",
                Hint = "During collapse, focus on reaching the exit. Don't waste time on side objectives!",
                Rules = "[b]COLLAPSE TRIGGERS:[/b]\n• Special event cards\n• Time limit reached\n• Critical failures\n\n[b]EFFECTS:[/b]\n• Countdown timer\n• Visual distortions\n• Increased urgency",
                DemoSetup = SetupCollapseDemo
            },
            
            new TutorialStep
            {
                Title = "STRATEGY TIPS",
                Instruction = "[b]WINNING STRATEGIES[/b]\n\n• Stay together when possible\n• Create lit pathways\n• Manage noise carefully\n• Save tokens for emergencies\n• Communicate with teammates\n• Plan escape routes\n• Don't abandon friends!",
                Hint = "Ready to play? Try PRACTICE MODE or start a real game!",
                Rules = "[b]ADVANCED TACTICS:[/b]\n• Leapfrog movement\n• Noise baiting\n• Token sharing\n• Filer manipulation\n• Safe zones\n• Sacrifice plays\n\nGood luck!",
                DemoSetup = SetupStrategyDemo
            }
        };
    }
    
    private void ConnectSignals()
    {
        backButton.Pressed += OnBackPressed;
        previousButton.Pressed += OnPreviousPressed;
        nextButton.Pressed += OnNextPressed;
        skipButton.Pressed += OnSkipPressed;
        practiceModeButton.Pressed += OnPracticeModePressed;
    }
    
    private void CreateDemoGrid()
    {
        // Create a simplified tutorial grid
        demoGrid = new TutorialDemoGrid();
        demoGrid.Name = "DemoGrid";
        tutorialGrid.AddChild(demoGrid);
        demoGrid.SetAnchorsAndOffsetsPreset(Control.Preset.FullRect);
        
        // Connect to demo grid events if interactive
        demoGrid.CellClicked += OnDemoCellClicked;
        demoGrid.ActionPerformed += OnDemoActionPerformed;
    }
    
    private void ShowStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= tutorialSteps.Count)
            return;
            
        currentStep = stepIndex;
        var step = tutorialSteps[currentStep];
        
        // Update UI
        stepIndicator.Text = $"STEP {currentStep + 1} OF {tutorialSteps.Count}";
        instructionText.Text = step.Instruction;
        hintText.Text = $"[b]CURRENT HINT:[/b]\n{step.Hint}";
        ruleText.Text = step.Rules;
        
        // Update navigation buttons
        previousButton.Disabled = currentStep == 0;
        nextButton.Disabled = currentStep == tutorialSteps.Count - 1;
        practiceModeButton.Visible = step.Interactive;
        
        // Setup demo
        step.DemoSetup?.Invoke(demoGrid);
        
        // Reset practice mode
        practiceMode = false;
        UpdatePracticeModeButton();
    }
    
    // Demo setup methods
    private void SetupOverviewDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.SetupBasicGame();
        grid.Interactive = false;
    }
    
    private void SetupGridDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.ShowGridLabels = true;
        grid.HighlightSpecialCells = true;
        grid.Interactive = false;
        
        // Show various cell states
        grid.SetCellLight(new Vector2I(3, 2), true);
        grid.SetCellLight(new Vector2I(4, 2), true);
        grid.SetCellLight(new Vector2I(3, 3), true);
    }
    
    private void SetupMovementDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.SetupMovementTutorial();
        grid.Interactive = true;
        grid.ShowValidMoves = true;
    }
    
    private void SetupSignalDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.SetupSignalTutorial();
        grid.Interactive = true;
        grid.ShowNoiseBar = true;
    }
    
    private void SetupIlluminateDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.SetupIlluminateTutorial();
        grid.Interactive = true;
        grid.ShowTokenCount = true;
    }
    
    private void SetupFilerDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.SetupFilerTutorial();
        grid.ShowFilerBehavior = true;
        grid.AnimateFilers = true;
    }
    
    private void SetupNoiseDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.SetupNoiseTutorial();
        grid.ShowNoiseBar = true;
        grid.HighlightNoiseEffects = true;
    }
    
    private void SetupSpecialDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.SetupSpecialLocationsTutorial();
        grid.ShowHiddenLocations = true;
    }
    
    private void SetupCollapseDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.SetupCollapseTutorial();
        grid.ShowCollapseEffects = true;
    }
    
    private void SetupStrategyDemo(TutorialDemoGrid grid)
    {
        grid.Reset();
        grid.SetupStrategyExamples();
        grid.ShowAllFeatures = true;
    }
    
    // Event handlers
    private void OnBackPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/screens/main_menu.tscn");
    }
    
    private void OnPreviousPressed()
    {
        ShowStep(currentStep - 1);
    }
    
    private void OnNextPressed()
    {
        ShowStep(currentStep + 1);
    }
    
    private void OnSkipPressed()
    {
        // Go directly to game setup
        GetTree().ChangeSceneToFile("res://scenes/screens/game_setup.tscn");
    }
    
    private void OnPracticeModePressed()
    {
        practiceMode = !practiceMode;
        UpdatePracticeModeButton();
        
        if (practiceMode)
        {
            demoGrid.EnablePracticeMode();
            hintText.Text = "[b]PRACTICE MODE ACTIVE[/b]\nTry out the mechanics freely. Click PRACTICE MODE again to return to the tutorial.";
        }
        else
        {
            ShowStep(currentStep); // Refresh current step
        }
    }
    
    private void UpdatePracticeModeButton()
    {
        practiceModeButton.Text = practiceMode ? "EXIT PRACTICE MODE" : "PRACTICE THIS STEP";
    }
    
    private void OnDemoCellClicked(Vector2I position)
    {
        if (!practiceMode)
        {
            // In tutorial mode, provide feedback based on the step
            var step = tutorialSteps[currentStep];
            if (step.Title.Contains("MOVEMENT"))
            {
                hintText.Text = $"[b]HINT:[/b]\nYou clicked cell {(char)('A' + position.X)}{position.Y + 1}. ";
                if (demoGrid.IsValidMove(position))
                {
                    hintText.Text += "That's a valid move!";
                }
                else
                {
                    hintText.Text += "That move is not allowed - too far or blocked.";
                }
            }
        }
    }
    
    private void OnDemoActionPerformed(string action, Vector2I position)
    {
        if (!practiceMode)
        {
            // Provide feedback on actions
            var step = tutorialSteps[currentStep];
            if (step.Title.Contains(action.ToUpper()))
            {
                hintText.Text = $"[b]EXCELLENT![/b]\nYou successfully performed {action}. Try it again or click NEXT to continue.";
            }
        }
    }
}

// Tutorial step data
public class TutorialStep
{
    public string Title { get; set; }
    public string Instruction { get; set; }
    public string Hint { get; set; }
    public string Rules { get; set; }
    public bool Interactive { get; set; }
    public System.Action<TutorialDemoGrid> DemoSetup { get; set; }
}