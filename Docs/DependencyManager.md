# CherryFramework DependencyManager Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [IInjectTarget Interface](#iinjecttarget-interface)
4. [DependencyContainer](#dependencycontainer)
5. [Binding Types](#binding-types)
6. [InjectAttribute](#injectattribute)
7. [Base Classes](#base-classes)
8. [InstallerBehaviourBase](#installerbehaviourbase)
9. [Performance Considerations](#performance-considerations)
10. [Common Issues and Solutions](#common-issues-and-solutions)
11. [Limitations](#limitations)
12. [Best Practices](#best-practices)
13. [Examples](#examples)

---

## Overview

The CherryFramework DependencyManager provides a lightweight dependency injection (DI) container that simplifies service location and promotes loose coupling throughout your application. It supports both singleton and transient lifestyles, with automatic injection into classes that implement `IInjectTarget`.

### Key Features

- **Automatic Injection**: Dependencies are automatically resolved and injected
- **Multiple Lifestyles**: Singleton and transient binding support
- **MonoBehaviour Support**: Special handling for Unity components
- **Hierarchical Injection**: Base class dependencies are also injected
- **Type Safety**: Generic binding methods
- **Automatic Cleanup**: Dependencies can be removed when no longer needed

### Important Requirements

- Only classes that implement `IInjectTarget` can use `[Inject]` attributes
- Classes derived from `InjectClass` and `InjectMonoBehaviour` receive injection automatically
- Installers must explicitly set `[DefaultExecutionOrder(-10000)]` (or any low value below zero)

---

## Core Concepts

### Architecture Diagram

```
┌──────────────────────────────────────────────────────┐
│                  DependencyContainer                 │
│                    (Singleton)                       │
├──────────────────────────────────────────────────────┤
│ + BindAsSingleton<T>()                               │
│ + Bind<T>()                                          │
│ + InjectDependencies<T>(T target)                    │
└──────────────────────────────────────────────────────┘
                              │
            ┌─────────────────┼─────────────────┐
            ▼                 ▼                 ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │  InjectClass  │InjectMonoBehaviour│   Installer   │
    │ (Abstract)    │ │  (Abstract)   │ │ BehaviourBase │
    └───────────────┘ └───────────────┘ └───────────────┘
            │                 │                 │
            ▼                 ▼                 │
    ┌───────────────┐ ┌──────────────────────┐  │
    │  Your Classes │ │  Your MonoBehaviours │  │
    │  (non-Mono)   │ │   (must derive)      │  │
    └───────────────┘ └──────────────────────┘  │
            │                 │                 │
            └─────────────────┼─────────────────┘
                              ▼
                    ┌────────────────────┐
                    │  [Inject]          │
                    │  Fields/Properties │
                    └────────────────────┘
```

### Key Components

| Component                | Purpose                                                        |
| ------------------------ | -------------------------------------------------------------- |
| `IInjectTarget`          | Marker interface for injectable classes                        |
| `DependencyContainer`    | Central DI container (singleton)                               |
| `InjectAttribute`        | Marks fields/properties for injection                          |
| `InjectClass`            | Base for non-MonoBehaviour injectable classes (auto-injection) |
| `InjectMonoBehaviour`    | Base for Unity component injection (auto-injection)            |
| `InstallerBehaviourBase` | Configures dependencies at startup                             |
| `BindingType`            | Defines dependency lifestyle (Singleton/Transient)             |

---

## IInjectTarget Interface

**Namespace**: `CherryFramework.DependencyManager`

**Purpose**: Marker interface that identifies classes eligible for dependency injection. Only classes that implement this interface can use the `[Inject]` attribute and receive injected dependencies.

```csharp
public interface IInjectTarget
{
    // Marker interface - no members required
}
```

**Automatic Injection**: Classes derived from `InjectClass` and `InjectMonoBehaviour` automatically implement `IInjectTarget` and receive injection without any additional code.

**Example**:

```csharp
// These classes automatically implement IInjectTarget and receive injection
public class MyService : InjectClass { }                    // Auto-injected
public class MyComponent : InjectMonoBehaviour { }          // Auto-injected on OnEnable

// Manual implementation (rare, not recommended)
public class CustomClass : IInjectTarget
{
    [Inject] private ILogger _logger; // Must be manually injected
}
```

---

## DependencyContainer

**Namespace**: `CherryFramework.DependencyManager`

**Purpose**: Central service locator and dependency injection container. Implemented as a singleton.

### Singleton Access

```csharp
// Get the container instance anywhere in your code
var container = DependencyContainer.Instance;
```

### Binding Methods

#### BindAsSingleton (with instance)

```csharp
public void BindAsSingleton<TService>(TService instance) where TService : class
```

Binds an existing instance as a singleton. The same instance will be returned for all injections.

**Example**:

```csharp
// Create and bind a service
var logger = new FileLogger("game.log");
DependencyContainer.Instance.BindAsSingleton<ILogger>(logger);

// Later injections receive the same instance
```

#### BindAsSingleton (with type)

```csharp
public void BindAsSingleton<TService>() where TService : class, new()
```

Binds a type as a singleton. The container will create the instance on first demand.

**Example**:

```csharp
// Bind as singleton (lazy creation)
DependencyContainer.Instance.BindAsSingleton<AnalyticsService>();
```

#### BindAsSingleton (with type and instance)

```csharp
public void BindAsSingleton(Type typeService, object instance)
```

Binds an existing instance to a specific type.

**Example**:

```csharp
DependencyContainer.Instance.BindAsSingleton(typeof(ILogger), fileLogger);
```

#### Bind (with BindingType)

```csharp
public void Bind<TService>(BindingType bindType) where TService : class, new()
```

Binds a type with specified lifestyle (Singleton or Transient).

**Example**:

```csharp
// Transient - new instance each time
DependencyContainer.Instance.Bind<EnemyFactory>(BindingType.Transient);
```

#### Bind with Interface

```csharp
public void Bind<TImpl, TService>(BindingType bindType) 
    where TImpl : class, new() 
    where TService : class
```

Binds an implementation type to a service interface.

**Example**:

```csharp
// Bind IRepository to FileRepository implementation
DependencyContainer.Instance.Bind<FileRepository, IRepository>(BindingType.Singleton);

// Now requests for IRepository return FileRepository
```

#### BindAsSingleton with Interface

```csharp
public void BindAsSingleton<TImpl, TService>(TImpl instance) 
    where TImpl : class 
    where TService : class
```

Binds an existing instance to a service interface.

**Example**:

```csharp
var repository = new FileRepository();
DependencyContainer.Instance.BindAsSingleton<FileRepository, IRepository>(repository);
```

### Management Methods

```csharp
// Check if dependency exists
if (DependencyContainer.Instance.HasDependency<ILogger>())
{
    Debug.Log("Logger is registered");
}

// Remove dependency (disposes if IDisposable)
DependencyContainer.Instance.RemoveDependency(typeof(ILogger));
```

---

## Binding Types

**Namespace**: `CherryFramework.DependencyManager`

**Purpose**: Defines the lifestyle of bound dependencies.

```csharp
public enum BindingType
{
    Singleton = 0,  // Single instance shared across all requests
    Transient = 1   // New instance created for each injection
}
```

### Singleton Lifestyle

- One instance created and shared
- Created on first demand (lazy) or provided instance
- Ideal for services that maintain state

**Example**:

```csharp
// Configuration service - should be singleton
DependencyContainer.Instance.BindAsSingleton<GameConfiguration>();

// Shared resources
DependencyContainer.Instance.BindAsSingleton<TextureCache>();
```

### Transient Lifestyle

- New instance created for each injection
- No shared state between injections
- Ideal for stateless services or factories

**Example**:

```csharp
// Factory classes - new each time
DependencyContainer.Instance.Bind<EnemyFactory, IUnitFactory>(BindingType.Transient);

// Request-scoped data
DependencyContainer.Instance.Bind<LevelData>(BindingType.Transient);
```

---

## InjectAttribute

**Namespace**: `CherryFramework.DependencyManager`

**Purpose**: Marks fields and properties for dependency injection

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : Attribute
{
}
```

### Usage Requirements

- Can only be used in classes that implement `IInjectTarget`
- Works automatically in classes derived from `InjectClass` or `InjectMonoBehaviour`
- Fields can be private, protected, or public
- Properties must have a setter

### Examples

#### Field Injection (Auto-injected)

```csharp
public class PlayerController : InjectMonoBehaviour  // Implements IInjectTarget, auto-injected
{
    [Inject] private IInputService _input;
    [Inject] private ILogger _logger;

    public void Move()
    {
        _logger.Log($"Moving with input: {_input.GetMovement()}");
    }
}
```

#### Property Injection (Auto-injected)

```csharp
public class GameManager : InjectClass  // Implements IInjectTarget, auto-injected
{
    [Inject] public IAnalyticsService Analytics { get; private set; }
    [Inject] public ISaveGameManager SaveGame { get; private set; }
}
```

#### Base Class Injection (Auto-injected)

```csharp
public abstract class BaseService : InjectClass  // Implements IInjectTarget, auto-injected
{
    [Inject] protected ILogger Logger;
}

public class PlayerService : BaseService  // Inherits IInjectTarget, auto-injected
{
    [Inject] private IPlayerRepository _repository;

    public void DoSomething()
    {
        Logger.Log("Base class injection works!");
    }
}
```

#### Invalid Usage (Will Not Be Injected)

```csharp
// This class does NOT implement IInjectTarget
public class RegularClass
{
    [Inject] private ILogger _logger; // Will NOT be injected!
}

// This will not trigger automatic injection
var regular = new RegularClass(); // _logger remains null
```

---

## Base Classes

### InjectClass (Abstract)

**Namespace**: `CherryFramework.DependencyManager`

**Purpose**: Base class for non-MonoBehaviour classes that need dependency injection. Automatically implements `IInjectTarget` and receives injection on construction.

**Inheritance**: `IInjectTarget`

```csharp
public abstract class InjectClass : IInjectTarget
{
    // Automatically receives injection when constructed
}
```

**Features**:

- Automatic injection on construction
- Implements `IInjectTarget`
- Safe to use `[Inject]` attributes
- No manual injection calls needed

**Example**:

```csharp
public class AnalyticsService : InjectClass
{
    [Inject] private ILogger _logger;
    [Inject] private IDataSender _dataSender;

    public void TrackEvent(string eventName)
    {
        // Dependencies already injected via constructor
        _logger.Log($"Tracking: {eventName}");
        _dataSender.Send(eventName);
    }
}

// Usage - injection happens automatically
var service = new AnalyticsService(); // Dependencies are injected
```

### InjectMonoBehaviour (Abstract)

**Namespace**: `CherryFramework.DependencyManager`

**Purpose**: Base class for Unity MonoBehaviour components that need dependency injection. Automatically implements `IInjectTarget` and receives injection when `OnEnable()` is called.

**Inheritance**: `MonoBehaviour`, `IInjectTarget`

```csharp
public abstract class InjectMonoBehaviour : MonoBehaviour, IInjectTarget
{
    protected virtual void OnEnable()
    {
        // Automatically injects dependencies
    }
}
```

**Features**:

- Automatic injection in `OnEnable()`
- Implements `IInjectTarget`
- Prevents duplicate injection
- Works with Unity lifecycle

**Example**:

```csharp
public class PlayerController : InjectMonoBehaviour
{
    [Inject] private IInputService _input;
    [Inject] private IPlayerModel _playerModel;

    [SerializeField] private float _speed = 5f;

    private void Start()
    {
        // Dependencies are already injected via OnEnable
        Debug.Log($"Player controller ready with input: {_input != null}");
    }

    private void Update()
    {
        var movement = _input.GetMovement();
        transform.Translate(movement * _speed * Time.deltaTime);
    }

    protected override void OnEnable()
    {
        base.OnEnable(); // Triggers automatic injection
        // Additional setup after injection
    }
}
```

---

## InstallerBehaviourBase

**Namespace**: `CherryFramework.DependencyManager`

**Purpose**: Base class for installer components that configure dependencies at startup.

**Important Note**: The `[DefaultExecutionOrder]` attribute is **not inherited** by derived classes. You must apply it to each installer class you create with a low negative value (e.g., `-10000`) to ensure it runs before other components.

```csharp
public abstract class InstallerBehaviourBase : MonoBehaviour
{
    protected abstract void Install();

    private void Awake()
    {
        Install();
    }

    // Protected binding methods
    protected void BindAsSingleton<TService>(TService instance) where TService : class;
    protected void BindAsSingleton<TService>() where TService : class, new();
    protected void BindAsSingleton(Type typeService, object instance);
    protected void Bind<TService>(BindingType bindType) where TService : class, new();
    protected void Bind<TImpl, TService>(BindingType bindType) where TImpl : class, new() where TService : class;
    protected void BindAsSingleton<TImpl, TService>(TImpl instance) where TImpl : class where TService : class;
}
```

**Features**:

- Installation in `Awake()`
- Provides convenient binding methods
- Tracks installed dependencies for automatic cleanup on destroy

### Example Installer

```csharp
[DefaultExecutionOrder(-10000)] // REQUIRED! Not inherited from base class
public class GameInstaller : InstallerBehaviourBase
{
    [SerializeField] private GameConfiguration _config;
    [SerializeField] private bool _useMockServices;

    protected override void Install()
    {
        // Bind configuration as singleton
        BindAsSingleton(_config);

        // Bind services
        if (_useMockServices)
        {
            // Use mock implementations for testing
            BindAsSingleton<MockAnalyticsService, IAnalyticsService>();
            BindAsSingleton<MockSaveGameService, ISaveGameService>();
        }
        else
        {
            // Use real implementations
            BindAsSingleton<AnalyticsService, IAnalyticsService>();
            BindAsSingleton<SaveGameService, ISaveGameService>();
        }

        // Bind with specific lifestyle
        Bind<EnemyFactory>(BindingType.Transient);
        Bind<LevelLoader>(BindingType.Singleton);

        // Bind existing instance
        var logger = new FileLogger("game.log");
        BindAsSingleton<ILogger>(logger);

        Debug.Log("Game dependencies installed");
    }
}
```

### Multiple Installers with Different Execution Order

```csharp
// Core installer (runs first)
[DefaultExecutionOrder(-10000)]
public class CoreInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        BindAsSingleton<ILogger, ConsoleLogger>();
        BindAsSingleton<IEventDispatcher, EventDispatcher>();
    }
}

// Scene-specific installer (runs after CoreInstaller)
[DefaultExecutionOrder(-9999)]
public class LevelInstaller : InstallerBehaviourBase
{
    [SerializeField] private LevelData _levelData;

    protected override void Install()
    {
        BindAsSingleton(_levelData);
        Bind<EnemySpawner>(BindingType.Transient);
        Bind<LevelManager>(BindingType.Singleton);
    }
}
```

---

## Performance Considerations

### 1. Injection Overhead

Dependency injection uses reflection to scan for `[Inject]` attributes. This happens:

- Once per class instance for `InjectClass` derivatives (on construction)
- Once per component for `InjectMonoBehaviour` derivatives (on first `OnEnable`)

**Impact**: Minimal for most games. For performance-critical code that creates many objects (e.g., pooling systems), consider:

```csharp
// For high-frequency object creation, consider object pooling
public class Bullet : InjectMonoBehaviour
{
    [Inject] private ILogger _logger; // Injected once per bullet

    // Better to avoid injection for thousands of objects
}

// Alternative: Use a manager pattern
public class BulletManager : InjectClass
{
    [Inject] private ILogger _logger; // Injected once

    public Bullet CreateBullet()
    {
        var bullet = new Bullet(); // Regular class, no injection
        bullet.Initialize(_logger); // Pass dependencies manually
        return bullet;
    }
}
```

### 2. Container Lookup

The `DependencyContainer.Instance` access is thread-safe but has minimal overhead. Cache references when possible:

```csharp
// GOOD - Cache after injection
public class MyService : InjectClass
{
    [Inject] private IExpensiveService _service; // Injected once

    public void DoWork()
    {
        _service.Process(); // Direct access, no lookup
    }
}

// BAD - Repeated container lookups
public class BadService : InjectClass
{
    public void DoWork()
    {
        var service = DependencyContainer.Instance.InjectDependencies(this); // Lookup each time
        service.Process();
    }
}
```

### 3. Singleton vs Transient

- **Singletons**: Created once, minimal overhead
- **Transient**: Created for each injection, more allocations

```csharp
// For frequently created objects, consider singleton with factory pattern
Bind<EnemyFactory>(BindingType.Singleton); // Created once

// Instead of
Bind<EnemyFactory>(BindingType.Transient); // Created for each injection
```

### 4. Memory Usage

- Each binding stores type information in dictionaries
- Singleton instances persist for container lifetime
- Transient instances are garbage-collected when no longer referenced

---

## Common Issues and Solutions

### Issue 1: Dependencies are Null

**Symptoms**: `NullReferenceException` when accessing injected fields/properties

**Causes**:

- Class doesn't implement `IInjectTarget`
- For `InjectMonoBehaviour`, `OnEnable()` wasn't called (object disabled)
- Dependency not registered in container

**Solutions**:

```csharp
// SOLUTION 1: Ensure class implements IInjectTarget
public class MyService : InjectClass // GOOD - implements IInjectTarget
{
    [Inject] private ILogger _logger; // Will be injected
}

// SOLUTION 2: Ensure component is enabled
public class MyComponent : InjectMonoBehaviour
{
    private void Start()
    {
        // If OnEnable didn't run (object started disabled), inject manually
        if (!Injected) Inject();
    }
}

// SOLUTION 3: Check dependency registration
[DefaultExecutionOrder(-10000)]
public class CheckInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        if (!DependencyContainer.Instance.HasDependency<ILogger>())
        {
            Debug.LogError("ILogger not registered! Creating default.");
            BindAsSingleton<ConsoleLogger, ILogger>();
        }
    }
}
```

### Issue 2: Installer Not Running First

**Symptoms**: Dependencies not available in `Start()` or `Awake()` of other components

**Cause**: Missing `[DefaultExecutionOrder]` on installer class

**Solution**:

```csharp
// GOOD - Explicit execution order
[DefaultExecutionOrder(-10000)] // CRITICAL: Runs before everything
public class GameInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        BindAsSingleton<ILogger, FileLogger>();
    }
}

// BAD - No execution order, may run too late
public class GameInstaller : InstallerBehaviourBase // No attribute!
{
    // May run after other components' Awake()
}
```

### Issue 3: Circular Dependencies

**Symptoms**: Stack overflow or unexpected null references

**Cause**: Two classes depend on each other

**Solution**:

```csharp
// PROBLEM: Circular dependency
public class ServiceA : InjectClass
{
    [Inject] private ServiceB _b; // Depends on B
}

public class ServiceB : InjectClass
{
    [Inject] private ServiceA _a; // Depends on A - CIRCULAR!
}

// SOLUTION 1: Use interfaces and restructure
public interface IServiceA { }
public interface IServiceB { }

public class ServiceA : InjectClass, IServiceA
{
    [Inject] private IServiceB _b; // Depends on interface
}

public class ServiceB : InjectClass, IServiceB
{
    [Inject] private IServiceA _a; // Depends on interface
}

// SOLUTION 2: Use events or callbacks instead of direct references
public class ServiceA : InjectClass
{
    public event Action OnSomethingHappened;
}

public class ServiceB : InjectClass
{
    [Inject] private ServiceA _a;

    public void Initialize()
    {
        _a.OnSomethingHappened += HandleSomething; // Event-based communication
    }
}
```

### Issue 4: Memory Leaks

**Symptoms**: Objects not being garbage collected, increasing memory usage

**Cause**: Dependencies not removed from container, or event handlers not unsubscribed

**Solutions**:

```csharp
// Installers auto-cleanup dependencies they installed
[DefaultExecutionOrder(-10000)]
public class LevelInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        BindAsSingleton<LevelData>(_levelData); // Auto-cleaned when installer destroyed
    }
}

// Manual cleanup for long-lived containers
public class GameManager : InjectClass
{
    [Inject] private ITemporaryService _service;

    public void Cleanup()
    {
        DependencyContainer.Instance.RemoveDependency(typeof(ITemporaryService));
        _service = null;
    }
}

// Always unsubscribe events. This is not needed if using CherryFramework.StateService
public class EventSubscriber : InjectMonoBehaviour
{
    [Inject] private EventDispatcher _events;

    private void OnEnable()
    {
        _events.Subscribe("GameEvent", HandleEvent);
    }

    private void OnDisable()
    {
        _events.Unsubscribe("GameEvent", HandleEvent); // CRITICAL
    }
}
```

### Issue 5: Multiple Bindings for Same Type

**Symptoms**: Only the first binding works, others produce error message

**Cause**: Container doesn't support multiple bindings for the same type

**Solution**:

```csharp
// PROBLEM: Can't have multiple bindings for same type
DependencyContainer.Instance.BindAsSingleton<ILogger, FileLogger>();
DependencyContainer.Instance.BindAsSingleton<ILogger, ConsoleLogger>(); // Error mesage!

// SOLUTION: Use factories or named bindings pattern
public interface ILogger { }
public class FileLogger : ILogger { }
public class ConsoleLogger : ILogger { }

// Create a factory that provides the right implementation
public interface ILoggerFactory
{
    ILogger GetLogger(string type);
}

public class LoggerFactory : InjectClass, ILoggerFactory
{
    [Inject] private FileLogger _fileLogger; // Both injected
    [Inject] private ConsoleLogger _consoleLogger;

    public ILogger GetLogger(string type)
    {
        return type switch
        {
            "file" => _fileLogger,
            "console" => _consoleLogger,
            _ => _consoleLogger
        };
    }
}

// Installer
[DefaultExecutionOrder(-10000)]
public class GameInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        BindAsSingleton<FileLogger>(); // Register concrete types
        BindAsSingleton<ConsoleLogger>();
        BindAsSingleton<LoggerFactory, ILoggerFactory>();
    }
}
```

### Issue 6: Injection in Static Classes

**Symptoms**: `[Inject]` attributes in static classes don't work

**Cause**: Static classes cannot be instantiated, so injection cannot occur

**Solution**:

```csharp
// PROBLEM: Static classes can't use injection
public static class StaticHelper
{
    [Inject] private static ILogger _logger; // NEVER injected
}

