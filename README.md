# CherryFramework

## Introduction

CherryFramework is a comprehensive, modular Unity framework designed to accelerate game development by providing battle-tested solutions for common game architecture challenges. It promotes clean architecture, decoupled components, and rapid prototyping through a cohesive set of integrated systems.

This is a complete, production-ready foundation for Unity development with:

* **Dependency Injection** for loose coupling and testability
* **Data Models** with automatic UI binding and persistence
* **Save/Load System** for game state with GUID-based identification
* **UI Framework** with navigation, widgets, and dynamic lists
* **Audio System** with 3D spatial support and pooling
* **State Management** for decoupled event communication
* **Tick Dispatcher** for optimized update frequencies
* **Object Pooling** for performance-critical objects

The framework is designed to be **modular** - use what you need, ignore what you don't. Each system works independently but integrates seamlessly when combined, allowing you to build anything from simple prototypes to complex, full-featured games.

---

## Where to start

1. Download project and open in Unity (built in Unity 6, but any version past 2020 should be fine)

2. Take a look at demo project in [Assets/Sample](Assets/Sample) folder. Game scene is located in `Assets/Sample/Scenes/dinoscene.unity`Try to launch it several times to see how save system is working

3. Read the Readme.md for Sample Game

4. Read the following docs (if needed)

---

## CherryFramework Overview

### Philosophy

CherryFramework is built on several core principles:

| Principle         | Description                                                                 |
| ----------------- | --------------------------------------------------------------------------- |
| **Decoupling**    | Components communicate through interfaces and events, not direct references |
| **Testability**   | Dependency injection makes unit testing straightforward                     |
| **Reusability**   | Generic implementations work across different projects                      |
| **Performance**   | Object pooling, efficient updates, and minimal allocations                  |
| **Extensibility** | Easy to extend or replace any system                                        |

---

## Core Systems

### 1. [Dependency Injection](Docs/DependencyManager.md)

The foundation of the framework, enabling loose coupling and testability. The DI container manages object lifetimes and automatically injects dependencies into classes that need them.

**Key Features**:

- Singleton and transient binding types
- Automatic injection in constructors (`InjectClass`) and `OnEnable()` (`InjectMonoBehaviour`)
- Hierarchical injection (base class members are also injected)
- Automatic cleanup of dependencies when installers are destroyed

**Example**:

```csharp
// Installer
[DefaultExecutionOrder(-10000)]
public class GameInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        BindAsSingleton<ILogger, FileLogger>();
        BindAsSingleton<PlayerModel>();
        Bind<EnemyFactory>(BindingType.Transient);
    }
}

// Usage
public class PlayerController : BehaviourBase
{
    [Inject] private ILogger _logger;
    [Inject] private PlayerModel _playerModel;

    private void Start()
    {
        _logger.Log("Player controller initialized");
    }
}
```

### 2. [Data Models System](Docs/DataModels.md)

An observable data layer with automatic UI binding and persistence. Models notify subscribers of changes and can be automatically saved to storage.

**Key Features**:

- Property change notification
- Value processing pipeline (formatting, calculations, validation)
- Automatic binding cleanup
- Singleton model management
- Pluggable storage (PlayerPrefs, file system, etc.)
- Code generation from templates

**Example**:

```csharp
public class PlayerData
{
    public int health;
}

// This is auto-generated, do not write yourself
public class PlayerModel : DataModelBase
{
    private PlayerData _template = new();

    public PlayerDataModel() : base()
    {
			 Getters.Add(nameof(health), new Func<System.Int32>(() => health));
			 Setters.Add(nameof(health), new Action<System.Int32>(o => health = o));
		 	 healthAccessor = new Accessor<System.Int32>(this, nameof(health));
    }

    public System.Int32 health
    {
        get => _template.health;
        set { _template.health = value;
        Send<System.Int32>(nameof(health), value); }
    }     
    [JsonIgnore]  
    public Accessor<System.Int32> healthAccessor;
}

// Setup model
var player = _modelService.GetOrCreateSingletonModel<PlayerDataModel>();
_modelService.DataStorage.RegisterModelInStorage(player);
_modelService.DataStorage.LoadModelData(player);

// UI Binding with value processing
player.healthAccessor.AddProcessor(health => Math.Min(health, 100));
Bindings.CreateBinding(player.healthAccessor, health =>
{
    NotifyHealthChange(player.healthAccessor.ProcessedValue);
});

_modelService.DataStorage.SaveModelToStorage(player);
```

