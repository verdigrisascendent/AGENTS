using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Game Grid - 8×6 playing field with fog of war and light mechanics
/// </summary>
public partial class GameGrid : Control
{
    // Grid constants
    private const int GridWidth = 8;
    private const int GridHeight = 6;
    private const float CellSize = 64.0f;
    
    // Grid state
    private Cell[,] cells = new Cell[GridWidth, GridHeight];
    private Dictionary<string, Vector2I> playerPositions = new();
    private List<Vector2I> filerPositions = new();
    private Vector2I? aidronPosition = null;
    private Vector2I? exitPosition = null;
    
    // Visual state
    private bool showAllCells = false; // Debug/DM mode
    private HashSet<Vector2I> visibleCells = new();
    private HashSet<Vector2I> exploredCells = new();
    
    // Current player
    private string currentPlayer = "";
    
    // Agents
    private GameStateGuardian gameStateGuardian;
    private HardwareBridgeEngineer hardwareBridge;
    
    // Signals
    [Signal]
    public delegate void CellClickedEventHandler(Vector2I position);
    [Signal]
    public delegate void SpecialLocationFoundEventHandler(string type, Vector2I position);
    
    public override void _Ready()
    {
        InitializeGrid();
        InitializeAgents();
        
        // Set up drawing
        CustomMinimumSize = new Vector2(GridWidth * CellSize, GridHeight * CellSize);
        
        GD.Print($"[GameGrid] Initialized {GridWidth}×{GridHeight} grid");
    }
    