// SOLUTION: Use singleton service pattern
public class HelperService : InjectClass
{
    [Inject] private ILogger _logger;

    private static HelperService _instance;

    public HelperService()
    {
        _instance = this;
    }

    public static void Log(string message)
    {
        _instance?._logger?.Log(message); // Access through instance
    }
}
```

### Issue 7: Injection in Unity Messages (Awake, Start)

**Symptoms**: Dependencies null in `Awake()`, available in `Start()`

**Cause**: `InjectMonoBehaviour` injects in `OnEnable()`, which runs after `Awake()` but before `Start()`

**Solution**:

```csharp
public class MyComponent : InjectMonoBehaviour
{
    [Inject] private ILogger _logger;

    private void Awake()
    {
        // Dependencies NOT injected yet (OnEnable not called)
        // _logger is null here
    }

    private void OnEnable()
    {
        base.OnEnable(); // Injection happens here
    }

    private void Start()
    {
        // Dependencies ARE injected (OnEnable ran)
        _logger.Log("Ready!"); // Works fine
    }

    // If you need dependencies in Awake, inject manually
    private void Awake()
    {
        Inject(); // Manual injection
        // Now _logger is available
    }
}
```

---

## Limitations

### 1. No Constructor Injection

The framework does not support constructor injection. All dependencies must be injected via fields or properties with the `[Inject]` attribute.

```csharp
// NOT SUPPORTED
public class MyService
{
    public MyService(ILogger logger) // This won't work
    {
    }
}

