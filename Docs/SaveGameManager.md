# CherryFramework SaveGameManager Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [SaveGameManager](#savegamemanager)
4. [IGameSaveData Interface](#igamesavedata-interface)
5. [PersistentObject](#persistentobject)
6. [SaveGameData Attribute](#savegamedata-attribute)
7. [Storage Integration](#storage-integration)
8. [Asynchronous Saving](#asynchronous-saving)
9. [Performance Considerations](#performance-considerations)
10. [Common Issues and Solutions](#common-issues-and-solutions)
11. [Best Practices](#best-practices)
12. [Examples](#examples)
13. [Summary](#summary)

---

## Overview

The CherryFramework SaveGameManager provides a comprehensive save game system that automatically persists data for game objects and components. It seamlessly integrates with the framework's dependency injection system and offers both automatic and manual save/load capabilities.

### Key Features

- **Automatic Persistence**: Save and load game object transforms and component data
- **Attribute-Based Marking**: Use `[SaveGameData]` to mark fields/properties for saving
- **Scene-Aware**: Identifies objects across different scenes via `SceneId:{buildIndex}.{guid}`
- **Spawnable Object Support**: Special handling for dynamically spawned objects with custom IDs and suffixes
- **Slot System**: Multiple save slots support
- **Callback System**: Pre/post save/load lifecycle hooks
- **PlayerPrefs Integration**: Built-in storage using Unity PlayerPrefs
- **Extensible Storage**: Implement custom storage with `IPlayerPrefs` interface
- **Automatic Cleanup on Destroy**: `PersistentObject` can optionally save its data and unregister automatically when destroyed

### Important Requirements

- Game objects must have a `PersistentObject` component to be saveable
- Components must implement `IGameSaveData` - it is a required constraint on`n  `SaveData<T>` / `LoadData<T>`. The four callbacks are optional: each one has an`n  empty default body, so you only override the ones you need
- Marked fields/properties must be serializable
- Data Models (`DataModelBase`) should use `ModelService` instead of SaveGameManager
- Scene objects require a guid — press **Fill Guid** in the `PersistentObject` inspector

---

## Core Concepts

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      SaveGameManager                        │
├─────────────────────────────────────────────────────────────┤
│ - Dictionary<IGameSaveData, PersistentObject> _components   │
│ - string SlotId                                             │
│ - IPlayerPrefs _playerPrefs                                 │
│                                                             │
│ + Register<T>(component)                                    │
│ + LoadData<T>(component)                                    │
│ + SaveData(component)                                       │
│ + SaveAllData()                                             │
│ + SetCurrentSlot(slotId)                                    │
└─────────────────────────────────────────────────────────────┘
                              │
            ┌─────────────────┼─────────────────┐
            ▼                 ▼                 ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │PersistentObject │IGameSaveData  │ │  IPlayerPrefs │
    │  (Component)  │ │  (Interface)  │ │  (Storage)    │
    └───────────────┘ └───────────────┘ └───────────────┘
            │                 │                 │
            ▼                 ▼                 │
    ┌───────────────┐ ┌───────────────┐         │
    │ - Transform   │ │ OnBeforeLoad()│         │
    │   (auto-saved)│ │ OnAfterLoad() │         │
    │ - GUID        │ │ OnBeforeSave()│         │
    │ - CustomId    │ │ OnAfterSave() │         │
    │ - Suffix      │ └───────────────┘         │
    └───────────────┘         │                 │
            │                 │                 │
            └─────────────────┼─────────────────┘
                              ▼
                    ┌─────────────────┐
                    │  [SaveGameData] │
                    │Fields/Properties│
                    └─────────────────┘
```

### Object Identification System

The SaveGameManager uses a sophisticated identification system to uniquely identify objects:

| Object Type           | ID Components                 | Example                                          | Use Case                         |
| --------------------- | ----------------------------- | ------------------------------------------------ | -------------------------------- |
| **Scene Objects**     | `SceneId:{buildIndex}.{guid}` | `SceneId:3.550e8400-e29b-41d4-a716-446655440000` | Static objects placed in scenes  |
| **Spawnable Objects** | `{customId}:{suffix}`         | `Enemy:42`                                       | Dynamically instantiated objects |

### Key Components

| Component               | Purpose                                                             |
| ----------------------- | ------------------------------------------------------------------- |
| `SaveGameManager`       | Central service for save game operations                            |
| `IGameSaveData`         | Interface for components that need save/load callbacks              |
| `PersistentObject`      | MonoBehaviour that marks game objects as persistent and manages IDs |
| `SaveGameDataAttribute` | Marks fields/properties for persistence                             |
| `IPlayerPrefs`          | Storage abstraction (default: PlayerPrefs)                          |

---

## SaveGameManager

**Namespace**: `CherryFramework.SaveGameManager`

**Purpose**: Central service that manages all save game operations, including registration, loading, saving, and slot management.

### Class Definition

```csharp
public class SaveGameManager
{
    // Properties
    public string SlotId { get; private set; }
    public IGameSaveData[] RegisteredComponents { get; }
    public PersistentObject[] RegisteredObjects { get; }

    // Constructor
    public SaveGameManager(IPlayerPrefs playerPrefs, bool debugMessages);

    // Registration
    public virtual bool Register<T>(T component, PersistentObject persistentObj = null) where T : IGameSaveData;
    public virtual bool UnRegister<T>(T component) where T : IGameSaveData;
    public virtual bool UnRegisterObject(PersistentObject persistentObj);

    // Data Operations (Synchronous)
    public virtual bool LoadData<T>(T component) where T : IGameSaveData;
    public virtual void SaveData<T>(T component) where T : IGameSaveData;
    public virtual void SaveDataForObject(PersistentObject persistentObj);
    public void SaveAllData();
    public virtual bool DeleteData<T>(T component) where T : IGameSaveData;

    // Slot Management
    public void SetCurrentSlot(string slotId);
}
```

### Constructor

```csharp
public SaveGameManager(IPlayerPrefs playerPrefs, bool debugMessages)
```

**Example**:

```csharp
// In installer
var saveGameManager = new SaveGameManager(new PlayerPrefsData(), debugMessages: true);
DependencyContainer.Instance.BindAsSingleton(saveGameManager);
```

### Registration

```csharp
public virtual bool Register<T>(T component, PersistentObject persistentObj = null) where T : IGameSaveData
```

**Example**:

```csharp
public class PlayerHealth : BehaviourBase, IGameSaveData
{
    [Inject] private readonly SaveGameManager _saveManager;

    [SaveGameData] private float _health;

    private void Start()
    {
        _saveManager.Register(this); // Auto-finds PersistentObject on same GameObject
    }
}
```

### Unregistration

#### UnRegister

```csharp
public virtual bool UnRegister<T>(T component) where T : IGameSaveData
```

Removes a single component from the registry. Logs an error and returns `false`
if the component was not registered.

#### UnRegisterObject

```csharp
public virtual bool UnRegisterObject(PersistentObject persistentObj)
```

Removes **all** components registered against a given `PersistentObject`.
Useful when an entire object is going away (e.g. it was despawned or the
scene was unloaded). `PersistentObject.OnDestroy` calls this automatically
(see [Automatic Cleanup on Destroy](#automatic-cleanup-on-destroy)).

```csharp
// Unregister every component that belongs to this object
_saveManager.UnRegisterObject(myPersistentObject);
```

### Data Operations

#### LoadData

```csharp
public virtual bool LoadData<T>(T component) where T : IGameSaveData
```

**Example**:

```csharp
public void LoadPlayer()
{
    if (_saveManager.LoadData(this))
    {
        Debug.Log($"Player loaded with health: {_health}");
    }
    else
    {
        Debug.Log("No saved data found, using defaults");
    }
}
```

#### SaveData

```csharp
public virtual void SaveData<T>(T component) where T : IGameSaveData
```

**Example**:

```csharp
public void SavePlayer()
{
    _saveManager.SaveData(this);
    Debug.Log("Player saved");
}
```

#### SaveAllData

```csharp
public void SaveAllData()
```

**Example**:

```csharp
private void OnApplicationQuit()
{
    _saveManager.SaveAllData();
    PlayerPrefs.Save();
}
```

#### SaveDataForObject

```csharp
public virtual void SaveDataForObject(PersistentObject persistentObj)
```

Saves the data of **all** components registered against a given
`PersistentObject`. Logs an error if the object has no registered components.
`PersistentObject` uses this in `OnDestroy` when `saveOnDestroy` is enabled.

```csharp
// Persist everything owned by this object (e.g. before despawning it)
_saveManager.SaveDataForObject(myPersistentObject);
```

#### DeleteData

```csharp
public virtual bool DeleteData<T>(T component) where T : IGameSaveData
```

**Example**:

```csharp
public void ResetSaveData()
{
    if (_saveManager.DeleteData(this))
    {
        Debug.Log("Save data found and deleted");
    }
}
```

### Slot Management

#### SetCurrentSlot

```csharp
public void SetCurrentSlot(string slotId)
```

**Example**:

```csharp
public void SwitchToSlot(string slotName)
{
    _saveManager.SetCurrentSlot(slotName);
    LoadAllData(); // Load from new slot
}
```

---

## IGameSaveData Interface

**Namespace**: `CherryFramework.SaveGameManager`

**Purpose**: Interface that components must implement to receive save/load lifecycle callbacks.

```csharp
public interface IGameSaveData
{
    void OnBeforeLoad() { }
    void OnAfterLoad() { }
    void OnBeforeSave() { }
    void OnAfterSave() { }
}
```

All methods have default empty implementations, so you only need to override the ones you need.

### Example Implementation

```csharp
public class PlayerInventory : MonoBehaviour, IGameSaveData
{
    [SaveGameData] private List<string> _items = new();
    [SaveGameData] private int _gold;

    private List<string> _backupItems;
    private int _backupGold;

    public void OnBeforeLoad()
    {
        // OnBeforeLoad runs BEFORE data is read. OnAfterLoad is only called
        // when the load SUCCEEDS, so this backup is for validating loaded
        // values - it cannot recover from a failed load.
        _backupItems = new List<string>(_items);
        _backupGold = _gold;
    }

    public void OnAfterLoad()
    {
        // Update UI with loaded data
        UpdateInventoryUI();
    }

    public void OnBeforeSave()
    {
        // Ensure data is valid before saving
        _gold = Mathf.Max(0, _gold);
    }

    public void OnAfterSave()
    {
        Debug.Log("Inventory saved");
    }

    private void UpdateInventoryUI() { }
}
```

---

## PersistentObject

**Namespace**: `CherryFramework.SaveGameManager`

**Purpose**: MonoBehaviour component that marks a GameObject as persistent and manages its unique identifier for save/load operations. This component is required for any object that needs to be saved.

**Important**: 

- This component must be attached to any GameObject that needs to be saved
- It automatically saves the object's transform (position, rotation, scale) when `saveTransform` is enabled
- Scene objects and spawnable objects use completely different identification systems

### Class Definition

```csharp
[DisallowMultipleComponent]
public class PersistentObject : BehaviourBase, IGameSaveData
{
    // Serialized Fields
    [SerializeField] private bool spawnableObject;
    [SerializeField] private string customId = "OBJ";
    [ReadOnly] public string guid;
    [SerializeField] private bool saveTransform;
    [SerializeField] private bool saveOnDestroy;
    [SerializeField] private bool forceReset;

    // Properties
    public bool ForceReset => forceReset;
    public int? CustomSuffix { get; private set; }

    // Methods
    public string GetObjectId();
    public void SetCustomSuffix(int suffix);
    public void OnBeforeLoad();
    public void OnAfterLoad();
    public void OnBeforeSave();
}
```

### Serialized Fields

| Field             | Type     | Description                                                              |
| ----------------- | -------- | ------------------------------------------------------------------------ |
| `spawnableObject` | `bool`   | Whether this is a dynamically spawned object (uses customId + suffix)    |
| `customId`        | `string` | Base identifier for spawnable objects (ignored for scene objects)        |
| `guid`            | `string` | Scene object identifier, filled by the **Fill Guid** button (read-only, ignored for spawnable) |
| `saveTransform`   | `bool`   | Whether to automatically save position, rotation, and scale              |
| `saveOnDestroy`   | `bool`   | If true, save all of this object's data automatically in `OnDestroy`    |
| `forceReset`      | `bool`   | If true, ignore saved data and use defaults when loading                 |

### Transform Saving

When `saveTransform` is enabled, the PersistentObject automatically saves:

- **Position** (`Vector3`)
- **Rotation** (`Quaternion`)  
- **Scale** (`Vector3`)

```csharp
// These fields are automatically saved when saveTransform is true
[SaveGameData] private Vector3 _position;
[SaveGameData] private Quaternion _rotation;
[SaveGameData] private Vector3 _scale;
```

When `saveTransform` is enabled, `PersistentObject` also registers itself and
loads its transform data automatically in `Start`, so no manual
`Register`/`LoadData` calls are needed for transform persistence.

### Automatic Cleanup on Destroy

In `OnDestroy`, `PersistentObject` checks whether it is still registered with
the `SaveGameManager`:

1. If `saveOnDestroy` is enabled, it saves all of the object's data via
   `SaveDataForObject(this)` - the last state is persisted before the object
   goes away.
2. It then always unregisters the object via `UnRegisterObject(this)`, so the
   registry never holds references to destroyed objects.

```csharp
// In the inspector, on a PersistentObject:
saveTransform = true;   // auto register + load in Start
saveOnDestroy = true;   // save everything on destroy, then unregister
```

This is the recommended setup for scene objects that should keep their state
across scene transitions.

### Object Identification System

#### For Scene Objects (spawnableObject = false)

Scene objects use a combination of scene build index and a globally unique identifier (GUID):

```
SceneId:{buildIndex}.{guid}
Example: SceneId:3.550e8400-e29b-41d4-a716-446655440000
```

**GUID Generation**:

- GUIDs are **not** automatic. Press the **Fill Guid** button in the
  `PersistentObject` inspector to generate one.
- The button appears only for non-spawnable objects placed in an open scene.
- **Never save without a guid.** Loading is skipped and an error is logged.
  Saving is worse: the empty segment is dropped from the storage key, so
  **every guid-less component of the same type overwrites the same slot**.
- The object id is `SceneId:{buildIndex}.{guid}`, so the scene must be in
  Build Settings. Until it is, `buildIndex` is `-1` — adding the scene later
  changes the id and orphans the already-saved data.
- A prefab asset that is not placed in a scene has no id at all. Use the
  spawnable mode (`spawnableObject`) for objects created at runtime.
- The guid field is read-only. Changing it detaches the object from its data.

**Example Scene Object**:

```csharp
// Place this on a door in your level and press "Fill Guid" in the Inspector
public class Door : PersistentObject
{
    [SaveGameData] private bool _isOpen;
}

// Object ID will be: "SceneId:3.550e8400-e29b-41d4-a716-446655440000"
//                       ^^^^^^^^        ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                       buildIndex      guid (generated by the button)
```

#### For Spawnable Objects (spawnableObject = true)

Spawnable objects use a custom base ID and a numeric suffix to distinguish between different copies of the same object type:

```
{customId}:{suffix}
Example: Enemy:42
```

| Component  | Purpose                                            | Example                     |
| ---------- | -------------------------------------------------- | --------------------------- |
| `customId` | Base identifier for the object type                | "Enemy", "Pickup", "Bullet" |
| `suffix`   | Separates different copies of the same object type | 0, 1, 2, 42, etc.           |

**Important**:

- `customId` is set in the inspector and should identify the object type
- `suffix` must be set at runtime via `SetCustomSuffix()` to make each copy unique
- Without a unique suffix, different copies of the same object will overwrite each other's save data

**Example Spawnable Object**:

```csharp
public class Enemy : PersistentObject
{
    [SaveGameData] private float _health;
    [SaveGameData] private Vector3 _position;

    public void Initialize(int enemyId)
    {
        // Set unique suffix for this instance
        SetCustomSuffix(enemyId);

        // Object ID will be something like: "Enemy:42"
        _health = 100f;
        _position = transform.position;
    }
}

// Spawning multiple enemies
public class EnemySpawner : MonoBehaviour
{
    private int _nextEnemyId = 0;

    public void SpawnEnemy(Vector3 position)
    {
        var enemyObj = Instantiate(enemyPrefab, position, Quaternion.identity);
        var enemy = enemyObj.GetComponent<Enemy>();
        enemy.Initialize(_nextEnemyId++); // Each enemy gets unique ID: Enemy:0, Enemy:1, etc.
    }
}
```

### Methods

#### GetObjectId

```csharp
public string GetObjectId()
```

Returns the unique identifier for this object.

**Example**:

```csharp
string id = persistentObject.GetObjectId();
Debug.Log($"Object ID: {id}");
```

#### SetCustomSuffix

```csharp
public void SetCustomSuffix(int suffix)
```

Sets the numeric suffix for spawnable objects.

**Example**:

```csharp
persistentObject.SetCustomSuffix(42);
```

---

## SaveGameData Attribute

**Namespace**: `CherryFramework.SaveGameManager`

**Purpose**: Marks fields and properties to be included in save/load operations.

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class SaveGameDataAttribute : Attribute
{
}
```

### Examples

```csharp
public class PlayerStats : MonoBehaviour, IGameSaveData
{
    [SaveGameData] private int _level;
    [SaveGameData] private float _experience;
    [SaveGameData] private List<string> _unlockedAbilities = new();
    [SaveGameData] private Vector3 _position; // Will be saved
}

public class GameSettings : MonoBehaviour, IGameSaveData
{
    [SaveGameData] public float Volume { get; set; }
    [SaveGameData] public bool Fullscreen { get; set; }
}
```

---

## Storage Integration

### PlayerPrefsData

**Namespace**: `CherryFramework.Utils.PlayerPrefsWrapper`

**Purpose**: Default storage implementation using Unity's PlayerPrefs.

```csharp
public class PlayerPrefsData : IPlayerPrefs
{
    public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    public string GetString(string key) => PlayerPrefs.GetString(key);
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
    public void DeleteAll() => PlayerPrefs.DeleteAll();
    public void Save() => PlayerPrefs.Save();
}
```

### Key Generation Pattern

```
{objectId}-{slotId}-{componentType}
```

**Examples**:

- `SceneId:3.550e8400-e29b-41d4-a716-446655440000-main-PlayerHealth`
- `Enemy:42-slot1-EnemyAI`
- `Pickup:7-default-PickupComponent`

### Custom Storage Implementation

You can implement your own storage by implementing `IPlayerPrefs`:

```csharp
public class FileSystemStorage : IPlayerPrefs
{
    private string _savePath = Application.persistentDataPath;

    public void SetString(string key, string value)
    {
        File.WriteAllText(Path.Combine(_savePath, key + ".json"), value);
    }

    public string GetString(string key)
    {
        var path = Path.Combine(_savePath, key + ".json");
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    public bool HasKey(string key)
    {
        return File.Exists(Path.Combine(_savePath, key + ".json"));
    }

    public void DeleteKey(string key)
    {
        File.Delete(Path.Combine(_savePath, key + ".json"));
    }

    public void DeleteAll()
    {
        foreach (var file in Directory.GetFiles(_savePath, "*.json"))
            File.Delete(file);
    }

    public void Save() { } // File.WriteAllText already saves
}
```

### Setting Up with Installer

```csharp
[DefaultExecutionOrder(-10000)]
public class SaveSystemInstaller : InstallerBehaviourBase
{
    [SerializeField] private bool _debugMessages = true;

    protected override void Install()
    {
        // Use default PlayerPrefs storage
        var saveManager = new SaveGameManager(new PlayerPrefsData(), _debugMessages);
        BindAsSingleton(saveManager);
    }
}
```

---

## Asynchronous Saving

`SaveGameManager` is **synchronous only**. There is no `SaveDataAsync`, no
task-based API, and no `SaveToStorageAsync`. Saving reads your fields by
reflection and writes through `PlayerPrefs.SetString` / `Save`, which Unity
requires to happen on the main thread.

### Do not wrap saving in Task.Run

```csharp
// WRONG - do not do this
await Task.Run(() => _saveManager.SaveAllData());
```

`SaveAllData` iterates your components and, for each one, calls
`Debug.Log`, reflects over MonoBehaviour fields, mutates dictionaries and
touches `PlayerPrefs`. Off the main thread that produces an
`InvalidOperationException` from the collection or a hard crash from
PlayerPrefs - and the error usually surfaces far from the call site.

### The safe alternative: spread the work over frames

A coroutine that saves a few components per frame keeps the cost bounded
and stays on the main thread:

```csharp
public class BatchedSaver : BehaviourBase
{
    [Inject] private SaveGameManager _saveManager;

    // Keep your own list of components to save
    private readonly List<IGameSaveData> _toSave = new();

    public System.Collections.IEnumerator SaveEverything(float budgetMs = 2f)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var component in _toSave)
        {
            _saveManager.SaveData(component);

            if (stopwatch.ElapsedMilliseconds < budgetMs)
                continue;

            // frame budget spent - hand control back to Unity
            stopwatch.Restart();
            yield return null;
        }
    }
}
```

If one save is simply too slow, the usual fix is to save less: persist
only what changed, or move large payloads to `ModelService`.

### Custom storage

If your data does not fit in `PlayerPrefs`, implement `IPlayerPrefs` and
pass it to the `SaveGameManager` constructor. That is the supported
extension point - do the write on the main thread inside your
implementation.

---


---

## Performance Considerations

### 1. Registration Overhead

```csharp
// GOOD - Register once
private void Start()
{
    _saveManager.Register(this); // One-time lookup
}

// BAD - Avoid registering multiple times
private void Update()
{
    _saveManager.Register(this); // Don't do this!
}
```

### 2. Save/Load Frequency

```csharp
// GOOD - Save at meaningful intervals
public void OnLevelComplete()
{
    _saveManager.SaveAllData();
}

// BAD - Save too frequently
private void Update()
{
    _saveManager.SaveAllData(); // DON'T save every frame!
}
```

`PlayerPrefs.Save()` is the expensive part - it flushes the whole preferences
file to disk. Calling it every frame will wear the disk and stall the game.

### 3. Transform Saving Overhead

```csharp
// Transform saving is a per-component Inspector decision. Tick
// "Save Transform" on the PersistentObject of a moving object and leave it
// unticked for static scenery - never re-declare the field in a derived
// class, it would shadow the base one and never be read.
```

### 4. JSON Serialization Overhead

```csharp
// GOOD - Simple serializable types
[SaveGameData] private int _score;
[SaveGameData] private string _name;
[SaveGameData] private List<int> _ids;

// BAD - Complex nested structures
[SaveGameData] private Dictionary<CustomClass, List<OtherClass>> _complex; // Slow
```

---

## Common Issues and Solutions

### Issue 1: GUID Not Generated

**Symptoms**: Scene object's GUID field empty, object won't save

**Causes**: Scene not in Build Settings, or object is a prefab in the Project tab

**Solution**:

```csharp
// Place object on a scene
// Add scene to Build Settings
// File → Build Settings → Add Open Scenes

// Check in code
#if UNITY_EDITOR
private void OnValidate()
{
    if (!spawnableObject && gameObject.scene.IsValid() && 
        gameObject.scene.buildIndex < 0)
    {
        Debug.LogWarning($"Scene {gameObject.scene.name} not in Build Settings! GUID won't generate.");
    }
}
#endif
```

### Issue 2: Spawnable Objects Overwriting Each Other

**Symptoms**: Multiple spawned objects have same saved data

**Cause**: Missing or duplicate suffixes

**Solution**:

```csharp
public class Enemy : PersistentObject
{
    private static int _globalCounter = 0;
    private static readonly object _lock = new object();

    public void Initialize()
    {
        lock (_lock)
        {
            SetCustomSuffix(_globalCounter++); // Thread-safe unique ID
        }
    }
}

// Better: Use spawner-specific counter
public class EnemySpawner : MonoBehaviour
{
    private int _localCounter;

    public Enemy SpawnEnemy()
    {
        var spawnedEnemies = _modelService.GetOrCreateSingletonModel<SpawnDataModel>();
        var enemy = Instantiate(enemyPrefab).GetComponent<Enemy>();
        enemy.SetCustomSuffix(spawnedEnemies.EnemyCounter++); // Unique per spawner
        return enemy;
    }
}
```

### Issue 3: Transform Not Saving

**Symptoms**: Position/rotation/scale not persisting

**Cause**: `saveTransform` not enabled

**Solution**: tick `saveTransform` **on the `PersistentObject` component** in
the Inspector. `PersistentObject.Start()` reads its own field, and that field
is `private` - so re-declaring `saveTransform` in your derived class creates a
*second* field that shadows it and is never read. Setting it from code has the
same problem:

```csharp
// WRONG - this declares a NEW field, not the base class one
[SerializeField] private bool saveTransform = true;

// WRONG - PrivatePersistentObject.saveTransform is not accessible from a
// derived class (CS0122), and there is no setter.
```

There is no public setter, by design: transform saving is meant to be an
Inspector decision. If you need it from code, toggle the serialized field on
the component via `SerializedObject`, or save the transform yourself:

```csharp
// Do it yourself - works regardless of the flag
[SaveGameData] private Vector3 _customPosition;
[SaveGameData] private Quaternion _customRotation;

public void OnBeforeSave()
{
    _customPosition = transform.position;
    _customRotation = transform.rotation;
}

public void OnAfterLoad()
{
    transform.position = _customPosition;
    transform.rotation = _customRotation;
}
```

### Issue 4: Data Model Usage

**Symptoms**: Using SaveGameManager with DataModelBase

**Cause**: Data models should use ModelService

**Solution**:

```csharp
// WRONG
public class PlayerModel : DataModelBase, IGameSaveData { }

// RIGHT - Use ModelService
var bridge = new PlayerPrefsBridge(new PlayerPrefsData());
var modelService = new ModelService(bridge, true);
var playerModel = modelService.GetOrCreateSingletonModel<PlayerModel>();
bridge.RegisterModelInStorage(playerModel);
```

### Issue 5: Scene Build Index Changes

**Symptoms**: Save data lost after reordering scenes

**Cause**: Scene ID uses build index

**Solution**:

```csharp
public class VersionedSave : IGameSaveData
{
    [SaveGameData] private int _savedBuildIndex;
    [SaveGameData] private string _sceneName;
    [SaveGameData] private int _dataVersion = 1;

    public void OnBeforeSave()
    {
        _savedBuildIndex = gameObject.scene.buildIndex;
        _sceneName = gameObject.scene.name;
    }

    public void OnAfterLoad()
    {
        var currentIndex = gameObject.scene.buildIndex;
        var currentName = gameObject.scene.name;

        if (_savedBuildIndex != currentIndex && _sceneName == currentName)
        {
            Debug.Log($"Scene build index changed from {_savedBuildIndex} to {currentIndex}, migrating data...");
            // Perform data migration if needed
        }
    }
}
```

### Issue 6: Large Save Files Blocking Main Thread

**Symptoms**: Frame rate drops during save/load

**Cause**: Synchronous I/O on large files

**Solution**: do the save on the main thread, and hide the frame hitch with a
coroutine if it is visible. Do **not** move `SaveAllData` to a background thread
- it touches `PlayerPrefs`, calls `Debug.Log` and mutates dictionaries, none of
which are thread-safe.

```csharp
public class SaveTrigger : BehaviourBase   // [Inject] needs BehaviourBase
{
    [Inject] private SaveGameManager _saveManager;

    [SerializeField] private GameObject _savingIcon;

    public System.Collections.IEnumerator SaveGame()
    {
        _savingIcon.SetActive(true);
        yield return null; // let the icon render before we block

        _saveManager.SaveAllData(); // main thread, as it must be

        _savingIcon.SetActive(false);
    }
}
```

See [Asynchronous Saving](#asynchronous-saving) for the batched alternative that
also keeps the frame budget down.

---

## Best Practices

### 1. Always Check Build Settings for Scene Objects

```csharp
private void Start()
{
    if (!spawnableObject && gameObject.scene.buildIndex < 0)
    {
        Debug.LogWarning($"Scene {gameObject.scene.name} not in Build Settings! " +
                        "Add it to Build Settings for GUID generation.", gameObject);
    }
}
```

### 2. Always Set Unique Suffixes for Spawnable Objects

```csharp
public abstract class SpawnablePersistent : PersistentObject
{
    private static int _globalCounter;

    protected void AssignUniqueId()
    {
        SetCustomSuffix(_globalCounter++);
    }
}

public class Enemy : SpawnablePersistent
{
    public void Initialize()
    {
        AssignUniqueId(); // Each enemy gets unique ID
    }
}
```

### 3. Enable Transform Saving Only When Needed

```csharp
// Static scenery: leave "Save Transform" unticked in the Inspector
public class StaticScenery : PersistentObject
{
    [SaveGameData] private int _variant;
}

// Moving platform: tick "Save Transform" in the Inspector.
// Do NOT declare saveTransform here - it would shadow the base field.
public class MovingPlatform : PersistentObject
{
    [SaveGameData] private float _offset;
}

// A door that never moves: only its state needs saving
public class Door : PersistentObject
{
    [SaveGameData] private bool _isOpen;
}
```

### 4. Register and Load in Start/Awake

**Only for components that are NOT `PersistentObject` subclasses.** If your
class derives from `PersistentObject` and "Save Transform" is ticked, the base
`Start()` already calls `Register` + `LoadData` - calling them again just
produces "already registered".

```csharp
// A helper component that only stores data, with its own PersistentObject
// sitting next to it on the same GameObject
public class Inventory : BehaviourBase, IGameSaveData
{
    [Inject] private SaveGameManager _saveManager;

    [SaveGameData] private int _slots;

    private void Start()
    {
        if (_saveManager.Register(this))
            _saveManager.LoadData(this);
    }
}
```

For a `PersistentObject` subclass, register manually **only** when
"Save Transform" is off - as shown in the `SaveableEnemy` example above.

### 5. Save at Meaningful Points

```csharp
public class Checkpoint : BehaviourBase   // not MonoBehaviour: [Inject] needs the base class
{
    [Inject] private SaveGameManager _saveManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _saveManager.SaveAllData();
            ShowSaveNotification();
        }
    }
}

public class LevelComplete : BehaviourBase
{
    [Inject] private SaveGameManager _saveManager;

    public void CompleteLevel()
    {
        _saveManager.SaveAllData();
        LoadNextLevel();
    }
}
```

### 6. Handle Save Slots Properly

```csharp
public class SaveSlotManager : BehaviourBase
{
    [Inject] private SaveGameManager _saveManager;

    public void SaveToSlot(int slotIndex)
    {
        _saveManager.SetCurrentSlot($"slot_{slotIndex}");
        _saveManager.SaveAllData();
        PlayerPrefs.SetInt("LastSlot", slotIndex);
        PlayerPrefs.Save();
    }

    public void LoadFromSlot(int slotIndex)
    {
        _saveManager.SetCurrentSlot($"slot_{slotIndex}");
        _saveManager.SaveAllData(); // Actually loads data
        UpdateGameState();
    }

    public void DeleteSlot(int slotIndex)
    {
        _saveManager.SetCurrentSlot($"slot_{slotIndex}");
        // Delete all data for this slot
        foreach (var component in _saveManager.RegisteredComponents)
        {
            _saveManager.DeleteData(component);
        }
    }
}
```

### 7. Validate Loaded Data

```csharp
public void OnAfterLoad()
{
    // Clamp values to valid ranges
    _health = Mathf.Clamp(_health, 0, _maxHealth);
    _level = Mathf.Max(1, _level);

    // Remove invalid items
    _inventory.RemoveAll(item => item == null);

    // Ensure collections aren't too large
    if (_inventory.Count > 50)
    {
        _inventory = _inventory.Take(50).ToList();
    }
}
```

### 8. Force Reset for Development

`forceReset` is a `private` field on `PersistentObject` with only a read-only
`ForceReset` property, and the `OnBeforeLoad` / `OnAfterLoad` methods are
**not** `virtual`. So neither of the following compiles:

```csharp
// WRONG - CS0122: forceReset is private and has no setter
private void Awake() => forceReset = true;

// WRONG - CS0115: PersistentObject.OnAfterLoad() is not virtual
public override void OnAfterLoad() { }
```

Do the reset from `OnBeforeLoad` instead - it is a plain public method you can
implement directly:

```csharp
public class DevReset : PersistentObject
{
    public void OnBeforeLoad()
    {
        // Runs before data is read, so any default you set here is overwritten
        // by the loaded values. To hard-reset, delete the stored key first.
        PlayerPrefs.DeleteKey(GetObjectId());
    }
}
```

The `forceReset` checkbox in the Inspector is the supported way to use it - it
is read by `SaveGameManager` via the read-only `ForceReset` property.

### 9. Large Save Budget

```csharp
// Saving is synchronous by design. Spread the components over frames with a
// coroutine instead of moving the whole save to a background thread.
public class LargeGameSaveManager : BehaviourBase
{
    [Inject] private SaveGameManager _saveManager;

    [SerializeField] private GameObject _savingIcon;
    private readonly List<IGameSaveData> _toSave = new();

    public System.Collections.IEnumerator SaveGame(float budgetMs = 4f)
    {
        ShowSavingIcon();
        yield return null;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var component in _toSave)
        {
            _saveManager.SaveData(component);

            if (stopwatch.ElapsedMilliseconds < budgetMs)
                continue;

            stopwatch.Restart();
            yield return null;
        }

        HideSavingIcon();
        ShowSaveCompleteMessage();
    }
}
```

### 10. Validate Loaded Data in OnAfterLoad

```csharp
public class SafeLoad : PersistentObject
{
    [SaveGameData] private PlayerData _data;
    private PlayerData _backup;

    public void OnBeforeLoad()
    {
        // Runs before the data is read, so copy whatever is there now
        _backup = _data.Clone();
    }

    public void OnAfterLoad()
    {
        // Only called when the load succeeded - this is where validation
        // belongs
        if (ValidateData(_data))
            return;

        Debug.LogError("Loaded data invalid, restoring backup");
        _data = _backup;
    }

    private bool ValidateData(PlayerData data)
    {
        return data != null && data.health > 0;
    }
}
```

Note the base class: `SafeLoad` derives from `PersistentObject`, not from a
plain `IGameSaveData` implementation. `Register` only accepts a
`MonoBehaviour` that sits on a GameObject carrying `PersistentObject` (or a
`PersistentObject` passed directly) - anything else is rejected with
"not MonoBehaviour and PersistentObject is NULL".

---

## Examples

### Complete Player Save System

```csharp
// 1. Player component with save data
public class Player : BehaviourBase, IGameSaveData
{
    [Inject] private SaveGameManager _saveManager;

    [Header("Save Data")]
    [SaveGameData] private string _playerName = "Hero";
    [SaveGameData] private int _level = 1;
    [SaveGameData] private int _experience;
    [SaveGameData] private float _health = 100;
    [SaveGameData] private float _maxHealth = 100;
    [SaveGameData] private Vector3 _position;
    [SaveGameData] private Quaternion _rotation;
    [SaveGameData] private List<string> _inventory = new();
    [SaveGameData] private Dictionary<string, int> _questProgress = new();

    private CharacterController _controller;
    private bool _isLoaded;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (_saveManager.Register(this))
        {
            _saveManager.LoadData(this);
        }
    }

    public void OnBeforeSave()
    {
        // Update position before saving
        _position = transform.position;
        _rotation = transform.rotation;
    }

    public void OnAfterLoad()
    {
        // Apply position safely
        if (_controller != null)
        {
            _controller.enabled = false;
            transform.position = _position;
            transform.rotation = _rotation;
            _controller.enabled = true;
        }

        _isLoaded = true;
        UpdateUI();
        Debug.Log($"Player loaded: Level {_level}, Health {_health}, Items: {_inventory.Count}");
    }

    public void AddExperience(int amount)
    {
        _experience += amount;
        CheckLevelUp();
        _saveManager.SaveData(this);
    }

    private void CheckLevelUp()
    {
        while (_experience >= GetExpForNextLevel())
        {
            _experience -= GetExpForNextLevel();
            _level++;
            _maxHealth += 20;
            _health = _maxHealth;
        }
    }

    private int GetExpForNextLevel() => _level * 100;

    public void AddItem(string itemId)
    {
        _inventory.Add(itemId);
        _saveManager.SaveData(this);
    }

    public void RemoveItem(string itemId)
    {
        _inventory.Remove(itemId);
        _saveManager.SaveData(this);
    }

    public void UpdateQuest(string questId, int progress)
    {
        _questProgress[questId] = progress;
        _saveManager.SaveData(this);
    }

    private void UpdateUI()
    {
        // Update UI elements
    }
}

// 2. Scene object with transform saving.
// Tick "Save Transform" on the PersistentObject in the Inspector - that is the
// only supported way to enable it, the field is private.
//
// PersistentObject.Start() already registers the object and loads its data when
// that flag is on, so do NOT register and load it again here: you would just
// get an "already registered" error.
public class MovingPlatform : PersistentObject
{
    // Injected automatically: PersistentObject inherits BehaviourBase, so
    // OnEnable() injects every [Inject] field in the hierarchy. The service
    // you get is the same one the base class uses.
    [Inject] private SaveGameManager _saveManager;

    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _moveDistance = 5f;
    [SaveGameData] private float _offset;
    [SaveGameData] private bool _isActive = true;

    private Vector3 _startPos;

    private void Start()
    {
        // Data was already loaded by PersistentObject.Start(), so _offset and
        // _isActive already hold the saved values here.
        _startPos = transform.position;
    }

    private void Update()
    {
        if (!_isActive) return;

        float movement = Mathf.Sin(Time.time + _offset) * _moveDistance;
        transform.position = _startPos + Vector3.right * movement;
    }

    public void Activate(bool active)
    {
        _isActive = active;
        _saveManager.SaveData(this);
    }
}

// 3. Spawnable enemy with unique ID.
// "Spawnable Object" and "Custom Id" are private [SerializeField] properties,
// so they must be ticked/typed in the Inspector (they cannot be set from
// script). Only the per-instance suffix can be assigned at runtime.
public class SaveableEnemy : PersistentObject
{
    private static int _nextId;

    [Inject] private SaveGameManager _saveManager;

    [SaveGameData] private float _health;
    [SaveGameData] private Vector3 _position;
    [SaveGameData] private string _enemyType;
    [SaveGameData] private bool _isAlive = true;

    public void Initialize(string type, Vector3 spawnPos)
    {
        // Silent no-op unless "Spawnable Object" is ticked in the Inspector
        SetCustomSuffix(_nextId++);

        _enemyType = type;
        _health = GetMaxHealthForType(type);
        _position = spawnPos;
        transform.position = spawnPos;
        _isAlive = true;

        // Register by hand: PersistentObject.Start() only auto-registers when
        // saveTransform is on, which is off for spawnable objects.
        _saveManager.Register(this);
        _saveManager.LoadData(this);

        if (!_isAlive)
        {
            gameObject.SetActive(false);
        }
    }

    private float GetMaxHealthForType(string type) => type switch
    {
        "Goblin" => 50f,
        "Orc" => 100f,
        "Boss" => 500f,
        _ => 75f
    };

    public void TakeDamage(float damage)
    {
        if (!_isAlive) return;

        _health -= damage;
        if (_health <= 0)
        {
            Die();
            return;
        }

        _saveManager.SaveData(this);
    }

    private void Die()
    {
        _isAlive = false;
        gameObject.SetActive(false);
        _saveManager.SaveData(this);
    }
}

// 4. Save slot manager
// Coroutines, not Task.Run: PlayerPrefs, Debug.Log and the reflection inside
// SaveData must stay on the main thread, and Task.Delay continuations would
// resume off it - touching TMP_Text there throws.
public class SaveSlotUI : BehaviourBase
{
    [Inject] private SaveGameManager _saveManager;

    [SerializeField] private GameObject _savingIcon;
    [SerializeField] private TMP_Text _statusText;

    public System.Collections.IEnumerator SaveToSlot(int slotIndex)
    {
        _savingIcon.SetActive(true);
        _statusText.text = "Saving...";
        yield return null; // let the UI render before we block

        _saveManager.SetCurrentSlot($"slot_{slotIndex}");
        _saveManager.SaveAllData();

        PlayerPrefs.SetInt("LastSlot", slotIndex);
        PlayerPrefs.Save();

        _savingIcon.SetActive(false);
        yield return ShowMessage("Game Saved!", 2f);
    }

    public System.Collections.IEnumerator LoadFromSlot(int slotIndex)
    {
        _savingIcon.SetActive(true);
        _statusText.text = "Loading...";
        yield return null;

        _saveManager.SetCurrentSlot($"slot_{slotIndex}");

        // Loading is per component - LoadData, not SaveAllData
        foreach (var component in GetComponentsInChildren<MonoBehaviour>())
            _saveManager.LoadData(component as IGameSaveData);

        _savingIcon.SetActive(false);
        yield return ShowMessage("Game Loaded!", 2f);
    }

    private System.Collections.IEnumerator ShowMessage(string text, float seconds)
    {
        _statusText.text = text;
        yield return new WaitForSeconds(seconds);
        _statusText.text = "";
    }

    public void NewGame()
    {
        _saveManager.SetCurrentSlot("slot_0");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }
}

// 5. Auto-save manager with coroutines
public class AutoSaveManager : BehaviourBase
{
    [Inject] private SaveGameManager _saveManager;

    [SerializeField] private float _autoSaveInterval = 300f; // 5 minutes
    [SerializeField] private bool _saveOnPause = true;
    [SerializeField] private bool _saveOnQuit = true;

    private float _saveTimer;

    private void Update()
    {
        _saveTimer += Time.unscaledDeltaTime;

        if (_saveTimer >= _autoSaveInterval)
        {
            _saveTimer = 0f;
            StartCoroutine(AutoSaveCoroutine());
        }
    }

    private IEnumerator AutoSaveCoroutine()
    {
        Debug.Log("Auto-saving...");

        int total = _saveManager.RegisteredComponents.Length;
        int saved = 0;

        foreach (var component in _saveManager.RegisteredComponents)
        {
            _saveManager.SaveData(component);
            saved++;

            // Yield every 10 saves to keep game responsive
            if (saved % 10 == 0)
            {
                yield return null;
            }
        }

        Debug.Log($"Auto-save complete: {saved} components saved");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && _saveOnPause)
        {
            _saveManager.SaveAllData();
        }
    }

    private void OnApplicationQuit()
    {
        if (_saveOnQuit)
        {
            _saveManager.SaveAllData();
        }
    }
}
```

---

## Summary

### Architecture Diagram Recap

```
┌─────────────────────────────────────────────────────────────┐
│                      SaveGameManager                        │
├─────────────────────────────────────────────────────────────┤
│  Registration Map: IGameSaveData → PersistentObject         │
│  Current Slot: "slot_0"                                     │
│  Storage: IPlayerPrefs (PlayerPrefsData by default)         │
└─────────────────────────────────────────────────────────────┘
         │                          │
         │ 1. Register()            │ 2. GetObjectId()
         ▼                          ▼
┌─────────────────┐         ┌─────────────────┐
│  IGameSaveData  │────────▶│ PersistentObject│
│  Component      │         │  - spawnable?   │
└─────────────────┘         │  - customId     │
         │                  │  - guid         │
         │ 3.[SaveGameData] │  - suffix       │
         ▼                  └─────────────────┘
┌─────────────────┐                 │
│ Fields/Props    │                 │ 4. Generate ID
│ marked for save │                 ▼
└─────────────────┘        ┌─────────────────┐
                           │   Object ID     │
                           │ SceneId:3.guid  │
                           │   or            │
                           │ Enemy:42        │
                           └─────────────────┘
                                    │
                                    │ 5. Create key
                                    ▼
                           ┌─────────────────┐
                           │  Storage Key    │
                           │ {id}-{slot}-{type}│
                           └─────────────────┘
                                    │
                                    │ 6. Save/Load
                                    ▼
                           ┌─────────────────┐
                           │   IPlayerPrefs  │
                           │   (Storage)     │
                           └─────────────────┘
```

### Method Summary

| Category         | Method                              | Description                                          |
| ---------------- | ----------------------------------- | ---------------------------------------------------- |
| **Registration** | `Register(component)`               | Register a component for saving                      |
|                  | `UnRegister(component)`             | Unregister a single component                        |
|                  | `UnRegisterObject(persistentObj)`   | Unregister all components of an object               |
| **Loading**      | `LoadData(component)`               | Load data for a component                            |
| **Saving**       | `SaveData(component)`               | Save a single component                              |
|                  | `SaveDataForObject(persistentObj)`  | Save all components of an object                     |
|                  | `SaveAllData()`                     | Save all components (synchronous)                    |
| **Async Saving** | *none - it is synchronous*         | Spread large saves over frames with a coroutine        |
| **Deletion**     | `DeleteData(component)`             | Delete saved data for a component                    |
| **Slot**         | `SetCurrentSlot(slotId)`            | Change current save slot                             |

### Key Points

| #   | Key Point                                                                          | Why It Matters                                            |
| --- | ---------------------------------------------------------------------------------- | --------------------------------------------------------- |
| 1   | **PersistentObject automatically saves transform** when `saveTransform` is enabled | No need to manually save position/rotation/scale          |
| 2   | **Scene objects need a guid** — press **Fill Guid** in the Inspector     | Without it the object is not saved (and slot keys collide)   |
| 3   | **Spawnable objects use customId + suffix**                                        | `customId` identifies the type, `suffix` separates copies |
| 4   | **The scene must be in Build Settings**                                           | Otherwise `buildIndex` is `-1` and ids change later        |
| 5   | **Each spawnable copy needs a unique suffix**                                      | Prevents data conflicts between identical objects         |
| 6   | **Components must implement IGameSaveData**                                        | Required constraint on SaveData/LoadData; callbacks optional |
| 7   | **Mark fields with [SaveGameData]**                                                | Only marked fields are persisted                          |
| 8   | **Data models should use ModelService**                                            | SaveGameManager is for MonoBehaviour components           |
| 9   | **Default storage is synchronous**                                                 | Can cause frame drops with large save files               |
| 10  | **Do not move saving to a background thread**                                        | PlayerPrefs, Debug.Log and reflection are not thread-safe |

### When to Use Synchronous vs Asynchronous Saving

| Game Size  | Save File Size | Recommended Approach               |
| ---------- | -------------- | ---------------------------------- |
| Small      | < 100KB        | Synchronous (default)              |
| Medium     | 100KB - 1MB    | Depends on save frequency          |
| Large      | > 1MB          | Synchronous, batched over frames    |
| Very Large | > 10MB         | Batched over frames, or move to ModelService |

### When to Use SaveGameManager

| Use SaveGameManager      | Use ModelService              |
| ------------------------ | ----------------------------- |
| MonoBehaviour components | DataModelBase-derived classes |
| Transform positions      | Game settings                 |
| Enemy health/state       | Player statistics             |
| Inventory items          | Configuration data            |
| Scene object states      | Global game state             |
| Spawnable objects        | Singleton data                |

The SaveGameManager provides a robust, attribute-based save system that seamlessly handles both static scene objects and dynamically spawned objects, with automatic transform saving and unique identification through GUIDs and custom IDs. For larger games, developers can easily extend it with asynchronous saving mechanisms to maintain smooth performance.
