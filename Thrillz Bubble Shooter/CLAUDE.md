# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Thrillz Bubble Shooter** is a Unity 2022.3.47f1 game with a custom implementation in the **Brain** layer.

### Important: About BubbleShooterGameToolkit

The `Assets/BubbleShooterGameToolkit/` directory contains a **reference asset purchased for inspiration only**. It is NOT part of the actual game implementation and should NOT be used, linked, or integrated in any way.

**DO NOT:**
- Use toolkit classes, managers, or systems in Brain code
- Create dependencies on toolkit code
- Follow toolkit's architecture patterns unless explicitly reimplemented in Brain
- Reference toolkit namespaces in new code

**DO:**
- Study toolkit code for implementation ideas
- Use it as a reference for bubble shooter mechanics
- Understand bubble shooter game patterns from it
- Implement your own versions inspired by toolkit concepts

## Architecture

### Game Architecture (Brain Layer)

All actual game implementation lives in `Assets/Brain/Scripts/` with the following structure:

**Core** (`Assets/Brain/Scripts/Core/`)
- `StateMachine<T>` - Generic state machine using enum-based phases
- `State<T>` - State pattern with enter/exit callbacks
- `LevelData` - Level configuration data structures

**Managers** (`Assets/Brain/Scripts/Managers/`)
- `GameController` - Root game flow controller with state machine
- `LevelManager` - Level loading, progression, and completion handling
- `UIManager` - UI system initialization and coordination
- `ScoreManager` - Score tracking and management
- `UndoStateManager` - Undo/redo functionality
- `HapticManager` - Haptic feedback integration

**Gameplay** (`Assets/Brain/Scripts/Gameplay/`)
- `Level` - Level lifecycle management (Init, Reveal, Hide, Complete)
- `Cameras` - Camera management and transitions

**UI** (`Assets/Brain/Scripts/UI/`)
- UI components and panels for the game
- Currently contains base UI structure

**Util** (`Assets/Brain/Scripts/Util/`)
- `UnitySingleton<T>` - Singleton pattern for MonoBehaviours
- `ObjectPooler` - Object pooling system
- `Helper` - Utility functions
- `JsonHelper` - JSON serialization helpers
- `PersistentObject` - DontDestroyOnLoad wrapper

### State Machine Pattern

The game uses a custom generic state machine (`Brain.Core.StateMachine<T>`):
- Takes any enum as phase type
- Supports enter/exit callbacks via `State<T>` objects
- Used in `GameController` for game phases (Initializing, Playing)
- Pattern: Define enum → Create states with callbacks → Add to machine → Change states

Example from GameController:
```csharp
public enum GamePhase { Initializing, Playing }
_stateMachine = new StateMachine<GamePhase>("Game State Machine");
_stateMachine.AddState(new State<GamePhase>(GamePhase.Initializing, OnInitializingEnter, null));
_stateMachine.ChangeState(GamePhase.Initializing);
```

### Singleton Pattern

The Brain layer uses `UnitySingleton<T>` for manager classes:
- Simple FindObjectOfType-based singleton
- Prevents multiple instances with automatic cleanup
- Access via `ClassName.Instance`
- Check existence with `ClassName.Exists()`

## Core Systems

### Game Flow

Current game flow managed by `GameController`:
1. **Initializing Phase**: Sets up game seed from PlayerPrefs, initializes managers
2. **Playing Phase**: (To be implemented) Main gameplay loop

The Level system (`LevelManager` + `Level`) handles:
- Current level tracking
- Level initialization via `Level.Init()`
- Level completion events via `OnLevelCompleted` UnityAction
- Tutorial detection (first level is tutorial)

### Level Implementation Status

The Level system is currently skeletal:
- `LevelManager.LoadCurrentLevel()` calls `LevelInstance.Init()`
- `Level.RevealLevel()` and `HideLevel()` have placeholders for board toggling
- `Level.HandleLevelCompleted()` fires completion events
- **Actual bubble shooter gameplay mechanics need to be implemented**

When implementing gameplay, refer to the toolkit for patterns but build custom systems.

### Object Pooling

Use `Brain.Util.ObjectPooler` for frequently spawned objects:
- Pre-instantiate pools in editor
- Good candidates: bubbles, projectiles, effects, UI elements
- Reduces instantiation overhead during gameplay

## Development Notes

### Unity Editor Workflow

This is a Unity project - development happens primarily in Unity Editor:
- Open project in Unity 2022.3.47f1
- Press Play in editor to test
- Build via File → Build Settings
- No command-line build commands are used

### Third-Party Integrations

Key plugins (`Assets/Plugins/` and `Assets/Third-Party/`):
- **DOTween** - Animation library (used extensively throughout)
- **TextMesh Pro** - Text rendering
- **NaughtyAttributes** - Inspector enhancements
- **Toony Colors Pro** - Shader/visual effects
- **NiceVibrations** - Haptic feedback
- **ParticleImage** - UI particle effects

