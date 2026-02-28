# CherryFramework.BaseClasses Documentation

## Overview

The `CherryFramework.BaseClasses` namespace provides foundational abstract classes that establish consistent patterns for dependency injection, binding management, and resource cleanup across the framework.

---

## Interface: `IBindingsContainer`

**Purpose**: Provides access to a `Bindings` collection for managing data model bindings with automatic cleanup.

**Namespace**: `CherryFramework.BaseClasses`

**Properties**:

| Name       | Type       | Description                              |
| ---------- | ---------- | ---------------------------------------- |
| `Bindings` | `Bindings` | Collection of active data model bindings |

**Implementations**: `BehaviourBase`, `GeneralClassBase`

**Example**:

```csharp
public class MyComponent : BehaviourBase
{
    private void Start()
    {
        // Bindings will auto-cleanup when component is destroyed
        Bindings.CreateBinding(
            _playerModel.HealthAccessor,
            health => UpdateHealthBar(health)
        );
    }
}
```

---

## Interface: `IUnsubscriber`

**Purpose**: Enables automatic cleanup of subscriptions and event handlers when an object is destroyed.

**Namespace**: `CherryFramework.BaseClasses`

**Methods**:

| Method                                  | Description                                             |
| --------------------------------------- | ------------------------------------------------------- |
| `void AddUnsubscription(Action action)` | Registers a callback to execute on destruction/disposal |

**Implementations**: `BehaviourBase`, `GeneralClassBase`

**Example**:

```csharp
public class MyService : GeneralClassBase
{
    public MyService()
    {
        EventManager.Subscribe("GameEvent", HandleEvent);
        // Note that if you use StateService, the unsubscriptions are added automatically
        AddUnsubscription(() => EventManager.Unsubscribe("GameEvent", HandleEvent));
    }

    private void HandleEvent() { }
}
```

---

## Class: `GeneralClassBase` (Abstract)

**Purpose**: Base class for non-MonoBehaviour classes needing framework integration. Ideal for services, managers, and controllers.

**Namespace**: `CherryFramework.BaseClasses`

**Inheritance**: `InjectClass` → `IBindingsContainer` → `IUnsubscriber` → `IDisposable`

**Key Features**:

- Automatic dependency injection
- Binding management with auto-cleanup
- Subscription cleanup on disposal
- IDisposable implementation

**Properties**:

| Name       | Type       | Description                              |
| ---------- | ---------- | ---------------------------------------- |
| `Bindings` | `Bindings` | Collection of active data model bindings |

**Methods**:

| Method                                  | Description                                      |
| --------------------------------------- | ------------------------------------------------ |
| `void AddUnsubscription(Action action)` | Registers cleanup callback for disposal          |
| `virtual void Dispose()`                | Releases bindings and executes cleanup callbacks |

**Example - Service Class**:

```csharp
public class AnalyticsService : GeneralClassBase
{
    [Inject] private readonly ILogger _logger;

    private Timer _flushTimer;

    public AnalyticsService()
    {
        _flushTimer = new Timer(FlushEvents);
        AddUnsubscription(() => _flushTimer?.Dispose());
    }

    public void TrackEvent(string name)
    {
        _logger.Log($"Event: {name}");
    }

    private void FlushEvents(object state) { }
}

// Usage
var service = new AnalyticsService();
service.TrackEvent("GameStart");
service.Dispose(); // Cleans up timer and bindings
```

**Example - With Model Bindings**:

```csharp
public class ScoreManager : GeneralClassBase
{
    [Inject] private readonly ScoreModel _scoreModel;

    public ScoreManager()
    {
        Bindings.CreateBinding(
            _scoreModel.ValueAccessor,
            score => OnScoreChanged(score)
        );
    }

    private void OnScoreChanged(int score)
    {
        Debug.Log($"Score updated: {score}");
    }
}
```

---

## Class: `BehaviourBase` (Abstract)

**Purpose**: Base class for Unity MonoBehaviour components that need framework integration. Foundation for all visual components and game objects.

