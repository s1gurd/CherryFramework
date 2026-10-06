# CherryFramework DependencyManager Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [What Triggers Automatic Injection](#what-triggers-automatic-injection)
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
14. [Summary](#summary)

---

## Overview

The CherryFramework DependencyManager provides a lightweight dependency injection (DI) container that simplifies service location and promotes loose coupling throughout your application. It supports both singleton and transient lifestyles, with automatic injection into any class deriving from `InjectClass` or `InjectMonoBehaviour`.

### Why Use Dependency Injection?

Without DI, a class that needs logging has to build its own logger:

```csharp
// No DI: the implementation is hard-wired into the class
public class PlayerService
{
    private readonly FileLogger _logger = new FileLogger("game.log");
}
```

`PlayerService` now *depends on a concrete type*. You cannot swap it for a
console logger, and a unit test has to create real files to test the class.

With DI the class only states what it needs:

```csharp
// With DI: the class asks for the abstraction
public class PlayerService
{
    [Inject] private ILogger _logger;
}
```

Now the implementation is decided in exactly one place - the installer - and
anybody can replace it. That is the whole idea: **depend on the abstraction,
choose the implementation at the edge**.

### About the Types in These Examples

`ILogger`, `ConsoleLogger`, `FileLogger`, `IAnalyticsService`,
`AnalyticsService`, `IRepository<T>` and `FileRepository<T>` below are **not**
framework classes - they are ordinary types you would write yourself, used to
demonstrate the container. Define them once, in your own project:

```csharp
public interface ILogger { void Log(string message); }

public class FileLogger : ILogger
{
    private readonly string _path;
    public FileLogger(string path) => _path = path;

    public void Log(string message)
        => System.IO.File.AppendAllText(_path, message + "\n");
}

public class ConsoleLogger : ILogger
{
    public void Log(string message) => Debug.Log(message);
}

public interface IAnalyticsService { void Track(string eventName); }

public class AnalyticsService : IAnalyticsService
{
    public void Track(string eventName) => Debug.Log("track " + eventName);
}

public interface IRepository<T>
{
    void Save(T data);
    T Load();
}
```

Because they need constructor arguments, `FileLogger` is registered with an
instance (`BindAsSingleton(new FileLogger("game.log"))`) rather than by type.

### Key Features

- **Automatic Injection**: Dependencies are automatically resolved and injected
- **Multiple Lifestyles**: Singleton and transient binding support
- **MonoBehaviour Support**: Special handling for Unity components
- **Hierarchical Injection**: Base class dependencies are also injected
- **Type Safety**: Generic binding methods
- **Automatic Cleanup**: Dependencies can be removed when no longer needed

### Important Requirements

- Automatic injection requires a base class: inherit `InjectMonoBehaviour`
  (or `BehaviourBase`) for MonoBehaviours, `InjectClass` (or
  `GeneralClassBase`) for plain classes. For anything else you must call
  `DependencyContainer.Instance.InjectDependencies(target)` yourself — the
  `[Inject]` attribute itself has no such requirement.
- Classes derived from `InjectClass` and `InjectMonoBehaviour` receive injection automatically
- `[DefaultExecutionOrder]` is **not inherited** — every installer must declare
  `[DefaultExecutionOrder(-10000)]` (or any low negative value) itself

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
| `InjectClass`           | Base class for plain classes; injects in constructor        |
| `DependencyContainer`    | Central DI container (singleton)                               |
| `InjectAttribute`        | Marks fields/properties for injection                          |
| `InjectClass`            | Base for non-MonoBehaviour injectable classes (auto-injection) |
| `InjectMonoBehaviour`    | Base for Unity component injection (auto-injection)            |
| `InstallerBehaviourBase` | Configures dependencies at startup                             |
| `BindingType`            | Defines dependency lifestyle (Singleton/Transient)             |

---

## What Triggers Automatic Injection

There is no marker interface. Injection is triggered by the **base class**,
and by nothing else:

| Base class                            | When injection runs                        |
| ------------------------------------- | ------------------------------------------ |
| `InjectClass`                         | in the constructor                        |
| `InjectMonoBehaviour` (and `BehaviourBase`, which derives from it) | in `OnEnable()` |

Any other object is left untouched until you ask for it yourself with
`DependencyContainer.Instance.InjectDependencies(target)`.

```csharp
// Auto-injected - base class does the work
public class MyService : InjectClass { }            // injected in constructor
public class MyComponent : BehaviourBase { }        // injected in OnEnable

// Not auto-injected - nothing runs unless you call it
public class CustomClass
{
    [Inject] private ILogger _logger;              // stays null
}

var c = new CustomClass();
DependencyContainer.Instance.InjectDependencies(c); // now it is filled
```

This is verified behaviour, not a convention: the container reflects over the
type and its base types for `[Inject]` members, and never checks what the type
implements.

**Order matters.** `InjectDependencies` only fills fields for dependencies that
are already bound, and binding happens in the installer's `Awake()`. That is
why installers carry `[DefaultExecutionOrder(-10000)]` — it is not inherited,
so every installer declares it. If you call `InjectDependencies` manually,
prefer `Start()`, which always runs after every `Awake`.

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

#### RemoveDependency

```csharp
public void RemoveDependency(Type type)
```

Removes a binding from the container. Behaviour:

- If the type is **not registered**, an error is logged and the method simply
  returns (it does **not** throw).
- If the bound instance implements `IDisposable`, it is disposed - and at most
  once, even if both `RemoveDependency` and `Dispose` are called for it
  (the container keeps a set of already-disposed instances).

#### Dispose

```csharp
public void Dispose()
```

Disposes every registered singleton instance that implements `IDisposable`
(each at most once). Use it when tearing down the whole container (e.g. at
application quit).

#### GetInstance

```csharp
internal T GetInstance<T>()
```

Internal accessor used by the framework. Returns the singleton instance, or
logs an error and returns `default` if the type is not registered.

**Note**: injection caches its reflection results per type
(a `ConcurrentDictionary` of `InjectCache`), so repeated injection of the same
type is cheap after the first call.

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

- Works in any class; automatic injection requires a base class (see above)
- Works automatically in classes derived from `InjectClass` or `InjectMonoBehaviour`
- Fields can be private, protected, or public
- Properties must have a setter

### Examples

#### Field Injection (Auto-injected)

```csharp
public class PlayerController : InjectMonoBehaviour  // auto-injected on OnEnable
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
public class GameManager : InjectClass  // auto-injected in constructor
{
    [Inject] public IAnalyticsService Analytics { get; private set; }
    [Inject] public ISaveGameManager SaveGame { get; private set; }
}
```

#### Base Class Injection (Auto-injected)

```csharp
public abstract class BaseService : InjectClass  // auto-injected in constructor
{
    [Inject] protected ILogger Logger;
}

public class PlayerService : BaseService  // inherits injection
{
    [Inject] private IPlayerRepository _repository;

    public void DoSomething()
    {
        Logger.Log("Base class injection works!");
    }
}
```

#### Not Injected Automatically (but injectable by hand)

```csharp
// No base class, so nothing triggers automatic injection
public class RegularClass
{
    [Inject] private ILogger _logger; // stays null unless you inject it
}

var regular = new RegularClass();
Logger.Log("before: " + (_logger == null)); // _logger is null

// Manual injection works on ANY object - no base class, no marker interface
DependencyContainer.Instance.InjectDependencies(regular);
Logger.Log("after:  " + (_logger != null)); // _logger is filled
```

If you cannot add a base class, call `InjectDependencies` yourself (or make
the class derive from `InjectClass`, which does it for you).

---

## Base Classes

### InjectClass (Abstract)

**Namespace**: `CherryFramework.DependencyManager`

**Purpose**: Base class for non-MonoBehaviour classes that need dependency injection. Receives injection in its constructor.

**Inheritance**: `object`

```csharp
public abstract class InjectClass
{
    // Automatically receives injection when constructed
}
```

**Features**:

- Automatic injection on construction
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

**Purpose**: Base class for Unity MonoBehaviour components that need dependency injection. Receives injection when `OnEnable()` is called.

**Inheritance**: `MonoBehaviour`

```csharp
public abstract class InjectMonoBehaviour : MonoBehaviour
{
    protected virtual void OnEnable()
    {
        // Automatically injects dependencies
    }
}
```

**Features**:

- Automatic injection in `OnEnable()`
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
            Bind<MockAnalyticsService, IAnalyticsService>(BindingType.Singleton);
            Bind<MockSaveGameService, ISaveGameService>(BindingType.Singleton);
        }
        else
        {
            // Use real implementations
            Bind<AnalyticsService, IAnalyticsService>(BindingType.Singleton);
            Bind<SaveGameService, ISaveGameService>(BindingType.Singleton);
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
        Bind<ILogger, ConsoleLogger>(BindingType.Singleton);
        Bind<IEventDispatcher, EventDispatcher>(BindingType.Singleton);
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

**Impact**: Minimal for most games. The container caches the resolved members
per type, so only the first injection of a given type pays for reflection.
The rule of thumb: inject once into a field, then use the field.

```csharp
// GOOD - inject once (InjectClass injects in the constructor), then use it
public class BulletManager : InjectClass
{
    [Inject] private ILogger _logger; // injected once

    public void Report(string message) => _logger.Log(message);
}
```

Do **not** call `InjectDependencies` to "look up" a service. It injects the
object you hand it and returns *that same object* - it does not resolve
anything for you:

```csharp
// BAD - re-injects on every call, and 'service' is this BadService
public class BadService : InjectClass
{
    public void DoWork()
    {
        var service = DependencyContainer.Instance.InjectDependencies(this);
        // 'service' == this, not a dependency, and the work repeats every call
    }
}
```

A MonoBehaviour cannot be created with `new` - if you need many instances of a
component, either use the framework's object pool or make the helper a plain
class that the component receives:

```csharp
// Plain class - safe to construct as many times as you need
public class Bullet
{
    public void Initialize(ILogger logger) => _logger = logger; // passed in
    private ILogger _logger;
}
```

### 2. Singleton vs Transient

- **Singletons**: Created once, minimal overhead
- **Transient**: Created for each injection, more allocations

```csharp
// For frequently created objects, consider singleton with factory pattern
Bind<EnemyFactory>(BindingType.Singleton); // Created once

// Instead of
Bind<EnemyFactory>(BindingType.Transient); // Created for each injection
```

### 3. Memory Usage

- Each binding stores type information in dictionaries
- Singleton instances persist for container lifetime
- Transient instances are garbage-collected when no longer referenced

---

## Common Issues and Solutions

### Issue 1: Dependencies are Null

**Symptoms**: `NullReferenceException` when accessing injected fields/properties

**Causes**:

- Class has no base class, so nothing triggers injection
- For `InjectMonoBehaviour`, `OnEnable()` wasn't called (object disabled)
- Dependency not registered in container

**Solutions**:

```csharp
// SOLUTION 1: Derive from a base class
public class MyService : InjectClass // GOOD - auto-injected
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
            Bind<ConsoleLogger, ILogger>(BindingType.Singleton);
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
        Bind<ILogger, FileLogger>(BindingType.Singleton);
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

// Any MonoBehaviour that must be injected by hand has to call Inject()
// itself, and OnEnable has to be an 'override' - declaring it 'private'
// hides the base method and injection never runs.
//
// Prefer inheriting BehaviourBase over InjectMonoBehaviour: it releases
// bindings and unsubscriptions automatically in OnDestroy, so you do not
// have to hand-write the teardown below.
public class EventSubscriber : BehaviourBase
{
    [Inject] private StateService _stateService;

    private StateSubscription _sub;

    protected override void OnEnable()
    {
        base.OnEnable(); // injection happens here

        _sub = _stateService.AddStateSubscription(
            s => s.IsEventActive("GameEvent"),
            HandleEvent);
    }

    protected override void OnDestroy()
    {
        _stateService.RemoveSubscription(_sub); // CRITICAL
        base.OnDestroy();
    }

    private void HandleEvent() { }
}
```

### Issue 5: Multiple Bindings for Same Type

**Symptoms**: Only the first binding works, others produce error message

**Cause**: Container doesn't support multiple bindings for the same type

**Solution**:

```csharp
// PROBLEM: Can't have multiple bindings for same type
DependencyContainer.Instance.Bind<ILogger, FileLogger>(BindingType.Singleton);
DependencyContainer.Instance.Bind<ILogger, ConsoleLogger>(BindingType.Singleton); // Error!

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
        Bind<LoggerFactory, ILoggerFactory>(BindingType.Singleton);
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

    // Injection happens in OnEnable, so in Awake the field is still null.
    private void Awake()
    {
        // _logger is null here!
        // Need a dependency this early? Inject manually:
        Inject();
        _logger.Log("Ready in Awake");
    }

    // OnEnable must be an 'override'. If you declare it 'private' you are
    // hiding the base method, the base injection never runs, and _logger
    // stays null.
    protected override void OnEnable()
    {
        base.OnEnable();
        _logger.Log("Ready in OnEnable");
    }

    private void Start()
    {
        _logger.Log("Ready in Start");
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
DependencyContainer.Instance.Bind<FileLogger, ILogger>(BindingType.Singleton); // Works
DependencyContainer.Instance.Bind<ConsoleLogger, ILogger>(BindingType.Singleton); // Error!
```

### 3. No Named Bindings

The framework does not support named bindings or conditional bindings. You cannot have multiple bindings for the same type differentiated by name or condition.

### 4. No Open Generic Bindings

The container does not support binding open generic types. You must bind closed generic types explicitly.

```csharp
// NOT SUPPORTED
Bind(typeof(IRepository<>), typeof(FileRepository<>)); // Won't work

// REQUIRED
Bind<FileRepository<PlayerData>, IRepository<PlayerData>>(BindingType.Singleton);
Bind<FileRepository<ScoreData>, IRepository<ScoreData>>(BindingType.Singleton);
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
public class MyService { } // No automatic injection
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
        Bind<ILogger, FileLogger>(BindingType.Singleton);
        Bind<IAnalytics, AnalyticsService>(BindingType.Singleton);
        Bind<ISaveGame, SaveGameService>(BindingType.Singleton);
    }
}
```

### 4. Use Interface-Based Programming

Always depend on interfaces, not concrete types:

```csharp
// GOOD
Bind<FileRepository, IRepository>(BindingType.Singleton);
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
using System;
using CherryFramework.DependencyManager;
using UnityEngine;

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
[Serializable]
public class PlayerData
{
    public string Name;
    public int Level;
}

public class JsonRepository<T> : InjectClass, IRepository<T> where T : class
{
    public void Save(T data) => Debug.Log("save " + data);
    public T Load() => null;
}

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
        Bind<PlayerService, IPlayerService>(BindingType.Singleton);
        Bind<AnalyticsService, IAnalyticsService>(BindingType.Singleton);

        // Bind repository
        Bind<JsonRepository<PlayerData>, IRepository<PlayerData>>(BindingType.Singleton);

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

| `InjectClass`            | Non-MonoBehaviour base        | Auto-injection on construction      |
| `InjectMonoBehaviour`    | MonoBehaviour base            | Auto-injection on OnEnable          |
| `InjectAttribute`        | Marks injectable members      | Works in any class                    |
| `InstallerBehaviourBase` | Dependency configuration      | Must add `[DefaultExecutionOrder]`  |
| `DependencyContainer`    | Central DI container          | Singleton access                    |

### Critical Requirements Summary

1. **`[Inject]` needs a base class for automatic injection** - otherwise call `InjectDependencies` yourself
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