// SUPPORTED
public class MyService : InjectClass
{
    [Inject] private ILogger _logger; // This works
}
```

### 2. Single Binding Per Type

The container only supports one binding per type. Registering multiple implementations for the same interface will result in only the first registration being used.

```csharp
// Only the first binding is effective
DependencyContainer.Instance.BindAsSingleton<FileLogger, ILogger>(); // Works
DependencyContainer.Instance.BindAsSingleton<ConsoleLogger, ILogger>(); // Error!
```

### 3. No Named Bindings

The framework does not support named bindings or conditional bindings. You cannot have multiple bindings for the same type differentiated by name or condition.

### 4. No Open Generic Bindings

The container does not support binding open generic types. You must bind closed generic types explicitly.

```csharp
// NOT SUPPORTED
Bind(typeof(IRepository<>), typeof(FileRepository<>)); // Won't work

// REQUIRED
BindAsSingleton<FileRepository<PlayerData>, IRepository<PlayerData>>();
BindAsSingleton<FileRepository<ScoreData>, IRepository<ScoreData>>();
```

### 5. No Property Injection Without Setters

Properties used for injection must have a setter (public, private, or protected). Read-only properties cannot be injected.

```csharp
public class MyService : InjectClass
{
    [Inject] public ILogger Logger { get; private set; } // OK - has setter