**Namespace**: `CherryFramework.BaseClasses`

**Inheritance**: `InjectMonoBehaviour` → `IBindingsContainer` → `IUnsubscriber`

**Key Features**:

- Automatic dependency injection via `OnEnable()`
- Binding management with auto-cleanup on destroy
- Subscription cleanup on destroy
- Unity lifecycle integration

**Properties**:

| Name       | Type       | Description                              |
| ---------- | ---------- | ---------------------------------------- |
| `Bindings` | `Bindings` | Collection of active data model bindings |

**Methods**:

| Method                                  | Description                                      |
| --------------------------------------- | ------------------------------------------------ |
| `void AddUnsubscription(Action action)` | Registers cleanup callback for OnDestroy         |
| `protected virtual void OnDestroy()`    | Releases bindings and executes cleanup callbacks |

**Example - Player Controller**:

```csharp
public class PlayerController : BehaviourBase
{
    [Inject] private readonly InputService _input;
    [Inject] private readonly PlayerModel _model;

    [SerializeField] private float _speed = 5f;
    [SerializeField] private HealthBar _healthBar;

    protected override void OnEnable()
    {
        base.OnEnable(); // Triggers injection

        // Bind to model changes
        Bindings.CreateBinding(
            _model.HealthAccessor,
            health => UpdateHealthUI(health)
        );

        // Register cleanup
        AddUnsubscription(() => {
            _input.UnregisterPlayer(this);
        });
    }

    private void UpdateHealthUI(float health)
    {
        _healthBar.SetValue(health);

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        gameObject.SetActive(false);
    }
}
```

**Example - UI Controller**:

```csharp
public class SettingsPanel : BehaviourBase
{
    [Inject] private readonly SettingsModel _settings;

    [SerializeField] private Slider _volumeSlider;
    [SerializeField] private Toggle _fullscreenToggle;
    [SerializeField] private Button _closeButton;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Initialize UI with current values
        _volumeSlider.value = _settings.Volume;
        _fullscreenToggle.isOn = _settings.IsFullscreen;

        // Setup event handlers
        _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        _closeButton.onClick.AddListener(OnCloseClicked);

        // Register cleanup
        AddUnsubscription(() => {
            _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            _fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        });
    }

    private void OnVolumeChanged(float volume)
    {
        _settings.Volume = volume;
        AudioListener.volume = volume;
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        _settings.IsFullscreen = isFullscreen;
        Screen.fullScreen = isFullscreen;
    }

    private void OnCloseClicked()
    {
        gameObject.SetActive(false);
    }
}
```



---

## Summary Table

| Class                | Type      | Use Case                                | Key Methods                          | Auto-Cleanup     |
| -------------------- | --------- | --------------------------------------- | ------------------------------------ | ---------------- |
| `GeneralClassBase`   | Abstract  | Services, Managers, Controllers         | `Dispose()`, `AddUnsubscription()`   | On `Dispose()`   |
| `BehaviourBase`      | Abstract  | GameObjects, Components, UI Controllers | `OnDestroy()`, `AddUnsubscription()` | On `OnDestroy()` |
| `IBindingsContainer` | Interface | Binding access                          | `Bindings` property                  | N/A              |
| `IUnsubscriber`      | Interface | Cleanup registration                    | `AddUnsubscription()`                | N/A              |

---

## Best Practices Quick Reference

### 1. Always Call Base Methods

```csharp
protected override void OnEnable()
{
    base.OnEnable(); // Essential for injection
    // Custom code
}

protected override void OnDestroy()
{
    // Custom cleanup
    base.OnDestroy(); // Ensures binding cleanup
}
```

### 2. Register All Cleanup for External and Custom Services

```csharp
AddUnsubscription(() => {
    // Clean up events, timers, resources
    someEvent -= Handler;
    timer?.Dispose();
    pool?.Clear();
});
```

### 3. Use Bindings for Model Connections

```csharp
Bindings.CreateBinding(
    model.PropertyAccessor,
    value => UpdateUI(value),
    true // Immediate update
);
```

 
