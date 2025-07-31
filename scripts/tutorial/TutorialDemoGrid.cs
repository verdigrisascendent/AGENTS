using Godot;
using System.Collections.Generic;

/// <summary>
/// Tutorial Demo Grid - Simplified interactive grid for tutorial demonstrations
/// </summary>
public partial class TutorialDemoGrid : Control
{
    [Signal]
    public delegate void CellClickedEventHandler(Vector2I position);
    
    [Signal]
    public delegate void ActionPerformedEventHandler(string action, Vector2I position);
    
    // Grid configuration
    private const int GridWidth = 8;
    private const int GridHeight = 6;
    private const int CellSize = 64;
    private const int CellPadding = 4;
    
    // Display options
    public bool Interactive { get; set; } = false;
    public bool ShowGridLabels { get; set; } = false;
    public bool ShowValidMoves { get; set; } = false;
    public bool ShowNoiseBar { get; set; } = false;
    public bool ShowTokenCount { get; set; } = false;
    public bool ShowFilerBehavior { get; set; } = false;
    public bool AnimateFilers { get; set; } = false;
    public bool HighlightSpecialCells { get; set; } = false;
    public bool HighlightNoiseEffects { get; set; } = false;
    public bool ShowHiddenLocations { get; set; } = false;
    public bool ShowCollapseEffects { get; set; } = false;
    public bool ShowAllFeatures { get; set; } = false;
    
    // Grid state
    private bool[,] litCells = new bool[GridWidth, GridHeight];
    private Vector2I playerPosition = new Vector2I(0, 0);
    private List<Vector2I> filerPositions = new();
    private Vector2I? aidronPosition;
    private Vector2I? exitPosition;
    private int noiseLevel = 0;
    private int playerTokens = 2;
    private bool practiceMode = false;
    
    // Visual elements
    private Font gameFont;
    private Color gridColor = new Color("#0000AA");
    private Color litColor = Colors.White;
    private Color darkColor = new Color("#000040");
    private Color playerColor = Colors.Red;
    private Color filerColor = new Color("#FF6600");
    private Color aidronColor = Colors.Green;
    private Color exitColor = Colors.Magenta;
    
    public override void _Ready()
    {
        gameFont = ThemeDB.FallbackFont;
        CustomMinimumSize = new Vector2(GridWidth * (CellSize + CellPadding), GridHeight * (CellSize + CellPadding));
    }
    
    public override void _Draw()
    {
        DrawBackground();
        DrawGrid();
        DrawSpecialLocations();
        DrawEntities();
        DrawUI();
        
        if (ShowCollapseEffects)
        {
            DrawCollapseEffects();
        }
    }
    
