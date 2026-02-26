# CherryFramework Dependency Injection Documentation

## Overview

The CherryFramework Dependency Injection (DI) system is a lightweight, Unity-integrated IoC (Inversion of Control) container that provides automatic dependency management for both standard C# classes and MonoBehaviour components. It offers a simple yet powerful way to manage object lifetimes and dependencies throughout your application.

## Core Concepts

### Architecture Components

The DI system consists of several key components working together:

1. **DependencyContainer** - The core container that manages dependency registration and resolution
2. **InjectAttribute** - Marks fields and properties for dependency injection
3. **IInjectTarget** - Marker interface for injectable classes
4. **InjectClass** - Base class for non-MonoBehaviour injectable classes
5. **InjectMonoBehaviour** - Base class for MonoBehaviour injectable components
6. **InstallerBehaviourBase** - Base class for organizing dependency registrations

## Component Documentation

### 1. DependencyContainer

The heart of the DI system, this singleton container manages all dependency registrations and injections.

#### Key Features:

- **Singleton Access**: Global access via `DependencyContainer.Instance`
- **Lifetime Management**: Supports Singleton and Transient lifetimes
- **Auto-Injection**: Automatically injects dependencies into marked fields/properties
- **Type Safety**: Generic methods ensure compile-time type checking
- **Interface Support**: Bind implementations to interfaces
- **Automatic Disposal**: Proper cleanup of `IDisposable` dependencies

#### Lifetime Types:

| Type          | Description                                  |
| ------------- | -------------------------------------------- |
| **Singleton** | Single instance shared across all injections |
| **Transient** | New instance created for each injection      |

### 2. InjectAttribute

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : Attribute
```

Marks fields and properties for dependency injection. Can be applied to:

- Public, private, and protected fields
- Public and private properties with setters

### 3. InjectClass

Abstract base class for non-MonoBehaviour classes requiring dependency injection.

#### Features:

- Automatic injection during construction
- Lazy injection support via `EnsureDependencies()`
- Prevents multiple injection attempts

#### Example:

```csharp
public class PlayerService : InjectClass
{
    [Inject] private ILoggerService _logger;
    [Inject] public IDataManager DataManager { get; private set; }

    public void DoSomething()
    {
        // Dependencies are already injected via constructor
        _logger.Log("Doing something...");
    }
}
```

### 4. InjectMonoBehaviour

Abstract base class for MonoBehaviour components that need dependency injection.

#### Features:

- Automatic injection when component is enabled
- Configurable injection timing via `OnEnable()` override
- Injection state tracking

#### Example:

```csharp
public class PlayerController : InjectMonoBehaviour
{
    [Inject] private IInputService _input;
    [Inject] public IAudioService Audio { get; private set; }

    private void Update()
    {
        // Dependencies are injected when component is enabled
        var input = _input.GetMovement();
    }
}
```

### 5. InstallerBehaviourBase

Base class for organizing dependency registrations in a modular way.

#### Features:

- Automatic installation on Awake()
- Tracks installed dependencies for cleanup
- Provides convenience methods for all binding types
- Automatic cleanup on destroy

#### Example:

```csharp
public class GameInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        // Bind services
        BindAsSingleton<PlayerService>();
        BindAsSingleton<ScoreManager>();

        // Bind with interfaces
        Bind<FileDataManager, IDataManager>(BindingType.Singleton);
        Bind<UnityAudioService, IAudioService>(BindingType.Singleton);

        // Bind existing instances
        var config = new GameConfig();
        BindAsSingleton<IGameConfig>(config);
    }
}
```

## Usage Guide

### 1. Setting Up Dependencies

#### Basic Bindings:

```csharp
// Singleton binding (creates instance)
DependencyContainer.Instance.BindAsSingleton<PlayerService>();

// Singleton with existing instance
var service = new PlayerService();
DependencyContainer.Instance.BindAsSingleton(service);

// Transient binding
DependencyContainer.Instance.Bind<PlayerService>(BindingType.Transient);

// Interface binding
DependencyContainer.Instance.Bind<FileLogger, ILogger>(BindingType.Singleton);
```

#### Using Installers:

```csharp
// We need the installer to initialize before any other objects in scene
[DefaultExecutionOrder(-10000)]
public class ServiceInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        BindAsSingleton<NetworkService>();
        BindAsSingleton<AnalyticsService>();
        Bind<FileLogger, ILogger>(BindingType.Singleton);
    }
}
```

### 2. Receiving Dependencies

#### For Standard Classes:

```csharp
public class GameManager : InjectClass
{
    [Inject] private IPlayerService _player;
    [Inject] public IUIService UI { get; private set; }

    public void StartGame()
    {
        // Dependencies automatically available
        _player.Spawn();
        UI.ShowGameScreen();
    }
}
```

or:

```csharp
public class SomeClass : IInjectTarget
{
    [Inject] private IPlayerService _player;

    public void StartGame()
    {
        // We need to inject manually
        DependencyContainer.Instance.InjectDependencies(this);
        _player.Spawn();
    }
}
```

#### For MonoBehaviour Components:

```csharp
public class EnemyController : InjectMonoBehaviour
{
    [Inject] private IEnemyPool _pool;
    [Inject] public IPhysicsService Physics { get; private set; }

    private void Start()
    {
        // Dependencies injected via OnEnable()
        _pool.Register(this);
    }
}
```

### 3. Manual Injection

```csharp
// Manual injection when needed
var player = new Player();
DependencyContainer.Instance.InjectDependencies(player);

