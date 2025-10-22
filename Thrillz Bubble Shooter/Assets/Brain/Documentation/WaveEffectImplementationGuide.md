# Wave Effect Implementation Guide
## Ball Grid Placement Bounce Animation

---

## Overview
This guide provides step-by-step instructions for implementing a wave/bounce effect that propagates through nearby balls when a new ball is added to the grid. The effect creates satisfying visual feedback that ripples outward from the placement point.

---

## Visual Effect Description
When a ball snaps to the grid:
- Immediate neighbors bounce slightly upward/outward and back
- Effect ripples through 2-3 layers of surrounding balls
- Amplitude decreases with distance from impact point
- Creates a "ripple through water" effect but with discrete ball movements
- Total duration: approximately 0.5 seconds

---

## Implementation Steps

### Step 1: Create WaveEffectManager
**File Location:** `Assets/Brain/Scripts/Managers/WaveEffectManager.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Brain.Util;
using Brain.Gameplay;

namespace Brain.Managers
{
    public class WaveEffectManager : UnitySingleton<WaveEffectManager>
    {
        [Header("Wave Effect Settings")]
        [SerializeField] private float _waveAmplitude = 0.2f;      // Initial bounce height
        [SerializeField] private float _waveDuration = 0.15f;      // Duration per animation phase
        [SerializeField] private float _delayBetweenLayers = 0.02f; // Delay between ripple layers
        [SerializeField] private int _maxWaveLayers = 3;           // How many layers outward
        [SerializeField] private float _amplitudeAttenuation = 0.05f; // Reduction per layer
        [SerializeField] private Ease _waveEase = Ease.OutElastic;  // Bouncy feel

        public void TriggerWaveEffect(Ball centerBall)
        {
            if (centerBall == null) return;
            StartCoroutine(PropagateWave(centerBall));
        }

        private IEnumerator PropagateWave(Ball centerBall)
        {
            Vector3 waveOrigin = centerBall.transform.position;
            HashSet<Ball> processedBalls = new HashSet<Ball>();
            List<Ball> currentLayer = new List<Ball>();
            List<Ball> nextLayer = new List<Ball>();

            // Add center ball to processed set
            processedBalls.Add(centerBall);

            // Start with immediate neighbors
            for (int i = 0; i < 6; i++)
            {
                Ball neighbor = centerBall.Neighbors[i];
                if (neighbor != null && !processedBalls.Contains(neighbor))
                {
                    currentLayer.Add(neighbor);
                    processedBalls.Add(neighbor);
                }
            }

            float currentAmplitude = _waveAmplitude;

            // Process each layer
            for (int layer = 0; layer < _maxWaveLayers; layer++)
            {
                if (currentLayer.Count == 0 || currentAmplitude <= 0)
                    break;

                // Animate all balls in current layer
                foreach (Ball ball in currentLayer)
                {
                    if (ball != null && (ball.Flags & BallFlags.AnimatingWave) == 0)
                    {
                        Vector3 direction = (ball.transform.position - waveOrigin).normalized;
                        ball.PlayWaveAnimation(direction, currentAmplitude, _waveDuration, _waveEase);

                        // Find neighbors for next layer
                        for (int i = 0; i < 6; i++)
                        {
                            Ball neighbor = ball.Neighbors[i];
                            if (neighbor != null && !processedBalls.Contains(neighbor))
                            {
                                nextLayer.Add(neighbor);
                                processedBalls.Add(neighbor);
                            }
                        }
                    }
                }

                // Wait before starting next layer
                yield return new WaitForSeconds(_delayBetweenLayers);

                // Prepare for next layer
                currentLayer.Clear();
                currentLayer.AddRange(nextLayer);
                nextLayer.Clear();

                // Reduce amplitude for next layer
                currentAmplitude -= _amplitudeAttenuation;
            }
        }
    }
}
```

---

### Step 2: Extend Ball.cs
**File:** `Assets/Brain/Scripts/Gameplay/Ball.cs`

Add this method to the Ball class:

```csharp
public void PlayWaveAnimation(Vector3 direction, float amplitude, float duration, Ease easeType)
{
    // Prevent overlapping wave animations
    if ((Flags & BallFlags.AnimatingWave) != 0)
        return;

    // Set animating flag
    Flags |= BallFlags.AnimatingWave;

    // Animate the first child (visual representation), not the ball itself
    Transform visual = transform.GetChild(0);
    if (visual == null)
    {
        Flags &= ~BallFlags.AnimatingWave;
        return;
    }

    // Create bounce sequence
    Sequence waveSequence = DOTween.Sequence();

    // Phase 1: Move outward in wave direction
    waveSequence.Append(visual.DOLocalMove(direction * amplitude, duration)
        .SetEase(easeType));

    // Phase 2: Return to original position
    waveSequence.Append(visual.DOLocalMove(Vector3.zero, duration)
        .SetEase(Ease.InOutSine));

    // Clear flag when animation completes
    waveSequence.OnComplete(() =>
    {
        Flags &= ~BallFlags.AnimatingWave;
    });
}
```

---

### Step 3: Modify BallFlags Enum
**File:** `Assets/Brain/Scripts/Gameplay/BallFlags.cs`

Add the new flag to the enum:

```csharp
[System.Flags]
public enum BallFlags
{
    None = 0,
    Pinned = 1 << 0,
    Falling = 1 << 1,
    Destroying = 1 << 2,
    MarkedForDestroy = 1 << 3,
    MarkedForMatch = 1 << 4,
    MarkConnected = 1 << 5,
    Root = 1 << 6,
    AnimatingWave = 1 << 7  // NEW: Add this flag
}
```

