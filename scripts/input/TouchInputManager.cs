using Godot;
using System.Collections.Generic;

/// <summary>
/// Touch Input Manager - Optimizes touch controls for iPad
/// </summary>
public partial class TouchInputManager : Node
{
    [Signal]
    public delegate void TapEventHandler(Vector2 position);
    
    [Signal]
    public delegate void SwipeEventHandler(Vector2 start, Vector2 end, SwipeDirection direction);
    
    [Signal]
    public delegate void LongPressEventHandler(Vector2 position);
    
    [Signal]
    public delegate void PinchEventHandler(float scale);
    
    public enum SwipeDirection
    {
        Up,
        Down,
        Left,
        Right
    }
    
    // Touch tracking
    private Dictionary<int, TouchInfo> activeTouches = new();
    private const float SwipeThreshold = 50.0f;
    private const float LongPressTime = 0.5f;
    private const float TapMaxTime = 0.3f;
    private const float TapMaxDistance = 20.0f;
    
    // Gesture detection
    private bool isPinching = false;
    private float initialPinchDistance = 0.0f;
    
    public override void _Ready()
    {
        // Enable multi-touch
        InputMap.AddAction("touch_tap");
        InputMap.AddAction("touch_swipe");
        InputMap.AddAction("touch_long_press");
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventScreenTouch touchEvent)
        {
            HandleTouch(touchEvent);
        }
        else if (@event is InputEventScreenDrag dragEvent)
        {
            HandleDrag(dragEvent);
        }
    }
    
    private void HandleTouch(InputEventScreenTouch touchEvent)
    {
        if (touchEvent.Pressed)
        {
            // Touch started
            activeTouches[touchEvent.Index] = new TouchInfo
            {
                StartPosition = touchEvent.Position,
                CurrentPosition = touchEvent.Position,
                StartTime = Time.GetUnixTimeFromSystem(),
                Index = touchEvent.Index
            };
            
            // Check for multi-touch gestures
            if (activeTouches.Count == 2)
            {
                StartPinchGesture();
            }
        }
        else
        {
            // Touch ended
            if (activeTouches.TryGetValue(touchEvent.Index, out var touch))
            {
                var duration = Time.GetUnixTimeFromSystem() - touch.StartTime;
                var distance = touch.StartPosition.DistanceTo(touchEvent.Position);
                
                // Detect tap
                if (duration < TapMaxTime && distance < TapMaxDistance)
                {
                    EmitSignal(SignalName.Tap, touchEvent.Position);
                }
                // Detect swipe
                else if (distance > SwipeThreshold)
                {
                    var direction = DetectSwipeDirection(touch.StartPosition, touchEvent.Position);
                    EmitSignal(SignalName.Swipe, touch.StartPosition, touchEvent.Position, (int)direction);
                }
                
                activeTouches.Remove(touchEvent.Index);
                
                // End pinch if needed
                if (activeTouches.Count < 2)
                {
                    isPinching = false;
                }
            }
        }
    }
    
    private void HandleDrag(InputEventScreenDrag dragEvent)
    {
        if (activeTouches.TryGetValue(dragEvent.Index, out var touch))
        {
            touch.CurrentPosition = dragEvent.Position;
            
            // Update pinch gesture
            if (isPinching && activeTouches.Count >= 2)
            {
                UpdatePinchGesture();
            }
        }
    }
    
    public override void _Process(double delta)
    {
        // Check for long press
        var currentTime = Time.GetUnixTimeFromSystem();
        
        foreach (var touch in activeTouches.Values)
        {
            if (!touch.LongPressTriggered)
            {
                var duration = currentTime - touch.StartTime;
                var distance = touch.StartPosition.DistanceTo(touch.CurrentPosition);
                
                if (duration >= LongPressTime && distance < TapMaxDistance)
                {
                    touch.LongPressTriggered = true;
                    EmitSignal(SignalName.LongPress, touch.CurrentPosition);
                }
            }
        }
    }
    
    private SwipeDirection DetectSwipeDirection(Vector2 start, Vector2 end)
    {
        var diff = end - start;
        
        if (Mathf.Abs(diff.X) > Mathf.Abs(diff.Y))
        {
            // Horizontal swipe
            return diff.X > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            // Vertical swipe
            return diff.Y > 0 ? SwipeDirection.Down : SwipeDirection.Up;
        }
    }
    
    private void StartPinchGesture()
    {
        if (activeTouches.Count >= 2)
        {
            var touches = new List<TouchInfo>(activeTouches.Values);
            initialPinchDistance = touches[0].CurrentPosition.DistanceTo(touches[1].CurrentPosition);
            isPinching = true;
        }
    }
    
    private void UpdatePinchGesture()
    {
        if (activeTouches.Count >= 2)
        {
            var touches = new List<TouchInfo>(activeTouches.Values);
            var currentDistance = touches[0].CurrentPosition.DistanceTo(touches[1].CurrentPosition);
            
            if (initialPinchDistance > 0)
            {
                var scale = currentDistance / initialPinchDistance;
                EmitSignal(SignalName.Pinch, scale);
            }
        }
    }
    
    // Helper methods for game-specific gestures
    public bool IsTapOnGrid(Vector2 tapPosition, Rect2 gridRect)
    {
        return gridRect.HasPoint(tapPosition);
    }
    
    public Vector2I GetGridCellFromTap(Vector2 tapPosition, Rect2 gridRect, int gridWidth, int gridHeight)
    {
        if (!gridRect.HasPoint(tapPosition))
            return new Vector2I(-1, -1);
            
        var relativePos = tapPosition - gridRect.Position;
        var cellWidth = gridRect.Size.X / gridWidth;
        var cellHeight = gridRect.Size.Y / gridHeight;
        
        var x = Mathf.FloorToInt(relativePos.X / cellWidth);
        var y = Mathf.FloorToInt(relativePos.Y / cellHeight);
        
        return new Vector2I(
            Mathf.Clamp(x, 0, gridWidth - 1),
            Mathf.Clamp(y, 0, gridHeight - 1)
        );
    }
    
    // iPad-specific optimizations
    public void ConfigureForIPad()
    {
        // Adjust touch areas for iPad Pro 11"
        var screenSize = DisplayServer.ScreenGetSize();
        
        // Increase touch target sizes
        ProjectSettings.SetSetting("input_devices/pointing/ios/touch_delay", 0.05);
        
        GD.Print($"[TouchInputManager] Configured for iPad with screen size: {screenSize}");
    }
    
    public bool IsMultiTouchActive()
    {
        return activeTouches.Count > 1;
    }
    
    public int GetActiveTouchCount()
    {
        return activeTouches.Count;
    }
}

// Helper class
public class TouchInfo
{
    public Vector2 StartPosition { get; set; }
    public Vector2 CurrentPosition { get; set; }
    public double StartTime { get; set; }
    public int Index { get; set; }
    public bool LongPressTriggered { get; set; }
}