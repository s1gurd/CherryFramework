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
- **Two-way Binding**: Bind UI elements to model data
- **Value Processing**: Transform values through pipelines
- **Automatic Persistence**: Save/load models to PlayerPrefs or custom storage
- **Code Generation**: Auto-create model classes from templates
- **Singleton Management**: Global model instances
- **Type Safety**: Generic accessors and bindings

---

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

    // Methods
    public T GetOrCreateSingletonModel<T>() where T : DataModelBase, new();
    public bool MakeModelSingleton<T>(T source) where T : DataModelBase;
}
```

### Properties

| Name          | Type                         | Description                          |
| ------------- | ---------------------------- | ------------------------------------ |
| `DataStorage` | `ModelDataStorageBridgeBase` | Storage bridge for persisting models |

### Methods

| Method                           | Description                                |
| -------------------------------- | ------------------------------------------ |
| `GetOrCreateSingletonModel<T>()` | Gets existing singleton or creates new one |
| `MakeModelSingleton<T>()`        | Registers an existing model as singleton   |

### Usage Examples

#### Basic Setup in Installer

```csharp
public class GameInstaller : InstallerBehaviourBase
{
    [SerializeField] private bool _debugModels = true;

    protected override void Install()
    {
        // Create storage bridge
        var bridge = new PlayerPrefsBridge<PlayerPrefsData>();

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

        // Register callback to be called when the data becomes ready
        // Also note invokeImmediate: false - this ensures that binding does not get invoked right after registering
        Bindings.CreateBinding(_gameState.ReadyAccessor, ContinueLoading, invokeImmediate: false);

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
    [JsonIgnore] public Accessor<bool> ReadyAccessor { get; }

    // Protected dictionaries for property access
    protected Dictionary<string, Delegate> Getters { get; }
    protected Dictionary<string, Delegate> Setters { get; }

    // Methods
    public void AddBinding<T>(string memberName, DownwardBindingHandler handler, bool invokeImmediate);
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
public void AddBinding<T>(string memberName, DownwardBindingHandler handler, bool invokeImmediate)
```

Registers a binding for a model property.

**Parameters**:

- `memberName`: Name of the property
- `handler`: Binding handler with callback
- `invokeImmediate`: Whether to invoke with current value immediately

#### Send

```csharp
protected void Send<T>(string memberName, T value)
```

Notifies all subscribers of a property change.

### Example Model

```csharp
[Serializable]
public class PlayerModel : DataModelBase
{
    private int _health;
    private int _maxHealth;
    private string _playerName;

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
        set
        {
            _maxHealth = value;
            Send(nameof(MaxHealth), value);
        }
    }

    public string PlayerName
    {
        get => _playerName;
        set
        {
            _playerName = value;
            Send(nameof(PlayerName), value);
        }
    }

    // Accessors for binding
    [JsonIgnore] public Accessor<int> HealthAccessor { get; private set; }
    [JsonIgnore] public Accessor<int> MaxHealthAccessor { get; private set; }
    [JsonIgnore] public Accessor<string> PlayerNameAccessor { get; private set; }
}
```

---

## Accessor&lt;T&gt;

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
    public DownwardBindingHandler BindDownwards(Action<T> callback, bool invokeImmediate = true);
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

        // Binding with value processing
        _playerModel.ScoreAccessor
            .AddProcessor(score => score * 100) // Convert to points
            .AddProcessor(score => Mathf.RoundToInt(score)); // Round

        // Processors run in priority order (lower numbers first)
        _playerModel.NameAccessor
            .AddProcessor(name => name.ToUpper(), priority: 10) // Runs first
            .AddProcessor(name => $"Player: {name}", priority: 0); // Runs second
    }
}
```

---

Bindings System

### DownwardBindingHandler

**Namespace**: `CherryFramework.DataModels`

**Purpose**: Represents a one-way binding from model to subscriber.

```csharp
public class DownwardBindingHandler
{
    public readonly DataModelBase Model;
}

public class DownwardBindingHandler<T> : DownwardBindingHandler
{
    public Action<T> DownwardCallback { get; }
}
```

### Bindings Container

**Namespace**: `CherryFramework.DataModels`

**Purpose**: Manages a collection of bindings with automatic cleanup. Used with `BehaviourBase` and `GeneralClassBase`.

```csharp
public class Bindings
{
    // Methods
    public DownwardBindingHandler CreateBinding<T>(Accessor<T> accessor, Action<T> callback, bool invokeImmediate = true);
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
public class TempUI : MonoBehaviour, IInjectTarget
{
    [Inject] private readonly ModelService _modelService;
    private DownwardBindingHandler _binding;

    private void OnEnable()
    {
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

        _player.GoldAccessor
            // Stage 1: Apply modifiers (highest priority = runs last)
            .AddProcessor(gold => ApplyGuildBonus(gold), priority: 100)
            // Stage 2: Apply tax (medium priority)
            .AddProcessor(gold => ApplyTax(gold), priority: 50)
            // Stage 3: Format for display (lowest priority = runs first)
            .AddProcessor(gold => FormatGold(gold), priority: 0);
        
        // Bind to UI. Not that Processed Value is used to set text
        Bindings.CreateBinding(_player.GoldAccessor, _ => goldText.text = _player.GoldAccessor.ProcessedValue);
    }

    private int ApplyGuildBonus(int gold) => Mathf.RoundToInt(gold * 1.1f);
    private int ApplyTax(int gold) => Mathf.RoundToInt(gold * 0.95f);
    private string FormatGold(int gold) => $"{gold:N0} GP";
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
    public virtual bool LoadModelData(DataModelBase model, bool makeReady = true);
    public virtual bool SaveModelToStorage(DataModelBase model);
    public virtual bool DeleteModelFromStorage(DataModelBase model);
    public virtual void SaveAllModelsById(string id);
    public virtual void SaveAllModelsBySlot(string slotId);
    public virtual void SaveAllModels();
    public virtual DataModelBase[] GetAllRegisteredModels();
    public virtual void UnregisterModelFromStorage(DataModelBase model);
}
```

### PlayerPrefsBridge&lt;T&gt;

**Namespace**: `CherryFramework.DataModels.ModelDataStorageBridges`

**Purpose**: Concrete implementation using Unity PlayerPrefs with JSON serialization.

```csharp
public class PlayerPrefsBridge<T> : ModelDataStorageBridgeBase where T : IPlayerPrefs, new()
{
    private readonly IPlayerPrefs _playerPrefs = new T();

    // Key generation pattern: {id}-{slotId}-{type}
    // For singletons: SINGLETON-{slotId}-{type}
}
```

### Example: Setting Up Storage

```csharp
// In your installer
protected override void Install()
{
    // Create bridge with PlayerPrefs storage
    var bridge = new PlayerPrefsBridge<PlayerPrefsData>();

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

**Template Class** (ExampleData.cs):

```csharp
namespace CherryFramework.DataModels.Templates
{
    public class ExampleData
    {
        public string Foo = "foo";
        public int Bar;
        public float Baz;
    }
}
```

**Generated Model** (ExampleModel.Generated.cs):

```csharp
// <auto-generated/>
namespace GeneratedDataModels
{
    [Serializable]
    public class ExampleModel : DataModelBase
    {
        private ExampleData _template = new();

