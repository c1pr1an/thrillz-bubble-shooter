# Unity Brain Development Guidelines

This skill provides comprehensive guidelines for developing code in the Unity Brain layer, including architecture patterns, code standards, and best practices.

---

## CRITICAL ARCHITECTURAL RULES

### BubbleShooterGameToolkit: DO NOT USE

**CRITICAL:** The `Assets/BubbleShooterGameToolkit/` directory contains a **reference asset purchased for inspiration only**. It is NOT part of the actual game implementation.

**DO NOT:**
- Use toolkit classes, managers, or systems in Brain code
- Create dependencies on toolkit code
- Follow toolkit's architecture patterns unless explicitly reimplemented in Brain
- Reference toolkit namespaces in new code

**DO:**
- Study toolkit code for implementation ideas only
- Use it as a reference for bubble shooter mechanics
- Understand game patterns from it
- Implement your own versions inspired by toolkit concepts

### Namespace Requirement

**All game code MUST use the `Brain.*` namespace pattern:**
- `Brain.Managers` - Manager classes
- `Brain.Gameplay` - Gameplay logic
- `Brain.Core` - Core systems (state machine, data)
- `Brain.UI` - UI components
- `Brain.Util` - Utilities and helpers
- `Brain.Audio` - Audio management

Third-party plugins use their own namespaces (e.g., `DG.Tweening` for DOTween).

---

## BRAIN ARCHITECTURE PATTERNS

### Singleton Pattern: UnitySingleton<T>

The Brain layer uses `UnitySingleton<T>` for manager classes:

**Pattern:**
```csharp
using Brain.Util;

namespace Brain.Managers
{
    public class GameManager : UnitySingleton<GameManager>
    {
        // Implementation
    }
}
```

**Usage:**
- Simple FindObjectOfType-based singleton
- Prevents multiple instances with automatic cleanup
- Access via `ClassName.Instance`
- **DO NOT check existence before using** - Assume singletons always exist

**CRITICAL RULE:**
```csharp
// WRONG - Do not do this
if (GameManager.Exists())
{
    GameManager.Instance.DoSomething();
}

// CORRECT - Just use the instance directly
GameManager.Instance.DoSomething();
```

**Rationale:**
- Singletons are core systems that should always exist during gameplay
- If a singleton doesn't exist, that's a critical setup error that should fail loudly
- Checking existence hides bugs and makes code verbose
- The singleton pattern guarantees existence when properly initialized

**When to use:**
- Manager classes that need global access
- Systems that should only exist once per scene
- Controllers that coordinate multiple systems

---

### State Machine Pattern: StateMachine<T>

The game uses a custom generic state machine (`Brain.Core.StateMachine<T>`):

**Pattern:**
```csharp
using Brain.Core;

public enum GamePhase { Initializing, Playing, Paused }

public class GameController : UnitySingleton<GameController>
{
    private StateMachine<GamePhase> _stateMachine;

    private void Awake()
    {
        _stateMachine = new StateMachine<GamePhase>("Game State Machine");
        _stateMachine.AddState(new State<GamePhase>(GamePhase.Initializing, OnInitializingEnter, OnInitializingExit));
        _stateMachine.AddState(new State<GamePhase>(GamePhase.Playing, OnPlayingEnter, null));
    }

    private void Start()
    {
        _stateMachine.ChangeState(GamePhase.Initializing);
    }

    private void OnInitializingEnter()
    {
        // Setup logic
    }

    private void OnInitializingExit()
    {
        // Cleanup logic
    }

    private void OnPlayingEnter()
    {
        // Start gameplay
    }
}
```

**Key Features:**
- Takes any enum as phase type
- Supports enter/exit callbacks via `State<T>` objects
- Pattern: Define enum → Create states with callbacks → Add to machine → Change states

**When to use:**
- Game flow management (menu, playing, paused, game over)
- Level state management (loading, active, completing)
- Complex UI flows with distinct states

---

### MonoBehaviour Lifecycle Best Practices

**Initialization Order:**
```csharp
private void Awake()
{
    // Initialize singletons
    // Set up internal state
    // DO NOT call other components yet
}

private void OnEnable()
{
    // Subscribe to events
    // DO NOT check singleton existence - just use Instance directly
    GameManager.Instance.OnLevelComplete += HandleLevelComplete;
}

private void Start()
{
    // Safe to call other components
    // Perform first-frame setup
}

private void OnDisable()
{
    // ALWAYS unsubscribe from events (prevent memory leaks and missing reference errors)
    // DO NOT check singleton existence - just use Instance directly
    GameManager.Instance.OnLevelComplete -= HandleLevelComplete;
}

private void OnDestroy()
{
    // Final cleanup
}
```