    [Inject] public IAnalytics Analytics { get; } // NOT OK - no setter
}
```

### 6. No Injection into Static Members

Static fields and properties cannot be injected, as injection works on instances only.

```csharp
public class MyService : InjectClass
{
    [Inject] private static ILogger _logger; // NEVER injected
}
```

### 7. No Injection into Unity-Serialized Fields with [Inject]

While you can combine `[Inject]` with `[SerializeField]`, the injection happens at runtime, not during Unity serialization.

```csharp
public class MyComponent : InjectMonoBehaviour
{
    [Inject] [SerializeField] private SettingsModel _model; // Injected at runtime

    // In the Inspector, you'll see this field but the value will be overwritten by injection
}
```

### 8. No Circular Dependency Detection

The container does not automatically detect circular dependencies. They will manifest as stack overflows or unexpected behavior.

### 9. No Child Containers or Scopes

The framework does not support creating child containers with their own lifetimes or scoped dependencies.

### 10. No Built-in Profiling

There are no built-in tools for profiling injection performance or debugging dependency resolution.

### 11. No Lazy Dependencies

Dependencies are resolved immediately during injection. There's no built-in support for `Lazy<T>` or factory delegates.

### 12. Platform Limitations

The framework uses reflection which works on all Unity-supported platforms, but some platforms (like IL2CPP with code stripping) may require additional configuration to preserve injected members.

```csharp
// Use [Preserve] attribute to prevent stripping
public class MyService : InjectClass
{
    [Inject] [Preserve] private ILogger _logger; // Prevent stripping
}
```

---

## Best Practices

### 1. Always Derive from Base Classes for Automatic Injection

```csharp
// GOOD - Automatically injected
public class MyService : InjectClass { }

