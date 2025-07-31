using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Filer AI - Implements the behavior of Filers that hunt players in darkness
/// </summary>
public partial class FilerAI : RefCounted
{
    // Filer behavior modes based on noise level
    public enum FilerMode
    {
        Dormant,    // 0-4: Patrol edges
        Alert,      // 5-7: Seek sounds
        Hunting,    // 8-12: Hunt actively
        Crisis      // 13+: Third Filer spawns
    }
    
    private GameStateGuardian gameStateGuardian;
    private RandomNumberGenerator rng = new();
    
    public FilerAI()
    {
        rng.Randomize();
    }
    
    public void Initialize(GameStateGuardian guardian)
    {
        gameStateGuardian = guardian;
    }
    
    public List<FilerMove> ProcessFilerTurn(FilerState state)
    {
        var moves = new List<FilerMove>();
        var mode = GetFilerMode(state.NoiseLevel);
        
        // Process each filer
        for (int i = 0; i < state.FilerPositions.Count; i++)
        {
            var filerPos = state.FilerPositions[i];
            var move = DetermineFilerMove(i, filerPos, state, mode);
            moves.Add(move);
        }
        
        // Check if we need to spawn third filer
        if (mode == FilerMode.Crisis && state.FilerPositions.Count < 3)
        {
            var spawnPos = DetermineSpawnPosition(state);
            if (spawnPos != null)
            {
                moves.Add(new FilerMove
                {
                    FilerId = state.FilerPositions.Count,
                    FromPosition = spawnPos.Value,
                    ToPosition = spawnPos.Value,
                    Action = FilerAction.Spawn
                });
            }
        }
        
        return moves;
    }
    
    private FilerMode GetFilerMode(int noiseLevel)
    {
        return noiseLevel switch
        {
            <= 4 => FilerMode.Dormant,
            <= 7 => FilerMode.Alert,
            <= 12 => FilerMode.Hunting,
            _ => FilerMode.Crisis
        };
    }
    
    private FilerMove DetermineFilerMove(int filerId, Vector2I currentPos, FilerState state, FilerMode mode)
    {
        Vector2I targetPos = currentPos;
        FilerAction action = FilerAction.Move;
        
        // Check if any player is adjacent and in darkness
        var adjacentDarkPlayer = GetAdjacentDarkPlayer(currentPos, state);
        if (adjacentDarkPlayer != null)
        {
            // Attempt to file the player
            return new FilerMove
            {
                FilerId = filerId,
                FromPosition = currentPos,
                ToPosition = currentPos,
                Action = FilerAction.File,
                TargetPlayer = adjacentDarkPlayer
            };
        }
        
        // Determine movement based on mode
        switch (mode)
        {
            case FilerMode.Dormant:
                targetPos = PatrolEdges(currentPos, state);
                break;
                
            case FilerMode.Alert:
                targetPos = SeekNearestSound(currentPos, state);
                break;
                
            case FilerMode.Hunting:
            case FilerMode.Crisis:
                targetPos = HuntNearestPlayer(currentPos, state);
                break;
        }
        
        return new FilerMove
        {
            FilerId = filerId,
            FromPosition = currentPos,
            ToPosition = targetPos,
            Action = FilerAction.Move
        };
    }
    