---

### Step 4: Integrate into GridManager
**File:** `Assets/Brain/Scripts/Managers/GridManager.cs`

Modify the `AddBallToGrid` method (around line 140):

```csharp
public void AddBallToGrid(Ball ball, Vector3 worldPosition)
{
    // [Existing code for finding position and snapping ball...]

    // Update neighbors
    UpdateNeighbors(ball);
    UpdateAdjacentNeighbors(ball);

    // NEW: Trigger wave effect after ball is placed
    if (WaveEffectManager.Exists())
    {
        WaveEffectManager.Instance.TriggerWaveEffect(ball);
    }

    // [Existing code for phantom ball manager...]
    PhantomBallManager.Instance.OnBallAddedToGrid(ball);
}
```

---

### Step 5: Unity Inspector Setup

1. **Create WaveEffectManager GameObject:**
   - In Unity, create empty GameObject in scene
   - Name it "WaveEffectManager"
   - Add the `WaveEffectManager` script component
   - Configure settings in Inspector

2. **Recommended Initial Settings:**
   ```
   Wave Amplitude: 0.2
   Wave Duration: 0.15
   Delay Between Layers: 0.02
   Max Wave Layers: 3
   Amplitude Attenuation: 0.05
   Wave Ease: OutElastic
   ```

3. **Alternative Presets:**

   **Subtle Effect (for frequent placements):**
   ```
   Wave Amplitude: 0.15
   Wave Duration: 0.1
   Delay Between Layers: 0.01
   Max Wave Layers: 2
   Amplitude Attenuation: 0.075
   Wave Ease: OutQuad
   ```

   **Strong Effect (for power-ups/special events):**
   ```
   Wave Amplitude: 0.3
   Wave Duration: 0.2
   Delay Between Layers: 0.03
   Max Wave Layers: 5
   Amplitude Attenuation: 0.04
   Wave Ease: OutBack
   ```

---

## Technical Notes

### Performance Considerations
- Wave effect is non-blocking (uses coroutines)
- AnimatingWave flag prevents balls from participating in multiple simultaneous waves
- DOTween handles animation interpolation efficiently
- Maximum affected balls: ~19 for 3 layers, ~37 for 5 layers

### Visual Hierarchy
- Animates ball's first child transform (visual mesh/sprite)
- Preserves actual grid position (parent transform unchanged)
- Allows other systems to query true position during animation

### Hexagonal Grid Pattern
The neighbor system uses hexagonal adjacency:
- Each ball has up to 6 neighbors
- Different offset patterns for even/odd rows
- Wave naturally follows hexagonal shape

### Timing Breakdown
With default settings:
- Layer 0 (immediate neighbors): Start at 0ms, amplitude 0.20
- Layer 1 (distance 2): Start at 20ms, amplitude 0.15
- Layer 2 (distance 3): Start at 40ms, amplitude 0.10
- Total effect duration: ~340ms

---

## Debugging Tips

1. **Visual Debug Mode:**
   Add to WaveEffectManager:
   ```csharp
   [SerializeField] private bool _debugMode = false;

   // In PropagateWave, add:
   if (_debugMode)
       Debug.Log($"Wave Layer {layer}: {currentLayer.Count} balls, amplitude: {currentAmplitude}");
   ```

2. **Test Different Scenarios:**
   - Edge balls (fewer neighbors)
   - Corner placements
   - Rapid successive placements
   - During grid scrolling

3. **Common Issues:**
   - **No animation:** Check if ball has child transform
   - **Overlapping waves:** Verify AnimatingWave flag is working
   - **Performance:** Reduce maxWaveLayers or increase delayBetweenLayers

---

## Extension Ideas

### 1. Directional Waves
Instead of radial propagation, create directional waves:
```csharp
// For horizontal wave (left to right)
Vector3 waveDirection = Vector3.right;
// Animate based on dot product with wave direction
```

### 2. Color-Based Propagation
Only propagate through matching colors:
```csharp
if (neighbor.Color == centerBall.Color)
    currentLayer.Add(neighbor);
```

### 3. Amplitude Based on Match Size
Stronger waves for bigger matches:
```csharp
float matchBonus = Mathf.Min(matchCount * 0.05f, 0.3f);
_waveAmplitude = baseAmplitude + matchBonus;
```

### 4. Sound Integration
Trigger audio based on wave layers:
```csharp
// In PropagateWave
AudioManager.Instance.PlayWaveSound(layer, currentAmplitude);
```

---

## Testing Checklist

- [ ] Wave triggers when ball is placed on grid
- [ ] Neighbors bounce in sequence (layer by layer)
- [ ] Amplitude decreases with distance
- [ ] Animation completes within ~0.5 seconds
- [ ] No visual glitches or jumps
- [ ] Performance remains smooth with multiple waves
- [ ] Works correctly at grid edges
- [ ] Integrates well with existing animations (fall, destroy)
- [ ] Flags properly set and cleared
- [ ] No interference with game logic (matching, orphan detection)

---

## References

- Original toolkit implementation: `Assets/BubbleShooterGameToolkit/Scripts/Gameplay/Animations/WaveEffectProcessor.cs`
- DOTween documentation: https://dotween.demigiant.com/documentation.php
- Hexagonal grid patterns: `Assets/Brain/Scripts/Util/GridUtils.cs`

---

## Notes

This implementation is adapted from the BubbleShooterGameToolkit reference but built specifically for the Brain architecture. It uses the same conceptual approach (layer-based propagation with attenuation) but integrates with Brain's systems and patterns.