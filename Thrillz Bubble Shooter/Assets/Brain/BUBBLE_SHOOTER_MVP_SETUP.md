# Bubble Shooter - Setup Guide

This guide explains how to set up the Bubble Shooter game in Unity after all scripts have been created.

---

## Overview

Core bubble shooter mechanics:
- ✅ Hexagonal grid (11 columns × 66 rows total)
- ✅ 60 rows of procedurally generated balls (rows 4-63)
- ✅ 6 colored balls (Yellow, Blue, Red, Green, Purple, Pink)
- ✅ Shooter at bottom center with mouse aiming
- ✅ Match 3+ same color balls to destroy them
- ✅ Orphaned balls fall when disconnected from top
- ✅ Custom trajectory physics with wall bouncing
- ✅ Scrolling grid system (grid moves down as you progress)
- ✅ Win/Lose conditions with 2-minute timer
- ✅ Seeded procedural generation for reproducible levels
- ✅ Pure gameplay focus - no menus

---

## Scene Setup

### 1. Create Scene Hierarchy

Create a new scene or modify the existing one with this structure:

```
Scene
├── Main Camera (default)
├── GameController (Empty GameObject)
├── GridContainer (Empty GameObject)
├── Shooter (Empty GameObject)
├── Managers (Empty GameObject)
│   ├── GridManager
│   ├── LevelGenerator
│   ├── GridScrollManager
│   ├── GameConditionsManager
│   ├── MatchingManager
│   ├── DestroyManager
│   └── SeparatingBallManager
└── Boundaries (Optional - for visual reference)
```

### 2. Camera Setup

**Main Camera:**
- Position: `(0, 0, -10)`
- Projection: Orthographic
- Size: `8` (adjust based on your screen resolution)
- Background: Solid color (e.g., dark blue/purple)

---

## Prefab Creation

### 1. Ball Prefabs (6 variants)

**Create 6 ball prefabs - one for each color:**

For each color (Yellow, Blue, Red, Green, Purple, Pink):

1. Create a GameObject named `Ball_Yellow` (or appropriate color)
2. Add components:
   - `SpriteRenderer` (or MeshRenderer if using 3D spheres)
   - `CircleCollider2D` (Radius: ~0.4-0.5)
   - `Ball` script (Brain.Gameplay.Ball)
3. **Set the Ball Color in Inspector:**
   - In the Ball component, set "Ball Color" to the matching enum value (Yellow/Blue/Red/Green/Purple/Pink)
4. Create visual:
   - **Option A (2D)**: Use a circle sprite (Unity: GameObject → 2D Object → Sprites → Circle)
   - **Option B (3D)**: Add a Sphere mesh (GameObject → 3D Object → Sphere, scale to ~0.8)
   - Color it appropriately (yellow for Ball_Yellow, blue for Ball_Blue, etc.)
   - **You can add custom sprites, materials, textures, and particle systems here**
5. Make it a prefab by dragging to Project folder
6. Repeat for all 6 colors

**Ball Prefab Settings:**
- Layer: Default
- Tag: Untagged
- Scale: `(1, 1, 1)` - adjust size as needed (0.8 works well for 3D spheres)
- Sorting Layer: Default (for 2D sprites)
- Order in Layer: 0

**Customization Benefits:**
- Each prefab can have unique visuals (different sprites, materials, shaders)
- Add particle systems to each prefab (e.g., sparkles on yellow, bubbles on blue)
- Add unique animations or visual effects per color
- Full control over how each color looks and behaves visually

**Required Prefabs:**
1. `Ball_Yellow` - Ball Color: Yellow (enum value 0)
2. `Ball_Blue` - Ball Color: Blue (enum value 1)
3. `Ball_Red` - Ball Color: Red (enum value 2)
4. `Ball_Green` - Ball Color: Green (enum value 3)
5. `Ball_Purple` - Ball Color: Purple (enum value 4)
6. `Ball_Pink` - Ball Color: Pink (enum value 5)

---

## GameObject Component Setup

### 1. GameController

- GameObject: `GameController` (empty)
- Add Component: `GameController` script
- This handles game initialization and state management
- No inspector settings needed

### 2. GridContainer

- GameObject: `GridContainer` (empty)
- Position: `(0, 3, 0)` - adjust Y to position grid vertically
- This will be the parent of all spawned grid balls

### 3. Shooter Setup

**Shooter GameObject:**
- Position: `(0, -6, 0)` - bottom center of screen (adjust based on camera size)
- Add Component: `LaunchContainer` script

**LaunchContainer Inspector Settings:**
- **Ball Spawn Point**: Create child GameObject at `(0, 0, 0)` and assign it here
- **Note**: Ball prefabs are referenced from GridManager (single source of truth)
- **Aim Line**:
  - Create child GameObject
  - Add `LineRenderer` component
  - Set Width: 0.1
  - Set Material: Default (or create a simple colored material)
  - Set Color: White or yellow
  - Positions will be set by code
- **Aim Line Length**: 3
- **Min Aim Angle**: 10
- **Max Aim Angle**: 170