    private void InitializeGrid()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                cells[x, y] = new Cell();
            }
        }
    }
    
    private void InitializeAgents()
    {
        var megaAgent = GetNode<MegaAgent>("/root/GameInitializer/MegaAgent");
        
        gameStateGuardian = new GameStateGuardian();
        gameStateGuardian.Initialize(megaAgent);
        
        hardwareBridge = GetNode<HardwareBridgeEngineer>("/root/GameInitializer/HardwareBridge");
    }
    
    public override void _Draw()
    {
        DrawGrid();
        DrawCells();
        DrawEntities();
        DrawFogOfWar();
    }
    
    private void DrawGrid()
    {
        // Draw grid lines
        var gridColor = new Color("#444466");
        
        // Vertical lines
        for (int x = 0; x <= GridWidth; x++)
        {
            var startPos = new Vector2(x * CellSize, 0);
            var endPos = new Vector2(x * CellSize, GridHeight * CellSize);
            DrawLine(startPos, endPos, gridColor, 1.0f);
        }
        
        // Horizontal lines
        for (int y = 0; y <= GridHeight; y++)
        {
            var startPos = new Vector2(0, y * CellSize);
            var endPos = new Vector2(GridWidth * CellSize, y * CellSize);
            DrawLine(startPos, endPos, gridColor, 1.0f);
        }
    }
    
    private void DrawCells()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var cell = cells[x, y];
                var pos = new Vector2I(x, y);
                var rect = new Rect2(x * CellSize, y * CellSize, CellSize, CellSize);
                
                // Skip if not visible (fog of war)
                if (!showAllCells && !visibleCells.Contains(pos) && !exploredCells.Contains(pos))
                    continue;
                
                // Draw cell background based on light status
                if (cell.PermanentLight)
                {
                    DrawRect(rect, new Color("#FFFFAA")); // Bright yellow for permanent light
                }
                else if (cell.IsLit)
                {
                    DrawRect(rect, new Color("#FFFF66")); // Yellow for temporary light
                }
                else if (exploredCells.Contains(pos))
                {
                    DrawRect(rect, new Color("#222244")); // Dark blue for explored but dark
                }
                
                // Draw special locations
                if (pos == aidronPosition)
                {
                    DrawCircle(GetCellCenter(x, y), CellSize * 0.3f, new Color("#00FF00"));
                    DrawString(GetThemeDefaultFont(), GetCellCenter(x, y) - new Vector2(8, -4), "A", 
                              HorizontalAlignment.Center, -1, 16, Colors.Black);
                }
                else if (pos == exitPosition)
                {
                    DrawRect(new Rect2(rect.Position + Vector2.One * CellSize * 0.2f, 
                            rect.Size - Vector2.One * CellSize * 0.4f), 
                            new Color("#FF00FF"));
                    DrawString(GetThemeDefaultFont(), GetCellCenter(x, y) - new Vector2(8, -4), "E", 
                              HorizontalAlignment.Center, -1, 16, Colors.White);
                }
            }
        }
    }
    
    private void DrawEntities()
    {
        // Draw players
        foreach (var kvp in playerPositions)
        {
            var playerName = kvp.Key;
            var pos = kvp.Value;
            
            if (!IsPositionVisible(pos))
                continue;
            
            var center = GetCellCenter(pos.X, pos.Y);
            var color = GetPlayerColor(playerName);
            
            // Draw player circle
            DrawCircle(center, CellSize * 0.25f, color);
            DrawCircleOutline(center, CellSize * 0.25f, Colors.White, 2.0f);
            
            // Draw player initial
            DrawString(GetThemeDefaultFont(), center - new Vector2(6, -4), 
                      playerName.Substring(0, 1), HorizontalAlignment.Center, -1, 16, Colors.White);
        }
        
        // Draw filers
        foreach (var pos in filerPositions)
        {
            if (!IsPositionVisible(pos))
                continue;
                
            var center = GetCellCenter(pos.X, pos.Y);
            
            // Draw filer as red square
            var filerRect = new Rect2(center - Vector2.One * CellSize * 0.3f, 
                                     Vector2.One * CellSize * 0.6f);
            DrawRect(filerRect, new Color("#CC0000"));
            DrawRect(filerRect, new Color("#FF0000"), false, 2.0f);
            
            DrawString(GetThemeDefaultFont(), center - new Vector2(6, -4), "F", 
                      HorizontalAlignment.Center, -1, 16, Colors.White);
        }
    }
    
    private void DrawFogOfWar()
    {
        if (showAllCells)
            return;
            
        // Draw fog over non-visible cells
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var pos = new Vector2I(x, y);
                
                if (!visibleCells.Contains(pos))
                {
                    var rect = new Rect2(x * CellSize, y * CellSize, CellSize, CellSize);
                    
                    if (exploredCells.Contains(pos))
                    {
                        // Semi-transparent for explored areas
                        DrawRect(rect, new Color(0, 0, 0, 0.5f));
                    }
                    else
                    {
                        // Fully opaque for unexplored areas
                        DrawRect(rect, Colors.Black);
                    }
                }
            }
        }
    }
    
    private void DrawCircleOutline(Vector2 center, float radius, Color color, float width = 1.0f)
    {
        int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * Mathf.Tau;
            float angle2 = ((i + 1) / (float)segments) * Mathf.Tau;
            
            var point1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
            var point2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;
            
            DrawLine(point1, point2, color, width);
        }
    }
    
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
            {
                var localPos = mouseButton.Position;
                var gridX = (int)(localPos.X / CellSize);
                var gridY = (int)(localPos.Y / CellSize);
                
                if (gridX >= 0 && gridX < GridWidth && gridY >= 0 && gridY < GridHeight)
                {
                    EmitSignal(SignalName.CellClicked, new Vector2I(gridX, gridY));
                }
            }
        }
    }
    
    private Vector2 GetCellCenter(int x, int y)
    {
        return new Vector2(x * CellSize + CellSize / 2, y * CellSize + CellSize / 2);
    }
    
    private bool IsPositionVisible(Vector2I pos)
    {
        return showAllCells || visibleCells.Contains(pos);
    }
    
    private Color GetPlayerColor(string playerName)
    {
        return playerName switch
        {
            "Elmer" => new Color("#FF0000"),
            "Toplop" => new Color("#00FF00"),
            "Peye" => new Color("#FFFF00"),
            "Draqthur" => new Color("#FF00FF"),
            "Bluepea" => new Color("#00FFFF"),
            _ => Colors.White
        };
    }
    
    // Public methods for game state updates
    public void SetCurrentPlayer(string playerName)
    {
        currentPlayer = playerName;
        UpdateVisibility();
    }
    
    public void SetPlayerPosition(string playerName, Vector2I position)
    {
        if (position.X >= 0 && position.X < GridWidth && position.Y >= 0 && position.Y < GridHeight)
        {
            // Remove from old position
            if (playerPositions.TryGetValue(playerName, out var oldPos))
            {
                cells[oldPos.X, oldPos.Y].RemovePlayer(playerName);
            }
            
            // Add to new position
            playerPositions[playerName] = position;
            cells[position.X, position.Y].AddPlayer(playerName);
            
            UpdateVisibility();
            SyncToLED();
            QueueRedraw();
        }
    }
    
    public void SetFilerPositions(List<Vector2I> positions)
    {
        // Clear old filer positions
        foreach (var pos in filerPositions)
        {
            if (pos.X >= 0 && pos.X < GridWidth && pos.Y >= 0 && pos.Y < GridHeight)
            {
                cells[pos.X, pos.Y].HasFiler = false;
            }
        }
        
        // Set new positions
        filerPositions = positions.Where(p => 
            p.X >= 0 && p.X < GridWidth && p.Y >= 0 && p.Y < GridHeight).ToList();
            
        foreach (var pos in filerPositions)
        {
            cells[pos.X, pos.Y].HasFiler = true;
        }
        
        SyncToLED();
        QueueRedraw();
    }
    
    public void SetCellLight(Vector2I position, bool lit, bool permanent = false)
    {
        if (position.X >= 0 && position.X < GridWidth && position.Y >= 0 && position.Y < GridHeight)
        {
            var cell = cells[position.X, position.Y];
            cell.IsLit = lit;
            cell.PermanentLight = permanent;
            
            UpdateVisibility();
            SyncToLED();
            QueueRedraw();
        }
    }
    
    public void SetSpecialLocation(string type, Vector2I position)
    {
        if (position.X >= 0 && position.X < GridWidth && position.Y >= 0 && position.Y < GridHeight)
        {
            switch (type)
            {
                case "Aidron":
                    aidronPosition = position;
                    cells[position.X, position.Y].HasAidron = true;
                    break;
                case "Exit":
                    exitPosition = position;
                    cells[position.X, position.Y].HasExit = true;
                    break;
            }
            
            EmitSignal(SignalName.SpecialLocationFound, type, position);
            QueueRedraw();
        }
    }
    
    private void UpdateVisibility()
    {
        visibleCells.Clear();
        
        // Add cells that are lit
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                if (cells[x, y].IsLit)
                {
                    var pos = new Vector2I(x, y);
                    visibleCells.Add(pos);
                    exploredCells.Add(pos);
                }
            }
        }
        
        // Add cells adjacent to current player
        if (playerPositions.TryGetValue(currentPlayer, out var playerPos))
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var checkPos = playerPos + new Vector2I(dx, dy);
                    if (checkPos.X >= 0 && checkPos.X < GridWidth && 
                        checkPos.Y >= 0 && checkPos.Y < GridHeight)
                    {
                        visibleCells.Add(checkPos);
                        exploredCells.Add(checkPos);
                    }
                }
            }
        }
    }
    
    private void SyncToLED()
    {
        // Build board state for LED sync
        var boardState = new Dictionary<Vector2I, TileState>();
        
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var cell = cells[x, y];
                var tileState = new TileState
                {
                    IsLit = cell.IsLit,
                    IsPermanentLight = cell.PermanentLight,
                    HasMemorySpark = false, // TODO: Implement memory sparks
                    IsExit = cell.HasExit,
                    IsAidron = cell.HasAidron
                };
                
                boardState[new Vector2I(x, y)] = tileState;
            }
        }
        
        // Send to hardware bridge
        var syncParams = new Godot.Collections.Dictionary<string, object>
        {
            ["board_state"] = boardState
        };
        hardwareBridge.Execute("sync_game_state", syncParams);
    }
}

// Cell data class
public class Cell
{
    public bool IsLit { get; set; }
    public bool PermanentLight { get; set; }
    public int LightDuration { get; set; }
    public HashSet<string> Players { get; } = new();
    public bool HasFiler { get; set; }
    public bool HasAidron { get; set; }
    public bool HasExit { get; set; }
    public bool Collapsed { get; set; }
    
    public void AddPlayer(string playerName)
    {
        Players.Add(playerName);
    }
    
    public void RemovePlayer(string playerName)
    {
        Players.Remove(playerName);
    }
}