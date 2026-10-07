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

**1. Open the project in Unity 6000.3.x** (developed and tested on
`6000.3.25f1`). Older versions will not work: the project pins
`com.unity.inputsystem 1.20.0` and `com.unity.ugui 2.0.0`, both of which are
Unity 6 packages.

**2. Run the demo.** Open `Assets/Sample/Scenes/dinoscene.unity` and press Play.
You get an endless runner - Space to jump, the obstacle speed ramps up.

**3. Watch it work in the Console.** The Sample ships with debug logging turned
on, so the very first Play already prints what the framework is doing: every
save and load carries its storage key and raw JSON, every model load too, and
the `StateService` prints only in frames where something was emitted. Each
service is covered where it belongs:

- [Save Game System](#3-save-game-system) - the full console walkthrough of the
  delete-then-save cycle, what survives a restart, and how keys are built
- [Data Models System](#2-data-models-system) - the model half of the output, and
  why `GameStatistics` accumulates across runs while `GameState` does not

If you want to read the whole trace in one place, [Debug output](#debug-output)
later in this file lists every service and its `debugMessages` flag.

**4. Read the Sample README**, which walks through each system against the real
scripts: [Assets/Sample/README.md](Assets/Sample/README.md).

**5. Read the system docs**, in this order if you are new:

| Doc | Read it when |
| --- | --- |
| [BaseClasses.md](Docs/BaseClasses.md) | first - the four base classes everything derives from |
| [DependencyManager.md](Docs/DependencyManager.md) | you register services and inject them |
| [SaveGameManager.md](Docs/SaveGameManager.md) | you persist MonoBehaviour state |
| [DataModels.md](Docs/DataModels.md) | you want observable models and UI binding |
| [UI.md](Docs/UI.md) | you build screens, widgets and dynamic lists |
| [TickDispatcher.md](Docs/TickDispatcher.md) | you want to replace `Update()` |
| [StateService.md](Docs/StateService.md) | you need decoupled event/status reactions |
| [SoundService.md](Docs/SoundService.md) | you play sounds by key |
| [SimplePool.md](Docs/SimplePool.md) | you pool frequently spawned objects |

### Debug output

Every service except `SoundService` takes a `debugMessages` flag, and the flags
are independent:

| Service                 | How to set it                | What it prints |
| ----------------------- | ---------------------------- | -------------- |
| `ModelService`          | `new ModelService(bridge, debugMessages)` | every model load / save / remove, with key and JSON |
| `SaveGameManager`       | `new SaveGameManager(prefs, debugMessages)` | every component load / save, with key and JSON |
| `ViewService`           | `new ViewService(root, debugMessages)` | the navigation history on each push/pop |
| `StateService`          | `new StateService(debugMessages)` | every emitted event, status change and subscription run |

`SoundService` has no such flag - it logs errors only (unknown key, missing
emitter).

In the Sample every one of these is `true`, so a fresh Play fills the Console
with the full picture. It is noisy - `StateService` logs in every frame where
something was emitted - so turn the flags back off once you have seen it work.

`ViewService` also prints things worth recognising, because they explain the
routing rather than the data:

```
[State Service] Set status "GameRunning" at time 5,713958
[State Service] Invoked 4 events at time 5,713958
[View Service] History push:
#PlayerDead(Clone)/
```

`Invoked 4 events` appears **only in frames where something was emitted** - that
is the `StateService` gate, visible in the log. (`Time.time` prints with the
machine's locale, hence the comma.)

### Installing into your own project

Copy `Assets/CherryFramework` (and `Assets/ThirdParty/Plugins/DOTween`, which
it depends on) into your project. Then create one component deriving from
`InstallerBehaviourBase`, add it to a GameObject, and fill its `[SerializeField]`
references **in the Inspector** - an unassigned reference stays `null` and
injection will fail:

```csharp
[DefaultExecutionOrder(-10000)]
public class GameInstaller : InstallerBehaviourBase
{
    [SerializeField] private GlobalAudioSettings _audioSettings;
    [SerializeField] private List<AudioEventsCollection> _audioEvents;
    [SerializeField] private RootPresenterBase _uiRoot;   // scene object

    protected override void Install()
    {
        // debugMessages: true while you are learning - it prints what the
        // services are doing. Set it to false once you have seen the output.
        BindAsSingleton(new Ticker());
        BindAsSingleton(new StateService(true));
        BindAsSingleton(new SoundService(_audioSettings, _audioEvents));
        BindAsSingleton(new ViewService(_uiRoot, true));
    }
}
```

`[DefaultExecutionOrder(-10000)]` is **not inherited** - every installer has to
declare it, otherwise `Install()` runs in the default order and other objects
can receive `[Inject]` fields before they are filled.

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
using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.StateService;
using CherryFramework.TickDispatcher;
using CherryFramework.Utils.PlayerPrefsWrapper;

// Installer: add this component to any GameObject in the scene.
// [DefaultExecutionOrder] is NOT inherited, so every installer subclass must
// declare it again - otherwise Install() runs in the default order and other
// objects can reach [Inject] fields before they are filled.
// Install() is called from Awake(), before your gameplay objects start.
[DefaultExecutionOrder(-10000)]
public class GameInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        var playerPrefs = new PlayerPrefsData();
        BindAsSingleton(new SaveGameManager(playerPrefs, true));
        BindAsSingleton(new StateService(true));
        BindAsSingleton(new Ticker());
    }
}

// Usage: inherit BehaviourBase - it injects your [Inject] fields in OnEnable,
// so they are already filled by the time Start() runs.
public class PlayerController : BehaviourBase
{
    [Inject] private Ticker _ticker;
    [Inject] private StateService _stateService;

    private void Start()
    {
        _stateService.EmitEvent("GameStarted");
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

**Model keys and what you see in the Console**

Models use their own key namespace, so their lines are easy to tell apart from
`SaveGameManager`'s in the same trace:

```
[Model Service - PlayerPrefs] Loaded model by key: SINGLETON-GeneratedDataModels.GameStatisticsModel from PlayerPrefs: {"GameRunning":false,"MaxDistance":16,"TotalRunTime":16,"TotalDistance":52,"TriesNum":5}
[Model Service - PlayerPrefs] Saved model SINGLETON-GeneratedDataModels.GameStatisticsModel with content: {..., "TriesNum":6}
```

A model that was never written gets its own line instead of the load one, and
that is normal on a first Play:

```
[Model Service - PlayerPrefs] NOT FOUND model by key: SINGLETON-GeneratedDataModels.GameStateDataModel in PlayerPrefs
```

Whether a miss leaves the model at its defaults depends on the `ReadyMode` you
load with: `MakeReadyAnyway` marks it ready regardless, while
`MakeReadyWhenDataFound` keeps it un-ready until something is written - see
[Storage Bridges](Docs/DataModels.md#storage-bridges).

A `SINGLETON-` prefix means a singleton model; `SceneId:0.<guid>-<type>` and
`Obstacle:0-...` in the same Console belong to
[Save Game System](#3-save-game-system) instead.

**Which models accumulate.** Persistence does not reset anything by itself - the
Sample decides what to clear on quit. `GameManager.OnApplicationQuit` deletes
`GameState` from storage before saving both models, so:

| Model | Across runs | Why |
| --- | --- | --- |
| `GameState` | rewritten | `DistanceTraveled`, `RunTime` describe the run in progress, so they are cleared |
| `GameStatistics` | accumulates | `Tries`, `TotalDistance`, `TotalRunTime`, `MaxDistance` are career totals |

**Before you edit a data template, know the generator.** Models are generated,
never written by hand: a `PlayerDataModel` that looks hand-written above is
produced from a `*.DataModels.Templates` class. If you change a template, run
**`Tools → UnityCodeGen → Generate`** - the whole
`Assets/Scripts/GeneratedDataModels/` folder is deleted and rebuilt, so any
edit made directly inside it is lost. Details in
[Code Generation](Docs/DataModels.md#code-generation).

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
- GUID-based identification for scene objects (fill the guid with the **Fill Guid** button in the Inspector)
- Custom ID + suffix system for spawnable objects
- Multiple save slot support
- Pre/post save/load lifecycle callbacks
- Force reset option for development

**Object Identification**:

- **Scene Objects**: `SceneId:{buildIndex}.{guid}` (e.g., `SceneId:3.550e8400-e29b-41d4-a716-446655440000`)
- **Spawnable Objects**: `{customId}:{suffix}` (e.g., `Enemy:42` where suffix separates copies)

#### Seeing the save/load cycle in the Console

With `SaveGameManager`'s `debugMessages` on, a Play prints the load half of the
cycle:

```
[Save Game Manager] Loaded component Sample.Player with key SceneId:0.7abb6373-...-Sample.Player found data: {"_direction":{}, "_jumpState":0}
[Save Game Manager] Loaded component Sample.Spawner with key SceneId:0.58f6ad04-...-Sample.Spawner found data: {"_spawnedObjects":[0,0,4,5]}
```

Stopping the editor prints the other half - every key deleted, then immediately
written back:

```
[Save Game Manager] Deleted data for component Sample.Player with key SceneId:0.7abb6373-...
[Save Game Manager] Deleted data for component ... with key Obstacle:0-...
[Save Game Manager] Saved key SceneId:0.7abb6373-...-Sample.Player with {"_direction":{},"_jumpState":0}
[Save Game Manager] Saved key SceneId:0.58f6ad04-...-Sample.Spawner with {"_spawnedObjects":[0,6,4,1]}
[Save Game Manager] Saved key Obstacle:0-... with {"_position":{"x":-5.581851,...},"_rotation":...,"_scale":...}
[Save Game Manager] Saved key Obstacle:1-... with {"_position":{"x":-0.74753,...},...}
```

That delete-then-save pairing is the whole trick, and the Sample does it on
purpose. In `GameManager.OnApplicationQuit`, every registered component is sent
through `DeleteData` first so leftovers from an earlier run cannot leak, and
then `SaveAllData()` writes the whole session back in the same frame.

Note that `DeleteData` only removes the entry from storage - it does not reset
the fields of the object that is still alive in memory.

**Try it yourself:**

1. Play for a few seconds, then **stop the editor** (not just close the scene).
   You see a `Deleted data for component ...` line per key, each followed by its
   `Saved key ...`.
2. Press Play again and compare the loaded coordinates with the saved ones: the
   Player and all four obstacles come back at exactly the same spots.

**What this restores on the next Play:**

- the **Player's** position (`SceneId:0.7abb...-Sample.Player`)
- every **spawned obstacle** where it was left (`Obstacle:0-3`, each storing
  `_position`, `_rotation`, `_scale` - the transform is saved automatically)
- the **Spawner's** list of live obstacle indices, so it knows what to respawn

A key that was never written shows up as `Not found data for component ...`;
that is normal, not an error. To wipe everything, delete the app's PlayerPrefs
entries.

The models in the same trace are explained in
[Data Models System](#2-data-models-system).

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

A thin wrapper over Unity's `AudioSource`: look a sound up by a string key, the framework plays it through a pooled emitter, with 3D spatial support.

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

- Pool parameterised by a component type `T`
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
using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.StateService;
using GeneratedDataModels;

// Plain C# class (not a MonoBehaviour). InjectClass fills the [Inject] fields
// before the constructor body runs; Dispose() fires the cleanup callbacks.
public class MyService : GeneralClassBase
{
    [Inject] private StateService _stateService;

    public MyService()
    {
        _stateService.EmitEvent("ServiceCreated");

        AddUnsubscription(() => _stateService.EmitEvent("ServiceDisposed"));
    }
}

// MonoBehaviour version. BehaviourBase injects in OnEnable and releases all
// bindings in OnDestroy, so there is nothing to unhook by hand.
public class MyComponent : BehaviourBase
{
    [Inject] private GameStateDataModel _gameState;

    protected override void OnEnable()
    {
        base.OnEnable();

        Bindings.CreateBinding(_gameState.DistanceTraveledAccessor, OnDistanceChanged);
    }

    private void OnDistanceChanged(int distance)
    {
        Debug.Log($"Distance: {distance}");
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
2. Ensure [dependencies](#dependencies-included-in-project): DOTween, Newtonsoft.Json, etc, see Dependencies Section
3. Add framework namespaces to your assembly definition files

### 2. Initial Setup

This is the full installer. A minimal four-line version is shown earlier, in
[Installing into your own project](#installing-into-your-own-project).

```csharp
using CherryFramework.DataModels;
using CherryFramework.DataModels.ModelDataStorageBridges;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.SoundService;
using CherryFramework.StateService;
using CherryFramework.TickDispatcher;
using CherryFramework.UI.Views;
using CherryFramework.Utils.PlayerPrefsWrapper;
using UnityEngine;

// [DefaultExecutionOrder] is NOT inherited - every installer must declare it,
// otherwise Install() can run after objects that expect their [Inject] fields.
[DefaultExecutionOrder(-10000)]
public class ProjectInstaller : InstallerBehaviourBase
{
    // These are Inspector references - an unassigned one stays null and
    // injection will fail with a null reference later.
    [SerializeField] private RootPresenterBase _rootUI;
    [SerializeField] private GlobalAudioSettings _audioSettings;
    [SerializeField] private List<AudioEventsCollection> _audioCollections;

    protected override void Install()
    {
        // One shared IPlayerPrefs instance for both save systems, so a value
        // written by SaveGameManager and by ModelService lands in the same store
        var playerPrefs = new PlayerPrefsData();

        BindAsSingleton(new Ticker());
        BindAsSingleton(new StateService(true));
        BindAsSingleton(new SaveGameManager(playerPrefs, true));
        BindAsSingleton(new ModelService(new PlayerPrefsBridge(playerPrefs), true));

        BindAsSingleton(new ViewService(_rootUI, true));
        BindAsSingleton(new SoundService(_audioSettings, _audioCollections));
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

##### Dependencies (included in project)

- JSON - com.unity.nuget.newtonsoft-json
- Code Generation - https://github.com/AnnulusGames/UnityCodeGen.git?path=/Assets/UnityCodeGen
- UI animations and timers - https://dotween.demigiant.com/ or https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676
- Editor Decoration - https://github.com/v0lt13/EditorAttributes.git

If you want to integrate save game data to Steam or other cloud services, I advise to use https://github.com/richardelms/FileBasedPlayerPrefs - a direct replacement for Unity's PlayerPrefs, that stores user data in an ordinary JSON files