**Shooter Hierarchy:**
```
Shooter
├── BallSpawnPoint (Empty GameObject at 0,0,0)
└── AimLine (GameObject with LineRenderer)
```

### 4. GridManager

- GameObject: `Managers/GridManager` (empty)
- Add Component: `GridManager` script

**GridManager Inspector Settings:**
- **Max Columns**: 11
- **Max Rows**: 66
- **Ball Width**: 1.0
- **Ball Height**: 0.87
- **Ball Prefabs (Size: 6)**: Drag all 6 ball prefabs in order:
  - Element 0: Ball_Yellow
  - Element 1: Ball_Blue
  - Element 2: Ball_Red
  - Element 3: Ball_Green
  - Element 4: Ball_Purple
  - Element 5: Ball_Pink
- **Grid Container**: Drag the GridContainer GameObject

### 5. LevelGenerator

- GameObject: `Managers/LevelGenerator` (empty)
- Add Component: `LevelGenerator` script

**LevelGenerator Inspector Settings:**
- **Total Rows**: 60
- **Start Row**: 4
- **Fill Rate**: 0.8 (80% of cells filled)

### 6. GridScrollManager

- GameObject: `Managers/GridScrollManager` (empty)
- Add Component: `GridScrollManager` script

**GridScrollManager Inspector Settings:**
- **Death Line Row**: 0
- **Target Buffer Rows**: 4

### 7. GameConditionsManager

- GameObject: `Managers/GameConditionsManager` (empty)
- Add Component: `GameConditionsManager` script

**GameConditionsManager Inspector Settings:**
- **Game Duration**: 120 (2 minutes)

### 8. MatchingManager

- GameObject: `Managers/MatchingManager` (empty)
- Add Component: `MatchingManager` script

**MatchingManager Inspector Settings:**
- **Min Match Count**: 3

### 9. DestroyManager

- GameObject: `Managers/DestroyManager` (empty)
- Add Component: `DestroyManager` script

**DestroyManager Inspector Settings:**
- **Delay Between Destructions**: 0.05

### 10. SeparatingBallManager

- GameObject: `Managers/SeparatingBallManager` (empty)
- Add Component: `SeparatingBallManager` script

**SeparatingBallManager Inspector Settings:**
- **Max Iterations**: 5
- **Delay Between Falls**: 0.05

---

## Testing the Game

### 1. Play Mode

Press Play in Unity. You should see:
1. Grid spawns with 60 rows of procedurally generated balls (rows 4-63)
2. Initial view shows rows 0-15 (rows 0-3 empty, rows 4-15 with balls)
3. Shooter appears at bottom with a ball
4. Aim line shows your shooting direction
5. 2-minute timer starts
6. Click to shoot - ball travels and snaps to grid
7. Matches of 3+ destroy
8. Orphaned balls fall with gravity
9. Grid moves down as bottom rows are cleared
10. Win: Clear all 60 rows
11. Lose: Ball touches row 0 OR timer expires

### 2. Debug Controls (Editor Only)

- `P` - Pause (Time.timeScale = 0)
- `O` - Resume (Time.timeScale = 1)
- `I` - Slow motion (Time.timeScale = 0.3)
- `Escape` - Restart scene

### 3. Common Issues

**Grid doesn't appear:**
- Check GridManager has all 6 Ball Prefabs assigned
- Verify Ball Prefabs array order: Yellow=0, Blue=1, Red=2, Green=3, Purple=4, Pink=5
- Check Grid Container is assigned
- Check GridContainer position is visible in camera (should be ~0, 3, 0)
- Check each ball prefab has a renderer
- Verify MaxRows is set to 66
- Check LevelGenerator is generating balls (Start Row: 4, Total Rows: 60)

**Balls don't shoot:**
- Check GridManager has ball prefabs assigned (LaunchContainer gets them from GridManager)
- Check Ball Spawn Point is assigned in LaunchContainer
- Check camera is assigned (Main Camera tag)
- Check mouse input is working

**Balls don't stick to grid:**
- Check all ball prefabs have CircleCollider2D
- Check BallLaunch script is on Ball prefab (added at runtime)
- Check grid balls have colliders enabled

**Matches don't destroy:**
- Check all managers are in scene
- Check each ball prefab has Ball Color set correctly in inspector (Yellow=0, Blue=1, Red=2, etc.)
- Check minimum match count is 3

**Wrong ball visuals spawning:**
- Verify Ball Prefabs array order in GridManager matches BallColor enum order
- Make sure each prefab has the correct Ball Color set in inspector
- Remember: Only assign prefabs in GridManager, not LaunchContainer

**Balls don't fall:**
- Check SeparatingBallManager is in scene
- Check orphaned detection logic (top row balls are roots)

---

## Customization

### Adjust Grid Spacing

In `GridManager`:
- **Ball Width**: Distance between ball centers horizontally
- **Ball Height**: Distance between ball rows vertically
- For perfect hexagons: `Ball Height = Ball Width * 0.866`

### Change Game Duration