### 3. [Save Game System](Docs/SaveGameManager.md)

A comprehensive save/load system for game objects and components. Supports both static scene objects and dynamically spawned objects with unique identification.

**Key Features**:

- Automatic transform saving (position, rotation, scale)
- GUID-based identification for scene objects (auto-generated for scenes in Build Settings)
- Custom ID + suffix system for spawnable objects
- Multiple save slot support
- Pre/post save/load lifecycle callbacks
- Force reset option for development

**Object Identification**:

- **Scene Objects**: `SceneId:{buildIndex}.{guid}` (e.g., `SceneId:3.550e8400-e29b-41d4-a716-446655440000`)
- **Spawnable Objects**: `{customId}:{suffix}` (e.g., `Enemy:42` where suffix separates copies)

**Example**:

```csharp
public class Player : BehaviourBase, IGameSaveData
{
    [Inject] private readonly SaveGameManager _saveManager;

    [SaveGameData] private int _level;
    [SaveGameData] private float _health;

    private void Start()
    {
        _saveManager.Register(this);
        _saveManager.LoadData(this);
    }

    public void OnAfterLoad()
    {
        // Validate loaded data
        _health = Mathf.Clamp(_health, 0, 100);
        UpdateUI();
    }
}
```

### 4. [UI System](Docs/UI.md)

A modular UI framework with navigation, stateful widgets, and dynamic list rendering.

**Key Features**:

- View stack navigation with back support
- Modal and popup screen types
- Child presenter hierarchy
- Widget state machine with smooth transitions
- Pooled list rendering with staggered animations
- Declarative animation sequencing

**Animation Types**:

- `UiFade` - CanvasGroup alpha fading
- `UiScale` - Scale transitions
- `UiSlide` - Slide based on element dimensions
- `UiTextFade` - Text alpha fading
- `UiActive` - GameObject active state toggling

**Example**:

```csharp
// Navigation
_viewService.PopView<SettingsPresenter>();

// Widget with states
_healthBar.SetState(_health > 50 ? "Healthy" : "Warning");

// Dynamic list with pooling
_populator.UpdateElements(items, 0.05f); // Staggered appearance

// Custom presenter
public class MainMenuPresenter : PresenterBase
{
    [SerializeField] private Button _playButton;

    protected override void OnPresenterInitialized()
    {
        _playButton.onClick.AddListener(() => 
            ViewService.PopView<GameplayPresenter>());
    }
}
```

### 5. [Audio System](Docs/SoundService.md)

A lightweight, event-based audio system with 3D spatial support and automatic pooling.

**Key Features**:

- Event-based playback using string keys
- Automatic emitter pooling
- 3D spatial audio with listener-camera integration
- Position blending between emitter and camera
- Volume fading (FadeIn/FadeOut)
- Per-sound handler system for individual control
- Looping sound support
- Completion callbacks

**Positioning Modes**:

- `positionToListener`: 0 = at emitter, 1 = at camera
- `orientToListener`: 0 = emitter orientation, 1 = facing camera

**Example**:

```csharp
// Play one-shot
_soundService.Play("explosion", transform.position);

// Fade in music
uint musicHandler = _soundService.FadeIn("background_music", null, 2f);

// Control individual sound
_soundService.FadeOut(musicHandler, 1.5f);

// With completion callback
_soundService.Play("level_complete", null, 0f, () => {
    LoadNextLevel();
});
```

### 6. [State Service](Docs/StateService.md)

An event and state management system for decoupled communication between components.

**Key Features**:

- Events (one-frame notifications) vs Statuses (persistent states)
- Type-safe payload events
- Conditional subscriptions
- One-time subscriptions with auto-removal
- Frame-aware tracking (when events were emitted)
- Automatic cleanup via IUnsubscriber



**Example**:

```csharp
// Emit event with payload
_stateService.EmitEvent("PlayerDied", new DeathData {
    position = transform.position,
    killer = "Boss"
});

// Subscribe with condition
_stateService.AddStateSubscription(
    accessor => accessor.IsEventActive("PlayerDied"),
    () => ShowGameOverScreen(),
    this
);

// Status tracking
_stateService.SetStatus("IsInventoryOpen");
// Later...
if (_stateService.IsStatusActive("IsInventoryOpen"))
{
    // Inventory is open
}
```

### 7. [Tick Dispatcher](Docs/TickDispatcher.md)

A centralized update management system with configurable tick frequencies.

**Key Features**:

- Configurable tick periods per component (e.g., 0.2s = 5 fps)
- Support for regular Tick, LateTick, FixedTick
- Automatic cleanup via IUnsubscriber
- Activity checking for MonoBehaviours
- Centralized control over all updates
- Proper delta time calculation accounting for tick periods

**Example**:

```csharp
public class EnemyAI : ITickable
{
    public void Tick(float deltaTime)
    {
        // Expensive AI calculations - runs at 5 fps
        UpdatePathfinding();
        UpdateBehavior();
    }
}

// Register with 0.2s period (5 fps)
_ticker.Register(enemyAI, 0.2f);

// For critical updates
public class InputHandler : IFixedTickable
{
    public void FixedTick(float deltaTime)
    {
        // Must run every frame
        ProcessInput();
    }
}

_ticker.Register(inputHandler); // Every frame
```

### 8. [Object Pooling](Docs/SimplePool.md)

A generic pooling system for performance-critical objects.

**Key Features**:

- Type-safe generic implementation
- Per-sample pooling (separate pools for different prefabs)
- Automatic instance creation when pool empty
- Active object tracking
- Automatic cleanup of destroyed objects
- Pool clearing for scene changes

**Example**:

```csharp
private SimplePool<Bullet> _bulletPool = new();

private void Awake()
{
    // Prewarm pool
    for (int i = 0; i < 20; i++)
    {
        var bullet = _bulletPool.Get(_bulletPrefab);
        bullet.gameObject.SetActive(false);
    }
}

public void Shoot()
{
    var bullet = _bulletPool.Get(_bulletPrefab, firePoint.position, firePoint.rotation);
    bullet.Initialize();
}

// In bullet script
private void OnTriggerEnter(Collider other)
{
    gameObject.SetActive(false); // Return to pool
}
```

---

## [Base Classes](Docs/BaseClasses.md)

All framework components derive from these foundational classes:

| Class                 | Purpose                                | Auto Features                              |
| --------------------- | -------------------------------------- | ------------------------------------------ |
| `InjectClass`         | Non-MonoBehaviour with DI              | Constructor injection                      |
| `InjectMonoBehaviour` | MonoBehaviour with DI                  | OnEnable injection                         |
| `BehaviourBase`       | MonoBehaviour + bindings + cleanup     | Binding cleanup, unsubscription on destroy |
| `GeneralClassBase`    | Non-MonoBehaviour + bindings + cleanup | Binding cleanup, IDisposable               |

**Example**:

```csharp
public class MyService : GeneralClassBase
{
    [Inject] private ILogger _logger;

    public MyService()
    {
        // Auto-injected
        _logger.Log("Service created");

        // Register cleanup
        AddUnsubscription(() => {
            _logger.Log("Service cleaned up");
        });
    }
}

public class MyComponent : BehaviourBase
{
    [Inject] private PlayerModel _player;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Bindings auto-cleanup on destroy
        Bindings.CreateBinding(_player.HealthAccessor, OnHealthChanged);
    }
}
```

---

## Framework Benefits

