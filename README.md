# CherryFramework

CherryFramework is a comprehensive Unity-based framework designed to streamline the development of complex games and applications by promoting modularity, separation of concerns, and reusable components. It provides a set of integrated systems that handle common tasks such as dependency injection, data modeling with bindings, UI management, sound, saving/loading, state management, and update scheduling. The framework encourages an MVVM-like pattern with data models, accessors, and bindings, while also offering a robust dependency injection container to decouple systems.

---

## Where to start

1. Download project and open in Unity (any version past 2020 should be fine)

2. Take a look at demo project in `Assets/Sample` folder. Game scene is located in `Assets/Sample/Scenes/dinoscene.unity`

3. Read the following docs (if needed)

---

## Core Systems

### 1. **Dependency Injection (DI)**

The DI system is built around the `DependencyContainer` singleton, which registers and resolves dependencies. It supports singleton and transient lifetimes. Classes can receive injected dependencies via `[Inject]` attributes on fields or properties. Base classes like `InjectMonoBehaviour` and `InjectClass` automatically trigger injection on `OnEnable` or construction. `InstallerBehaviourBase` provides a convenient way to register dependencies in a scene.

### 2. **Data Models and Bindings**

Data models inherit from `DataModelBase` and use a code generation system (via `ModelsGenerator`) to create strongly-typed models from template classes. Each model exposes properties and corresponding `Accessor` objects, which allow for downward binding (UI updates) and value processing. `Accessor` manages value processors and invokes bindings. The `Bindings` class (in `BehaviourBase`) aggregates bindings for automatic cleanup.

### 3. **UI Framework**

The UI layer is structured around presenters, widgets, and animations.

- **Presenters** (`PresenterBase`, `RootPresenterBase`) manage screens and transitions. `ViewService` handles navigation, maintaining a history stack, and supports modal and pop-up behaviors.
- **Widgets** (`WidgetBase`) encapsulate reusable UI components with multiple states (e.g., button states) and use animation sequences defined in `UiAnimationBase` subclasses (fade, scale, slide, etc.).
- **Populators** (`PopulatorBase`, `PopulatorElementBase`) provide a pool-based system for dynamically generating lists of UI elements from data.
  
  
  
### 4. **Sound Service**
  
  `SoundService` manages audio playback using a pool of `AudioEmitter` objects. It loads audio events from `AudioEventsCollection` ScriptableObjects and plays them with optional fading, delays, and callbacks. Emitters can follow transforms or be positioned relative to a listener camera. The service uses a simple pool (`SimplePool`) to reuse emitters.
  
### 5. **Save Game Management**
  
  `SaveGameManager` saves and loads data for `IGameSaveData` components (e.g., `PersistentObject`) using `PlayerPrefs` (via `IPlayerPrefs` abstraction). It uses reflection to serialize fields/properties marked with `[SaveGameData]`. The `PersistentObject` component generates unique IDs (GUIDs or custom) and can save transform data. For data models, a separate `ModelService` with a `ModelDataStorageBridgeBase` (e.g., `PlayerPrefsBridge`) handles persistence.
  
### 6. **State Management**
  
  `StateService` provides a centralized way to manage application state using events and statuses. It allows emitting events with payloads, setting/unsetting statuses, and subscribing to state changes with conditions. It runs on a ticker and processes subscriptions after state updates.
  
### 7. **Tick System**
  
  `Ticker` is a custom update manager that uses `ITickable`, `ILateTickable`, and `IFixedTickable` interfaces. It groups objects with the same tick period and invokes them in bulk, improving performance. It can also check MonoBehaviour activity before ticking. 
  
### 8. **Utilities and Helpers**
- **SimplePool**: Generic object pool for Unity components.
- **ValueProcessor**: Allows chaining transformations on data model values.

## Architectural Overview

CherryFramework follows a service-oriented architecture with a strong emphasis on dependency injection to decouple systems. Key patterns include:

- **MVVM-like data binding**: Data models notify views through `Accessor` bindings, similar to view models in MVVM.
- **Composition over inheritance**: UI elements are built from composable animations and states.
- **Service Locator / DI**: The `DependencyContainer` acts as both a service locator and an IoC container.
- **Code generation**: Reduces boilerplate for data models, ensuring consistency.
- **Pooling**: Reuses objects (audio emitters, UI elements) to minimize garbage collection.

The framework is designed to be extended; developers can create custom data models, UI animations, storage bridges, and more by implementing provided interfaces or inheriting from base classes.

---

## Typical Usage Flow

1. **Setup**: Create an installer (derived from `InstallerBehaviourBase`) to bind services (e.g., `SoundService`, `ViewService`, `SaveGameManager`).
2. **Data Models**: Define template classes (e.g., `PlayerDataTemplate`) in a specific namespace; the code generator (press `Tools - UnityCodeGen - Generate` main menu) produces concrete models with accessors.
3. **UI**: Build presenters and widgets in scenes, referencing them in `ViewService` for navigation.
4. **Persistence**: Use `PersistentObject` on GameObjects that need to save state data, and manipulate them with `SaveGameManager`. For data models, use `ModelService` with a storage bridge.
5. **State**: Emit events or set statuses in `StateService` to drive game logic, and subscribe to changes.
6. **Tick**: Implement tickable interfaces on systems that need per-frame or periodic updates, and register them with `Ticker`.
   
   

---

**Dependencies:**
Code Generation - https://github.com/AnnulusGames/UnityCodeGen.git?path=/Assets/UnityCodeGen
UI animations and timers - https://dotween.demigiant.com/ or https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676
Editor Decoration - https://github.com/v0lt13/EditorAttributes

If you want to integrate save game data to Steam or other cloud services, I advise to use https://github.com/richardelms/FileBasedPlayerPrefs - a direct replacement to Unity's PlayerpRefs, that stores user data in an ordinary JSON files
