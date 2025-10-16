using System;

namespace Brain.Gameplay
{
    /// <summary>
    /// Ball state flags using bitwise operations for efficient state management
    /// Adapted from BubbleShooterGameToolkit reference implementation
    /// </summary>
    [Flags]
    public enum BallFlags
    {
        None = 0,
        Pinned = 1 << 0,              // 1 - Ball is pinned to grid (static)
        Falling = 1 << 1,             // 2 - Ball is falling (orphaned)
        Destroying = 1 << 2,          // 4 - Ball is being destroyed
        MarkedForDestroy = 1 << 3,    // 8 - Ball is queued for destruction
        MarkedForMatch = 1 << 4,      // 16 - Ball was found in match check
        MarkConnected = 1 << 5,       // 32 - Ball is connected to root (for orphan detection)
        Root = 1 << 6                 // 64 - Ball is a root (top row, can hold other balls)
    }
}
