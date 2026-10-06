# CherryFramework Sample Game Project: Endless Runner

## Project Overview

This sample project demonstrates the practical implementation of the CherryFramework in a complete, playable endless runner game. It showcases how the framework's various systems work together to create a cohesive game experience with proper architecture, state management, and data persistence.

### Game Description

**Endless Runner** is a simple yet complete game where players control a character that automatically runs forward, jumping over obstacles to survive as long as possible. The game increases in difficulty over time, features power-ups, and tracks player statistics across multiple sessions.

**Start Scene**: `Sample/Scenes/dinoscene.unity` Please read the [scene structure overview](Scene.md)

---

## Project Structure

Verified against the files on disk. The generated models are **not** inside
`Sample` - they go to `Assets/Scripts/GeneratedDataModels/`.

```
Assets/
├── Sample/
│   ├── Scenes/
│   │   └── dinoscene.unity            # Main game scene (the only scene in Build Settings)
│   ├── Scripts/
│   │   ├── GameInstaller.cs            # DI container configuration
│   │   ├── GameManager.cs              # Core game logic
│   │   ├── Player.cs                   # Player controller
│   │   ├── Ground.cs                   # Scrolling ground
│   │   ├── Obstacle.cs                 # Base obstacle class
│   │   ├── RocketPowerUp.cs            # Power-up implementation
│   │   ├── Spawner.cs                  # Object spawning system
│   │   ├── AnimatedSprite.cs           # Sprite animation
│   │   ├── Settings/
│   │   │   ├── EventKeys.cs            # Event/status key constants
│   │   │   ├── GameSettings.cs         # ScriptableObject for game config
│   │   │   └── InputSystem_Actions.cs  # Generated Input System wrapper
│   │   ├── DataModels.Templates/       # Namespace must end with DataModels.Templates
│   │   │   ├── GameStateData.cs
│   │   │   └── GameStatistics.cs
│   │   └── UI/
│   │       ├── GamePaused.cs           # Pause menu
│   │       ├── PlayerDead.cs           # Death screen
│   │       ├── PlayerStats.cs          # Statistics display
│   │       ├── GameStatsHUD.cs         # In-game HUD
│   │       ├── HUDControl.cs           # HUD state management
│   │       ├── PowerUpNotification.cs  # Power-up UI
│   │       └── SpeedUpNotification.cs  # Speed increase UI
│   └── Settings/
│       ├── DinoGameSettings.asset          # Game configuration (GameSettings.cs)
│       ├── AudioSettings.asset             # GlobalAudioSettings
│       ├── AudioEventsCollection.asset     # Sound event keys
│       ├── InputSystem.inputsettings.asset # Input System project settings
│       └── InputSystem_Actions.inputactions
└── Scripts/
    └── GeneratedDataModels/           # GENERATED - do not edit
        ├── GameStateDataModel.Generated.cs
        ├── GameStatisticsModel.Generated.cs
        └── ExampleDataModel.Generated.cs
```

### Regenerating the models

The `*.DataModels.Templates` classes are **sources only**. The generated
`*Model` classes live outside `Sample` entirely - see
`Assets/Scripts/GeneratedDataModels/` in the tree above. To rebuild them after
editing a template:

1. Edit the template (for example `Scripts/DataModels.Templates/GameStateData.cs`).
   Its **namespace must end with `DataModels.Templates`**.
2. Run the menu item **`Tools → UnityCodeGen → Generate`**.
3. The whole `Assets/Scripts/GeneratedDataModels/` folder is deleted and
   regenerated.

The model name is derived as `<template name with "Template" removed> + "Model"`,
so `GameStateData` becomes `GameStateDataModel` and `EnemyStatsTemplate`
becomes `EnemyStatsModel`.

---

## Core Systems Integration

### 1. Dependency Injection Setup

The `GameInstaller` configures all services and dependencies at startup:

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
using Sample.Scripts.Settings;
using UnityEngine;