### Unity Packages

Key packages (`Packages/manifest.json`):
- Unity Ads (4.4.2)
- Unity Purchasing (4.11.0)
- Unity Analytics (3.8.1)
- Cinemachine (2.10.1)
- Input System (1.7.0)
- Universal Render Pipeline (URP)

### Namespace Convention

All game code uses the `Brain.*` namespace pattern:
- `Brain.Managers` - Manager classes
- `Brain.Gameplay` - Gameplay logic
- `Brain.Core` - Core systems (state machine, data)
- `Brain.UI` - UI components
- `Brain.Util` - Utilities and helpers
- `Brain.Audio` - Audio management (if implemented)

Third-party plugins use their own namespaces (e.g., `DG.Tweening` for DOTween).

### Random Seed Management

Game uses seeded random for reproducibility:
- Seed stored in PlayerPrefs as "GameSeed"
- Initialized in `GameController.OnInitializingEnter()`
- Use `UnityEngine.Random` for deterministic behavior

### Debugging

Editor-only debug controls in `GameController`:
- `P` - Pause (Time.timeScale = 0)
- `O` - Resume (Time.timeScale = 1)
- `I` - Slow motion (Time.timeScale = 0.3)
- `Escape` - Restart scene

Frame rate set to 120 FPS in editor, 60 FPS in builds.

### MonoBehaviour Lifecycle

When modifying managers:
- Initialize in `Awake()` for singletons
- Subscribe to events in `OnEnable()`
- Always unsubscribe in `OnDisable()` to prevent memory leaks and missing reference errors
- Use coroutines for multi-frame operations

### Current Implementation Patterns

**Game Initialization:**
1. `GameController.Awake()` sets up state machine
2. `GameController.Start()` configures application (multitouch, frame rate)
3. State changes to `GamePhase.Initializing`
4. `OnInitializingEnter()` loads match seed, initializes `UIManager`
5. (Playing phase transition to be implemented)

**Level Flow (Placeholder):**
1. `LevelManager.SetCurrentLevelIndex(int)` sets level
2. `LevelManager.LoadCurrentLevel()` calls `LevelInstance.Init()`
3. `Level.Init()` resets level state
4. (Gameplay loop to be implemented)
5. `Level.HandleLevelCompleted(bool)` fires `OnLevelCompleted` event
6. `LevelManager.HandleLevelEnded(bool)` receives completion callback

## Reference Assets

### BubbleShooterGameToolkit Reference

Location: `Assets/BubbleShooterGameToolkit/`

Useful reference implementations to study (do NOT integrate):
- **Gameplay/Managers/LevelManager.cs** - Complete game loop with ball grid, matching, destruction
- **Gameplay/BubbleContainers/** - Ball launching and trajectory system
- **Gameplay/PlayObjects/** - Ball types, powerups, attachments
- **LevelSystem/** - Level loading, data structures, procedural generation
- **System/EventManager.cs** - Event-driven architecture pattern
- **Settings/** - ScriptableObject-based configuration system
- **CommonUI/** - Menu and popup management patterns

Documentation: https://candy-smith.gitbook.io/bubble-shooter-toolkit/getting-started/specification

Use as inspiration to implement your own custom systems in the Brain layer.

## CRITICAL: Planning Over Coding
**THIS IS THE MOST IMPORTANT RULE:**
- **99% of interactions should be planning and alignment - NOT coding**
- **ALWAYS prioritize planning over implementation**
- **The user is NOT an engineer - ensure FULL alignment before ANY coding**
- **If the user and AI are not fully aligned, NEVER attempt to code**
- **Coding is an insignificant part of the interaction - planning is everything**
- **Always engage in thorough discussion and planning before implementation**
- **Repeatedly check for understanding and alignment throughout planning**

## Code Implementation Rules
**CRITICAL: Always follow these rules before doing ANY work:**
- **ALWAYS explicitly ask permission before creating any files, scripts, or documentation**
- **ONLY do exactly what the user asks for - nothing more, nothing less**
- Never use vague terms like "ready to implement" or "ready to code"
- Wait for clear permission before creating anything
- Do not take initiative to create files, plan ahead, or do extra work without explicit request

## Communication and Formatting Guidelines
**Follow these formatting and communication rules in all responses:**

### Formatting Standards
- Use dashes (---) to separate sections for better visual organization
- Add proper spacing between sections and bullet points
- Use **bold headers** with clear hierarchy
- Format options and recommendations clearly with proper spacing
- Always mark recommendations with **(Recommended)** when giving advice

### Communication Style
- Be conversational and natural, not overly verbose or technical
- Don't treat the user like a beginner unless they specifically ask for basic explanations
- Keep responses concise and focused - avoid unnecessary preamble or postamble
- Get straight to the point without excessive explanation
- Use human-like language, not AI-formal speech patterns
- When giving options, clearly state your recommendation and reasoning