**Critical Rules:**
- Initialize in `Awake()` for singletons
- Subscribe to events in `OnEnable()`
- **ALWAYS unsubscribe in `OnDisable()`** to prevent memory leaks and missing reference errors
- **NEVER check singleton existence** - Use `ClassName.Instance` directly
- If a singleton doesn't exist, let it fail with a clear error
- Use coroutines for multi-frame operations

---

### Object Pooling Pattern

Use `Brain.Util.ObjectPooler` for frequently spawned objects:

**When to use:**
- Bubbles, projectiles, particles
- UI elements that spawn/despawn frequently
- Effects and visual feedback objects
- Any object instantiated more than 5-10 times during gameplay

**Pattern:**
```csharp
using Brain.Util;

public class BubbleSpawner : MonoBehaviour
{
    [SerializeField] private ObjectPooler _bubblePooler;

    public void SpawnBubble(Vector3 position)
    {
        GameObject bubble = _bubblePooler.GetPooledObject();
        if (bubble != null)
        {
            bubble.transform.position = position;
            bubble.SetActive(true);
        }
    }
}
```

**Setup:**
- Pre-instantiate pools in editor (configure ObjectPooler component)
- Reduces instantiation overhead during gameplay
- Objects are disabled, not destroyed when returned to pool

---

## CODE STANDARDS

### Class Structure Order

Classes should follow this strict ordering:

1. **Constants** - `private const` or `public const`
2. **Static Fields** - Prefixed with `s_`
3. **Private Fields** - Prefixed with `_`
4. **Properties** - Public properties with getters/setters
5. **Events** - Public events and delegates
6. **Unity Lifecycle Methods** - `Awake()`, `Start()`, `Update()`, etc.
7. **Public Methods** - Methods exposed to other classes
8. **Private Methods** - Internal class methods
9. **UI Handler Methods** - Methods used as UI OnClick handlers (at the end)

---

### Naming Conventions

| Element | Format | Grammar | Examples |
|---------|--------|---------|----------|
| **Classes / Structs** | `PascalCase` | Use **nouns** | `Weapon`, `PlayerStats`, `GridManager` |
| **Interfaces** | `IPascalCase` | Use **nouns** or **adjectives** | `IWeapon`, `IReloadable` |
| **Properties / Public Fields** | `PascalCase` | Use **nouns** or **adjectives** | `MaxHealth`, `IsAlive`, `CurrentAmmo` |
| **Private Fields** | `_camelCase` | Use **nouns** or **adjectives** | `_currentHealth`, `_isVisible`, `_isReloading` |
| **Methods** | `PascalCase()` | Use **verbs** | `FireWeapon()`, `ResetPosition()`, `StartReload()` |
| **Method Parameters** | `camelCase` | Use **nouns** or **adjectives** | `int damageAmount`, `bool isEnabled` |
| **Local Variables** | `camelCase` | Use **nouns** | `clickPosition`, `ammoUsed` |
| **Temporary Variables** | `camelCase` | Avoid single letters except `i` for loops | `tempPosition`, `cachedValue` |
| **Constants** | `SCREAMING_CAPS` | Use **nouns** or **adjectives** | `MAX_HEALTH`, `DEFAULT_SPEED`, `MAX_AMMO` |
| **Static Fields** | `s_camelCase` | Use **nouns** | `s_totalEnemies`, `s_instanceCount` |
| **Events** | `OnPascalCase` | Use `On` prefix + past/present tense | `OnPlayerDied`, `OnLevelCompleted`, `OnReloadStarted` |
| **UI Handlers** | `OnElementAction` | Use `On` + element + action | `OnPlayButtonClicked()`, `OnReloadButtonPressed()` |

#### Grammar Rules Summary
- **Nouns** for things that represent objects or data (classes, fields, properties)
- **Verbs** for things that perform actions (methods)
- **Adjectives** for things that describe state (boolean properties/fields)
- **Past/Present tense** for events describing what happened or is happening

---

### Code Organization

#### Grouping with Comments
Use comments to separate logical sections within a class:

```csharp
// Constants
private const int MAX_AMMO = 30;

// Static Fields
private static int s_totalWeapons;

// Private Fields
private int _currentAmmo;

// Properties
public int CurrentAmmo { get; private set; }

// Events
public event Action OnReloadStarted;
```

#### Method Organization
- Group Unity lifecycle methods together
- Group related functionality together
- Place UI handler methods at the end of the class

---

### Formatting Rules