// [DefaultExecutionOrder] is not inherited, so the installer declares it itself
[DefaultExecutionOrder(-10000)]
public class GameInstaller : InstallerBehaviourBase
{
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private GlobalAudioSettings globalAudioSettings;
    [SerializeField] private RootPresenterBase uiRoot;
    [SerializeField] private AudioEventsCollection audioEvents;

    protected override void Install()
    {
        // ONE shared IPlayerPrefs instance for both save systems. Creating two
        // separate PlayerPrefsData objects would leave SaveGameManager and
        // ModelService writing to different stores, so data saved by one would
        // be invisible to the other.
        var playerPrefs = new PlayerPrefsData();

        // Core services
        BindAsSingleton<Ticker>();
        // debugMessages: true prints every emitted event, status change and
        // subscription run. Noisy, but it is the fastest way to see the
        // event/status system working.
        BindAsSingleton(new StateService(true));
        BindAsSingleton(new SaveGameManager(playerPrefs, true));
        BindAsSingleton(new ModelService(new PlayerPrefsBridge(playerPrefs), true));
        BindAsSingleton(new SoundService(globalAudioSettings, audioEvents));
        BindAsSingleton(new ViewService(uiRoot, true));

        // Game-specific dependencies
        BindAsSingleton(new InputSystem_Actions());
        BindAsSingleton(gameSettings);
        BindAsSingleton(Camera.main);
        BindAsSingleton(gameObject.AddComponent<GameManager>());
    }
}
```

### Reading the Console

`SaveGameManager`, `ModelService` and `ViewService` are all constructed with
`debugMessages: true`, so pressing Play immediately shows what the framework is
doing. Real output from this scene:

```
[Model Service - PlayerPrefs] Loaded model by key: SINGLETON-GeneratedDataModels.GameStatisticsModel from PlayerPrefs: {"GameRunning":false,"MaxDistance":16,"TotalRunTime":16,"TotalDistance":52,"TriesNum":5}
[Save Game Manager] Loaded component Sample.Player with key SceneId:0.7abb6373-7d8c-43dc-9c12-6c6114f78bba-Sample.Player found data: {"_direction":{}, "_jumpState":0}
[Save Game Manager] Loaded component Sample.Spawner with key SceneId:0.58f6ad04-11e7-41f9-86cf-a87ef00b0a3b-Sample.Spawner found data: {"_spawnedObjects":[0,0,4,5]}
[State Service] Set status "GameRunning" at time 5,713958
[State Service] Invoked 4 events at time 5,713958
[View Service] History push:
#PlayerDead(Clone)/
```

`Time.time` prints with the machine's locale, hence the comma.

On the way out (stopping the editor) you see the other half of the cycle. Every
key is deleted and then immediately written back:

```
[Save Game Manager] Deleted data for component Sample.Player with key SceneId:0.7abb6373-...
[Save Game Manager] Deleted data for component ... with key Obstacle:0-...
[Model Service - PlayerPrefs] Removed model GeneratedDataModels.GameStateDataModel from Player Prefs...

