# CherryFramework StateService Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [StateService](#stateservice)
4. [StateAccessor](#stateaccessor)
5. [Event System](#event-system)
6. [Status System](#status-system)
7. [StateSubscription](#statesubscription)
8. [Performance Considerations](#performance-considerations)
9. [Common Issues and Solutions](#common-issues-and-solutions)
10. [Best Practices](#best-practices)
11. [Examples](#examples)
12. [Summary](#summary)

---

## Overview

The CherryFramework StateService provides a powerful event and state management system for Unity applications. It enables decoupled communication between components through a centralized event bus and status tracking system, with support for conditional subscriptions and payload data.

### Why Use StateService?

| Problem                                             | Solution with StateService                                 |
| --------------------------------------------------- | ---------------------------------------------------------- |
| Tight coupling between components                   | Decoupled event-based communication                        |
| Complex conditional logic scattered throughout code | Centralized condition-based subscriptions                  |
| Manual event handler cleanup                        | Auto-unsubscribe via IUnsubscriber                         |
| Tracking temporary vs persistent states             | Separate event (one-frame) and status (persistent) systems |
| Passing data with events                            | Type-safe payload events                                   |

### Key Features

- **Event System**: Emit and receive events with optional payload data
- **Status Tracking**: Track boolean states with lifetime information
- **Conditional Subscriptions**: Subscribe to events with custom conditions
- **One-time Subscriptions**: Auto-unsubscribe after first invocation
- **Frame-Aware**: Tracks when events were emitted/statuses changed
- **Type-Safe Payloads**: Generic payload events for data passing
- **Automatic Cleanup**: Subscriptions tied to object lifetimes
- **Debug Mode**: Optional logging for debugging

---

## Core Concepts

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      StateService                           │
├─────────────────────────────────────────────────────────────┤
│ - Dictionary<string, EventBase> _currentEvents              │
│ - Dictionary<string, EventBase> _pastEvents                 │
│ - Dictionary<string, StateStatus> _activeStatuses           │
│ - Dictionary<string, StateStatus> _inactiveStatuses         │
│ - Dictionary<object, List<StateSubscription>> _subscriptions│
│                                                             │
│ + EmitEvent(string key)                                     │
│ + EmitEvent<T>(string key, T payload)                       │
│ + SetStatus(string key) / UnsetStatus(string key)           │
│ + AddStateSubscription(condition, callback, subscriber)     │
└─────────────────────────────────────────────────────────────┘
                              │
            ┌─────────────────┼─────────────────┐
            ▼                 ▼                 ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │    Events     │ │   Statuses    │ │ Subscriptions │
    │  (1-frame)    │ │ (Persistent)  │ │  (Callbacks)  │
    └───────────────┘ └───────────────┘ └───────────────┘
            │                 │                 │
            ▼                 ▼                 ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │ BasicEvent    │ │ StateStatus   │ │ Condition     │
    │PayloadEvent<T>│ │ - EmitFrame   │ │ Callback      │
    │ - EmitFrame   │ │               │ │ DestroyAfter  │
    └───────────────┘ └───────────────┘ └───────────────┘
```

### Event vs Status

| Feature         | Event                          | Status                            |
| --------------- | ------------------------------ | --------------------------------- |
| **Duration**    | One frame (volatile)           | Persistent until changed          |
| **Usage**       | One-time notifications         | Long-lived states                 |
| **Examples**    | "PlayerDied", "LevelComplete"  | "IsGamePaused", "IsInventoryOpen" |
| **Persistence** | Cleared after one update cycle | Remains until explicitly unset    |
| **History**     | Available via GetEvent()       | Available via GetStatus()         |

### Subscription Model

Subscriptions are tied to subscriber objects for automatic cleanup:

```csharp
// Subscription tied to this object
_stateService.AddStateSubscription(
    accessor => accessor.IsEventActive("GameStarted"),
    () => Debug.Log("Game started!"),
    this // Will auto-unsubscribe when this object is destroyed
);
```

### StateAccessor

The StateAccessor provides a query interface for subscription conditions, exposing only relevant state-checking methods:

```csharp
public class StateAccessor
{
    public bool IsEventActive(string key);           // Event emitted this frame?
    public bool IsStatusJustBecameActive(string key); // Status activated this frame?
    public bool IsStatusJustBecameInactive(string key); // Status deactivated this frame?
}
```

---

## StateService

**Namespace**: `CherryFramework.StateService`

**Purpose**: Central service that manages all events, statuses, and subscriptions.

### Class Definition

```csharp
public class StateService : GeneralClassBase, ILateTickable
{
    // Constructor
    public StateService(bool debugMessages);

    // Event Methods
    public void EmitEvent<T>(string key, T payload);
    public void EmitEvent(string key);
    public bool IsEventActive(string key);
    public bool EventPassed(string key);
    public EventBase GetEvent(string key);
    public T GetPayload<T>(string key);
    public bool TryGetEvent<T>(string key, out PayloadEvent<T> result);
    public bool TryGetPayload<T>(string key, out T result);

    // Status Methods
    public void SetStatus(string key);
    public void UnsetStatus(string key);
    public bool IsStatusActive(string key);
    public bool IsStatusJustBecameActive(string key);
    public bool IsStatusInactive(string key);
    public bool IsStatusJustBecameInactive(string key);
    public StateStatus GetStatus(string key);

    // Subscription Methods
    public StateSubscription AddStateSubscription(
        Predicate<StateAccessor> condition, 
        Action callback,
        object obj = null, 
        bool destroyAfterInvoke = false
    );

    public void RemoveSubscription(StateSubscription subscription, object subscriber = null);
    public void RemoveAllSubscriptions(object subscriber);
}
```

### Constructor

```csharp
public StateService(bool debugMessages)
```

**Example**:

```csharp
// In installer
var stateService = new StateService(debugMessages: true);
DependencyContainer.Instance.BindAsSingleton(stateService);
```

### Event Methods

#### EmitEvent (with payload)

```csharp
public void EmitEvent<T>(string key, T payload)
```

**Example**:

```csharp
stateService.EmitEvent("PlayerDied", new DeathData {
    position = transform.position,
    killer = "Boss"
});
```

#### EmitEvent (simple)

```csharp
public void EmitEvent(string key)
```

**Example**:

```csharp
stateService.EmitEvent("GameStarted");
```

#### IsEventActive

```csharp
public bool IsEventActive(string key)
```

**Example**:

```csharp
if (stateService.IsEventActive("LevelComplete"))
{
    ShowVictoryScreen();
}
```

#### EventPassed

```csharp
public bool EventPassed(string key)
```

**Example**:

```csharp
if (stateService.EventPassed("TutorialShown"))
{
    // Don't show tutorial again this session
}
```

#### GetEvent

```csharp
public EventBase GetEvent(string key)
```

**Example**:

```csharp
var evt = stateService.GetEvent("PlayerDied");
if (evt != null)
{
    Debug.Log($"Player died at frame {evt.EmitFrame}");
}
```

#### GetPayload

```csharp
public T GetPayload<T>(string key)
```

**Example**:

```csharp
int score = stateService.GetPayload<int>("ScoreUpdated");
// Returns 0 if event not found or payload wrong type
```

#### TryGetEvent / TryGetPayload

```csharp
public bool TryGetEvent<T>(string key, out PayloadEvent<T> result)
public bool TryGetPayload<T>(string key, out T result)
```

**Example**:

```csharp
if (stateService.TryGetPayload<Vector3>("ExplosionPosition", out var position))
{
    SpawnExplosionEffect(position);
}
```

### Status Methods

#### SetStatus

```csharp
public void SetStatus(string key)
```

**Example**:

```csharp
stateService.SetStatus("IsGamePaused");
```

#### UnsetStatus

```csharp
public void UnsetStatus(string key)
```

**Example**:

```csharp
stateService.UnsetStatus("IsGamePaused");
```

#### IsStatusActive

```csharp
public bool IsStatusActive(string key)
```

**Example**:

```csharp
if (stateService.IsStatusActive("IsInventoryOpen"))
{
    // Don't allow movement while inventory is open
}
```

#### IsStatusJustBecameActive

```csharp
public bool IsStatusJustBecameActive(string key)
```

**Example**:

```csharp
if (stateService.IsStatusJustBecameActive("IsGamePaused"))
{
    ShowPauseMenu();
}
```

#### IsStatusInactive

```csharp
public bool IsStatusInactive(string key)
```

**Example**:

```csharp
if (stateService.IsStatusInactive("TutorialActive"))
{
    // Tutorial is not running
}
```

#### IsStatusJustBecameInactive

```csharp
public bool IsStatusJustBecameInactive(string key)
```

**Example**:

```csharp
if (stateService.IsStatusJustBecameInactive("IsGamePaused"))
{
    HidePauseMenu();
}
```

#### GetStatus

```csharp
public StateStatus GetStatus(string key)
```

**Example**:

```csharp
var status = stateService.GetStatus("BossFightActive");
if (status != null)
{
    Debug.Log($"Boss fight active for {Time.frameCount - status.EmitFrame} frames");
}
```

### Subscription Methods

#### AddStateSubscription

```csharp
public StateSubscription AddStateSubscription(
    Predicate<StateAccessor> condition, 
    Action callback,
    object obj = null, 
    bool destroyAfterInvoke = false
)
```

**Parameters**:

- `condition`: Function that checks StateAccessor and returns true to trigger
- `callback`: Action to invoke when condition is met
- `obj`: Subscriber object (for auto-cleanup, uses callback target if null)
- `destroyAfterInvoke`: If true, subscription is removed after first invocation

**Returns**: The created subscription

**Examples**:

```csharp
// Basic subscription
_stateService.AddStateSubscription(
    accessor => accessor.IsEventActive("GameStarted"),
    () => Debug.Log("Game started!"),
    this
);

// One-time subscription
_stateService.AddStateSubscription(
    accessor => accessor.IsEventActive("PlayerDied"),
    () => ShowGameOverScreen(),
    this,
    destroyAfterInvoke: true
);

// Complex condition
_stateService.AddStateSubscription(
    accessor => accessor.IsStatusActive("BossFight") && 
                accessor.IsEventActive("BossDefeated"),
    () => SpawnRewards(),
    this
);

// Status transition
_stateService.AddStateSubscription(
    accessor => accessor.IsStatusJustBecameActive("IsNightTime"),
    () => EnableNightMode(),
    this
);
```

#### RemoveSubscription

```csharp
public void RemoveSubscription(StateSubscription subscription, object subscriber = null)
```

**Example**:

```csharp
var sub = _stateService.AddStateSubscription(condition, callback, this);
// Later...
_stateService.RemoveSubscription(sub);
```

#### RemoveAllSubscriptions

```csharp
public void RemoveAllSubscriptions(object subscriber)
```

**Example**:

```csharp
// Automatically called when subscriber implements IUnsubscriber
// Can also be called manually
_stateService.RemoveAllSubscriptions(this);
```

---

## StateAccessor

**Namespace**: `CherryFramework.StateService`

**Purpose**: Query interface passed to subscription conditions for checking event and status state.

### Class Definition

```csharp
public class StateAccessor
{
    private StateService _stateService;

    public StateAccessor(StateService stateService);

    public bool IsEventActive(string key);
    public bool IsStatusJustBecameActive(string key);
    public bool IsStatusJustBecameInactive(string key);
}
```

### Methods

| Method                                   | Description                                 |
| ---------------------------------------- | ------------------------------------------- |
| `IsEventActive(string key)`              | Checks if event was emitted this frame      |
| `IsStatusJustBecameActive(string key)`   | Checks if status was activated this frame   |
| `IsStatusJustBecameInactive(string key)` | Checks if status was deactivated this frame |

### Usage in Subscriptions

```csharp
_stateService.AddStateSubscription(
    accessor => accessor.IsEventActive("PlayerHit") && 
                accessor.IsStatusJustBecameActive("Invulnerable"),
    () => PlayShieldSparkEffect(),
    this
);

_stateService.AddStateSubscription(
    accessor => accessor.IsStatusJustBecameInactive("Invulnerable"),
    () => DisableShield(),
    this
);
```

---

## Event System

### EventBase

**Namespace**: `CherryFramework.StateService`

**Purpose**: Base class for all events.

```csharp
public abstract class EventBase
{
    public int EmitFrame; // Frame number when event was emitted

    protected EventBase(int emitFrame)
    {
        EmitFrame = emitFrame;
    }
}
```

### PayloadEvent<T>

**Namespace**: `CherryFramework.StateService`

**Purpose**: Event with typed payload data.

```csharp
public class PayloadEvent<T> : EventBase
{
    public T Payload;

    public PayloadEvent(T payload, int emitFrame) : base(emitFrame)
    {
        Payload = payload;
    }
}
```

### Event Lifecycle

```
Frame 1: EmitEvent("PlayerDied") 
         → Event added to _currentEvents

Frame 1 (LateTick): 
         → Event moved to _pastEvents
         → Subscriptions processed

Frame 2: Event available via GetEvent() but IsEventActive() returns false
         → Event remains in _pastEvents for history
```

---

## Status System

### StateStatus

**Namespace**: `CherryFramework.StateService`

**Purpose**: Tracks status activation time.

```csharp
public class StateStatus : EventBase
{
    public StateStatus(int emitTime) : base(emitTime)
    {
    }
}
```

### Status Lifecycle

```
Frame 1: SetStatus("BossFight")
         → Status added to _becameActiveStatuses

Frame 1 (LateTick):
         → Status moved to _activeStatuses
         → "JustBecameActive" true for this frame only

Frame 2-10: Status remains in _activeStatuses
            → IsStatusActive() returns true
            → IsStatusJustBecameActive() returns false

Frame 11: UnsetStatus("BossFight")
          → Status added to _becameInactiveStatuses

Frame 11 (LateTick):
          → Status moved to _inactiveStatuses
          → "JustBecameInactive" true for this frame only
```

---

## Performance Considerations

### 1. Subscription Evaluation

Subscriptions are evaluated every frame during `LateTick`. Keep the number reasonable:

```csharp
// GOOD - Reasonable number of subscriptions (dozens)
for (int i = 0; i < 50; i++)
{
    _stateService.AddStateSubscription(condition, callback, this);
}

// BAD - Thousands of subscriptions will impact performance
for (int i = 0; i < 5000; i++) // Too many!
{
    _stateService.AddStateSubscription(condition, callback, this);
}
```

### 2. Condition Complexity

Keep conditions simple and efficient:

```csharp
// GOOD - Simple conditions
accessor => accessor.IsEventActive("GameStart")

// GOOD - Multiple checks but still efficient
accessor => accessor.IsStatusActive("BossFight") && 
           accessor.IsEventActive("BossDefeated")

// BAD - Expensive operations in conditions
accessor => {
    var result = ExpensiveCalculation(); // DON'T do heavy work here
    return accessor.IsEventActive("Something") && result > 100;
}
```

### 3. Payload Size

Keep payload data small and relevant:

```csharp
// GOOD - Small, relevant data
stateService.EmitEvent("PlayerDied", new DeathData {
    position = transform.position,
    killerName = "Boss"
});

// BAD - Huge data structures
stateService.EmitEvent("LevelData", entireLevelData); // Better to reference by ID
```

### 4. Subscription Cleanup

Use auto-cleanup to prevent memory leaks:

```csharp
// GOOD - Auto-cleanup with destroyAfterInvoke for one-time events
_stateService.AddStateSubscription(
    accessor => accessor.IsEventActive("TutorialComplete"),
    () => UnlockFeature(),
    this,
    destroyAfterInvoke: true
);

// GOOD - Subscriber implements IUnsubscriber for auto-cleanup
public class MyClass : GeneralClassBase // Auto-cleans on Dispose
{
    private void Setup()
    {
        _stateService.AddStateSubscription(condition, callback, this);
    }
}
```

### 5. Event History

Past events are kept indefinitely. For long-running applications, consider cleanup:

```csharp
// If needed, you could extend StateService with cleanup methods
public void ClearEventsOlderThan(int frames)
{
    // Custom cleanup logic
}
```

---

## Common Issues and Solutions

### Issue 1: Subscriptions Not Triggering

**Symptoms**: Callbacks never called even when condition should be true

**Solutions**:

```csharp
// SOLUTION 1: Check correct state method
// For events in current frame
accessor => accessor.IsEventActive("EventName")

// For status changes
accessor => accessor.IsStatusJustBecameActive("StatusName")

// SOLUTION 2: Ensure subscription exists before event
public class SafeSubscriber : InjectClass
{
    [Inject] private StateService _stateService;

    public SafeSubscriber()
    {
        // Subscribe in constructor - runs before any events
        _stateService.AddStateSubscription(
            accessor => accessor.IsEventActive("GameStarted"),
            OnGameStarted,
            this
        );
    }
}

// SOLUTION 3: Debug condition
var sub = _stateService.AddStateSubscription(
    accessor => {
        bool result = accessor.IsEventActive("Test");
        Debug.Log($"Condition evaluated: {result}");
        return result;
    },
    () => Debug.Log("Callback triggered!"),
    this
);
```

### Issue 2: Memory Leaks from Subscriptions

**Symptoms**: Subscriptions accumulate, never removed

**Solutions**:

```csharp
// SOLUTION 1: Use IUnsubscriber
public class CleanClass : GeneralClassBase // Auto-cleans on Dispose
{
    private void Setup()
    {
        _stateService.AddStateSubscription(
            accessor => accessor.IsEventActive("SomeEvent"),
            OnSomeEvent,
            this // Will auto-unsubscribe when this is disposed
        );
    }
}

// SOLUTION 2: Manual cleanup
public class ManualCleanup : MonoBehaviour
{
    [Inject] private StateService _stateService;
    private StateSubscription _subscription;

    private void Start()
    {
        _subscription = _stateService.AddStateSubscription(
            accessor => accessor.IsEventActive("SomeEvent"),
            OnSomeEvent,
            this
        );
    }

    private void OnDestroy()
    {
        _stateService.RemoveSubscription(_subscription);
    }
}

// SOLUTION 3: One-time subscriptions
_stateService.AddStateSubscription(
    accessor => accessor.IsEventActive("OneTimeEvent"),
    () => Debug.Log("This will only run once"),
    this,
    destroyAfterInvoke: true // Auto-removed after first trigger
);
```

### Issue 3: Events vs Statuses Confusion

**Symptoms**: Using events for persistent states or statuses for one-time triggers

**Solutions**:

```csharp
// WRONG - Using event for persistent state
stateService.EmitEvent("IsGamePaused"); // Only lasts one frame!

// RIGHT - Use status for persistent state
stateService.SetStatus("IsGamePaused"); // Stays until UnsetStatus

// WRONG - Using status for one-time trigger
stateService.SetStatus("PlayerDied"); // Stays forever!

// RIGHT - Use event for one-time trigger
stateService.EmitEvent("PlayerDied"); // Lasts one frame
```

### Issue 4: Payload Type Mismatch

**Symptoms**: `GetPayload<T>` returns default values or throws

**Solutions**:

```csharp
// SOLUTION 1: Use TryGetPayload for safe access
if (_stateService.TryGetPayload<int>("Score", out int score))
{
    UpdateScoreUI(score);
}
else
{
    // Handle missing or wrong type
}

// SOLUTION 2: Define event key constants with type info
public static class GameEvents
{
    public const string ScoreUpdated = "ScoreUpdated<int>";
    public const string PlayerDied = "PlayerDied<DeathData>";
}

// Usage
_stateService.EmitEvent(GameEvents.ScoreUpdated, 100);

// Later
if (_stateService.TryGetPayload<int>(GameEvents.ScoreUpdated, out int score))
{
    // Safe access
}
```

### Issue 5: Multiple Triggers

**Symptoms**: Callback called multiple times for same event

**Solutions**:

```csharp
// PROBLEM: This will trigger every frame while condition is true
_stateService.AddStateSubscription(
    accessor => accessor.IsStatusActive("BossAlive"),
    () => Debug.Log("Boss is alive!"), // Called every frame!
    this
);

// SOLUTION 1: Use JustBecameActive for one-time triggers
_stateService.AddStateSubscription(
    accessor => accessor.IsStatusJustBecameActive("BossAlive"),
    () => Debug.Log("Boss spawned!"), // Called once
    this
);

// SOLUTION 2: Use destroyAfterInvoke for one-time events
_stateService.AddStateSubscription(
    accessor => accessor.IsEventActive("BossDefeated"),
    () => Debug.Log("Boss defeated!"), // Called once then removed
    this,
    destroyAfterInvoke: true
);
```

### Issue 6: Debug Messages Overwhelming

**Symptoms**: Console flooded with state service messages

**Solution**: Control debug mode via installer

```csharp
[DefaultExecutionOrder(-10000)]
public class StateInstaller : InstallerBehaviourBase
{
    [SerializeField] private bool _debugMode;

    protected override void Install()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // Enable debug in development builds
        var stateService = new StateService(debugMessages: _debugMode);
#else
        // Never debug in release builds
        var stateService = new StateService(debugMessages: false);
#endif
        BindAsSingleton(stateService);
    }
}
```

---

## Best Practices

### 1. Define Event and Status Keys as Constants

```csharp
public static class GameEvents
{
    // Events (one-time notifications)
    public static class Events
    {
        public const string GameStarted = "game_started";
        public const string GameOver = "game_over";
        public const string LevelComplete = "level_complete";
        public const string PlayerDied = "player_died";
    }

    // Statuses (persistent states)
    public static class Statuses
    {
        public const string IsPaused = "is_paused";
        public const string IsInventoryOpen = "is_inventory_open";
        public const string IsBossFight = "is_boss_fight";
        public const string IsTutorialActive = "is_tutorial_active";
    }

    // Payload events with expected types
    public static class PayloadEvents
    {
        public const string ScoreChanged = "score_changed<int>";
        public const string HealthChanged = "health_changed<float>";
        public const string PlayerPosition = "player_position<Vector3>";
    }
}

// Usage
_stateService.EmitEvent(GameEvents.Events.GameStarted);
_stateService.SetStatus(GameEvents.Statuses.IsPaused);
```

### 2. Use Statuses for Persistent States, Events for Triggers

```csharp
// GOOD - Status for long-lasting state
_stateService.SetStatus("IsNightTime");
// ... much later ...
if (_stateService.IsStatusActive("IsNightTime"))
{
    // Still night
}

// GOOD - Event for one-time trigger
_stateService.EmitEvent("DayStarted");

// BAD - Using event for persistent state
_stateService.EmitEvent("IsNightTime"); // Only lasts one frame!

// BAD - Using status for one-time trigger
_stateService.SetStatus("PlayerDied"); // Stays forever!
```

### 3. Clean Up Subscriptions Properly

```csharp
public class MyBehaviour : BehaviourBase // Auto-cleans on destroy
{
    [Inject] private StateService _stateService;

    private void Start()
    {
        // Subscriptions automatically cleaned up when this object destroys
        _stateService.AddStateSubscription(
            accessor => accessor.IsEventActive(GameEvents.Events.GameStarted),
            OnGameStarted,
            this
        );

        // One-time subscription auto-removed after trigger
        _stateService.AddStateSubscription(
            accessor => accessor.IsEventActive(GameEvents.Events.PlayerDied),
            OnPlayerDied,
            this,
            destroyAfterInvoke: true
        );
    }

    private void OnGameStarted() { }
    private void OnPlayerDied() { }
}
```

### 4. Use Payload Events for Data

```csharp
// Define data structure
[Serializable]
public class DamageEventData
{
    public GameObject source;
    public GameObject target;
    public int damage;
    public bool isCritical;
}

// Emit with payload
public void DealDamage(GameObject target, int damage, bool critical)
{
    var damageData = new DamageEventData
    {
        source = gameObject,
        target = target,
        damage = damage,
        isCritical = critical
    };

    _stateService.EmitEvent("damage_dealt", damageData);
}

// Subscribe with payload
_stateService.AddStateSubscription(
    accessor => accessor.IsEventActive("damage_dealt"),
    () => {
        if (_stateService.TryGetPayload<DamageEventData>("damage_dealt", out var data))
        {
            Debug.Log($"{data.source} dealt {data.damage} damage to {data.target}");
            if (data.isCritical)
            {
                ShowCriticalHitEffect(data.target.transform.position);
            }
        }
    },
    this
);
```

### 5. Combine Multiple Conditions

```csharp
// Complex game logic with multiple conditions
_stateService.AddStateSubscription(
    accessor => 
        accessor.IsStatusActive(GameEvents.Statuses.IsBossFight) &&
        accessor.IsEventActive("BossPhase2") &&
        !accessor.IsStatusActive("IsPlayerDead"),
    () => {
        // Boss entered phase 2 while player is alive
        StartPhase2Sequence();
    },
    this
);

// Tutorial flow
_stateService.AddStateSubscription(
    accessor => 
        accessor.IsStatusActive("TutorialActive") &&
        accessor.IsEventActive("PlayerMoved") &&
        accessor.IsEventActive("PlayerJumped"),
    () => {
        // Player performed both required actions
        CompleteTutorialStep();
    },
    this,
    destroyAfterInvoke: true
);
```

### 6. Use for UI State Management

```csharp
public class UIManager : BehaviourBase
{
    [Inject] private StateService _stateService;

    private void Start()
    {
        // Show/hide UI based on game state
        _stateService.AddStateSubscription(
            accessor => accessor.IsStatusJustBecameActive("IsInventoryOpen"),
            () => ShowInventoryPanel(),
            this
        );

        _stateService.AddStateSubscription(
            accessor => accessor.IsStatusJustBecameInactive("IsInventoryOpen"),
            () => HideInventoryPanel(),
            this
        );

        // Show pause menu
        _stateService.AddStateSubscription(
            accessor => accessor.IsStatusJustBecameActive("IsGamePaused"),
            () => ShowPauseMenu(),
            this
        );
    }

    private void ShowInventoryPanel() { }
    private void HideInventoryPanel() { }
    private void ShowPauseMenu() { }
}
```

### 7. Use for Game Flow Control

```csharp
public class GameFlowController : BehaviourBase
{
    [Inject] private StateService _stateService;

    private void Start()
    {
        // Start game
        _stateService.AddStateSubscription(
            accessor => accessor.IsEventActive("GameInitialized"),
            () => {
                _stateService.SetStatus("Phase_Intro");
                StartCoroutine(IntroSequence());
            },
            this,
            destroyAfterInvoke: true
        );

        // Phase transitions
        _stateService.AddStateSubscription(
            accessor => accessor.IsEventActive("IntroComplete"),
            () => {
                _stateService.UnsetStatus("Phase_Intro");
                _stateService.SetStatus("Phase_Gameplay");
            },
            this,
            destroyAfterInvoke: true
        );

        // Boss trigger
        _stateService.AddStateSubscription(
            accessor => accessor.IsEventActive("ReachedBossRoom"),
            () => {
                _stateService.UnsetStatus("Phase_Gameplay");
                _stateService.SetStatus("Phase_Boss");
                StartBossFight();
            },
            this,
            destroyAfterInvoke: true
        );
    }

    private IEnumerator IntroSequence() { yield return null; }
    private void StartBossFight() { }
}
```

### 8. Debug with StateService

```csharp
public class StateDebugger : BehaviourBase
{
    [Inject] private StateService _stateService;

    [SerializeField] private bool _showDebugOverlay;

    private Dictionary<string, bool> _lastFrameStates = new();

    private void OnGUI()
    {
        if (!_showDebugOverlay) return;

        int y = 10;
        GUI.Label(new Rect(10, y, 200, 20), "--- Active Statuses ---");
        y += 25;

        // Note: Would need to extend StateService to expose status list
        // This is pseudo-code for illustration
        foreach (var status in GetActiveStatuses())
        {
            GUI.Label(new Rect(10, y, 300, 20), status);
            y += 20;
        }
    }
}
```

---

## Examples

### Complete Game State Management

```csharp
// 1. Define game events and statuses
public static class GameState
{
    public static class Events
    {
        public const string GameStarted = "game_started";
        public const string GameOver = "game_over";
        public const string LevelComplete = "level_complete";
        public const string PlayerDied = "player_died";
        public const string CheckpointReached = "checkpoint_reached";
    }

    public static class Statuses
    {
        public const string IsPlaying = "is_playing";
        public const string IsPaused = "is_paused";
        public const string IsGameOver = "is_game_over";
        public const string IsLevelComplete = "is_level_complete";
        public const string IsBossFight = "is_boss_fight";
    }

    public static class Payloads
    {
        public const string ScoreChanged = "score_changed<int>";
        public const string HealthChanged = "health_changed<float>";
        public const string ItemCollected = "item_collected<string>";
    }
}

// 2. Installer
[DefaultExecutionOrder(-10000)]
public class GameStateInstaller : InstallerBehaviourBase
{
    [SerializeField] private bool _debugState = true;

    protected override void Install()
    {
        var stateService = new StateService(_debugState);
        BindAsSingleton(stateService);
    }
}

// 3. Game manager controlling flow
public class GameManager : BehaviourBase
{
    [Inject] private StateService _state;

    private int _score;
    private float _playerHealth;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Subscribe to game events
        _state.AddStateSubscription(
            accessor => accessor.IsEventActive(GameState.Events.GameStarted),
            OnGameStarted,
            this
        );

        _state.AddStateSubscription(
            accessor => accessor.IsEventActive(GameState.Events.PlayerDied),
            OnPlayerDied,
            this
        );

        _state.AddStateSubscription(
            accessor => accessor.IsEventActive(GameState.Events.LevelComplete),
            OnLevelComplete,
            this,
            destroyAfterInvoke: true
        );

        // Subscribe to state changes
        _state.AddStateSubscription(
            accessor => accessor.IsStatusJustBecameActive(GameState.Statuses.IsPaused),
            OnGamePaused,
            this
        );

        _state.AddStateSubscription(
            accessor => accessor.IsStatusJustBecameInactive(GameState.Statuses.IsPaused),
            OnGameResumed,
            this
        );

        // Subscribe to score changes with payload
        _state.AddStateSubscription(
            accessor => accessor.IsEventActive(GameState.Payloads.ScoreChanged),
            () => {
                if (_state.TryGetPayload<int>(GameState.Payloads.ScoreChanged, out int newScore))
                {
                    UpdateScoreUI(newScore);
                }
            },
            this
        );
    }

    private void Start()
    {
        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        yield return new WaitForSeconds(1f);

        _state.SetStatus(GameState.Statuses.IsPlaying);
        _state.EmitEvent(GameState.Events.GameStarted);
    }

    public void AddScore(int amount)
    {
        _score += amount;
        _state.EmitEvent(GameState.Payloads.ScoreChanged, _score);
    }

    public void PlayerHit(float damage)
    {
        _playerHealth -= damage;
        _state.EmitEvent(GameState.Payloads.HealthChanged, _playerHealth);

        if (_playerHealth <= 0)
        {
            _state.EmitEvent(GameState.Events.PlayerDied);
        }
    }

    public void TogglePause()
    {
        if (_state.IsStatusActive(GameState.Statuses.IsPaused))
        {
            _state.UnsetStatus(GameState.Statuses.IsPaused);
        }
        else
        {
            _state.SetStatus(GameState.Statuses.IsPaused);
        }
    }

    private void OnGameStarted() => Debug.Log("Game started!");
    private void OnGamePaused() => Time.timeScale = 0f;
    private void OnGameResumed() => Time.timeScale = 1f;
    private void OnPlayerDied() => _state.EmitEvent(GameState.Events.GameOver);
    private void OnLevelComplete() => Debug.Log("Level complete!");
    private void UpdateScoreUI(int score) => Debug.Log($"Score: {score}");
}

// 4. Player controller responding to state
public class PlayerController : BehaviourBase
{
    [Inject] private StateService _state;

    private void Start()
    {
        _state.AddStateSubscription(
            accessor => accessor.IsStatusJustBecameActive(GameState.Statuses.IsGameOver),
            () => DisablePlayer(),
            this
        );

        _state.AddStateSubscription(
            accessor => accessor.IsStatusActive(GameState.Statuses.IsPaused),
            () => { /* Disable input */ },
            this
        );

        _state.AddStateSubscription(
            accessor => !accessor.IsStatusActive(GameState.Statuses.IsPaused),
            () => { /* Enable input */ },
            this
        );
    }

    private void DisablePlayer()
    {
        gameObject.SetActive(false);
    }
}

// 5. UI responding to state
public class GameUI : BehaviourBase
{
    [Inject] private StateService _state;

    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _gameOverScreen;
    [SerializeField] private TMP_Text _scoreText;

    private void Start()
    {
        _state.AddStateSubscription(
            accessor => accessor.IsStatusJustBecameActive(GameState.Statuses.IsPaused),
            () => _pauseMenu.SetActive(true),
            this
        );

        _state.AddStateSubscription(
            accessor => accessor.IsStatusJustBecameInactive(GameState.Statuses.IsPaused),
            () => _pauseMenu.SetActive(false),
            this
        );

        _state.AddStateSubscription(
            accessor => accessor.IsStatusJustBecameActive(GameState.Statuses.IsGameOver),
            () => _gameOverScreen.SetActive(true),
            this
        );

        _state.AddStateSubscription(
            accessor => accessor.IsEventActive(GameState.Payloads.ScoreChanged),
            () => {
                if (_state.TryGetPayload<int>(GameState.Payloads.ScoreChanged, out int score))
                {
                    _scoreText.text = $"Score: {score}";
                }
            },
            this
        );
    }
}
```

### Achievement System with StateService

```csharp
public class AchievementSystem : BehaviourBase
{
    [Inject] private StateService _state;

    private Dictionary<string, bool> _unlockedAchievements = new();

    private void Start()
    {
        // First blood achievement
        _state.AddStateSubscription(
            accessor => accessor.IsEventActive("EnemyKilled"),
            () => UnlockAchievement("FIRST_BLOOD"),
            this,
            destroyAfterInvoke: true
        );

        // Survivalist achievement (reach level 5 without dying)
        _state.AddStateSubscription(
            accessor => accessor.IsEventActive("LevelUp") &&
                        _state.TryGetPayload<int>("LevelUp", out int level) &&
                        level >= 5,
            () => UnlockAchievement("SURVIVALIST"),
            this
        );

        // No damage boss kill
        _state.AddStateSubscription(
            accessor => accessor.IsStatusActive("BossFight") &&
                        accessor.IsEventActive("BossDefeated") &&
                        !accessor.IsEventActive("PlayerDamaged"),
            () => UnlockAchievement("FLAWLESS_VICTORY"),
            this
        );

        // Combo master (10 kills in 10 seconds)
        int killCount = 0;
        float comboTime = 0;

        _state.AddStateSubscription(
            accessor => accessor.IsEventActive("EnemyKilled"),
            () => {
                killCount++;
                comboTime = 10f;

                if (killCount >= 10)
                {
                    UnlockAchievement("COMBO_MASTER");
                }
            },
            this
        );
    }

    private void Update()
    {
        // Decay combo timer
        if (comboTime > 0)
        {
            comboTime -= Time.deltaTime;
            if (comboTime <= 0)
            {
                killCount = 0;
            }
        }
    }

    private void UnlockAchievement(string achievementId)
    {
        if (_unlockedAchievements.ContainsKey(achievementId)) return;

        _unlockedAchievements[achievementId] = true;
        _state.EmitEvent("AchievementUnlocked", achievementId);
        Debug.Log($"Achievement unlocked: {achievementId}");
    }
}
```

### Tutorial System with StateService

```csharp
public class TutorialSystem : BehaviourBase
{
    [Inject] private StateService _state;

    [SerializeField] private GameObject _movementPrompt;
    [SerializeField] private GameObject _jumpPrompt;
    [SerializeField] private GameObject _shootPrompt;

    private bool _movementDone;
    private bool _jumpDone;
    private bool _shootDone;

    private void Start()
    {
        // Start tutorial when game starts
        _state.AddStateSubscription(
            accessor => accessor.IsEventActive("GameStarted"),
            () => {
                _state.SetStatus("TutorialActive");
                ShowMovementPrompt();
            },
            this,
            destroyAfterInvoke: true
        );

        // Movement tutorial
        _state.AddStateSubscription(
            accessor => accessor.IsStatusActive("TutorialActive") &&
                        !_movementDone &&
                        accessor.IsEventActive("PlayerMoved"),
            () => {
                _movementDone = true;
                HideMovementPrompt();
                ShowJumpPrompt();
            },
            this
        );

        // Jump tutorial
        _state.AddStateSubscription(
            accessor => accessor.IsStatusActive("TutorialActive") &&
                        _movementDone &&
                        !_jumpDone &&
                        accessor.IsEventActive("PlayerJumped"),
            () => {
                _jumpDone = true;
                HideJumpPrompt();
                ShowShootPrompt();
            },
            this
        );

        // Shoot tutorial
        _state.AddStateSubscription(
            accessor => accessor.IsStatusActive("TutorialActive") &&
                        _movementDone &&
                        _jumpDone &&
                        !_shootDone &&
                        accessor.IsEventActive("PlayerShot"),
            () => {
                _shootDone = true;
                HideShootPrompt();
                _state.UnsetStatus("TutorialActive");
                _state.EmitEvent("TutorialComplete");
            },
            this
        );

        // Timeout: if player doesn't move for 10 seconds, show hint
        float timeSinceLastMove = 0;

        _state.AddStateSubscription(
            accessor => accessor.IsStatusActive("TutorialActive") && !_movementDone,
            () => {
                timeSinceLastMove += Time.deltaTime;
                if (timeSinceLastMove > 10f)
                {
                    ShowMovementHint();
                    timeSinceLastMove = 0;
                }
            },
            this
        );
    }

    private void ShowMovementPrompt() => _movementPrompt.SetActive(true);
    private void HideMovementPrompt() => _movementPrompt.SetActive(false);
    private void ShowMovementHint() => Debug.Log("Hint: Use WASD to move");

    private void ShowJumpPrompt() => _jumpPrompt.SetActive(true);
    private void HideJumpPrompt() => _jumpPrompt.SetActive(false);

    private void ShowShootPrompt() => _shootPrompt.SetActive(true);
    private void HideShootPrompt() => _shootPrompt.SetActive(false);
}
```

---

## Summary

### ### Method Summary

| Category          | Method                                                  | Description                            |
| ----------------- | ------------------------------------------------------- | -------------------------------------- |
| **Events**        | `EmitEvent(key)`                                        | Emit simple event                      |
|                   | `EmitEvent<T>(key, payload)`                            | Emit event with payload                |
|                   | `IsEventActive(key)`                                    | Check if event active this frame       |
|                   | `EventPassed(key)`                                      | Check if event occurred previously     |
|                   | `TryGetPayload<T>(key, out T)`                          | Safely get payload                     |
| **Statuses**      | `SetStatus(key)`                                        | Activate persistent status             |
|                   | `UnsetStatus(key)`                                      | Deactivate status                      |
|                   | `IsStatusActive(key)`                                   | Check if status active                 |
|                   | `IsStatusJustBecameActive(key)`                         | Check if status activated this frame   |
|                   | `IsStatusJustBecameInactive(key)`                       | Check if status deactivated this frame |
| **Subscriptions** | `AddStateSubscription(condition, callback, subscriber)` | Add conditional subscription           |
|                   | `RemoveSubscription(subscription)`                      | Remove specific subscription           |
|                   | `RemoveAllSubscriptions(subscriber)`                    | Remove all for subscriber              |

### Key Points

| #   | Key Point                                                 | Why It Matters                                  |
| --- | --------------------------------------------------------- | ----------------------------------------------- |
| 1   | **Events are one-frame, Statuses are persistent**         | Choose the right tool for the job               |
| 2   | **Subscriptions are tied to subscriber objects**          | Automatic cleanup prevents memory leaks         |
| 3   | **Conditions use StateAccessor, not direct StateService** | Encapsulation and controlled API                |
| 4   | **JustBecameActive/Inactive detect transitions**          | Perfect for one-time reactions to state changes |
| 5   | **Payload events are type-safe**                          | Pass complex data without casting               |
| 6   | **Frame numbers are tracked**                             | Useful for debugging and time-based logic       |
| 7   | **Debug mode logs all events and status changes**         | Invaluable for development                      |
| 8   | **Subscriptions can be one-time**                         | Auto-remove after first trigger                 |

### When to Use Events vs Statuses

| Use Events For                        | Use Statuses For                   |
| ------------------------------------- | ---------------------------------- |
| One-time notifications                | Persistent game states             |
| Triggers that should only happen once | Modes (paused, inventory open)     |
| Passing data with payload             | Long-lasting conditions            |
| Frame-specific reactions              | States that can be queried anytime |
| Player actions (died, jumped)         | Game phases (boss fight, tutorial) |

The StateService provides a robust, decoupled communication system that makes complex game logic easier to manage and debug, with automatic cleanup to prevent memory leaks.
