using System;

namespace Brain.Gameplay
{
    [Flags]
    public enum BallFlags
    {
        None = 0,
        Pinned = 1 << 0,              // Ball is pinned to grid (static)
        Falling = 1 << 1,             // Ball is falling (orphaned)
        Destroying = 1 << 2,          // Ball is being destroyed
        MarkedForDestroy = 1 << 3,    // Ball is queued for destruction
        MarkedForMatch = 1 << 4,      // Ball was found in match check
        MarkConnected = 1 << 5,       // Ball is connected to root (for orphan detection)
        Root = 1 << 6                 // Ball is a root (top row, can hold other balls)
    }
}