[Save Game Manager] Saved key SceneId:0.7abb6373-...-Sample.Player with {"_direction":{},"_jumpState":0}
[Save Game Manager] Saved key SceneId:0.58f6ad04-...-Sample.Spawner with {"_spawnedObjects":[0,6,4,1]}
[Save Game Manager] Saved key Obstacle:0-... with {"_position":{"x":-5.581851,...},"_rotation":...,"_scale":...}
[Save Game Manager] Saved key Obstacle:1-... with {"_position":{"x":-0.74753,...},...}
[Save Game Manager] Saved key Obstacle:2-... with {"_position":{"x":3.33741283,...},...}
[Save Game Manager] Saved key Obstacle:3-... with {"_position":{"x":7.39093876,...},...}
[Model Service - PlayerPrefs] Saved model SINGLETON-GeneratedDataModels.GameStateDataModel with content: {"GameSpeed":3.0,...,"DistanceTraveled":18,"RunTime":6}
[Model Service - PlayerPrefs] Saved model SINGLETON-GeneratedDataModels.GameStatisticsModel with content: {"GameRunning":false,...,"MaxDistance":18,"TotalDistance":70,"TriesNum":6}
```

Press Play again and those same numbers come back in the `Loaded key ...` lines.
`TriesNum` went from 5 to 6 in the meantime - that increment across runs is the
save system visibly working.

### How to read a key

Every line is `[Service] action with key <key>` followed by the JSON behind it.
The key tells you which system owns the data:

| Key shape | Owner | Meaning |
| --------- | ----- | ------- |
| `SINGLETON-GeneratedDataModels.XxxModel` | `ModelService` | a data model, stored as one JSON blob |
| `SceneId:{buildIndex}.{guid}-{Type}` | `SaveGameManager` | a scene object; `0` is this scene's build index |
| `{customId}:{suffix}-{Type}` | `SaveGameManager` | a **spawned** object; `Obstacle:0` is the first one |

The suffix matters: without it, every copy of the same spawned prefab would
fight over one key. `Spawner` restores the same list of obstacle indices it
spawned last time, which is why `"_spawnedObjects":[0,0,4,5]` comes back.

A missing key produces `NOT FOUND model by key: ... in PlayerPrefs`, which is
the normal first-run message rather than an error.

### What survives a restart here

Verified by stopping and replaying the scene:

- **Scene objects.** `Player` returns to its saved position
  (`SceneId:0.7abb6373-...-Sample.Player`).
- **Spawned obstacles.** `Obstacle:0` .. `Obstacle:3` each store `_position`,
  `_rotation` and `_scale`, and every cactus and bird reappears at exactly the
  coordinate it was left at. `Spawner` also restores its
  `"_spawnedObjects":[0,6,4,1]` index list, so it knows which ones to respawn.
- **Data models.** `GameStatistics` accumulates *across* runs (`Tries`,
  `TotalDistance`, `TotalRunTime`, `MaxDistance` are never reset), while
  `GameState` (`DistanceTraveled`, `RunTime`, `GameSpeed`) is rewritten each run.

The order on quit is what makes this work: `ClearData()` deletes every
component key, then `SaveAllData()` immediately writes them all back - which is
why the log shows `Deleted data ...` and `Saved key ...` for the same keys in
sequence. See `Assets/Sample/Scripts/GameManager.cs:161`.

A key that was never written shows up as `NOT FOUND model by key: ...` or
`Not found data for component ...`; that is normal, not an error.

One thing worth knowing: Newtonsoft serialises Unity's `Vector3` in full,
including its derived `normalized` / `magnitude` / `sqrMagnitude` members, so
transforms occupy noticeably more space than you would expect.

### 2. Data Models

The game uses two generated data models from templates:

**GameStateData** (Template):

```csharp
namespace Sample.DataModels.Templates
{
    [Serializable]
    public class GameStateData
    {
        public float GameSpeed;
        public float JumpForce;
        public bool PlayerDead;
        public int DistanceTraveled;
        public int RunTime;
    }
}
```

**GameStatistics** (Template):

```csharp
namespace Sample.DataModels.Templates
{
    [Serializable]
    public class GameStatistics
    {
        public bool GameRunning;
        public int MaxDistance;
        public int TotalRunTime;
        public int TotalDistance;
        public int TriesNum;
    }
}
```

After code generation, these become full `DataModelBase` classes with accessors and binding support.

### 3. State Management

Event keys are defined centrally:

```csharp
public static class EventKeys
{
    // State Service keys
    public const string GameRunning = "GameRunning";
    public const string SpeedUpGame = "SpeedUpGame";
    public const string RocketPowerUp = "RocketPowerUp";

    // Audio Event keys
    public const string Jump = "jump";
    public const string GameOver = "gameover";
}
```

The `GameManager` uses these to control game flow:

```csharp
// Switch input schemes based on game state
_stateService.AddStateSubscription(s => s.IsStatusJustBecameActive(EventKeys.GameRunning), () => {
    _inputSystem.Player.Enable();
});

_stateService.AddStateSubscription(s => s.IsStatusJustBecameInactive(EventKeys.GameRunning), () => {
    _inputSystem.Player.Disable();
});