// GOOD - Automatically injected
public class MyComponent : InjectMonoBehaviour { }  

// BAD - Must manually inject
public class MyService : IInjectTarget { } // No automatic injection
```

### 2. Always Apply DefaultExecutionOrder to Installers

```csharp
[DefaultExecutionOrder(-10000)] // CRITICAL: Not inherited!
public class MyInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        // Installation code
    }
}
```

### 3. Install Dependencies Early

```csharp
[DefaultExecutionOrder(-10000)]
public class ProjectInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        BindAsSingleton<ILogger, FileLogger>();
        BindAsSingleton<IAnalytics, AnalyticsService>();
        Bind<ISaveGame, SaveGameService>(BindingType.Singleton);
    }
}
```

### 4. Use Interface-Based Programming

Always depend on interfaces, not concrete types:

```csharp
// GOOD
BindAsSingleton<FileRepository, IRepository>();
[Inject] private IRepository _repository;

// BAD
BindAsSingleton<FileRepository>();
[Inject] private FileRepository _repository;
```

### 5. Choose Appropriate Lifestyles

```csharp
// Singleton for shared state
BindAsSingleton<GameState>();
BindAsSingleton<Configuration>();

// Transient for stateless or factory classes
Bind<EnemyFactory>(BindingType.Transient);
Bind<Bullet>(BindingType.Transient);
```

### 6. Always Call Base.OnEnable()

```csharp
public class MyComponent : InjectMonoBehaviour
{
    protected override void OnEnable()
    {
        base.OnEnable(); // ALWAYS call this for automatic injection
        // Custom initialization
    }
}
```

### 7. Use Properties for Optional Dependencies

```csharp
public class UIManager : InjectClass
{
    [Inject] public ILogger Logger { get; set; } // Optional