        public ExampleModel() : base()
        {
            Getters.Add(nameof(Foo), new Func<string>(() => Foo));
            Setters.Add(nameof(Foo), new Action<string>(o => Foo = o));
            FooAccessor = new Accessor<string>(this, nameof(Foo));

            Getters.Add(nameof(Bar), new Func<int>(() => Bar));
            Setters.Add(nameof(Bar), new Action<int>(o => Bar = o));
            BarAccessor = new Accessor<int>(this, nameof(Bar));

            Getters.Add(nameof(Baz), new Func<float>(() => Baz));
            Setters.Add(nameof(Baz), new Action<float>(o => Baz = o));
            BazAccessor = new Accessor<float>(this, nameof(Baz));
        }

        public string Foo
        {
            get => _template.Foo;
            set { _template.Foo = value; Send<string>(nameof(Foo), value); }
        }
        [JsonIgnore] public Accessor<string> FooAccessor;

        public int Bar
        {
            get => _template.Bar;
            set { _template.Bar = value; Send<int>(nameof(Bar), value); }
        }
        [JsonIgnore] public Accessor<int> BarAccessor;

        public float Baz
        {
            get => _template.Baz;
            set { _template.Baz = value; Send<float>(nameof(Baz), value); }
        }
        [JsonIgnore] public Accessor<float> BazAccessor;
    }
}
```

### Generator Configuration

**CodeGenConstants.cs** defines:

- Output paths and namespaces
- Template patterns
- Attribute copying rules
- Property templates

To generate models:

1. Create template classes in `*.DataModels.Templates` namespace
2. Use "Tools → UnityCodeGen → Generate" menu
3. Generated models appear in `Assets/GeneratedDataModels/`

### Using Generated Models

```csharp
public class GameManager : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;
    private ExampleModel _example;

    private void Start()
    {
        _example = _modelService.GetOrCreateSingletonModel<ExampleModel>();

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
    [Inject] private readonly ModelService _modelService;

    private void Start()
    {
        // Save on important events
        EventManager.Subscribe("PlayerLevelUp", SaveGame);
        EventManager.Subscribe("QuestCompleted", SaveGame);

        // Register cleanup
        AddUnsubscription(() => {
            EventManager.Unsubscribe("PlayerLevelUp", SaveGame);
            EventManager.Unsubscribe("QuestCompleted", SaveGame);
        });
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

// 2. Generated model (PlayerProfileModel.Generated.cs) - Auto-generated

// 3. Installer setup
public class GameInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        var bridge = new PlayerPrefsBridge<PlayerPrefsData>();
        var modelService = new ModelService(bridge, debugMessages: true);

        BindAsSingleton(modelService);
    }
}

// 4. Game manager using models
public class GameManager : BehaviourBase
{
    [Inject] private readonly ModelService _modelService;
    private PlayerProfileModel _profile;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Get or create profile
        _profile = _modelService.GetOrCreateSingletonModel<PlayerProfileModel>();

        // Register and load
        _modelService.DataStorage.RegisterModelInStorage(_profile);
        _modelService.DataStorage.LoadModelData(_profile);

        // Bind to level changes
        Bindings.CreateBinding(_profile.levelAccessor, OnLevelChanged);
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

    private PlayerProfileModel _profile;

    protected override void OnEnable()
    {
        base.OnEnable();

        _profile = _modelService.GetOrCreateSingletonModel<PlayerProfileModel>();

        // Bind to profile properties
        Bindings.CreateBinding(_profile.playerNameAccessor, UpdateName);
        Bindings.CreateBinding(_profile.levelAccessor, UpdateLevel);
        Bindings.CreateBinding(_profile.goldAccessor, UpdateGold);
        Bindings.CreateBinding(_profile.achievementsAccessor, UpdateAchievements);

        // Processed binding for experience bar
        _profile.experienceAccessor.AddProcessor(exp => (float)exp / GetExpForLevel(_profile.level));
        Bindings.CreaateBinding(_profile.experienceAccessor, _ => _expSlider.value = _profile.experienceAccessor.ProcessedValue);
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