// Emit speed-up events periodically
_speedUpTimer = DOTween.Sequence();
_speedUpTimer.SetLoops(-1);
_speedUpTimer.AppendInterval(_gameSettings.speedIncreasePeriod);
_speedUpTimer.AppendCallback(() => {
    _gameState.GameSpeed += _gameSettings.gameSpeedIncrease;
    _stateService.EmitEvent(EventKeys.SpeedUpGame);
});
```

### 4. UI Navigation

The UI system demonstrates navigation between different screens:

```csharp
// Show pause menu
_viewService.PopView<GamePaused>(out var view);
view.SetMenuState(runStarted);

// Show statistics
statisticsBtn.onClick.AddListener(() => ViewService.PopView<PlayerStats>());

// Back navigation
_inputSystem.UI.Back.started += _ => {
    if (_viewService.ActiveView is not PlayerDead && (!_viewService.IsLastView || _gameStatistics.GameRunning)) 
        _viewService.Back();
};

// Game over screen
public void OnPlayerDead()
{
    _speedUpTimer?.Kill();
    _viewService.PopView<PlayerDead>();
    _soundService.Play(EventKeys.GameOver, transform); 
}
```

### 5. Widget-Based UI Components

The HUD uses widgets to switch between states:

```csharp
public class HUDControl : WidgetBase
{
    [Inject] private readonly StateService _stateService;

    private void Start()
    {
        _stateService.AddStateSubscription(
            s => s.IsStatusJustBecameActive(EventKeys.GameRunning), 
            () => SetState(1));  // Show gameplay HUD

        _stateService.AddStateSubscription(
            s => s.IsStatusJustBecameInactive(EventKeys.GameRunning), 
            () => SetState(0));  // Show menu HUD
    }
}
```

Notifications use widget elements for animated appearances:

```csharp
public class SpeedUpNotification : WidgetElement
{
    [Inject] private readonly StateService _stateService;
    [Inject] private readonly GameSettings _gameSettings;

    private void Start()
    {
        _stateService.AddStateSubscription(
            s => s.IsEventActive(EventKeys.SpeedUpGame), 
            () => {
                Show().AppendInterval(_gameSettings.notificationShowTime)
                      .AppendCallback(() => Hide());
            });
    }
}
```

### 6. Tick Dispatcher for Optimized Updates

Instead of traditional Update methods, components use the Ticker service:

```csharp
public class Ground : BehaviourBase, ITickable
{
    [Inject] private readonly Ticker _ticker;

    private void Start()
    {
        _stateService.AddStateSubscription(
            s => s.IsStatusJustBecameActive(EventKeys.GameRunning), 
            () => _ticker.Register(this));

        _stateService.AddStateSubscription(
            s => s.IsStatusJustBecameInactive(EventKeys.GameRunning), 
            () => _ticker.UnRegister(this));
    }

    public void Tick(float deltaTime)
    {
        // Update ground scrolling
        _meshRenderer.material.mainTextureOffset += Vector2.right * (speed * deltaTime);

        // Track distance and time
        UpdateDistanceAndTime(deltaTime);
    }
}
```

### 7. Object Pooling for Performance

The spawner uses `SimplePool` to reuse obstacles:

```csharp
public class Spawner : BehaviourBase, IGameSaveData
{
    private SimplePool<PersistentObject> _objectPool = new();

    private void Spawn()
    {
        var randomIndex = _objectsToSpawnChanced[Random.Range(0, _objectsToSpawnChanced.Count)];
        var newObj = _objectPool.Get(
            _gameSettings.spawnObjects[randomIndex].source, 
            transform.position, 
            Quaternion.identity
        );

        newObj.gameObject.SetActive(true);
    }
}
```

### 8. Save/Load System

Player progress and game state are automatically saved:

```csharp
public class Player : BehaviourBase, IFixedTickable, IGameSaveData
{
    [SaveGameData] private Vector3 _direction;
    [SaveGameData] private JumpState _jumpState;