    // Required dependencies via field injection
    [Inject] private IViewService _viewService;
}
```

### 8. Avoid Circular Dependencies

```csharp
// BAD - Circular dependency
public class ServiceA : InjectClass
{
    [Inject] private ServiceB _b;
}

public class ServiceB : InjectClass
{
    [Inject] private ServiceA _a; // Circular!
}

// GOOD - Use interface and restructure
public interface IServiceA { }
public interface IServiceB { }

public class ServiceA : InjectClass, IServiceA
{
    [Inject] private IServiceB _b; // Depends on interface
}
```

### 9. Clean Up Dependencies When Needed

```csharp
// Installers auto-cleanup
[DefaultExecutionOrder(-10000)]
public class SceneInstaller : InstallerBehaviourBase
{
    protected override void Install()
    {
        BindAsSingleton<SceneData>(_data); // Auto-cleaned when scene unloads
    }
}

// Manual cleanup for long-lived containers
public class TempService : InjectClass, IDisposable
{
    public void Dispose()
    {
        // Cleanup resources
    }
}

// Remove from container when done
DependencyContainer.Instance.RemoveDependency(typeof(TempService));
```

### 10. Use [Preserve] for IL2CPP Builds

```csharp
using UnityEngine.Scripting;

public class MyService : InjectClass
{
    [Inject] [Preserve] private ILogger _logger; // Prevent code stripping
}
```

---

## Examples

### Complete Application Setup with Automatic Injection

```csharp
// 1. Define interfaces
public interface ILogger
{
    void Log(string message);
}