In `GameConditionsManager`:
- **Game Duration**: Time limit in seconds (default: 120)

### Change Level Generation

In `LevelGenerator`:
- **Total Rows**: Number of rows to generate (default: 60)
- **Start Row**: First row with balls (default: 4)
- **Fill Rate**: Percentage of cells filled (default: 0.8 = 80%)

### Modify Shooting

In `LaunchContainer`:
- **Aim Line Length**: How far aim line extends
- **Min/Max Aim Angle**: Restrict shooting angles

In `BallLaunch` (script):
- **Speed**: Ball travel speed (line 20)
- **Check Distance**: Collision detection distance (line 21)

### Match Count

In `MatchingManager`:
- **Min Match Count**: Minimum balls needed to match (default: 3)

---

## Next Steps

Once core mechanics are working perfectly, you can add:

1. **Visual Polish**
   - Particle effects on destruction
   - Ball destruction animations
   - Grid scroll animations
   - Background art
   - Sound effects

2. **UI Integration**
   - Timer display
   - Win/Lose screens
   - Restart button
   - Seed display

3. **Trajectory Preview**
   - Show where ball will land
   - Bounce prediction

4. **Object Pooling**
   - Reuse balls instead of Instantiate/Destroy
   - Better performance for 60+ rows

5. **Advanced Features**
   - Special balls (bombs, rainbow)
   - Power-ups
   - Score system
   - Combo multipliers

---

## Architecture Reference

**Core Systems:**
- `GridUtils` - Hexagonal grid math and coordinate conversion
- `Ball` - Ball component with color, position, neighbors
- `BallLaunch` - Custom trajectory movement with CircleCast collision

**Game Managers:**
- `GameController` - Game initialization and state machine
- `GridManager` - 66-row grid structure and ball spawning
- `LevelGenerator` - Procedural 60-row generation with seed
- `GridScrollManager` - Grid movement and death line detection
- `GameConditionsManager` - Win/lose/timer logic
- `MatchingManager` - Flood-fill match detection
- `DestroyManager` - Sequential ball destruction
- `SeparatingBallManager` - Orphan detection and falling

**Gameplay:**
- `LaunchContainer` - Shooter controller
- `Cameras` - Cinemachine integration

**Key Algorithms:**
- **Hexagonal Grid**: Offset coordinates with row parity
- **Ball Attachment**: Distance-based nearest cell search (3x3 area)
- **Match Detection**: Recursive flood-fill from target ball
- **Orphan Detection**: Flood-fill from root balls, unmarked = orphaned
- **Grid Scroll**: Track lowest row, move grid down to maintain buffer

---

## File Reference

**Scripts Created:**
```
Assets/Brain/Scripts/
├── Core/
│   └── StateMachine.cs
│   └── State.cs
├── Gameplay/
│   ├── BallColor.cs
│   ├── BallFlags.cs
│   ├── Ball.cs
│   ├── BallLaunch.cs
│   └── LaunchContainer.cs
├── Managers/
│   ├── GameController.cs
│   ├── GridManager.cs
│   ├── LevelGenerator.cs
│   ├── GridScrollManager.cs
│   ├── GameConditionsManager.cs
│   ├── MatchingManager.cs
│   ├── DestroyManager.cs
│   └── SeparatingBallManager.cs
└── Util/
    └── GridUtils.cs
```

**Prefabs to Create:**
- Ball_Yellow.prefab (with Ball Color = Yellow)
- Ball_Blue.prefab (with Ball Color = Blue)
- Ball_Red.prefab (with Ball Color = Red)
- Ball_Green.prefab (with Ball Color = Green)
- Ball_Purple.prefab (with Ball Color = Purple)
- Ball_Pink.prefab (with Ball Color = Pink)

---

## Game Mechanics Summary

**Grid System:**
- 66 total rows (0-65)
- Row 0 is at BOTTOM (near shooter)
- Rows 0-3: Empty (death zone + buffer at bottom)
- Rows 4-63: 60 rows of balls (procedurally generated)
- Rows 0-15: Visible on screen initially
- Higher row numbers = higher on screen (row 65 at top)
- Grid moves UP as bottom rows cleared to maintain 4-row buffer above death line
- Grid never moves above starting position (anchored)

**Win Conditions:**
- Clear all 60 rows of balls

**Lose Conditions:**
- Any ball touches row 0 (death line)
- 2-minute timer expires

**Procedural Generation:**
- Seeded random using GameSeed from PlayerPrefs
- 80% fill rate (strategic gaps)
- 6 colors balanced randomly
- Reproducible with same seed

---

## Support

If you encounter issues:
1. Check Unity Console for errors
2. Verify all managers are in the scene (10 total)
3. Ensure GridManager has:
   - Max Rows = 66
   - All 6 ball prefabs assigned
   - Grid Container assigned
4. Check LevelGenerator settings (Total Rows: 60, Start Row: 4)
5. Verify ball prefabs have CircleCollider2D and Ball script
6. Use Debug.Log to trace execution
7. Test grid attachment first before other features