    private void Start()
    {
        _saveGameManager.Register(this);
        _saveGameManager.LoadData(this); // Restore jump state
    }
}
```

The `GameManager` handles saving on application quit:

```csharp
private void OnApplicationQuit()
{
    _gameStatistics.GameRunning = false;

    // Clear temporary data
    ClearData();

    // Save persistent data
    _saveGame.SaveAllData();
    _modelService.DataStorage.SaveModelToStorage(_gameState);
    _modelService.DataStorage.SaveModelToStorage(_gameStatistics);
}
```

### 9. Audio System

Sound effects are triggered throughout the game:

```csharp
// Jump sound
_soundService.Play(EventKeys.Jump, transform);

// Game over sound
_soundService.Play(EventKeys.GameOver, transform);
```

### 10. Value Processors for Power-ups

The rocket power-up demonstrates value processing:

```csharp
public class RocketPowerUp : Obstacle
{
    protected override void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !StateService.IsStatusActive(EventKeys.RocketPowerUp))
        {
            StateService.SetStatus(EventKeys.RocketPowerUp);

            // Add processor to multiply jump force
            var processor = GameState.JumpForceAccessor.AddProcessor(
                f => f * _gameSettings.jumpForceMultiplier
            );

            // Remove after lifetime
            DOTween.Sequence()
                .AppendInterval(_gameSettings.powerUpLifetime)
                .AppendCallback(() => {
                    StateService.UnsetStatus(EventKeys.RocketPowerUp);
                    GameState.JumpForceAccessor.RemoveProcessor(processor);
                });

            gameObject.SetActive(false);
        }
    }
}
```

---

## Game Flow

### Initial Startup

1. `GameInstaller` configures all dependencies
2. `GameManager` loads saved state from `ModelService`
3. UI shows either main menu or death screen based on previous session
4. `ViewService` manages initial view presentation

### Gameplay Loop

1. Player starts running → `GameRunning` status activated
2. Ground scrolls using `Ticker` updates
3. Obstacles spawn via pooled objects
4. Player jumps using physics-based movement
5. Game speed gradually increases, triggering notifications
6. Distance and time tracked in models

### Power-up Sequence

1. Player collects rocket power-up
2. `RocketPowerUp` status activated
3. Value processor multiplies jump force
4. Notification UI appears
5. After duration, status deactivated and processor removed

### Game Over

1. Player hits obstacle → `PlayerDead` set to true
2. Binding triggers death sequence
3. Speed-up timer killed
4. Death screen appears
5. Statistics updated

### Saving/Loading

- Game state auto-saves on quit
- Player jump state persists between sessions
- Spawned objects are tracked and respawned
- Statistics accumulate across all play sessions

---

## Key Framework Features Demonstrated

| Feature                  | Implementation                            |
| ------------------------ | ----------------------------------------- |
| **Dependency Injection** | All services injected via `[Inject]`      |
| **Data Models**          | Game state and statistics with bindings   |
| **State Service**        | GameRunning, RocketPowerUp statuses       |
| **View Service**         | Menu navigation, back stack               |
| **Widgets**              | HUD state switching, notifications        |
| **Populators**           | (Extensible for leaderboards)             |
| **Tick Dispatcher**      | Optimized updates for ground, obstacles   |
| **Object Pooling**       | Obstacle reuse                            |
| **Save/Load System**     | Player state, spawned objects, statistics |
| **Audio System**         | Jump and game over sounds                 |
| **Value Processors**     | Power-up jump force modification          |
| **Code Generation**      | Models from templates                     |

---

## Running the Sample

1. Open Unity project with CherryFramework installed
2. Navigate to `Sample/Scenes/dinoscene.unity`
3. Press Play
4. Use Space bar (or controller) to jump
5. Avoid obstacles
6. Collect rocket power-ups for enhanced jumping
7. Press Escape to pause
8. View statistics after game over

---

## Conclusion

This sample project demonstrates how CherryFramework's integrated systems work together to create a complete, production-ready game with clean architecture, minimal boilerplate, and robust functionality. Each framework component serves a specific purpose while seamlessly integrating with others, allowing developers to focus on gameplay rather than infrastructure.
