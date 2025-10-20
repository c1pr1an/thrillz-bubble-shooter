# Trajectory System Edge Cases Fix Plan

## Issues to Address

### Issue 1: Edge Column Ball Creates False Gap
**Problem:** When a ball is placed on the last column of the hex grid (near the wall), there's a small visual gap between the ball and the wall. The trajectory line can currently go through this gap, but the launched ball shouldn't be able to.

**Solution:** Detect when trajectory hits a ball on an edge column and prevent it from trying to squeeze past toward the wall.

### Issue 2: Raycast Too Precise for Aiming
**Problem:** Using a single line raycast allows players to aim between balls with unrealistic precision - they can thread the needle between balls that a real ball couldn't fit through.

**Solution:** Use CircleCast with a small radius (percentage of ball radius) for more realistic collision detection.

---

## Implementation Steps

### Step 1: Modify TrajectoryPredictor.cs - Add Percentage-Based CircleCast

Add new parameter:
```csharp
[Header("Trajectory Settings")]
[SerializeField] private int maxBounces = 3;
[SerializeField] private float maxDistance = 50f;
[SerializeField] private float ballRadius = 0.35f;
[Range(0f, 0.5f)]
[Tooltip("Collision check radius as percentage of ball radius (0.15 = 15%)")]
[SerializeField] private float trajectoryCheckRadiusPercent = 0.15f; // NEW
```

Update the raycast to use CircleCast:
```csharp
// In CalculateTrajectory method, replace the Raycast with:
float checkRadius = ballRadius * trajectoryCheckRadiusPercent;
RaycastHit2D hit = Physics2D.CircleCast(
    currentPos,
    checkRadius,  // Small percentage of actual ball radius
    currentDir,
    remainingDistance,
    LayerMask.GetMask("Default")
);
```

### Step 2: Add Edge Ball Detection

Add helper methods to TrajectoryPredictor:
```csharp
/// <summary>
/// Checks if a ball is on the edge column of the hex grid
/// </summary>
private bool IsEdgeColumnBall(Ball ball)
{
    int column = ball.Position.x;
    int maxCols = GridUtils.GetMaxColumns(ball.Position.y);
    return column == 0 || column == maxCols - 1;
}

/// <summary>
/// Checks if we're trying to squeeze past the ball toward the wall
/// </summary>
private bool IsSqueezeAttempt(Vector2 hitPoint, Vector2 currentPos, Vector2 direction)
{
    // If hit ball on left edge and trying to go further left
    if (hitPoint.x < currentPos.x && direction.x < 0)
        return true;

    // If hit ball on right edge and trying to go further right
    if (hitPoint.x > currentPos.x && direction.x > 0)
        return true;

    return false;
}
```

### Step 3: Update Ball Collision Logic

In the `CalculateTrajectory` method, modify the ball collision handling:
```csharp
if (hitBall && distanceToBall < distanceToWall)
{
    Ball hitBallComponent = hit.collider.GetComponent<Ball>();
    if (hitBallComponent != null && hitBallComponent.HasFlag(BallFlags.Pinned))
    {
        // NEW: Check if this is an edge ball and we're trying to squeeze past it
        if (IsEdgeColumnBall(hitBallComponent))
        {
            // Check if we're trying to go between the ball and the wall
            if (IsSqueezeAttempt(hit.point, currentPos, currentDir))
            {
                // Stop trajectory here - can't squeeze between edge ball and wall
                trajectoryPoints.Add(hit.point);
                break;
            }
        }

        // Normal ball collision - stop at hit point
        trajectoryPoints.Add(hit.point);
        break;
    }
}
```

### Step 4: Adjust Wall Bounds for CircleCast

When calculating wall collisions, account for the check radius:
```csharp
// In CalculateTrajectory, when setting up screen bounds:
float checkRadius = ballRadius * trajectoryCheckRadiusPercent;

// Adjust bounds inward by the check radius
Vector2 screenBoundsMin = new Vector2(-horzExtent + checkRadius, -vertExtent);
Vector2 screenBoundsMax = new Vector2(horzExtent - checkRadius, vertExtent);
```

---

## Testing Plan

1. **Test CircleCast radius:**
   - Try different percentages (10%, 15%, 20%)
   - Find the sweet spot that prevents unrealistic threading but still allows fun gameplay

2. **Test edge ball blocking:**
   - Place balls on the leftmost and rightmost columns
   - Try to aim trajectory between them and the wall
   - Verify trajectory stops at the ball instead of bouncing off wall

3. **Test normal gameplay:**
   - Ensure balls in middle columns don't block unnecessarily
   - Verify wall bounces still work when there's actual space

---

## Benefits

- **More realistic aiming** - Can't thread the needle with pixel-perfect precision
- **No impossible gaps** - Edge balls properly block the narrow gap to walls
- **Configurable precision** - Percentage-based radius is intuitive and tunable
- **Simple solution** - No complex geometry calculations, just logical checks

---

## Notes

- The `trajectoryCheckRadiusPercent` of 0.15 (15%) is a good starting point
- Can be adjusted in Unity Inspector with the Range slider
- The edge ball detection only affects balls on columns 0 and max-1
- Interior balls behave normally and allow bounces past them