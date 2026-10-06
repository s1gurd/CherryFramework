# CherryFramework TickDispatcher Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [Ticker](#ticker)
4. [Tickable Interfaces](#tickable-interfaces)
5. [TickerBehaviour](#tickerbehaviour)
6. [Performance Considerations](#performance-considerations)
7. [Common Issues and Solutions](#common-issues-and-solutions)
8. [Best Practices](#best-practices)
9. [Examples](#examples)

---

## Overview

The CherryFramework TickDispatcher provides a centralized update management system that replaces Unity's traditional `Update()`, `LateUpdate()`, and `FixedUpdate()` methods with a more flexible and performant tick-based architecture. It enables fine-grained control over update frequencies and automatic cleanup of update registrations.

### Why Use TickDispatcher?

| Problem with Unity's Update                        | Solution with TickDispatcher                          |
| -------------------------------------------------- | ----------------------------------------------------- |
| Every MonoBehaviour runs Update regardless of need | Register only components that need updates            |
| No built-in throttling of update frequency         | Per-component tick periods (e.g., 0.2s = 5 fps)       |
| Manual cleanup of update registrations             | Auto-unsubscribe via IUnsubscriber                    |
| Mixed update types in single component             | Separate interfaces for Update/LateUpdate/FixedUpdate |
| No central control over update order               | Ticker manages all updates centrally                  |

### Key Features

- **Multiple Update Types**: Support for Update, LateUpdate, and FixedUpdate equivalents
- **Configurable Frequencies**: Set custom tick periods per component
- **Automatic Cleanup**: Subscriptions auto-remove when objects are destroyed
- **Activity Checking**: Optional MonoBehaviour activity validation
- **Centralized Management**: Single point of control for all updates
- **Separate Interfaces Per Phase**: one interface for each update phase, so you implement only what you need
- **Performance Optimized**: Only tick when needed, based on time elapsed

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                         Ticker                              │
├─────────────────────────────────────────────────────────────┤
│ - List<Tickable> _tickables                                 │
│ - List<LateTickable> _lateTickables                         │
│ - List<FixedTickable> _fixedTickables                       │
│ + Register(obj, tickPeriod)                                 │
│ + UnRegister(obj)                                           │
└─────────────────────────────────────────────────────────────┘
                              │
            ┌─────────────────┼─────────────────┐
            ▼                 ▼                 ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │   ITickable   │ │ ILateTickable │ │ IFixedTickable│
    │   (Update)    │ │  (LateUpdate) │ │ (FixedUpdate) │
    └───────────────┘ └───────────────┘ └───────────────┘
            │                 │                 │
            ▼                 ▼                 ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │ Tick(float dt) │ │LateTick(float)│ │FixedTick(float)│
    └───────────────┘ └───────────────┘ └───────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │ TickerBehaviour │
                    │ (MonoBehaviour) │
                    └─────────────────┘
```

---

## Core Concepts

### Key Components

| Component         | Purpose                                                   |
| ----------------- | --------------------------------------------------------- |
| `Ticker`          | Central manager that processes all registered tickables   |
| `ITickable`       | Interface for components that need per-frame updates      |
| `ILateTickable`   | Interface for components that need late updates           |
| `IFixedTickable`  | Interface for components that need fixed timestep updates |
| `TickerBehaviour` | Unity MonoBehaviour that drives the Ticker                |
| `TickableBase<T>` | Internal base class for tickable wrappers - `private`, you cannot use it |

### Tick Periods

Each tickable can specify a custom tick period (minimum time between ticks):

| Period | Effective FPS | Use Case                                     |
| ------ | ------------- | -------------------------------------------- |
| 0f     | Every frame   | Critical updates (input, camera follow)      |
| 0.1f   | 10 fps        | Frequent but non-critical (UI animations)    |
| 0.2f   | 5 fps         | Moderate frequency (enemy AI updates)        |
| 0.5f   | 2 fps         | Low frequency (health regen, status effects) |
| 1.0f   | 1 fps         | Very low frequency (analytics, logging)      |

### Update Flow

```
Unity MonoBehaviour Loop:
    Update() → TickerBehaviour.Update() → Ticker.Update() → ITickable.Tick()

    LateUpdate() → TickerBehaviour.LateUpdate() → Ticker.LateUpdate() → ILateTickable.LateTick()

    FixedUpdate() → TickerBehaviour.FixedUpdate() → Ticker.FixedUpdate() → IFixedTickable.FixedTick()
```

---

## Ticker

**Namespace**: `CherryFramework.TickDispatcher`

**Purpose**: Central manager that processes all registered tickables and invokes their update methods at appropriate intervals.

### Class Definition

```csharp
public partial class Ticker : GeneralClassBase
{
    // Constructor
    public Ticker();

    // Registration Methods
    public void Register(ITickableBase obj, float tickPeriod = 0f);
    public void Register<T>(T obj, bool checkActivity, float tickPeriod = 0f) where T : MonoBehaviour, ITickableBase;

    // Removal
    public void UnRegister(ITickableBase obj);

    // Internal Update Methods (called by TickerBehaviour)
    internal void Update();
    internal void LateUpdate();
    internal void FixedUpdate();
}
```

### Constructor

```csharp
public Ticker()
```

Creates a new Ticker and automatically creates a GameObject with `TickerBehaviour` to drive updates.

**Example**:

```csharp
var ticker = new Ticker(); // Automatically creates TickerBehaviour
```

### Registration Methods

#### Register (simple)

```csharp
public void Register(ITickableBase obj, float tickPeriod = 0f)
```

Registers any tickable object. An object may implement **several** tickable
interfaces at once (e.g. `ITickable` + `ILateTickable` + `IFixedTickable`) -
it is then registered in every matching update phase with the same period.

**Parameters**:

- `obj`: The object to register (must implement at least one of the tickable interfaces)
- `tickPeriod`: Minimum time in seconds between ticks (0 = every frame)

**Timing note**: each registered entry seeds its `LastTick` with the current
`Time.time` at registration, and a tick only runs once
`Time.time >= LastTick + tickPeriod`. So with `tickPeriod = 0.2f` the **first
tick happens about 0.2 s after registration** - not in the same frame - and its
`deltaTime` is about 0.2. Only `tickPeriod = 0` ticks every frame, and then the
first `deltaTime` is ~0.

If the object implements `IUnsubscriber`, the Ticker automatically hooks an
unsubscription into it (see [Automatic Cleanup](#automatic-cleanup)).

**Example**:

```csharp
public class EnemyAI : ITickable
{
    public void Tick(float deltaTime)
    {
        // Update logic
    }
}

var enemy = new EnemyAI();
_ticker.Register(enemy, 0.2f); // Tick at 5 fps
```

#### Register with Activity Check

```csharp
public void Register<T>(T obj, bool checkActivity, float tickPeriod = 0f) 
    where T : MonoBehaviour, ITickableBase
```

Registers a MonoBehaviour tickable with optional activity checking.

**Parameters**:

- `obj`: MonoBehaviour that implements a tickable interface
- `checkActivity`: If true, only tick when MonoBehaviour is active and enabled
- `tickPeriod`: Minimum time between ticks

**Example**:

```csharp
public class PlayerController : MonoBehaviour, ITickable
{
    private void Start()
    {
        // Only tick when this component is active and enabled
        _ticker.Register(this, checkActivity: true, tickPeriod: 0f);
    }

    public void Tick(float deltaTime)
    {
        // Will only run if gameObject.activeInHierarchy and enabled
        HandleInput();
    }
}
```

### Removal Methods

#### UnRegister

```csharp
public void UnRegister(ITickableBase obj)
```

Removes a tickable from **every** update phase it was registered in - this
mirrors `Register`, which adds the object to all matching phases. An object
implementing `ITickable` and `ILateTickable` is therefore fully removed by a
single `UnRegister` call.

> Before this was fixed, `UnRegister` used a `switch` and stopped at the first
> matching interface, so a multi-interface object stayed registered in the
> remaining phases and kept ticking after `OnDestroy`. If you are upgrading,
> this is the one behavioural change to be aware of.

**Example**:

```csharp
public class Enemy : MonoBehaviour, ITickable
{
    private void OnDestroy()
    {
        _ticker.UnRegister(this); // Manual cleanup
    }
}
```

### Internal Update Methods

These methods are called by `TickerBehaviour` and should not be called directly:

```csharp
internal void Update()
{
    // Process ITickable objects
    // Check tick periods
    // Invoke Tick() on objects whose time has come
}

internal void LateUpdate()
{
    // Process ILateTickable objects
}

internal void FixedUpdate()
{
    // Process IFixedTickable objects
}
```

### Automatic Cleanup

The Ticker integrates with `IUnsubscriber` for automatic cleanup:

```csharp
public class AutoCleanupComponent : ITickable, IUnsubscriber
{
    public void AddUnsubscription(Action action)
    {
        // Ticker will call this to register cleanup
    }
}

// When object is disposed/destroyed, it auto-unregisters
```

---

## Tickable Interfaces

### ITickableBase

**Namespace**: `CherryFramework.TickDispatcher`

**Purpose**: Base marker interface for all tickable types.

```csharp
public interface ITickableBase
{
    // Marker interface - no members
}
```

### ITickable

**Namespace**: `CherryFramework.TickDispatcher`

**Purpose**: Interface for components that need per-frame updates (equivalent to Unity's Update).

```csharp
public interface ITickable : ITickableBase
{
    void Tick(float deltaTime);
}
```

**Parameters**:

- `deltaTime`: Time in seconds since the last tick (accounts for tick period)

**Example**:

```csharp
public class MovementController : ITickable
{
    private Transform _transform;
    private float _speed;

    public void Tick(float deltaTime)
    {
        // Move based on actual time passed
        _transform.Translate(Vector3.forward * _speed * deltaTime);
    }
}
```

### ILateTickable

**Namespace**: `CherryFramework.TickDispatcher`

**Purpose**: Interface for components that need updates after all regular updates (equivalent to Unity's LateUpdate).

```csharp
public interface ILateTickable : ITickableBase
{
    void LateTick(float deltaTime);
}
```

**Use Cases**:

- Camera follow (after all movement)
- IK adjustments
- Post-processing updates

**Example**:

```csharp
public class CameraFollow : ILateTickable
{
    private Transform _target;
    private Transform _camera;

    public void LateTick(float deltaTime)
    {
        // Update after all movement is done
        _camera.position = Vector3.Lerp(_camera.position, _target.position, 5f * deltaTime);
    }
}
```

### IFixedTickable

**Namespace**: `CherryFramework.TickDispatcher`

**Purpose**: Interface for components that need fixed timestep updates (equivalent to Unity's FixedUpdate).

```csharp
public interface IFixedTickable : ITickableBase
{
    void FixedTick(float deltaTime);
}
```

**Use Cases**:

- Physics calculations
- Rigidbody forces
- Network state updates

**Example**:

```csharp
public class PhysicsObject : IFixedTickable
{
    private Rigidbody _rigidbody;

    public void FixedTick(float deltaTime)
    {
        // Apply forces at fixed timestep
        _rigidbody.AddForce(Vector3.down * 9.81f, ForceMode.Acceleration);
    }
}
```

---

## TickerBehaviour

**Namespace**: `CherryFramework.TickDispatcher`

**Purpose**: Unity MonoBehaviour that drives the Ticker by calling its update methods from Unity's game loop.

### Class Definition

```csharp
public class TickerBehaviour : MonoBehaviour
{
    internal void Setup(Ticker ticker);

    private void Update();
    private void LateUpdate();
    private void FixedUpdate();
}
```

### How It Works

1. **Ticker** creates a GameObject with `TickerBehaviour` when constructed
2. **TickerBehaviour** holds a reference to the Ticker
3. Unity calls MonoBehaviour methods → TickerBehaviour forwards to Ticker

### Creation

You don't need to create TickerBehaviour manually:

```csharp
// This automatically creates the TickerBehaviour
var ticker = new Ticker();

// The TickerBehaviour is on a GameObject named "Ticker"
```

---

## Performance Considerations

### 1. Tick Period Optimization

Setting appropriate tick periods can significantly reduce CPU usage:

```csharp
// GOOD - Low frequency for non-critical updates
public class EnemyAI : ITickable
{
    public void Tick(float deltaTime)
    {
        // Expensive AI calculations - run at 5 fps
        UpdatePathfinding();
        UpdateBehavior();
    }
}

// Register with 0.2s period (5 fps)
_ticker.Register(enemyAI, 0.2f);

// GOOD - Critical updates every frame
public class InputHandler : ITickable
{
    public void Tick(float deltaTime)
    {
        // Must run every frame for responsive input
        ProcessInput();
    }
}

// Register with 0f period (every frame)
_ticker.Register(inputHandler, 0f);
```

### 2. Activity Checking Overhead

Enabling `checkActivity` adds a dictionary lookup per tick:

```csharp
// GOOD - Use for MonoBehaviours that may be disabled
_ticker.Register(this, checkActivity: true, 0.1f);

// BETTER - For always-active objects, skip check
_ticker.Register(this, checkActivity: false, 0.1f);

// BEST - For non-MonoBehaviours, no check needed
_ticker.Register(nonMonoBehaviour, 0.1f);
```

### 3. Number of Registered Tickables

The Ticker loops through all registered objects each frame:

```csharp
// GOOD - Reasonable number of tickables (hundreds)
for (int i = 0; i < 200; i++)
{
    _ticker.Register(enemyPool[i], 0.1f);
}

// BAD - Thousands of tickables will impact performance
for (int i = 0; i < 5000; i++) // Too many!
{
    _ticker.Register(bulletPool[i], 0f);
}

// SOLUTION - Use pooling with active tracking
public class BulletManager : ITickable
{
    private List<Bullet> _activeBullets = new();

    public void Tick(float deltaTime)
    {
        // Manually update only active bullets
        foreach (var bullet in _activeBullets)
        {
            bullet.Tick(deltaTime);
        }
    }
}
```

### 4. Delta Time Calculation

The Ticker correctly accumulates delta time based on actual time passed:

```csharp
public void Tick(float deltaTime)
{
    // deltaTime accounts for tick period
    // If tick period is 0.2s but 0.25s passed, deltaTime = 0.25
    _position += _velocity * deltaTime;
}
```

### 5. Removal Delayed Processing

Removals are processed with a one-frame delay to avoid modifying collections during iteration:

```csharp
// UnRegister() marks the object for removal; the list is actually cleaned at
// the start of the next update cycle, so collections are never modified while
// they are being iterated. The delay is an implementation detail - always call
// the public UnRegister(), never the internal RemoveTick/RemoveLateTick helpers.
_ticker.UnRegister(enemy);
```

---

## Common Issues and Solutions

### Issue 1: Object Not Ticking

**Symptoms**: Registered object's Tick/LateTick/FixedTick never called

**Causes**:

- Object registered with wrong interface
- Tick period too long (waiting for next tick)
- Activity checking enabled and object inactive
- Object destroyed but not unregistered

**Solutions**:

```csharp
// SOLUTION 1: Verify correct interface implementation
public class MyComponent : ITickable // Must implement ITickable
{
    public void Tick(float deltaTime) { } // This will be called
}

// SOLUTION 2: Check tick period
_ticker.Register(myObject, 5f); // Ticks every 5 seconds
// Either wait 5 seconds or reduce period

// SOLUTION 3: Handle activity checking
public class MyBehaviour : MonoBehaviour, ITickable
{
    private void Start()
    {
        // Will only tick when active AND enabled
        _ticker.Register(this, checkActivity: true, 0f);
    }

    public void Tick(float deltaTime)
    {
        // If you disable the component, ticks stop
    }
}
```

### Issue 2: Object Not Unregistering

**Symptoms**: Object continues to tick after being destroyed/disposed

**Causes**:

- Not implementing IUnsubscriber
- Manual removal not called
- Ticker reference still held

**Solutions**:

```csharp
// SOLUTION 1: Implement IUnsubscriber yourself.
// Note: implementing the interface is NOT enough on its own - something has
// to call Dispose(). Ticker only *registers* the cleanup; it never calls it.
public class CleanObject : ITickable, IUnsubscriber, IDisposable
{
    private Action _cleanup;

    public void AddUnsubscription(Action action) => _cleanup += action;

    public void Tick(float deltaTime) { }

    // Whoever owns this object must call Dispose() - the Ticker will not.
    public void Dispose() => _cleanup?.Invoke();
}

// SOLUTION 2: Manual cleanup
public class ManualClean : GeneralClassBase, ITickable
{
    [Inject] private Ticker _ticker;

    public void Tick(float deltaTime) { }

    public void Cleanup() => _ticker.UnRegister(this);
}

// SOLUTION 3 (the practical one): inherit BehaviourBase or GeneralClassBase
public class GoodClass : BehaviourBase, ITickable
{
    [Inject] private Ticker _ticker;

    private void Start()
    {
       _ticker.Register(this);
    }

    public void Tick(float deltaTime) { }
    // Nothing else to do: BehaviourBase implements IUnsubscriber and invokes
    // the cleanup in OnDestroy, so the ticker unregisters automatically.
}
```

There is no `using` pattern here. `Ticker` hooks `IUnsubscriber`, never
`IDisposable`, so wrapping a registration in `using { }` does nothing - the
object stays registered after the block ends.

### Issue 3: Activity Checking Not Working

**Symptoms**: Object ticks even when disabled/inactive

**Cause**: `checkActivity` not set to true during registration

**Solution**:

```csharp
public class MyBehaviour : MonoBehaviour, ITickable
{
    private void Start()
    {
        // CORRECT: Enable activity checking
        _ticker.Register(this, checkActivity: true, 0.1f);

        // INCORRECT: No activity checking
        // _ticker.Register(this, 0.1f); // Ticks even when disabled
    }

    public void Tick(float deltaTime)
    {
        // This will only run if gameObject.activeInHierarchy 
        // AND this component is enabled
    }
}
```

---

## Best Practices

### 1. Choose the Right Update Type

```csharp
// Use ITickable for:
// - Input handling
// - Movement updates
// - Animation state updates
// - UI updates

// Use ILateTickable for:
// - Camera follow
// - Post-processing
// - IK adjustments
// - Visual effects that should happen after movement

// Use IFixedTickable for:
// - Physics forces
// - Rigidbody manipulations
// - Network state interpolation
// - Anything that should be framerate-independent
```

### 2. Set Appropriate Tick Periods

```csharp
public class PerformanceOptimized : ITickable
{
    // Critical updates - every frame
    public void Tick(float deltaTime)
    {
        HandleInput();
    }
}

public class EnemyManager : GeneralClassBase, ITickable
{
    private List<Enemy> _enemies = new();
    private float _timeSinceLastUpdate;

    public void Tick(float deltaTime)
    {
        _timeSinceLastUpdate += deltaTime;

        // Update enemy AI at 5 fps
        if (_timeSinceLastUpdate >= 0.2f)
        {
            UpdateEnemies();
            _timeSinceLastUpdate = 0;
        }
    }

    private void UpdateEnemies()
    {
        // Expensive AI calculations
    }
}

// Registration
_ticker.Register(inputHandler, 0f);        // Every frame
_ticker.Register(enemyManager, 0f);         // Still 5 fps due to internal throttling
// OR
_ticker.Register(enemyManager, 0.2f);       // External throttling
```

### 3. Use Activity Checking for MonoBehaviours

```csharp
public class OptimizedBehaviour : BehaviourBase, ITickable
{
    [Inject] private Ticker _ticker;

    private void OnEnable()
    {
        // Register when enabled
        _ticker.Register(this, checkActivity: true, 0.1f);
    }

    private void OnDisable()
    {
        // Unregister when disabled
        _ticker.UnRegister(this);
    }

    public void Tick(float deltaTime)
    {
        // This will only run while component is enabled
        // and gameObject is active
    }
}
```

### 4. Inherit from GeneralClassBase or BehaviourBase for Auto-Cleanup

```csharp
public class SelfCleaning : GeneralClassBase, ITickable
{
    public void Tick(float deltaTime)
    {
        // Update logic
    }
}


```

### 5. Pool Tickable Objects

```csharp
public class TickablePool<T> where T : ITickable, new()
{
    private Stack<T> _pool = new();
    private List<T> _active = new();
    private Ticker _ticker;
    private float _tickPeriod;

    public TickablePool(Ticker ticker, int prewarmCount, float tickPeriod = 0f)
    {
        _ticker = ticker;
        _tickPeriod = tickPeriod;

        for (int i = 0; i < prewarmCount; i++)
        {
            _pool.Push(new T());
        }
    }

    public T Get()
    {
        T obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Pop();
        }
        else
        {
            obj = new T();
        }

        _active.Add(obj);
        _ticker.Register(obj, _tickPeriod);
        return obj;
    }

    public void Return(T obj)
    {
        _active.Remove(obj);
        _ticker.UnRegister(obj);
        _pool.Push(obj);
    }

    public void Clear()
    {
        foreach (var obj in _active)
        {
            _ticker.UnRegister(obj);
        }
        _active.Clear();
        _pool.Clear();
    }
}
```

### 6. Use Delta Time Correctly

```csharp
public class MovementHandler : ITickable
{
    private float _speed = 10f;
    private Vector3 _velocity;

    public void Tick(float deltaTime)
    {
        // CORRECT: Multiply by deltaTime for frame-rate independence
        transform.position += _velocity * deltaTime;

        // CORRECT: Use deltaTime for lerping
        float t = Mathf.Clamp01(_speed * deltaTime);
        transform.position = Vector3.Lerp(start, end, t);

        // INCORRECT: Assuming fixed time step
        // transform.position += _velocity; // Moves faster at higher FPS
    }
}
```

### 7. Group Related Updates

```csharp
// GOOD - Single tickable managing multiple systems
public class GameSystemManager : ITickable
{
    private PhysicsSystem _physics;
    private AISystem _ai;
    private UISystem _ui;

    public void Tick(float deltaTime)
    {
        // Update in specific order
        _physics.Tick(deltaTime);
        _ai.Tick(deltaTime);
        _ui.Tick(deltaTime);
    }
}

// BAD - Many individual tickables
_ticker.Register(physicsSystem, 0f);
_ticker.Register(aiSystem, 0f);
_ticker.Register(uiSystem, 0f);
// More overhead from Ticker loop
```

### 8. Debug Ticking Behavior

```csharp
public class DebuggableTickable : ITickable
{
    private string _name;
    private float _lastTickTime;
    private int _tickCount;

    public DebuggableTickable(string name)
    {
        _name = name;
    }

    public void Tick(float deltaTime)
    {
        _tickCount++;
        _lastTickTime = Time.time;

        // Actual logic here

        // Log occasionally
        if (_tickCount % 100 == 0)
        {
            Debug.Log($"{_name} ticked {_tickCount} times, last delta: {deltaTime}");
        }
    }

    public void LogStats()
    {
        Debug.Log($"{_name}: {_tickCount} ticks, last tick at {_lastTickTime}");
    }
}
```

---

## Examples

### Complete Game Loop with Ticker

```csharp
// 1. Installer setup
[DefaultExecutionOrder(-10000)]
public class TickInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        var ticker = new Ticker();
        BindAsSingleton(ticker);
    }
}

// 2. Player controller
public class PlayerController : BehaviourBase, ITickable, ILateTickable
{
    [Inject] private Ticker _ticker;
    [Inject] private InputService _input;

    private Vector3 _velocity;
    private Transform _cameraTarget;

    private void OnEnable()
    {
        _ticker.Register(this, checkActivity: true, 0f); // Every frame
    }

    private void OnDisable()
    {
        _ticker.UnRegister(this);
    }

    public void Tick(float deltaTime)
    {
        // Handle input and movement
        Vector2 moveInput = _input.GetMovement();
        _velocity = new Vector3(moveInput.x, 0, moveInput.y) * 5f;

        transform.position += _velocity * deltaTime;
    }

    public void LateTick(float deltaTime)
    {
        // Update camera target after movement
        if (_cameraTarget != null)
        {
            _cameraTarget.position = transform.position;
        }
    }
}

// 3. Enemy manager with pooled enemies
public class EnemyManager : GeneralClassBase, ITickable
{
    [Inject] private Ticker _ticker;

    private List<Enemy> _activeEnemies = new();
    private float _spawnTimer;
    private float _spawnInterval = 2f;

    public void Tick(float deltaTime)
    {
        // Spawn new enemies
        _spawnTimer += deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            SpawnEnemy();
            _spawnTimer = 0;
        }

        // Update enemy AI (enemies themselves are ITickable)
        // They're registered individually
    }

    private void SpawnEnemy()
    {
        var enemy = new Enemy();
        _activeEnemies.Add(enemy);
        _ticker.Register(enemy, 0.2f); // Enemy AI at 5 fps
    }

    public void EnemyDied(Enemy enemy)
    {
        _activeEnemies.Remove(enemy);
        _ticker.UnRegister(enemy);
    }
}

public class Enemy : ITickable
{
    private Vector3 _position;
    private float _health = 100;

    public void Tick(float deltaTime)
    {
        // AI logic - runs at 5 fps
        UpdatePathfinding();
        CheckForPlayer();

        // Health regeneration
        _health += 5f * deltaTime; // But deltaTime accounts for 0.2s period
        _health = Mathf.Min(_health, 100);
    }

    private void UpdatePathfinding() { }
    private void CheckForPlayer() { }
}

// 4. Physics system
public class PhysicsSystem : GeneralClassBase, IFixedTickable
{
    [Inject] private Ticker _ticker;

    private List<Rigidbody> _rigidbodies = new();
    private Vector3 _gravity = new Vector3(0, -9.81f, 0);

    public void Initialize()
    {
        // AddFixedTick / RemoveFixedTick are internal - use the public
        // Register / UnRegister. Register picks the right list from the
        // interfaces the object implements.
        _ticker.Register(this, 0.02f); // 50 Hz - this class implements IFixedTickable
    }

    public void FixedTick(float deltaTime)
    {
        // Apply gravity
        foreach (var rb in _rigidbodies)
        {
            rb.AddForce(_gravity, ForceMode.Acceleration);
        }

        // Physics checks
        PerformCollisionDetection();
    }

    private void PerformCollisionDetection() { }

    public void AddRigidbody(Rigidbody rb)
    {
        _rigidbodies.Add(rb);
    }
}

// 5. UI system with different tick rates
public class UIManager : GeneralClassBase, ITickable
{
    [Inject] private Ticker _ticker;
    [Inject] private HealthBar _healthBar;
    [Inject] private ScoreDisplay _scoreDisplay;
    [Inject] private FPSDisplay _fpsDisplay;

    private float _lastFPSTime;

    public void Initialize()
    {
        // Register with different tick periods
        _ticker.Register(this, 0f); // Manager itself ticks every frame
    }

    public void Tick(float deltaTime)
    {
        // Update UI elements at different rates internally
        UpdateHealthBar();      // Every frame
        UpdateScoreDisplay();   // Every frame
        UpdateFPSDisplay();     // Every frame, but FPSDisplay throttles internally
    }

    private void UpdateHealthBar()
    {
        // Critical UI - update every frame
    }

    private void UpdateScoreDisplay()
    {
        // Score updates frequently
    }

    private void UpdateFPSDisplay()
    {
        // Update FPS counter every 0.5 seconds
        if (Time.unscaledTime - _lastFPSTime > 0.5f)
        {
            _fpsDisplay.SetText($"FPS: {1f/Time.unscaledDeltaTime:F0}");
            _lastFPSTime = Time.unscaledTime;
        }
    }
}

// 6. Main game class tying everything together
public class Game : GeneralClassBase
{
    [Inject] private Ticker _ticker;
    [Inject] private PlayerController _player;
    [Inject] private EnemyManager _enemyManager;
    [Inject] private PhysicsSystem _physics;
    [Inject] private UIManager _ui;

    public void StartGame()
    {
        // All systems are already registered via their own initialization
        Debug.Log("Game started with TickDispatcher");
    }

    public override void Dispose()
    {
        // Ticker will auto-cleanup due to GeneralClassBase
        base.Dispose();
    }
}
```

### Performance Monitor Example

```csharp
// Ticker exposes only Register / UnRegister - it has no timing hooks and no
// counters, so you cannot get per-object statistics out of it. What you CAN
// do is measure the cost of your own tick callback:
public class TickPerformanceMonitor : BehaviourBase, ITickable
{
    [Inject] private Ticker _ticker;

    private readonly System.Diagnostics.Stopwatch _stopwatch = new();
    private float _reportInterval = 5f;
    private float _timeSinceLastReport;
    private int _tickCount;
    private double _totalMilliseconds;
    private double _maxMilliseconds;

    protected override void OnEnable()
    {
        base.OnEnable();
        _ticker.Register(this);
    }

    public void Tick(float deltaTime)
    {
        _stopwatch.Restart();
        DoWork();
        _stopwatch.Stop();

        double ms = _stopwatch.Elapsed.TotalMilliseconds;
        _tickCount++;
        _totalMilliseconds += ms;
        if (ms > _maxMilliseconds) _maxMilliseconds = ms;

        _timeSinceLastReport += deltaTime;
        if (_timeSinceLastReport < _reportInterval) return;

        Debug.Log($"[Tick] calls: {_tickCount}, avg: {_totalMilliseconds / _tickCount:F3}ms, max: {_maxMilliseconds:F3}ms");

        _tickCount = 0;
        _totalMilliseconds = 0;
        _maxMilliseconds = 0;
        _timeSinceLastReport = 0;
    }

    private void DoWork() { /* the work you want to measure */ }
}
```

Note the `Stopwatch` overhead is far smaller than one frame, but for accurate
per-object numbers across many objects you would have to add the measurement
inside `Ticker` itself.

---

## Summary

| Component         | Purpose                | Key Methods                  |
| ----------------- | ---------------------- | ---------------------------- |
| `Ticker`          | Central update manager | `Register()`, `UnRegister()` |
| `ITickable`       | Per-frame updates      | `Tick(float deltaTime)`      |
| `ILateTickable`   | Late updates           | `LateTick(float deltaTime)`  |
| `IFixedTickable`  | Fixed timestep updates | `FixedTick(float deltaTime)` |
| `TickerBehaviour` | Unity driver           | (Internal)                   |

### Key Benefits

- **Performance**: Control update frequency per component
- **Cleanup**: Automatic unregistration via IUnsubscriber
- **Flexibility**: Different update types for different needs
- **Centralization**: Single point of control for all updates
- **Activity Awareness**: Optional checking of MonoBehaviour state

The TickDispatcher provides a robust, performant alternative to Unity's built-in update methods with better control and automatic cleanup.