public interface IPlayerService
{
    void SavePlayer(PlayerData player);
    PlayerData LoadPlayer();
}

public interface IAnalyticsService
{
    void TrackEvent(string eventName);
}

// 2. Implement services (derive from InjectClass for auto-injection)
public class FileLogger : InjectClass, ILogger
{
    private string _path;

    public FileLogger(string path)
    {
        _path = path;
    }

    public void Log(string message)
    {
        Debug.Log($"[{DateTime.Now}] {message}");
    }
}

public class PlayerService : InjectClass, IPlayerService
{
    [Inject] private ILogger _logger;  // Auto-injected
    [Inject] private IRepository<PlayerData> _repository;  // Auto-injected

    public void SavePlayer(PlayerData player)
    {
        _logger.Log($"Saving player: {player.Name}");
        _repository.Save(player);
    }

    public PlayerData LoadPlayer()
    {
        _logger.Log("Loading player");
        return _repository.Load();
    }
}

public class AnalyticsService : InjectClass, IAnalyticsService
{
    [Inject] private ILogger _logger;  // Auto-injected

    public void TrackEvent(string eventName)
    {
        _logger.Log($"Analytics event: {eventName}");
        // Send to analytics service
    }
}

// 3. Create installer
[DefaultExecutionOrder(-10000)]
public class GameInstaller : InstallerBehaviourBase
{
    [SerializeField] private string _logPath = "game.log";

