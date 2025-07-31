using Godot;
using System;
using System.Collections.Generic;

public partial class MainGame : Node2D
{
    // Game state
    private GamePhase currentPhase = GamePhase.Opening;
    private List<Player> players = new List<Player>();
    private VaultGrid vaultGrid;
    private bool isCollapsing = false;
    private int collapseTimer = 0;
    
    // Visual elements
    private Node2D gameBoard;
    private Control touchArea;
    private List<ColorRect> gridTiles = new List<ColorRect>();
    
    // Touch handling
    private Vector2 touchStartPos;
    private bool isTouching = false;
    
    public enum GamePhase
    {
        Opening,
        Search,
        Network,
        Escape,
        Collapse,
        Ended
    }
    
    public override void _Ready()
    {
        GD.Print("Main Game starting...");
        
        gameBoard = GetNode<Node2D>("GameBoard");
        touchArea = GetNode<Control>("TouchArea");
        
        // Connect touch input
        touchArea.GuiInput += OnTouchInput;
        
        InitializeGame();
        CreateVisualGrid();
    }
    
    private void InitializeGame()
    {
        // Create the vault grid (11x16)
        vaultGrid = new VaultGrid(11, 16);
        
        // Create 2 players for now
        for (int i = 0; i < 2; i++)
        {
            var player = new Player($"Player{i + 1}", new Color(0.2f + i * 0.3f, 0.8f, 0.8f));
            players.Add(player);
        }
        
        // Place players at starting positions
        if (players.Count >= 2)
        {
            players[0].Position = new Vector2I(0, 8);
            players[1].Position = new Vector2I(10, 8);
        }
    }
    
    private void CreateVisualGrid()
    {
        const float tileSize = 40;
        const float spacing = 2;
        
        // Create visual tiles for the grid
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 11; x++)
            {
                var tile = new ColorRect();
                tile.Size = new Vector2(tileSize, tileSize);
                tile.Position = new Vector2(
                    x * (tileSize + spacing) - (11 * (tileSize + spacing) / 2),
                    y * (tileSize + spacing) - (16 * (tileSize + spacing) / 2)
                );
                tile.Color = new Color(0.2f, 0.2f, 0.3f);
                
                gameBoard.AddChild(tile);
                gridTiles.Add(tile);
            }
        }
        
        // Draw players
        UpdatePlayerVisuals();
    }
    
    private void UpdatePlayerVisuals()
    {
        // Reset all tiles to default color
        for (int i = 0; i < gridTiles.Count; i++)
        {
            gridTiles[i].Color = new Color(0.2f, 0.2f, 0.3f);
        }
        
        // Color tiles with players
        foreach (var player in players)
        {
            int index = player.Position.Y * 11 + player.Position.X;
            if (index >= 0 && index < gridTiles.Count)
            {
                gridTiles[index].Color = player.PlayerColor;
            }
        }
    }
    
    private void OnTouchInput(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touchEvent)
        {
            if (touchEvent.Pressed)
            {
                isTouching = true;
                touchStartPos = touchEvent.Position;
                OnTouchStart(touchEvent.Position);
            }
            else
            {
                isTouching = false;
                OnTouchEnd(touchEvent.Position);
            }
        }
        else if (@event is InputEventScreenDrag dragEvent && isTouching)
        {
            OnTouchDrag(dragEvent.Position);
        }
    }
    
    private void OnTouchStart(Vector2 position)
    {
        GD.Print($"Touch started at: {position}");
        
        // Convert touch position to grid coordinates
        var localPos = gameBoard.ToLocal(position);
        var gridPos = ScreenToGrid(localPos);
        
        GD.Print($"Grid position: {gridPos}");
    }
    
    private void OnTouchDrag(Vector2 position)
    {
        // Handle dragging for player movement
    }
    
    private void OnTouchEnd(Vector2 position)
    {
        GD.Print($"Touch ended at: {position}");
        
        // Simple tap to move for now
        var localPos = gameBoard.ToLocal(position);
        var gridPos = ScreenToGrid(localPos);
        
        if (IsValidGridPosition(gridPos) && players.Count > 0)
        {
            // Move first player to tapped position
            players[0].Position = gridPos;
            UpdatePlayerVisuals();
        }
    }
    
    private Vector2I ScreenToGrid(Vector2 localPos)
    {
        const float tileSize = 40;
        const float spacing = 2;
        
        int x = Mathf.RoundToInt((localPos.X + (11 * (tileSize + spacing) / 2)) / (tileSize + spacing));
        int y = Mathf.RoundToInt((localPos.Y + (16 * (tileSize + spacing) / 2)) / (tileSize + spacing));
        
        return new Vector2I(x, y);
    }
    
    private bool IsValidGridPosition(Vector2I pos)
    {
        return pos.X >= 0 && pos.X < 11 && pos.Y >= 0 && pos.Y < 16;
    }
    
    public override void _Process(double delta)
    {
        // Update game state
        switch (currentPhase)
        {
            case GamePhase.Opening:
                // Wait for all players to be ready
                break;
                
            case GamePhase.Search:
                // Main gameplay
                break;
                
            case GamePhase.Collapse:
                // Vault is collapsing
                UpdateCollapse(delta);
                break;
        }
    }
    
    private void UpdateCollapse(double delta)
    {
        // Visual feedback for collapse
        foreach (var tile in gridTiles)
        {
            tile.Modulate = new Color(1, 0.8f + (float)Math.Sin(Time.GetUnixTimeFromSystem() * 5) * 0.2f, 0.8f);
        }
    }
}

// Basic game classes
public class Player
{
    public string Name { get; set; }
    public Vector2I Position { get; set; }
    public Color PlayerColor { get; set; }
    public bool HasEscaped { get; set; }
    
    public Player(string name, Color color)
    {
        Name = name;
        PlayerColor = color;
        HasEscaped = false;
    }
}

public class VaultGrid
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    private bool[,] illuminated;
    
    public VaultGrid(int width, int height)
    {
        Width = width;
        Height = height;
        illuminated = new bool[width, height];
    }
    
    public bool IsIlluminated(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            return illuminated[x, y];
        return false;
    }
    
    public void Illuminate(int x, int y)
    {
        if (x >= 0 && x < Width && y >= 0 && y < Height)
            illuminated[x, y] = true;
    }
}