| Benefit                | Description                                                          |
| ---------------------- | -------------------------------------------------------------------- |
| **Rapid Development**  | Pre-built systems for common needs (save/load, audio, UI navigation) |
| **Clean Architecture** | Separation of concerns through dependency injection                  |
| **Testability**        | Mock-friendly design with interface-based programming                |
| **Performance**        | Object pooling, tick period optimization, minimal allocations        |
| **Memory Safety**      | Automatic cleanup through IUnsubscriber prevents leaks               |
| **Extensibility**      | Easy to customize or replace any system                              |
| **Consistency**        | Uniform patterns across projects reduce learning curve               |
| **Debug Support**      | Built-in logging and debugging tools                                 |

---

## When to Use CherryFramework

| Project Type      | Recommendation                                         |
| ----------------- | ------------------------------------------------------ |
| **Small Games**   | Perfect - quick setup, all essential systems included  |
| **Medium Games**  | Ideal - scales well, highly customizable               |
| **Large Games**   | Great foundation - can extend as needed                |
| **Prototypes**    | Excellent - get running in minutes, focus on gameplay  |
| **Game Jams**     | Perfect - infrastructure is ready, just add game logic |
| **Team Projects** | Great - consistent patterns help collaboration         |

---

## Getting Started

### 1. Installation

1. Copy CherryFramework into your Unity project's `Assets` folder
2. Ensure [dependencies](#Dependencies): DOTween, Newtonsoft.Json, etc, see Dependencies Section
3. Add framework namespaces to your assembly definition files

### 2. Initial Setup

```csharp
[DefaultExecutionOrder(-10000)]
public class ProjectInstaller : InstallerBehaviourBase
{
    [SerializeField] private RootPresenterBase _rootUI;
    [SerializeField] private GlobalAudioSettings _audioSettings;
    [SerializeField] private List<AudioEventsCollection> _audioCollections;

    protected override void Install()
    {
        // Core services
        BindAsSingleton(new SaveGameManager(new PlayerPrefsData(), true));
        BindAsSingleton(new StateService(true));
        BindAsSingleton(new Ticker());

        // UI
        BindAsSingleton(new ViewService(_rootUI, true));

        // Audio
        BindAsSingleton(new SoundService(_audioSettings, _audioCollections));

        // Models
        var modelService = new ModelService(new PlayerPrefsBridge<PlayerPrefsData>(), true);
        BindAsSingleton(modelService);
    }
}
```

### 3. Create Your First Component

```csharp
public class Player : BehaviourBase, IGameSaveData
{
    [Inject] private ModelService _modelService;
    [Inject] private SoundService _audio;
    [Inject] private StateService _state;
    [Inject] private SaveGameManager _saveManager;

    [SaveGameData] private Item[] _inventory;

    private void Start()
    {
        var _model = _modelService.GetOrCreateSingletonModel<PlayerDataModel>();
        // Load model data
        _modelService.DataStorage.RegisterModelInStorage(_model);
        _modelService.DataStorage.LoadModelData(_model);

        // Bind to model
        Bindings.CreateBinding(_model.HealthAccessor, OnHealthChanged);

        // Register with save system and get the inventory items (Item class must be [Serializable])
        saveManager.Register(this);
        saveManager.LoadData(this);
    }

    private void OnHealthChanged(float health)
    {
        if (health <= 0)
        {
            _state.EmitEvent("PlayerDied");
            _audio.Play("player_death", transform);
        }
    }

    private void OnDestroy()
    {
        _saveManager.SaveData(this);
        _modelService.DataStorage.SaveModelToStorage(_model);
    }
}
```



---

##### Dependencies

- JSON - com.unity.nuget.newtonsoft-json
- Code Generation - https://github.com/AnnulusGames/UnityCodeGen.git?path=/Assets/UnityCodeGen
- UI animations and timers - https://dotween.demigiant.com/ or https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676
- Editor Decoration - https://github.com/v0lt13/EditorAttributes

If you want to integrate save game data to Steam or other cloud services, I advise to use https://github.com/richardelms/FileBasedPlayerPrefs - a direct replacement to Unity's PlayerpRefs, that stores user data in an ordinary JSON files