// Check if dependencies exist
if (DependencyContainer.Instance.HasDependency<ILogger>())
{
    // Do something
}
```

### 4. Cleanup

```csharp
// Individual dependency removal
DependencyContainer.Instance.RemoveDependency(typeof(ILogger));

// Container disposal (called automatically by installers)
DependencyContainer.Instance.Dispose();
```

## Best Practices

### 1. Organize with Installers

- Create separate installers for different modules (Services, UI, Gameplay, etc.)
- Keep related bindings together
- Use installers to control dependency lifetimes
- Make sure that Installer classes are initialized before any inject targets

### 2. Favor Interface Bindings

```csharp
// Good - Testable and flexible
Bind<SqlDatabase, IDatabase>(BindingType.Singleton);

// Avoid - Tight coupling
BindAsSingleton<SqlDatabase>();
```

### 3. Proper Lifetime Management

- Use **Singleton** for stateless services and shared resources
- Use **Transient** for lightweight, short-lived objects
- Be mindful of memory usage with Transient bindings

### 4. Injection Points

- Prefer property injection for better visibility
- Use field injection for private dependencies
- Ensure properties have setters for injection

### 5. Constructor Logic

- Avoid complex logic in constructors of injectable classes
- Use `EnsureDependencies()` for lazy initialization if needed

## Advanced Patterns

### 1. Conditional Binding

```csharp
public void InstallDebugDependencies()
{
    if (Debug.isDebugBuild)
    {
        Bind<DebugLogger, ILogger>(BindingType.Singleton);
    }
    else
    {
        Bind<FileLogger, ILogger>(BindingType.Singleton);
    }
}
```

### 2. Factory Pattern with DI

```csharp
public class EnemyFactory : InjectClass
{
    [Inject] private IEnemyPool _pool;

    public Enemy CreateEnemy(EnemyType type)
    {
        var enemy = _pool.Get(type);
        DependencyContainer.Instance.InjectDependencies(enemy);
        return enemy;
    }
}
```

### 3. Module-Based Architecture

```csharp
public class ModuleInstaller : InstallerBehaviourBase
{
    [SerializeField] private bool _enableModule;

    protected override void Install()
    {
        if (!_enableModule) return;

        BindAsSingleton<ModuleService>();
        Bind<ModuleRepository, IRepository>(BindingType.Singleton);
    }
}
```

## Performance Considerations

1. **Injection Overhead**: Injection uses reflection, which has some performance cost. Cache injected instances when possible.
2. **Singleton vs Transient**: Singletons share instances and use less memory; Transients create new instances but may increase garbage collection.
3. **Lazy Injection**: Use `EnsureDependencies()` or `Inject()` to delay injection until needed.

## Troubleshooting

### Common Issues and Solutions

| Issue                                          | Solution                                             |
| ---------------------------------------------- | ---------------------------------------------------- |
| "Could not add binding... already installed"   | Check for duplicate bindings in installers           |
| "tried to receive injection... not registered" | Ensure dependency is registered before injection     |
| Property not being injected                    | Verify property has a public/private setter          |
| Circular dependencies                          | Restructure code to avoid circular references        |
| MonoBehaviour injection not working            | Ensure component inherits from `InjectMonoBehaviour` |

## Limitations

- No constructor injection support
- No automatic circular dependency detection
- Reflection-based injection may have performance impact in critical paths
- Limited to field and property injection only

## API Reference

### DependencyContainer Public Methods

#### Binding Methods

| Method                                               | Description                                              |
| ---------------------------------------------------- | -------------------------------------------------------- |
| `BindAsSingleton<TService>(TService instance)`       | Binds an existing instance as a singleton                |
| `BindAsSingleton<TService>()`                        | Creates and binds a new instance as a singleton          |
| `BindAsSingleton(Type typeService, object instance)` | Binds an existing instance as a singleton by type        |
| `Bind<TService>(BindingType bindType)`               | Binds a type with specified lifetime                     |
| `Bind<TImpl, TService>(BindingType bindType)`        | Binds an implementation to a service type                |
| `BindAsSingleton<TImpl, TService>(TImpl instance)`   | Binds an existing implementation instance as a singleton |

#### Injection Methods

| Method                            | Description                                                                  |
| --------------------------------- | ---------------------------------------------------------------------------- |
| `InjectDependencies<T>(T target)` | Injects dependencies into fields/properties marked with `[Inject]` attribute |

#### Query Methods

| Method                     | Description                                                |
| -------------------------- | ---------------------------------------------------------- |
| `HasDependency<T>()`       | Checks if a dependency of type T is registered             |
| `HasDependency(Type type)` | Checks if a dependency of the specified type is registered |

#### Cleanup Methods

| Method                        | Description                                                        |
| ----------------------------- | ------------------------------------------------------------------ |
| `RemoveDependency(Type type)` | Removes and disposes a specific dependency                         |
| `Dispose()`                   | Disposes the container and all registered IDisposable dependencies |

## Conclusion

The CherryFramework Dependency Injection system provides a robust, Unity-integrated solution for managing dependencies in your game or application. Its simple API, automatic injection capabilities, and proper lifetime management make it an excellent choice for projects of any size. By following the patterns and practices outlined in this documentation, you can create clean, testable, and maintainable code with proper separation of concerns.