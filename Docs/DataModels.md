# CherryFramework Data Models and Bindings Documentation

## Table of Contents

1. [Overview](#overview)
2. [ModelService](#modelservice)
3. [DataModelBase](#datamodelbase)
4. [Accessor&lt;T&gt;](#accessort)
5. [Bindings System](#bindings-system)
6. [Value Processors](#value-processors)
7. [Storage Bridges](#storage-bridges)
8. [Code Generation](#code-generation)
9. [Best Practices](#best-practices)
10. [Examples](#examples)

---

## Overview

The CherryFramework Data Models system provides a robust, observable data layer with automatic UI binding, change notification, and persistence. It implements a variant of the MVVM pattern where models notify subscribers of changes and can be automatically persisted to storage.

### Key Features

- **Centralized Model Management**: `ModelService` handles all model instances
- **Observable Properties**: Automatic change notification
- **One-way (downward) bindings**: the model pushes changes to your subscribers. The framework does **not** push UI input back into a model - call the model property setter yourself when you need that.
- **Binding Activation Modes**: Control when a binding first fires and whether it delivers updates before the model is ready (`BindingActivation` enum)
- **Value Processing**: Transform values through pipelines
- **Automatic Persistence**: Save/load models to PlayerPrefs or custom storage, with `ReadyMode` control over when a model becomes ready after loading
- **Transient Models**: Short-lived, ID-keyed model instances managed by `ModelService`
- **Code Generation**: Auto-create model classes from templates, including generic templates
- **Singleton Management**: Global model instances
- **Typed Accessors**: `Accessor<T>` is typed per member, so a binding callback receives a real `int`, not `object`

---

### Namespaces Used by the Examples

```csharp
using System;                                  // Func<>, Action<>
using CherryFramework.BaseClasses;              // BehaviourBase
using CherryFramework.DataModels;               // DataModelBase, Accessor<T>, Bindings
using CherryFramework.DataModels.ModelDataStorageBridges; // PlayerPrefsBridge
using CherryFramework.DependencyManager;        // [Inject], InstallerBehaviourBase
using CherryFramework.StateService;             // StateService
using CherryFramework.Utils.PlayerPrefsWrapper; // PlayerPrefsData
using Newtonsoft.Json;                          // JsonIgnore
using UnityEngine;                              // MonoBehaviour, Mathf, Debug
```

`[JsonIgnore]` here is `Newtonsoft.Json.JsonIgnoreAttribute`, **not** the one
from `System.Text.Json`. Generated models use Newtonsoft, and using the wrong
attribute leaves your accessor serialized into the save file.

## ModelService

**Namespace**: `CherryFramework.DataModels`

**Purpose**: Central service for managing all model instances, their lifecycle, and persistence. This is the primary entry point for working with data models in the framework.

### Class Definition

```csharp
public class ModelService
{
    // Properties
    public readonly ModelDataStorageBridgeBase DataStorage;

    // Constructor
    public ModelService(ModelDataStorageBridgeBase bridge, bool debugMessages);

    // Singleton methods
    public T GetOrCreateSingletonModel<T>() where T : DataModelBase, new();
    public bool MakeModelSingleton<T>(T source) where T : DataModelBase;

    // Transient methods (short-lived models keyed by a string ID)
    public T GetOrCreateTransientModel<T>(string id) where T : DataModelBase, new();
    public bool ReleaseTransientModel(string id);
    public bool ReleaseTransientModel(DataModelBase model);
}
```

### Properties

| Name          | Type                         | Description                          |
| ------------- | ---------------------------- | ------------------------------------ |
| `DataStorage` | `ModelDataStorageBridgeBase` | Storage bridge for persisting models |

### Methods

| Method                                       | Description                                                            |
| -------------------------------------------- | ---------------------------------------------------------------------- |
| `GetOrCreateSingletonModel<T>()`             | Gets existing singleton or creates new one                             |
| `MakeModelSingleton<T>()`                    | Registers an existing model as singleton                               |
| `GetOrCreateTransientModel<T>(id)`          | Gets existing transient model by ID or creates a new one with that ID  |
| `ReleaseTransientModel(id)`                 | Removes a transient model by ID, returns `true` if it existed          |
| `ReleaseTransientModel(model)`              | Removes a transient model instance (looked up by its `Id`), `true` if removed |

### Usage Examples

#### Basic Setup in Installer

```csharp
[DefaultExecutionOrder(-10000)]
public class GameInstaller : InstallerBehaviourBase
{
    [SerializeField] private bool _debugModels = true;

    protected override void Install()
    {
        // Create a shared PlayerPrefs implementation (reuse the same instance
        // for ModelService and SaveGameManager so both see the same data)
        var playerPrefs = new PlayerPrefsData();

        // Create storage bridge. This is a plain constructor argument, not DI -
        // `[Inject]` means something different in this framework.
        var bridge = new PlayerPrefsBridge(playerPrefs);

        // Create model service with bridge
        var modelService = new ModelService(bridge, _debugModels);

        // Bind as singleton for injection
        BindAsSingleton(modelService);
    }
}
```

#### Accessing Models

```csharp
public class GameManager : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;

    private PlayerModel _player;
    private SettingsModel _settings;

    private void Start()
    {
        // Get or create singleton models
        _player = _modelService.GetOrCreateSingletonModel<PlayerModel>();
        _settings = _modelService.GetOrCreateSingletonModel<SettingsModel>();

        // Register callback to be called when the data becomes ready.
        // ActivateImmediate registers the binding now without invoking it with the
        // current (still false) value - it only fires when Ready actually flips to true.
        Bindings.CreateBinding(_settings.ReadyAccessor, ContinueLoading, BindingActivation.ActivateImmediate);

        // Register for persistence
        _modelService.DataStorage.RegisterModelInStorage(_player);
        _modelService.DataStorage.RegisterModelInStorage(_settings);

        // Load saved data, Ready property in models are set to True
        _modelService.DataStorage.LoadModelData(_player);
        _modelService.DataStorage.LoadModelData(_settings);
    }

    private void ContinueLoading(bool ready)
    {
        if (ready)
            // continue
    }

    private void OnApplicationQuit()
    {
        // Save all models
        _modelService.DataStorage.SaveAllModels();
    }
}
```

#### Registering Existing Models as Singletons

```csharp
public class SaveGameLoader
{
    [Inject] private readonly ModelService _modelService;

    public void LoadSavedProfile(PlayerModel savedProfile)
    {
        // Register existing model as singleton
        if (_modelService.MakeModelSingleton(savedProfile))
        {
            Debug.Log("Saved profile registered as singleton");
        }
    }
}
```

---

## DataModelBase

**Namespace**: `CherryFramework.DataModels`

**Purpose**: Abstract base class for all data models. Provides property change notification, binding management, and serialization support.

### Class Definition

```csharp
public abstract class DataModelBase
{
    // Properties
    [JsonIgnore] public string Id { get; set; }
    [JsonIgnore] public string SlotId { get; set; }
    [JsonIgnore] public bool Ready { get; set; }

    // Public field (created in the constructor)
    [JsonIgnore] public Accessor<bool> ReadyAccessor;

    // Protected fields for property access
    protected Dictionary<string, Delegate> Getters;
    protected Dictionary<string, Delegate> Setters;

    // Methods
    public void AddBinding<T>(string memberName, DownwardBindingHandler handler);
    public void RemoveBinding(DownwardBindingHandler handler);
    public T GetValue<T>(string memberName);
    public void SetValue<T>(string memberName, T value);
    public void InvokeBinding<T>(string memberName);
    public void PauseBindings(bool pause);
    public void SetDebugMode(bool value);
    public void FillFrom(object instance);
    protected void Send<T>(string memberName, T value);
}
```

### Properties Explained

| Property        | Type                           | Description                                |
| --------------- | ------------------------------ | ------------------------------------------ |
| `Id`            | `string`                       | Unique identifier for non-singleton models |
| `SlotId`        | `string`                       | Slot identifier for save game slots        |
| `Ready`         | `bool`                         | Indicates if model is fully initialized    |
| `ReadyAccessor` | `Accessor<bool>`               | Accessor for Ready property                |
| `Getters`       | `Dictionary<string, Delegate>` | Property getter functions                  |
| `Setters`       | `Dictionary<string, Delegate>` | Property setter functions                  |

### Key Methods

#### AddBinding

```csharp
public void AddBinding<T>(string memberName, DownwardBindingHandler handler)
```

Registers a binding for a model property. Whether the callback is invoked
immediately with the current value (and whether updates are delivered before the
model is ready) is determined by the handler's `ActivationMode`
(`BindingActivation` enum, see [Binding Activation Modes](#binding-activation-modes)).

**Parameters**:

- `memberName`: Name of the property
- `handler`: Binding handler with callback and activation mode

#### Send

```csharp
protected void Send<T>(string memberName, T value)
```

Notifies all subscribers of a property change.

#### FillFrom

```csharp
public void FillFrom(object instance)
```

Copies same-named, same-typed members from an existing instance (for example a
template or a DTO) into this model. For each **writable** member of this model
it looks up a source property and a source field of the same name, and writes
whatever it finds - **a field wins over a property**, because the field lookup
runs second and overwrites. Only members of the same exact type are matched.

Note that the source members are looked up across the whole hierarchy including
non-public ones, and that `Ready`, `Id` and `SlotId` are writable properties
too - so a source object with those names will overwrite them.

Reflection results are cached per `(source type, model type)` pair, so
repeated calls are cheap.

```csharp
// Populate a model from a plain data class without manual assignment
var data = new PlayerData { Health = 100, PlayerName = "Alice" };
var model = _modelService.GetOrCreateSingletonModel<PlayerModel>();
model.FillFrom(data); // copies matching members (no Send, bindings not notified)
```

### Example Model

```csharp
[Serializable]
public class PlayerModel : DataModelBase
{
    private int _health;
    private int _maxHealth;
    private string _playerName;
    private int _level;
    private int _score;
    private int _mana;
    private int _maxMana;
    private int _experience;
    private int _gold;

    public PlayerModel()
    {
        // Register property accessors
        Getters.Add(nameof(Health), new Func<int>(() => Health));
        Setters.Add(nameof(Health), new Action<int>(o => Health = o));
        HealthAccessor = new Accessor<int>(this, nameof(Health));

        Getters.Add(nameof(MaxHealth), new Func<int>(() => MaxHealth));
        Setters.Add(nameof(MaxHealth), new Action<int>(o => MaxHealth = o));
        MaxHealthAccessor = new Accessor<int>(this, nameof(MaxHealth));

        Getters.Add(nameof(PlayerName), new Func<string>(() => PlayerName));
        Setters.Add(nameof(PlayerName), new Action<string>(o => PlayerName = o));
        PlayerNameAccessor = new Accessor<string>(this, nameof(PlayerName));

        // Same three lines for every other member. A plain auto-property is
        // enough as long as you still register it and expose an accessor.
        Getters.Add(nameof(Level), new Func<int>(() => Level));
        Setters.Add(nameof(Level), new Action<int>(o => Level = o));
        LevelAccessor = new Accessor<int>(this, nameof(Level));

        Getters.Add(nameof(Score), new Func<int>(() => Score));
        Setters.Add(nameof(Score), new Action<int>(o => Score = o));
        ScoreAccessor = new Accessor<int>(this, nameof(Score));

        Getters.Add(nameof(Mana), new Func<int>(() => Mana));
        Setters.Add(nameof(Mana), new Action<int>(o => Mana = o));
        ManaAccessor = new Accessor<int>(this, nameof(Mana));

        Getters.Add(nameof(MaxMana), new Func<int>(() => MaxMana));
        Setters.Add(nameof(MaxMana), new Action<int>(o => MaxMana = o));
        MaxManaAccessor = new Accessor<int>(this, nameof(MaxMana));

        Getters.Add(nameof(Experience), new Func<int>(() => Experience));
        Setters.Add(nameof(Experience), new Action<int>(o => Experience = o));
        ExperienceAccessor = new Accessor<int>(this, nameof(Experience));

        Getters.Add(nameof(Gold), new Func<int>(() => Gold));
        Setters.Add(nameof(Gold), new Action<int>(o => Gold = o));
        GoldAccessor = new Accessor<int>(this, nameof(Gold));
    }

    public int Health
    {
        get => _health;
        set
        {
            _health = Mathf.Clamp(value, 0, MaxHealth);
            Send(nameof(Health), _health);
        }
    }

    public int MaxHealth
    {
        get => _maxHealth;
        set { _maxHealth = value; Send(nameof(MaxHealth), value); }
    }

    public string PlayerName
    {
        get => _playerName;
        set { _playerName = value; Send(nameof(PlayerName), value); }
    }

    public int Level { get => _level; set { _level = value; Send(nameof(Level), value); } }
    public int Score { get => _score; set { _score = value; Send(nameof(Score), value); } }
    public int Experience { get => _experience; set { _experience = value; Send(nameof(Experience), value); } }
    public int Gold { get => _gold; set { _gold = value; Send(nameof(Gold), value); } }

    public int MaxMana
    {
        get => _maxMana;
        set { _maxMana = value; Send(nameof(MaxMana), value); }
    }

    public int Mana
    {
        get => _mana;
        set { _mana = Mathf.Clamp(value, 0, MaxMana); Send(nameof(Mana), _mana); }
    }

    // Accessors for binding
    [JsonIgnore] public Accessor<int> HealthAccessor { get; private set; }
    [JsonIgnore] public Accessor<int> MaxHealthAccessor { get; private set; }
    [JsonIgnore] public Accessor<string> PlayerNameAccessor { get; private set; }
    [JsonIgnore] public Accessor<int> LevelAccessor { get; private set; }
    [JsonIgnore] public Accessor<int> ScoreAccessor { get; private set; }
    [JsonIgnore] public Accessor<int> ManaAccessor { get; private set; }
    [JsonIgnore] public Accessor<int> MaxManaAccessor { get; private set; }
    [JsonIgnore] public Accessor<int> ExperienceAccessor { get; private set; }
    [JsonIgnore] public Accessor<int> GoldAccessor { get; private set; }
}
```

Every example further down uses these accessors, so a single consistent
`PlayerModel` backs all of them.

---

## Accessor<T>

**Namespace**: `CherryFramework.DataModels`

**Purpose**: Provides typed access to model properties with value processing pipeline support.

### Class Definition

```csharp
public class Accessor<T>
{
    // Constructor
    public Accessor(DataModelBase model, string memberName);

    // Properties
    public T Value { get; }              // Raw value
    public T ProcessedValue { get; }      // Value after processors

    // Methods
    public DownwardBindingHandler BindDownwards(Action<T> callback, BindingActivation activationMode = BindingActivation.InvokeImmediate);
    public void InvokeDownwardBindings();
    public ValueProcessor AddProcessor(Func<T, T> processor, int priority = 0);
    public void RemoveProcessor(ValueProcessor processor);
    public void RemoveAllProcessors();
}
```

### Methods Explained

| Method                     | Description                                      |
| -------------------------- | ------------------------------------------------ |
| `BindDownwards()`          | Creates a one-way binding from model to callback |
| `InvokeDownwardBindings()` | Manually triggers all bindings                   |
| `AddProcessor()`           | Adds a value transformer to the pipeline         |
| `RemoveProcessor()`        | Removes a specific processor                     |
| `RemoveAllProcessors()`    | Clears all processors                            |

### Example Usage

```csharp
public class GameUI : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;
    private PlayerModel _playerModel;

    private void Start()
    {
        _playerModel = _modelService.GetOrCreateSingletonModel<PlayerModel>();

        // Basic binding
        _playerModel.HealthAccessor.BindDownwards(health =>
        {
            healthBar.value = health;
            healthText.text = $"HP: {health}";
        });

        // Binding with value processing.
        // AddProcessor returns a ValueProcessor, NOT the accessor, so these
        // calls cannot be chained. Each one is a separate statement.
        _playerModel.ScoreAccessor.AddProcessor(score => score * 100);          // Convert to points
        _playerModel.ScoreAccessor.AddProcessor(score => Mathf.RoundToInt(score)); // Round

        // Processors run in priority order (lower numbers first)
        _playerModel.NameAccessor.AddProcessor(name => name.ToUpper(), priority: 10);      // Runs first
        _playerModel.NameAccessor.AddProcessor(name => $"Player: {name}", priority: 0);    // Runs second
    }
}
```

Calling `AddProcessor` twice on the same accessor adds **two** processors to the
pipeline, it does not replace the first one.

---

## Bindings System

### DownwardBindingHandler

**Namespace**: `CherryFramework.DataModels`

**Purpose**: Represents a one-way binding from model to subscriber.

```csharp
public class DownwardBindingHandler
{
    public readonly DataModelBase Model;
    public readonly BindingActivation ActivationMode;
}

public class DownwardBindingHandler<T> : DownwardBindingHandler
{
    public Action<T> DownwardCallback { get; }
}
```

### Binding Activation Modes

Bindings are created with a `BindingActivation` mode that controls two things:
whether the callback is invoked right away with the current value, and whether
updates are delivered while the model is not yet `Ready`.

```csharp
public enum BindingActivation
{
    InvokeImmediate = 0,  // default
    ActivateImmediate = 1,
    InvokeOnReady = 2,
    ActivateOnReady = 3
}
```

| Mode                | Invoked with current value at bind time | Delivers updates while model is not ready |
| ------------------- | --------------------------------------- | ----------------------------------------- |
| `InvokeImmediate`   | Yes                                     | Yes                                       |
| `ActivateImmediate` | No                                      | Yes                                       |
| `InvokeOnReady`     | Queued - fires once when the model becomes ready | No (updates only after ready)  |
| `ActivateOnReady`   | No                                      | No (updates only after ready)             |

Behaviour details:

- `InvokeOnReady` handlers registered while the model is not ready are queued in
  the model's on-ready callback list and invoked exactly once when `Ready`
  flips to `true`. Bindings on the `Ready` member itself are never queued, since
  the `Ready` setter already delivers that single update.
- While the model is not ready, `Send` silently skips `InvokeOnReady` and
  `ActivateOnReady` handlers, so they cannot observe stale (pre-ready) values.
- Setting `Ready` back to `false` (or re-setting it to `true`) logs a warning,
  since models are expected to transition to ready exactly once.

### Bindings Container

**Namespace**: `CherryFramework.DataModels`

**Purpose**: Manages a collection of bindings with automatic cleanup. Used with `BehaviourBase` and `GeneralClassBase`.

```csharp
public class Bindings
{
    // Methods
    public DownwardBindingHandler CreateBinding<T>(Accessor<T> accessor, Action<T> callback, BindingActivation activationMode = BindingActivation.InvokeImmediate);
    public void ReleaseAllBindings();
    public void ReleaseBinding(DownwardBindingHandler handler);
}
```

### Example: Managing Bindings

```csharp
public class PlayerHUD : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;
    private PlayerModel _player;

    private void Start()
    {
        _player = _modelService.GetOrCreateSingletonModel<PlayerModel>();

        // Create bindings that will auto-cleanup when component destroys
        Bindings.CreateBinding(_player.HealthAccessor, OnHealthChanged);
        Bindings.CreateBinding(_player.ManaAccessor, OnManaChanged);
        Bindings.CreateBinding(_player.LevelAccessor, OnLevelChanged);

        // Manual binding (won't auto-cleanup)
        var handler = _player.ExperienceAccessor.BindDownwards(OnExpChanged);

        // Later manual cleanup if needed
        // Bindings.ReleaseBinding(handler);
    }

    private void OnHealthChanged(float health)
    {
        healthBar.fillAmount = health / _player.MaxHealth;
    }

    private void OnManaChanged(float mana)
    {
        manaBar.fillAmount = mana / _player.MaxMana;
    }

    private void OnLevelChanged(int level)
    {
        levelText.text = $"Level {level}";
    }

    private void OnExpChanged(int exp)
    {
        expText.text = $"EXP: {exp}";
    }
}
```

### Manual Binding Without Auto-cleanup (not recommended, but possible)

```csharp
public class TempUI : MonoBehaviour
{
    [Inject] private readonly ModelService _modelService;
    private DownwardBindingHandler _binding;

    private void Start()
    {
        // A bare MonoBehaviour is not injected automatically - inject yourself
        DependencyContainer.Instance.InjectDependencies(this);

        var player = _modelService.GetOrCreateSingletonModel<PlayerModel>();
        _binding = player.HealthAccessor.BindDownwards(OnHealthChanged);
    }

    private void OnDisable()
    {
        // Must manually unbind
        if (_binding != null)
        {
            _binding.Model.RemoveBinding(_binding);
            _binding = null;
        }
    }

    private void OnHealthChanged(float health)
    {
        // Update UI
    }
}
```

--- 

## Value Processors

**Namespace**: `CherryFramework.DataModels`

**Purpose**: Transform values in a pipeline before they reach subscribers.

### Class Definition

```csharp
public class ValueProcessor
{
    public readonly DataModelBase Model;
    public readonly string MemberName;
    public readonly int Priority;
    public readonly Delegate Action;
}
```

### Priority System

Processors execute in order of priority (lower numbers run first):

```csharp
// Priority 0: Formatting
accessor.AddProcessor(value => $"${value:F2}", priority: 0);

// Priority 10: Calculation
accessor.AddProcessor(value => value * 1.1f, priority: 10); // Runs first

// Priority 20: Validation
accessor.AddProcessor(value => Mathf.Max(0, value), priority: 20); // Runs second
```

### Example: Multi-stage Processing

```csharp
public class CurrencyDisplay : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;
    private PlayerModel _player;

    private void Start()
    {
        _player = _modelService.GetOrCreateSingletonModel<PlayerModel>();

        // AddProcessor returns ValueProcessor, so these are separate statements.
        // Higher priority numbers run LAST.
        _player.GoldAccessor.AddProcessor(gold => ApplyGuildBonus(gold), priority: 100); // runs last
        _player.GoldAccessor.AddProcessor(gold => ApplyTax(gold), priority: 50);         // runs in the middle
        _player.GoldAccessor.AddProcessor(gold => gold, priority: 0);                   // runs first

        // Bind to UI. The callback receives the RAW value - read ProcessedValue instead.
        Bindings.CreateBinding(_player.GoldAccessor,
            _ => goldText.text = _player.GoldAccessor.ProcessedValue.ToString());
    }

    private int ApplyGuildBonus(int gold) => Mathf.RoundToInt(gold * 1.1f);
    private int ApplyTax(int gold) => Mathf.RoundToInt(gold * 0.95f);

    // A processor must return the SAME type as the accessor (int here).
    // Formatting to a string cannot be done in a processor - do it in the
    // binding callback instead, which is what the line above does.
}
```

---

## Storage Bridges

### ModelDataStorageBridgeBase (Abstract)

**Namespace**: `CherryFramework.DataModels.ModelDataStorageBridges`

**Purpose**: Abstract base for implementing model persistence.

```csharp
public abstract class ModelDataStorageBridgeBase
{
    protected const string SingletonPrefix = "SINGLETON";

    // Methods
    public virtual void Setup(Dictionary<Type, DataModelBase> singletonModels, bool debugMessages);
    public virtual bool ModelExistsInStorage(DataModelBase model);
    public virtual bool ModelExistsInStorage<T1>(string slotId = "", string id = "");
    public virtual bool RegisterModelInStorage(DataModelBase model);
    public virtual bool LoadModelData(DataModelBase model, ReadyMode makeReady = ReadyMode.MakeReadyAnyway);
    public virtual bool SaveModelToStorage(DataModelBase model);
    public virtual bool DeleteModelFromStorage(DataModelBase model);
    public virtual void SaveAllModelsById(string id);
    public virtual void SaveAllModelsBySlot(string slotId);
    public virtual void SaveAllModels();
    public virtual DataModelBase[] GetAllRegisteredModels();
    public virtual void UnregisterModelFromStorage(DataModelBase model);
}
```

### PlayerPrefsBridge

**Namespace**: `CherryFramework.DataModels.ModelDataStorageBridges`

**Purpose**: Concrete implementation using Unity PlayerPrefs with JSON serialization.
The `IPlayerPrefs` implementation is passed as a plain constructor argument (this is **not** dependency injection - `[Inject]` means something else in this framework), so the same instance can be shared with other services such as `SaveGameManager`.

```csharp
public class PlayerPrefsBridge : ModelDataStorageBridgeBase
{
    public PlayerPrefsBridge(IPlayerPrefs playerPrefs);

// Key generation pattern: {id}-{slotId}-{namespace.TypeName}
// Empty segments are dropped, so a model with no id or no slotId produces a
// shorter key. For a singleton with an empty SlotId the key is:
//   "SINGLETON-CherryFramework.Sample.PlayerModel"
// and NOT "SINGLETON--CherryFramework.Sample.PlayerModel".
}
```

### ReadyMode

Controls when a model is marked `Ready` after `LoadModelData`:

```csharp
public enum ReadyMode
{
    MakeReadyAnyway = 0,       // default - Ready is set even if no data was found
    MakeReadyWhenDataFound = 1, // Ready is set only if data existed in storage
    DoNotMakeReady = 100       // the bridge never touches Ready; the caller decides
}
```

- `MakeReadyAnyway` (default) is what most apps want for synchronous loads.
- `MakeReadyWhenDataFound` is useful to distinguish "new player, no save" from
  "save restored", e.g. to show an onboarding flow only on first launch.
- `DoNotMakeReady` is useful when several models must all load before the
  game considers itself ready - you then set `Ready` yourself once the last
  load completes.

### Example: Setting Up Storage

```csharp
// In your installer
protected override void Install()
{
    // One shared IPlayerPrefs instance for all services that need persistence
    var playerPrefs = new PlayerPrefsData();

    // Create bridge with PlayerPrefs storage
    var bridge = new PlayerPrefsBridge(playerPrefs);

    // Create model service with bridge
    var modelService = new ModelService(bridge, debugMessages: true);

    BindAsSingleton(modelService);
}

// Using storage in a manager
public class SaveManager : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;

    private void Start()
    {
        // Get models
        var player = _modelService.GetOrCreateSingletonModel<PlayerModel>();
        var settings = _modelService.GetOrCreateSingletonModel<SettingsModel>();

        // Register for persistence
        _modelService.DataStorage.RegisterModelInStorage(player);
        _modelService.DataStorage.RegisterModelInStorage(settings);

        // Load saved data
        _modelService.DataStorage.LoadModelData(player);
        _modelService.DataStorage.LoadModelData(settings);
    }

    private void SaveGame()
    {
        _modelService.DataStorage.SaveAllModels();
    }

    private void DeleteSave()
    {
        var player = _modelService.GetOrCreateSingletonModel<PlayerModel>();
        _modelService.DataStorage.DeleteModelFromStorage(player);
    }
}
```

### Auto-saving Models

```csharp
public class ModelsSaver : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;

    [SerializeField] private bool onDestroyThis;
    [SerializeField] private bool onApplicationLostFocus;
    [SerializeField] private bool onApplicationPause;
    [SerializeField] private bool onApplicationQuit = true;

    protected override void OnDestroy()
    {
        if (onDestroyThis)
            _modelService.DataStorage.SaveAllModels();
        base.OnDestroy();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && onApplicationPause)
            _modelService.DataStorage.SaveAllModels();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && onApplicationPause)
            _modelService.DataStorage.SaveAllModels();
    }

    private void OnApplicationQuit()
    {
        if (onApplicationQuit)
            _modelService.DataStorage.SaveAllModels();
    }
}
```

---

## Code Generation

**Namespace**: `CherryFramework.DataModels.Editor`

**Purpose**: Automatically generate model classes from templates.

### Template-Based Generation

**Template Class** (`ExampleData.cs`):

```csharp
namespace CherryFramework.DataModels.Templates
{
    public class ExampleData
    {
        public string Foo = "foo";
    }
}
```

### How the Model Gets Its Name

The generated class name is **not** the template name with "Model" appended.
The generator does this:

```csharp
baseModelName = templateName.Replace("Template", "") + "Model";
```

So the only thing it removes is the literal word `Template`:

| Template class      | Generated model         |
| ------------------- | ----------------------- |
| `ExampleData`       | `ExampleDataModel`      |
| `GameStateData`     | `GameStateDataModel`    |
| `EnemyStatsTemplate`| `EnemyStatsModel`       |

`ExampleData` contains no "Template", so nothing is stripped and you get
**`ExampleDataModel`**, not `ExampleModel`. If you want a shorter name, name
your template `ExampleTemplate`.

The output always lands in namespace `GeneratedDataModels`, in the folder
`Assets/Scripts/GeneratedDataModels` (see `CodeGenConstants`).

**Generated Model** (`ExampleDataModel.Generated.cs`):

```csharp
// <auto-generated/>
namespace GeneratedDataModels
{
    [Serializable]
    public class ExampleDataModel : DataModelBase
    {
        private ExampleData _template = new();

        public ExampleDataModel() : base()
        {
            Getters.Add(nameof(Foo), new Func<string>(() => Foo));
            Setters.Add(nameof(Foo), new Action<string>(o => Foo = o));
            FooAccessor = new Accessor<string>(this, nameof(Foo));
        }

        // The generator also emits a constructor that copies from a template
        // instance. It requires the template to implement ICloneable.
        public ExampleDataModel(ExampleData source) : this()
        {
            if (source != null)
            {
                if (source is ICloneable cloneable) { /* deep copy */ }
            }
        }

        public string Foo
        {
            get => _template.Foo;
            set { _template.Foo = value; Send<string>(nameof(Foo), value); }
        }
        [JsonIgnore] public Accessor<string> FooAccessor;
    }
}
```

Every **public field** of the template becomes a property plus a matching
`{Field}Accessor` field - so a template field named `JumpForce` yields
`JumpForceAccessor`, capital F.

### Generator Configuration

**CodeGenConstants.cs** defines:

- Output paths and namespaces
- Template patterns
- Attribute copying rules
- Property templates

To generate models:

1. Create template classes in `*.DataModels.Templates` namespace
2. Use "Tools → UnityCodeGen → Generate" menu
3. Generated models appear in `Assets/Scripts/GeneratedDataModels/`
   (the whole folder is deleted and regenerated on every run)

#### Generic Templates

Template classes may be generic. In that case the generated model class gets a
type-argument suffix and the template's generic constraints are propagated to it:

```csharp
// Template
public class StatsData<T> where T : struct, new()
{
    public T Value;
}

// Generated (file: StatsDataModel.T.Generated.cs)
public class StatsDataModel<T> : DataModelBase where T : struct, new()
{
    private StatsData<T> _template = new();
    ...
}
```

Supported constraint kinds: type constraints, `class` / `struct`, and `new()`.
Non-generic templates are unaffected and generate as before.

### Using Generated Models

```csharp
public class GameManager : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;
    private ExampleDataModel _example;

    private void Start()
    {
        _example = _modelService.GetOrCreateSingletonModel<ExampleDataModel>();

        // Use generated accessors
        _example.FooAccessor.BindDownwards(value => Debug.Log($"Foo changed: {value}"));

        // Set values (automatically notifies)
        _example.Foo = "Hello World";
        _example.Bar = 42;
    }
}
```

---

## Best Practices

### 1. Centralized Model Access

Always access models through `ModelService`:

```csharp
// GOOD
[Inject] private readonly ModelService _modelService;
private PlayerModel _player => _modelService.GetOrCreateSingletonModel<PlayerModel>();

// BAD - Direct instantiation
private PlayerModel _player = new PlayerModel();
```

### 2. Register for Persistence

```csharp
public class GameInitializer : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;

    private void Start()
    {
        var player = _modelService.GetOrCreateSingletonModel<PlayerModel>();
        var settings = _modelService.GetOrCreateSingletonModel<SettingsModel>();

        // Register with storage
        _modelService.DataStorage.RegisterModelInStorage(player);
        _modelService.DataStorage.RegisterModelInStorage(settings);

        // Load existing data
        _modelService.DataStorage.LoadModelData(player);
        _modelService.DataStorage.LoadModelData(settings);
    }
}
```

### 3. Use Bindings for UI Updates

```csharp
public class PlayerStatsUI : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;

    [SerializeField] private TMP_Text _healthText;
    [SerializeField] private Slider _healthBar;

    private void Start()
    {
        var player = _modelService.GetOrCreateSingletonModel<PlayerModel>();

        // Bindings auto-cleanup when this component destroys
        Bindings.CreateBinding(player.HealthAccessor, UpdateHealth);
        Bindings.CreateBinding(player.MaxHealthAccessor, _ => UpdateHealth(player.Health));
    }

    private void UpdateHealth(int health)
    {
        var player = _modelService.GetOrCreateSingletonModel<PlayerModel>();
        _healthText.text = $"{health}/{player.MaxHealth}";
        _healthBar.value = (float)health / player.MaxHealth;
    }
}
```

### 4. Use Value Processors for Formatting

```csharp
public class ScoreDisplay : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;

    private void Start()
    {
        var player = _modelService.GetOrCreateSingletonModel<PlayerModel>();

        player.ScoreAccessor.AddProcessor(str => $"Score: {str}");
        Bindings.CreateBinding(player.ScoreAccessor, _ => scoreText.text = player.ScoreAccessor.ProcessedValue);
    }
}
```

### 5. Debug Mode

```csharp
// Enable debug mode in installer
var modelService = new ModelService(bridge, debugMessages: true);

// Or per model
var player = modelService.GetOrCreateSingletonModel<PlayerModel>();
player.SetDebugMode(true);
```

### 6. Batch Updates

```csharp
public void LevelUp()
{
    var player = _modelService.GetOrCreateSingletonModel<PlayerModel>();

    // Pause notifications during batch update
    player.PauseBindings(true);

    player.Level++;
    player.Experience = 0;
    player.Health = player.MaxHealth;
    player.Mana = player.MaxMana;

    // Resume and send all changes
    player.PauseBindings(false);
    player.InvokeBinding<int>(nameof(player.Level));
    player.InvokeBinding<int>(nameof(player.Experience));
    player.InvokeBinding<int>(nameof(player.Health));
    player.InvokeBinding<int>(nameof(player.Mana));
}
```

### 7. Save Strategically

```csharp
public class AutoSave : BehaviourBase
{
    [Inject] private StateService _stateService;
    [Inject] private ModelService _modelService;

    private void Start()
    {
        // There is no EventManager in this framework. Decoupled reactions go
        // through StateService: a condition plus a callback.
        // BehaviourBase implements IUnsubscriber, so StateService removes both
        // subscriptions automatically on destroy - no AddUnsubscription needed.
        _stateService.AddStateSubscription(
            s => s.IsEventActive("PlayerLevelUp"),
            SaveGame,
            this);

        _stateService.AddStateSubscription(
            s => s.IsEventActive("QuestCompleted"),
            SaveGame,
            this);
    }

    private void SaveGame()
    {
        _modelService.DataStorage.SaveAllModels();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveGame();
    }
}
```

---

## Examples

### Complete Example: Player Profile System

```csharp
// 1. Define template (PlayerProfileData.cs)
namespace Game.DataModels.Templates
{
    [Serializable]
    public class PlayerProfileData
    {
        public string playerName;
        public int level = 1;
        public int experience;
        public int gold;
        public List<string> achievements = new();
    }
}

// 2. Generated model (PlayerProfileDataModel.Generated.cs) - Auto-generated

// 3. Installer setup
[DefaultExecutionOrder(-10000)]
public class GameInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        var bridge = new PlayerPrefsBridge(new PlayerPrefsData());
        var modelService = new ModelService(bridge, debugMessages: true);

        BindAsSingleton(modelService);
    }
}

// 4. Game manager using models
public class GameManager : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;
    private PlayerProfileDataModel _profile;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Get or create profile
        _profile = _modelService.GetOrCreateSingletonModel<PlayerProfileDataModel>();

        // Register and load
        _modelService.DataStorage.RegisterModelInStorage(_profile);
        _modelService.DataStorage.LoadModelData(_profile);

        // Bind to level changes
        Bindings.CreateBinding(_profile.LevelAccessor, OnLevelChanged);
    }

    public void AddExperience(int amount)
    {
        _profile.experience += amount;

        // Check level up
        while (_profile.experience >= GetExpForNextLevel())
        {
            _profile.experience -= GetExpForNextLevel();
            _profile.level++;
            UnlockLevelAchievement();
        }
    }

    public bool SpendGold(int amount)
    {
        if (_profile.gold < amount) return false;
        _profile.gold -= amount;
        return true;
    }

    private void OnLevelChanged(int level)
    {
        Debug.Log($"Player reached level {level}!");
    }

    private int GetExpForNextLevel() => _profile.level * 100;

    private void UnlockLevelAchievement()
    {
        _profile.achievements.Add($"LEVEL_{_profile.level}");
    }

    protected override void OnDestroy()
    {
        // Auto-save on destroy
        _modelService.DataStorage.SaveAllModels();
        base.OnDestroy();
    }
}

// 5. UI Controller
public class ProfileUI : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private Slider _expSlider;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private Transform _achievementsParent;
    [SerializeField] private GameObject _achievementPrefab;

    private PlayerProfileDataModel _profile;

    protected override void OnEnable()
    {
        base.OnEnable();

        _profile = _modelService.GetOrCreateSingletonModel<PlayerProfileDataModel>();

        // Bind to profile properties
        Bindings.CreateBinding(_profile.PlayerNameAccessor, UpdateName);
        Bindings.CreateBinding(_profile.LevelAccessor, UpdateLevel);
        Bindings.CreateBinding(_profile.GoldAccessor, UpdateGold);
        Bindings.CreateBinding(_profile.AchievementsAccessor, UpdateAchievements);

        // Processed binding for the experience bar.
        // The processor must return the SAME type as the accessor, so a float
        // has to be rounded back to int. The callback parameter is the RAW
        // value - read ProcessedValue for the computed one.
        _profile.ExperienceAccessor.AddProcessor(
            exp => Mathf.RoundToInt((float)exp / GetExpForLevel(_profile.level)));

        Bindings.CreateBinding(_profile.ExperienceAccessor,
            _ => _expSlider.value = _profile.ExperienceAccessor.ProcessedValue);
    }

    private void UpdateName(string name) => _nameText.text = name;

    private void UpdateLevel(int level) => _levelText.text = $"Level {level}";

    private void UpdateGold(int gold) => _goldText.text = $"{gold} GP";

    private void UpdateAchievements(List<string> achievements)
    {
        // Clear existing
        foreach (Transform child in _achievementsParent)
            Destroy(child.gameObject);

        // Create new items
        foreach (var achievement in achievements)
        {
            var item = Instantiate(_achievementPrefab, _achievementsParent);
            item.GetComponentInChildren<TMP_Text>().text = achievement;
        }
    }

    private int GetExpForLevel(int level) => level * 100;
}
```

---

## Summary

| Component         | Purpose                  | Key Features                              |
| ----------------- | ------------------------ | ----------------------------------------- |
| `ModelService`    | Central model management | Singleton handling, storage bridging      |
| `DataModelBase`   | Base model class         | Property notification, binding management |
| `Accessor<T>`     | Property access          | Value processing, binding creation        |
| `Bindings`        | Binding container        | Auto-cleanup, multiple binding management |
| `ValueProcessor`  | Value transformation     | Priority-based pipeline                   |
| `StorageBridge`   | Persistence              | JSON serialization, key generation        |
| `ModelsGenerator` | Code generation          | Template-based model creation             |

The Data Models system provides a robust, type-safe foundation for managing application state with automatic UI updates and persistence, all coordinated through the central `ModelService`.