    private string GetAdjacentDarkPlayer(Vector2I filerPos, FilerState state)
    {
        // Check all adjacent cells
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                var checkPos = filerPos + new Vector2I(dx, dy);
                
                // Check if position is valid
                if (checkPos.X < 0 || checkPos.X >= 8 || checkPos.Y < 0 || checkPos.Y >= 6)
                    continue;
                
                // Check if there's a player there
                foreach (var kvp in state.PlayerPositions)
                {
                    if (kvp.Value == checkPos)
                    {
                        // Check if player is in darkness
                        if (!state.LitCells.Contains(checkPos))
                        {
                            return kvp.Key;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    private Vector2I PatrolEdges(Vector2I currentPos, FilerState state)
    {
        // Patrol around the edges of the board
        var edgePositions = new List<Vector2I>();
        
        // Top edge
        for (int x = 0; x < 8; x++)
            edgePositions.Add(new Vector2I(x, 0));
            
        // Bottom edge
        for (int x = 0; x < 8; x++)
            edgePositions.Add(new Vector2I(x, 5));
            
        // Left edge (excluding corners)
        for (int y = 1; y < 5; y++)
            edgePositions.Add(new Vector2I(0, y));
            
        // Right edge (excluding corners)
        for (int y = 1; y < 5; y++)
            edgePositions.Add(new Vector2I(7, y));
        
        // Find nearest edge position
        return FindNearestValidPosition(currentPos, edgePositions, state);
    }
    
    private Vector2I SeekNearestSound(Vector2I currentPos, FilerState state)
    {
        // In this mode, filers move toward the last sound source
        // For now, move toward center of player positions
        if (state.PlayerPositions.Count == 0)
            return currentPos;
            
        var centerX = state.PlayerPositions.Values.Average(p => p.X);
        var centerY = state.PlayerPositions.Values.Average(p => p.Y);
        var soundCenter = new Vector2I((int)centerX, (int)centerY);
        
        return MoveToward(currentPos, soundCenter, state);
    }
    
    private Vector2I HuntNearestPlayer(Vector2I currentPos, FilerState state)
    {
        if (state.PlayerPositions.Count == 0)
            return currentPos;
            
        // Find nearest player
        Vector2I nearestPlayerPos = currentPos;
        float nearestDistance = float.MaxValue;
        
        foreach (var playerPos in state.PlayerPositions.Values)
        {
            float distance = currentPos.DistanceTo(playerPos);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlayerPos = playerPos;
            }
        }
        
        return MoveToward(currentPos, nearestPlayerPos, state);
    }
    
    private Vector2I MoveToward(Vector2I from, Vector2I target, FilerState state)
    {
        // Calculate direction
        var diff = target - from;
        var moveDir = new Vector2I(
            Mathf.Sign(diff.X),
            Mathf.Sign(diff.Y)
        );
        
        // Try to move in the best direction
        var candidates = new List<Vector2I>();
        
        // Diagonal move
        if (moveDir.X != 0 && moveDir.Y != 0)
        {
            candidates.Add(from + moveDir);
        }
        
        // Horizontal move
        if (moveDir.X != 0)
        {
            candidates.Add(from + new Vector2I(moveDir.X, 0));
        }
        
        // Vertical move
        if (moveDir.Y != 0)
        {
            candidates.Add(from + new Vector2I(0, moveDir.Y));
        }
        
        // Add some randomness for less predictable movement
        if (rng.Randf() < 0.2f)
        {
            candidates.Add(from + new Vector2I(rng.RandiRange(-1, 1), rng.RandiRange(-1, 1)));
        }
        
        // Find first valid move
        foreach (var candidate in candidates)
        {
            if (IsValidPosition(candidate, state))
            {
                return candidate;
            }
        }
        
        // If no valid move toward target, try any adjacent cell
        return FindAnyValidAdjacentPosition(from, state);
    }
    
    private Vector2I FindNearestValidPosition(Vector2I from, List<Vector2I> targets, FilerState state)
    {
        Vector2I nearest = from;
        float nearestDistance = float.MaxValue;
        
        foreach (var target in targets)
        {
            if (IsValidPosition(target, state))
            {
                float distance = from.DistanceTo(target);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = target;
                }
            }
        }
        
        return MoveToward(from, nearest, state);
    }
    
    private Vector2I FindAnyValidAdjacentPosition(Vector2I from, FilerState state)
    {
        var positions = new List<Vector2I>();
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                var pos = from + new Vector2I(dx, dy);
                if (IsValidPosition(pos, state))
                {
                    positions.Add(pos);
                }
            }
        }
        
        if (positions.Count > 0)
        {
            return positions[rng.RandiRange(0, positions.Count - 1)];
        }
        
        return from; // Stay in place if no valid moves
    }
    
    private bool IsValidPosition(Vector2I pos, FilerState state)
    {
        // Check bounds
        if (pos.X < 0 || pos.X >= 8 || pos.Y < 0 || pos.Y >= 6)
            return false;
            
        // Check if occupied by another filer
        if (state.FilerPositions.Contains(pos))
            return false;
            
        // Filers can move through players (to file them)
        // but prefer empty cells when possible
        
        return true;
    }
    
    private Vector2I? DetermineSpawnPosition(FilerState state)
    {
        // Spawn at a random edge position not occupied
        var edgePositions = new List<Vector2I>();
        
        // Collect all edge positions
        for (int x = 0; x < 8; x++)
        {
            edgePositions.Add(new Vector2I(x, 0));
            edgePositions.Add(new Vector2I(x, 5));
        }
        for (int y = 1; y < 5; y++)
        {
            edgePositions.Add(new Vector2I(0, y));
            edgePositions.Add(new Vector2I(7, y));
        }
        
        // Filter out occupied positions
        var validPositions = edgePositions.Where(p => IsValidPosition(p, state)).ToList();
        
        if (validPositions.Count > 0)
        {
            return validPositions[rng.RandiRange(0, validPositions.Count - 1)];
        }
        
        return null;
    }
}

// Data structures for Filer AI
public class FilerState
{
    public List<Vector2I> FilerPositions { get; set; } = new();
    public Dictionary<string, Vector2I> PlayerPositions { get; set; } = new();
    public HashSet<Vector2I> LitCells { get; set; } = new();
    public int NoiseLevel { get; set; }
    public int Round { get; set; }
}

public class FilerMove
{
    public int FilerId { get; set; }
    public Vector2I FromPosition { get; set; }
    public Vector2I ToPosition { get; set; }
    public FilerAction Action { get; set; }
    public string TargetPlayer { get; set; } // For filing action
}

public enum FilerAction
{
    Move,
    File,
    Spawn
}