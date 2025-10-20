# Code Standards and Formatting Rules

## Class Structure Order

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

## Naming Conventions

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

### Grammar Rules Summary
- **Nouns** for things that represent objects or data (classes, fields, properties)
- **Verbs** for things that perform actions (methods)
- **Adjectives** for things that describe state (boolean properties/fields)
- **Past/Present tense** for events describing what happened or is happening

---

## Code Organization

### Grouping with Comments
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

### Method Organization
- Group Unity lifecycle methods together
- Group related functionality together
- Place UI handler methods at the end of the class

---

## Formatting Rules

### Spacing and Indentation
- Use 4 spaces for indentation (no tabs)
- Add blank line between different sections
- No blank lines between fields of the same type
- Single blank line between methods

### Brackets and Braces
- Opening braces on new line for classes and methods
- Opening braces on same line for control structures with single statements
- Use single-line format for simple if statements with early returns:
  ```csharp
  if (_isReloading || _currentAmmo < ammoUsed) return;
  ```

### Properties
- Use auto-properties where possible
- Prefer `{ get; private set; }` for controlled access
- Place on single line when simple

### Events
- Use null-conditional operator for invoking: `OnEventName?.Invoke()`
- Use `Action` or `Action<T>` for simple events
- Use custom delegates only when necessary

---

## Best Practices

### Early Returns
Use early returns to reduce nesting:
```csharp
public void FireWeapon(int ammoUsed)
{
    if (_isReloading || _currentAmmo < ammoUsed) return;

    // Main logic here
}
```

### Variable Declaration
- Declare variables close to first use
- Use meaningful names over comments
- Mark temporary variables with inline comments when needed:
  ```csharp
  Vector3 clickPosition = Input.mousePosition; // Temporary variable
  ```

### Unity-Specific
- Prefer `Awake()` for initialization over `Start()` when order matters
- Always unsubscribe from events in `OnDisable()`
- Use Unity's built-in types (`Vector3`, `Quaternion`, etc.)

---

## Comments Policy

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

## Example Class Following Standards

```csharp
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

    // UI Handlers
    public void OnReloadButtonClicked()
    {
        ForceReload();
    }
}
```