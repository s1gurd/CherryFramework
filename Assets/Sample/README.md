# CherryFramework Sample Game Project: Endless Runner

## Project Overview

This sample project demonstrates the practical implementation of the CherryFramework in a complete, playable endless runner game. It showcases how the framework's various systems work together to create a cohesive game experience with proper architecture, state management, and data persistence.

### Game Description

**Endless Runner** is a simple yet complete game where players control a character that automatically runs forward, jumping over obstacles to survive as long as possible. The game increases in difficulty over time, features power-ups, and tracks player statistics across multiple sessions.

**Start Scene**: `Sample/Scenes/dinoscene.unity`

---

## Project Structure

```
Assets/
└── Sample/
    ├── Scenes/
    │   └── dinoscene.unity              # Main game scene
    ├── Scripts/
    │   ├── GameInstaller.cs              # DI container configuration
    │   ├── GameManager.cs                 # Core game logic
    │   ├── Player.cs                       # Player controller
    │   ├── Ground.cs                        # Scrolling ground
    │   ├── Obstacle.cs                      # Base obstacle class
    │   ├── RocketPowerUp.cs                  # Power-up implementation
    │   ├── Spawner.cs                         # Object spawning system
    │   ├── AnimatedSprite.cs                   # Sprite animation
    │   └── UI/
    │       ├── GamePaused.cs                     # Pause menu
    │       ├── PlayerDead.cs                      # Death screen
    │       ├── PlayerStats.cs                      # Statistics display
    │       ├── GameStatsHUD.cs                      # In-game HUD
    │       ├── HUDControl.cs                          # HUD state management
    │       ├── PowerUpNotification.cs                   # Power-up UI
    │       └── SpeedUpNotification.cs                     # Speed increase UI
    ├── Settings/
    │   ├── GameSettings.asset                # Game configuration
    │   ├── InputSystem_Actions.inputactions   # Input bindings
    │   └── EventKeys.cs                        # Event/status constants
    └── DataModels/
        ├── Templates/
        │   ├── GameStateData.cs                 # Game state template
        │   └── GameStatistics.cs                  # Statistics template
        └── (Generated)                          # Generated model classes
```

---

## Core Systems Integration

### 1. Dependency Injection Setup

The `GameInstaller` configures all services and dependencies at startup:

```csharp
[DefaultExecutionOrder(-10000)]
public class GameInstaller : InstallerBehaviourBase
{
    [SerializeField] private GameSettings gameSettings;
    [SerializeField] private GlobalAudioSettings globalAudioSettings;
    [SerializeField] private RootPresenterBase uiRoot;
    [SerializeField] private AudioEventsCollection audioEvents;

    protected override void Install()
    {
        // Core services
        BindAsSingleton<Ticker>();
        BindAsSingleton(new StateService(false));
        BindAsSingleton(new SaveGameManager(new PlayerPrefsData(), false));
        BindAsSingleton(new ModelService(new PlayerPrefsBridge<PlayerPrefsData>(), false));
        BindAsSingleton(new SoundService(globalAudioSettings, audioEvents));
        BindAsSingleton(new ViewService(uiRoot, false));

        // Game-specific dependencies
        BindAsSingleton(new InputSystem_Actions());
        BindAsSingleton(gameSettings);
        BindAsSingleton(Camera.main);
        BindAsSingleton(gameObject.AddComponent<GameManager>());
    }
}
```

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