#### Spacing and Indentation
- Use 4 spaces for indentation (no tabs)
- Add blank line between different sections
- No blank lines between fields of the same type
- Single blank line between methods

#### Brackets and Braces
- Opening braces on new line for classes and methods
- Opening braces on same line for control structures with single statements
- Use single-line format for simple if statements with early returns:
  ```csharp
  if (_isReloading || _currentAmmo < ammoUsed) return;
  ```

#### Properties
- Use auto-properties where possible
- Prefer `{ get; private set; }` for controlled access
- Place on single line when simple

#### Events
- Use null-conditional operator for invoking: `OnEventName?.Invoke()`
- Use `Action` or `Action<T>` for simple events
- Use custom delegates only when necessary

---

### Best Practices

#### Early Returns
Use early returns to reduce nesting:

```csharp
public void FireWeapon(int ammoUsed)
{
    if (_isReloading || _currentAmmo < ammoUsed) return;

    // Main logic here
}
```

#### Variable Declaration
- Declare variables close to first use
- Use meaningful names over comments
- Mark temporary variables with inline comments when needed:
  ```csharp
  Vector3 clickPosition = Input.mousePosition; // Temporary variable
  ```

#### Unity-Specific
- Prefer `Awake()` for initialization over `Start()` when order matters
- Always unsubscribe from events in `OnDisable()`
- Use Unity's built-in types (`Vector3`, `Quaternion`, etc.)

---

### Comments Policy

**Minimal commenting approach:**
- Code should be self-documenting through clear naming
- Only add comments for:
  - Section separators (Constants, Fields, etc.)
  - Complex algorithms that aren't immediately clear
  - Temporary variables when context is needed
  - TODO items during development

**Never comment:**
- What the code does (should be clear from naming)
- Obvious functionality
- Getter/setter properties
- Simple methods

---

## COMPLETE EXAMPLE

This example class demonstrates all standards and patterns:

```csharp
using System;
using System.Collections;
using UnityEngine;
using Brain.Managers;

namespace Brain.Gameplay
{
    public class Weapon : MonoBehaviour
    {
        // Constants
        private const int MAX_AMMO = 30;
        private const float RELOAD_TIME = 2.0f;

        // Static Fields
        private static int s_totalWeapons;

        // Private Fields
        private int _currentAmmo;
        private bool _isReloading;
        private float _lastFireTime;

        // Properties
        public int CurrentAmmo { get; private set; }
        public bool IsReloading { get; private set; }
        public float FireRate { get; set; }

        // Events
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;
        public event Action<Vector3> OnWeaponFired;

        private void Awake()
        {
            _currentAmmo = MAX_AMMO;
            s_totalWeapons++;
        }

        private void OnEnable()
        {
            GameController.Instance.OnGamePaused += HandleGamePaused;
        }

        private void OnDisable()
        {
            GameController.Instance.OnGamePaused -= HandleGamePaused;
        }

        private void OnDestroy()
        {
            s_totalWeapons--;
        }

        // Public Methods
        public void FireWeapon(int ammoUsed)
        {
            if (_isReloading || _currentAmmo < ammoUsed) return;
            if (Time.time - _lastFireTime < FireRate) return;

            _currentAmmo -= ammoUsed;
            _lastFireTime = Time.time;
            OnWeaponFired?.Invoke(transform.position);

            if (_currentAmmo <= 0)
                StartReload();
        }

        public void ForceReload()
        {
            if (_isReloading) return;
            StartReload();
        }

        // Private Methods
        private void StartReload()
        {
            _isReloading = true;
            OnReloadStarted?.Invoke();
            StartCoroutine(ReloadCoroutine());
        }

        private IEnumerator ReloadCoroutine()
        {
            yield return new WaitForSeconds(RELOAD_TIME);
            _currentAmmo = MAX_AMMO;
            _isReloading = false;
            OnReloadCompleted?.Invoke();
        }

        private void HandleGamePaused(bool isPaused)
        {
            enabled = !isPaused;
        }

        // UI Handlers
        public void OnReloadButtonClicked()
        {
            ForceReload();
        }
    }
}
```

---

## Additional Notes

### Available Third-Party Tools

The following tools are integrated and available for use:

- **DOTween** - Animation library (used extensively throughout the project)
- **TextMesh Pro** - Advanced text rendering
- **NaughtyAttributes** - Inspector enhancements for better editor experience
- **NiceVibrations** - Haptic feedback integration
- **ParticleImage** - UI particle effects

Use these tools following their respective documentation and best practices.

### When in Doubt

- Follow the patterns in existing Brain code
- Prioritize readability and maintainability
- Keep code simple and self-documenting
- Ask for clarification if architecture patterns are unclear