    protected override void Install()
    {
        // Bind logger with instance
        var logger = new FileLogger(_logPath);
        BindAsSingleton<ILogger>(logger);

        // Bind services
        BindAsSingleton<PlayerService, IPlayerService>();
        BindAsSingleton<AnalyticsService, IAnalyticsService>();

        // Bind repository
        BindAsSingleton<JsonRepository<PlayerData>, IRepository<PlayerData>>();

        Debug.Log("Game dependencies installed");
    }
}

// 4. Use in components (derive from InjectMonoBehaviour for auto-injection)
public class GameManager : InjectMonoBehaviour
{
    [Inject] private IPlayerService _playerService;  // Auto-injected
    [Inject] private IAnalyticsService _analytics;    // Auto-injected

    private void Start()
    {
        var player = _playerService.LoadPlayer();
        _analytics.TrackEvent("GameStarted");
    }
}

public class SettingsUI : InjectMonoBehaviour
{
    [Inject] private ILogger _logger;  // Auto-injected

    public void SaveSettings()
    {
        _logger.Log("Settings saved");
        // Save logic
    }
}
```

---

## Summary

| Component                | Purpose                       | Injection Behavior                  |
| ------------------------ | ----------------------------- | ----------------------------------- |
| `IInjectTarget`          | Marker for injectable classes | Required for injection              |
| `InjectClass`            | Non-MonoBehaviour base        | Auto-injection on construction      |
| `InjectMonoBehaviour`    | MonoBehaviour base            | Auto-injection on OnEnable          |
| `InjectAttribute`        | Marks injectable members      | Only works in IInjectTarget classes |
| `InstallerBehaviourBase` | Dependency configuration      | Must add `[DefaultExecutionOrder]`  |
| `DependencyContainer`    | Central DI container          | Singleton access                    |

### Critical Requirements Summary

1. **Only `IInjectTarget` classes can use `[Inject]`** - Classes must implement this interface
2. **Deriving from `InjectClass` or `InjectMonoBehaviour` provides automatic injection** - No manual injection calls needed
3. **Installers must have `[DefaultExecutionOrder]` with a low negative value** - This attribute is not inherited
4. **Constructor injection is not supported** - Use field/property injection with `[Inject]`
5. **All bindings are type-based** - Bind to interfaces for maximum flexibility

### Key Benefits

- **Automatic Injection**: No manual resolution code needed
- **Loose Coupling**: Components depend on abstractions, not concretions
- **Testability**: Easy to mock dependencies for unit tests
- **Flexibility**: Swap implementations without changing consuming code
- **Lifecycle Management**: Automatic cleanup