    public override void _GuiInput(InputEvent @event)
    {
        if (!Interactive || !@event.IsPressed())
            return;
            
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            var gridPos = ScreenToGrid(mouseEvent.Position);
            if (IsValidGridPosition(gridPos))
            {
                EmitSignal(SignalName.CellClicked, gridPos);
                
                if (practiceMode)
                {
                    HandlePracticeClick(gridPos);
                }
            }
        }
    }
    
    private void DrawBackground()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), darkColor);
    }
    
    private void DrawGrid()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var cellRect = GetCellRect(x, y);
                var cellColor = litCells[x, y] ? litColor : darkColor;
                
                // Draw cell background
                DrawRect(cellRect, cellColor);
                
                // Draw cell border
                DrawRect(cellRect, gridColor, false, 2.0f);
                
                // Draw grid labels if enabled
                if (ShowGridLabels)
                {
                    if (y == 0)
                    {
                        var label = ((char)('A' + x)).ToString();
                        var labelPos = new Vector2(cellRect.Position.X + CellSize / 2, cellRect.Position.Y - 10);
                        DrawString(gameFont, labelPos, label, HorizontalAlignment.Center, -1, 16, gridColor);
                    }
                    if (x == 0)
                    {
                        var label = (y + 1).ToString();
                        var labelPos = new Vector2(cellRect.Position.X - 20, cellRect.Position.Y + CellSize / 2);
                        DrawString(gameFont, labelPos, label, HorizontalAlignment.Center, -1, 16, gridColor);
                    }
                }
                
                // Highlight valid moves
                if (ShowValidMoves && IsValidMove(new Vector2I(x, y)))
                {
                    DrawRect(cellRect, new Color(0, 1, 0, 0.3f));
                }
            }
        }
    }
    
    private void DrawSpecialLocations()
    {
        if (HighlightSpecialCells || ShowHiddenLocations)
        {
            if (aidronPosition.HasValue)
            {
                var rect = GetCellRect(aidronPosition.Value.X, aidronPosition.Value.Y);
                DrawRect(rect, aidronColor, false, 4.0f);
                DrawString(gameFont, rect.GetCenter(), "A", HorizontalAlignment.Center, -1, 24, aidronColor);
            }
            
            if (exitPosition.HasValue)
            {
                var rect = GetCellRect(exitPosition.Value.X, exitPosition.Value.Y);
                DrawRect(rect, exitColor, false, 4.0f);
                DrawString(gameFont, rect.GetCenter(), "E", HorizontalAlignment.Center, -1, 24, exitColor);
            }
        }
    }
    
    private void DrawEntities()
    {
        // Draw player
        var playerRect = GetCellRect(playerPosition.X, playerPosition.Y);
        DrawCircle(playerRect.GetCenter(), CellSize / 3, playerColor);
        DrawString(gameFont, playerRect.GetCenter(), "P", HorizontalAlignment.Center, -1, 16, Colors.White);
        
        // Draw filers
        foreach (var filerPos in filerPositions)
        {
            var filerRect = GetCellRect(filerPos.X, filerPos.Y);
            DrawRect(new Rect2(filerRect.GetCenter() - new Vector2(CellSize / 4, CellSize / 4), 
                              new Vector2(CellSize / 2, CellSize / 2)), filerColor);
            DrawString(gameFont, filerRect.GetCenter(), "F", HorizontalAlignment.Center, -1, 20, Colors.Black);
            
            if (ShowFilerBehavior)
            {
                DrawFilerBehaviorIndicator(filerPos);
            }
        }
    }
    
    private void DrawUI()
    {
        var yOffset = 10;
        
        if (ShowNoiseBar)
        {
            var barPos = new Vector2(10, Size.Y - 40);
            var barSize = new Vector2(200, 20);
            
            // Background
            DrawRect(new Rect2(barPos, barSize), Colors.Black);
            
            // Fill
            var fillWidth = (noiseLevel / 15.0f) * barSize.X;
            var fillColor = noiseLevel switch
            {
                <= 4 => Colors.Green,
                <= 7 => Colors.Yellow,
                <= 12 => Colors.Orange,
                _ => Colors.Red
            };
            DrawRect(new Rect2(barPos, new Vector2(fillWidth, barSize.Y)), fillColor);
            
            // Label
            DrawString(gameFont, barPos + new Vector2(barSize.X + 10, 15), 
                      $"NOISE: {noiseLevel}", HorizontalAlignment.Left, -1, 16, Colors.White);
                      
            if (HighlightNoiseEffects)
            {
                var modeText = noiseLevel switch
                {
                    <= 4 => "DORMANT",
                    <= 7 => "ALERT",
                    <= 12 => "HUNTING",
                    _ => "CRISIS!"
                };
                DrawString(gameFont, barPos + new Vector2(barSize.X + 100, 15), 
                          $"[{modeText}]", HorizontalAlignment.Left, -1, 16, fillColor);
            }
        }
        
        if (ShowTokenCount)
        {
            var tokenPos = new Vector2(10, yOffset);
            DrawString(gameFont, tokenPos, $"TOKENS: {playerTokens}", 
                      HorizontalAlignment.Left, -1, 20, Colors.Yellow);
        }
    }
    
    private void DrawCollapseEffects()
    {
        // Flicker effect
        if (Time.GetUnixTimeFromSystem() % 2 < 1)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), new Color(1, 0, 0, 0.1f));
        }
        
        // Warning text
        var text = "VAULT COLLAPSING!";
        var textSize = gameFont.GetStringSize(text, HorizontalAlignment.Left, -1, 32);
        var textPos = new Vector2(Size.X / 2, 50);
        DrawString(gameFont, textPos, text, HorizontalAlignment.Center, -1, 32, Colors.Magenta);
    }
    
    private void DrawFilerBehaviorIndicator(Vector2I filerPos)
    {
        var mode = noiseLevel switch
        {
            <= 4 => "PATROL",
            <= 7 => "SEEK",
            <= 12 => "HUNT",
            _ => "RAGE"
        };
        
        var rect = GetCellRect(filerPos.X, filerPos.Y);
        DrawString(gameFont, rect.Position + new Vector2(0, -5), mode, 
                  HorizontalAlignment.Center, -1, 12, filerColor);
    }
    
    private Rect2 GetCellRect(int x, int y)
    {
        return new Rect2(
            x * (CellSize + CellPadding) + CellPadding,
            y * (CellSize + CellPadding) + CellPadding,
            CellSize,
            CellSize
        );
    }
    
    private Vector2I ScreenToGrid(Vector2 screenPos)
    {
        var x = (int)((screenPos.X - CellPadding) / (CellSize + CellPadding));
        var y = (int)((screenPos.Y - CellPadding) / (CellSize + CellPadding));
        return new Vector2I(x, y);
    }
    
    private bool IsValidGridPosition(Vector2I pos)
    {
        return pos.X >= 0 && pos.X < GridWidth && pos.Y >= 0 && pos.Y < GridHeight;
    }
    
    public bool IsValidMove(Vector2I targetPos)
    {
        if (!IsValidGridPosition(targetPos))
            return false;
            
        // Check if adjacent
        var diff = targetPos - playerPosition;
        if (Mathf.Abs(diff.X) > 1 || Mathf.Abs(diff.Y) > 1)
            return false;
            
        // Check movement rules
        if (litCells[targetPos.X, targetPos.Y])
            return true; // Can move to lit cells
            
        // Can move one cell into darkness from lit cell
        if (litCells[playerPosition.X, playerPosition.Y])
            return true;
            
        return false;
    }
    
    private void HandlePracticeClick(Vector2I gridPos)
    {
        if (IsValidMove(gridPos))
        {
            playerPosition = gridPos;
            EmitSignal(SignalName.ActionPerformed, "move", gridPos);
            QueueRedraw();
        }
    }
    
    // Public methods for tutorial setup
    public void Reset()
    {
        litCells = new bool[GridWidth, GridHeight];
        playerPosition = new Vector2I(0, 0);
        filerPositions.Clear();
        aidronPosition = null;
        exitPosition = null;
        noiseLevel = 0;
        playerTokens = 2;
        QueueRedraw();
    }
    
    public void SetupBasicGame()
    {
        Reset();
        
        // Light starting area
        SetCellLight(new Vector2I(0, 0), true);
        SetCellLight(new Vector2I(1, 0), true);
        SetCellLight(new Vector2I(0, 1), true);
        
        // Add some filers
        filerPositions.Add(new Vector2I(4, 3));
        filerPositions.Add(new Vector2I(6, 2));
        
        // Hidden locations
        aidronPosition = new Vector2I(5, 4);
        exitPosition = new Vector2I(7, 5);
        
        QueueRedraw();
    }
    
    public void SetupMovementTutorial()
    {
        Reset();
        playerPosition = new Vector2I(3, 3);
        
        // Create lit area around player
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var pos = playerPosition + new Vector2I(dx, dy);
                if (IsValidGridPosition(pos))
                {
                    SetCellLight(pos, true);
                }
            }
        }
        
        // Add one dark cell adjacent to show movement limit
        SetCellLight(new Vector2I(5, 3), false);
        
        QueueRedraw();
    }
    
    public void SetupSignalTutorial()
    {
        Reset();
        playerPosition = new Vector2I(4, 3);
        SetCellLight(playerPosition, true);
        noiseLevel = 2;
        QueueRedraw();
    }
    
    public void SetupIlluminateTutorial()
    {
        Reset();
        playerPosition = new Vector2I(2, 2);
        SetCellLight(playerPosition, true);
        SetCellLight(new Vector2I(2, 3), true);
        playerTokens = 2;
        QueueRedraw();
    }
    
    public void SetupFilerTutorial()
    {
        Reset();
        playerPosition = new Vector2I(1, 1);
        SetCellLight(new Vector2I(1, 1), true);
        SetCellLight(new Vector2I(2, 1), true);
        
        filerPositions.Add(new Vector2I(4, 1));
        filerPositions.Add(new Vector2I(5, 3));
        
        noiseLevel = 6; // Alert mode
        QueueRedraw();
    }
    
    public void SetupNoiseTutorial()
    {
        Reset();
        SetupBasicGame();
        noiseLevel = 0;
        QueueRedraw();
    }
    
    public void SetupSpecialLocationsTutorial()
    {
        Reset();
        playerPosition = new Vector2I(3, 3);
        
        // Light path to special locations
        for (int i = 0; i < 5; i++)
        {
            SetCellLight(new Vector2I(i, 3), true);
        }
        
        aidronPosition = new Vector2I(5, 3);
        exitPosition = new Vector2I(7, 3);
        QueueRedraw();
    }
    
    public void SetupCollapseTutorial()
    {
        Reset();
        SetupBasicGame();
        
        // Light more cells to show collapse effect
        for (int x = 0; x < GridWidth; x += 2)
        {
            for (int y = 0; y < GridHeight; y += 2)
            {
                SetCellLight(new Vector2I(x, y), true);
            }
        }
        
        QueueRedraw();
    }
    
    public void SetupStrategyExamples()
    {
        Reset();
        
        // Show various strategic positions
        playerPosition = new Vector2I(2, 2);
        SetCellLight(playerPosition, true);
        
        // Token-created safe path
        SetCellLight(new Vector2I(3, 2), true);
        SetCellLight(new Vector2I(4, 2), true);
        
        // Filer positions
        filerPositions.Add(new Vector2I(6, 2));
        filerPositions.Add(new Vector2I(4, 4));
        
        // Special locations visible
        aidronPosition = new Vector2I(5, 1);
        exitPosition = new Vector2I(7, 5);
        
        noiseLevel = 7;
        playerTokens = 1;
        
        QueueRedraw();
    }
    
    public void SetCellLight(Vector2I pos, bool lit)
    {
        if (IsValidGridPosition(pos))
        {
            litCells[pos.X, pos.Y] = lit;
        }
    }
    
    public void EnablePracticeMode()
    {
        practiceMode = true;
        Interactive = true;
        ShowAllFeatures = true;
        QueueRedraw();
    }
}