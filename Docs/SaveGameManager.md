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
- **Scene-Aware**: Automatically handles objects across different scenes with GUIDs
- **Spawnable Object Support**: Special handling for dynamically spawned objects with custom IDs and suffixes
- **Slot System**: Multiple save slots support
- **Callback System**: Pre/post save/load lifecycle hooks
- **PlayerPrefs Integration**: Built-in storage using Unity PlayerPrefs
- **Extensible Storage**: Implement custom storage with `IPlayerPrefs` interface
- **Force Reset Option**: Ability to reset saved data on load

### Important Requirements

- Objects must have a `PersistentObject` component to be saveable
- Components must implement `IGameSaveData` to receive save/load callbacks
- Marked fields/properties must be serializable
- Data Models (`DataModelBase`) should use `ModelService` instead of SaveGameManager
- Scene objects require GUIDs (auto-generated for scenes in build settings)

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

| Component               | Purpose                                                        |
| ----------------------- | -------------------------------------------------------------- |
| `SaveGameManager`       | Central service for save game operations                       |
| `IGameSaveData`         | Interface for components that need save/load callbacks         |
| `PersistentObject`      | MonoBehaviour that marks objects as persistent and manages IDs |
| `SaveGameDataAttribute` | Marks fields/properties for persistence                        |
| `IPlayerPrefs`          | Storage abstraction (default: PlayerPrefs)                     |

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

    // Data Operations (Synchronous)
    public virtual bool LoadData<T>(T component) where T : IGameSaveData;
    public virtual void SaveData(IGameSaveData component);
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
public class PlayerHealth : MonoBehaviour, IGameSaveData
{
    [SaveGameData] private float _health;

    private void Start()
    {
        var saveManager = DependencyContainer.Instance.GetInstance<SaveGameManager>();
        saveManager.Register(this); // Auto-finds PersistentObject on same GameObject
    }
}
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
public virtual void SaveData(IGameSaveData component)
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
        Debug.Log("Save data deleted");
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
        // Backup current state in case load fails
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
| `guid`            | `string` | Auto-generated GUID for scene objects (read-only, ignored for spawnable) |
| `saveTransform`   | `bool`   | Whether to automatically save position, rotation, and scale              |
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

### Object Identification System

#### For Scene Objects (spawnableObject = false)

Scene objects use a combination of scene build index and a globally unique identifier (GUID):

```
SceneId:{buildIndex}.{guid}
Example: SceneId:3.550e8400-e29b-41d4-a716-446655440000
```

**GUID Generation**:

- GUIDs are **automatically generated** in the Unity Editor
- This **only works for scenes that are included in Build Settings**
- If a scene is not in Build Settings, the GUID field will remain empty
- GUIDs are read-only and should not be modified manually

**Example Scene Object**:

```csharp
// Place this on a door in your level (scene must be in Build Settings)
public class Door : PersistentObject
{
    [SaveGameData] private bool _isOpen;

    private void Start()
    {
        // guid is auto-generated in Editor because scene is in Build Settings
        // Object ID will be: "SceneId:3.550e8400-e29b-41d4-a716-446655440000"
    }
}
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

The SaveGameManager currently provides synchronous save/load operations. However, developers can implement asynchronous saving mechanisms themselves for larger games where save operations might cause frame rate hits.

### Why Asynchronous Saving?

| Scenario                   | Synchronous            | Asynchronous           |
| -------------------------- | ---------------------- | ---------------------- |
| Small save files (< 100KB) | Fine                   | Overkill               |
| Large save files (> 1MB)   | May cause frame drops  | Recommended            |
| Frequent auto-saving       | Can impact performance | Better user experience |
| Network storage            | Must be async          | Required               |

### Implementing Async Save

Here's an example of how to extend the SaveGameManager with async operations:

```csharp
public static class SaveGameManagerExtensions
{
    public static async Task<bool> SaveDataAsync(this SaveGameManager manager, IGameSaveData component)
    {
        // Run save operation on background thread
        return await Task.Run(() =>
        {
            try
            {
                // Get the persistent object
                if (!TryGetPersistentObject(component, out var persistentObj))
                    return false;

                // Prepare data
                component.OnBeforeSave();
                var json = PrepareSaveData(component);
                var key = GenerateKey(component, persistentObj, manager.SlotId);

                // Save (using thread-safe storage)
                SaveToStorageAsync(key, json);

                component.OnAfterSave();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Async save failed: {e.Message}");
                return false;
            }
        });
    }

    public static async Task SaveAllDataAsync(this SaveGameManager manager)
    {
        var tasks = new List<Task<bool>>();

        foreach (var component in manager.RegisteredComponents)
        {
            tasks.Add(manager.SaveDataAsync(component));
        }

        await Task.WhenAll(tasks);
        Debug.Log($"Async save completed for {tasks.Count} components");
    }
}

// Usage
public class AutoSaveManager : MonoBehaviour
{
    [Inject] private SaveGameManager _saveManager;

    public async void AutoSave()
    {
        ShowSavingIcon();
        await _saveManager.SaveAllDataAsync();
        HideSavingIcon();
    }
}
```

### Async with Coroutines

For Unity projects that prefer coroutines over async/await:

```csharp
public class CoroutineSaveManager : MonoBehaviour
{
    [Inject] private SaveGameManager _saveManager;

    public IEnumerator SaveAllDataCoroutine()
    {
        var operation = StartCoroutine(SaveAsyncCoroutine());
        yield return operation;
        Debug.Log("Save complete");
    }

    private IEnumerator SaveAsyncCoroutine()
    {
        int total = _saveManager.RegisteredComponents.Length;
        int completed = 0;

        foreach (var component in _saveManager.RegisteredComponents)
        {
            // Yield every few saves to keep UI responsive
            if (completed % 5 == 0)
                yield return null;

            _saveManager.SaveData(component);
            completed++;
        }
    }
}
```

### Thread-Safe Storage Implementation

For true async operations, you need thread-safe storage:

```csharp
public class ThreadSafeFileStorage : IPlayerPrefs
{
    private readonly object _lock = new object();
    private string _savePath = Application.persistentDataPath;

    public void SetString(string key, string value)
    {
        Task.Run(() =>
        {
            lock (_lock)
            {
                File.WriteAllText(Path.Combine(_savePath, key + ".json"), value);
            }
        });
    }

    public string GetString(string key)
    {
        lock (_lock)
        {
            var path = Path.Combine(_savePath, key + ".json");
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        }
    }

    // Other methods...
}
```

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

// GOOD - Use async for large saves
public async void OnGameSave()
{
    await SaveAllDataAsync();
}

// BAD - Save too frequently
private void Update()
{
    _saveManager.SaveAllData(); // DON'T save every frame!
}
```

### 3. Transform Saving Overhead

```csharp
// GOOD - Only save if position matters
[SerializeField] private bool saveTransform = true; // For moving objects

// BETTER - Disable for static objects
[SerializeField] private bool saveTransform = false; // For static scenery
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

### 5. Batch Operations

```csharp
// GOOD - Batch related saves
public void SaveCheckpoint()
{
    _saveManager.SaveAllData(); // Save everything at once
}

// BAD - Multiple individual saves
public void SaveCheckpoint()
{
    _saveManager.SaveData(player);
    _saveManager.SaveData(inventory);
    _saveManager.SaveData(world); // Three separate disk writes
}
```

---

## Common Issues and Solutions

### Issue 1: GUID Not Generated

**Symptoms**: Scene object's GUID field empty, object won't save

**Causes**: Scene not in Build Settings

**Solution**:

```csharp
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
        var enemy = Instantiate(enemyPrefab).GetComponent<Enemy>();
        enemy.SetCustomSuffix(_localCounter++); // Unique per spawner
        return enemy;
    }
}
```

### Issue 3: Transform Not Saving

**Symptoms**: Position/rotation/scale not persisting

**Cause**: `saveTransform` not enabled

**Solution**:

```csharp
[SerializeField] private bool saveTransform = true; // Enable in inspector

// Or in code
private void Awake()
{
    saveTransform = true;
}

// Or manually save transform
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
var bridge = new PlayerPrefsBridge<PlayerPrefsData>();
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

**Solution**: Implement async saving

```csharp
public class AsyncSaveManager : MonoBehaviour
{
    [Inject] private SaveGameManager _saveManager;

    public async void SaveGameAsync()
    {
        var task = Task.Run(() =>
        {
            // Run on background thread
            _saveManager.SaveAllData();
        });

        await task;
        Debug.Log("Save completed without blocking main thread");
    }
}
```

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
public class StaticScenery : PersistentObject
{
    // saveTransform = false (static object) - set in inspector
}

public class MovingPlatform : PersistentObject
{
    [SerializeField] private bool saveTransform = true; // Moving object
}

public class Door : PersistentObject
{
    // Don't need transform for door that doesn't move
    [SaveGameData] private bool _isOpen; // Only save state
}
```

### 4. Register and Load in Start/Awake

```csharp
private void Start()
{
    if (_saveManager.Register(this))
    {
        _saveManager.LoadData(this);
    }
}
```

### 5. Save at Meaningful Points

```csharp
public class Checkpoint : MonoBehaviour
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

public class LevelComplete : MonoBehaviour
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
public class SaveSlotManager : MonoBehaviour
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

### 8. Use Force Reset for Development

```csharp
public class DevReset : PersistentObject
{
    #if UNITY_EDITOR
    private void Awake()
    {
        forceReset = true; // Always reset in editor
    }
    #endif

    public override void OnAfterLoad()
    {
        if (forceReset)
        {
            ResetToDefaults();
        }
    }

    private void ResetToDefaults()
    {
        // Reset logic
    }
}
```

### 9. Implement Async for Large Games

```csharp
public class LargeGameSaveManager : MonoBehaviour
{
    [Inject] private SaveGameManager _saveManager;

    public async void SaveGame()
    {
        ShowSavingIcon();

        // Run save on background thread
        await Task.Run(() =>
        {
            _saveManager.SaveAllData();
        });

        HideSavingIcon();
        ShowSaveCompleteMessage();
    }
}
```

### 10. Backup Data Before Load

```csharp
public class SafeLoad : IGameSaveData
{
    [SaveGameData] private PlayerData _data;
    private PlayerData _backup;

    public void OnBeforeLoad()
    {
        // Backup current data
        _backup = _data.Clone();
    }

    public void OnAfterLoad()
    {
        if (!ValidateData(_data))
        {
            Debug.LogError("Loaded data invalid, restoring backup");
            _data = _backup;
        }
    }

    private bool ValidateData(PlayerData data)
    {
        return data != null && data.health > 0;
    }
}
```

---

## Examples

### Complete Player Save System

```csharp
// 1. Player component with save data
public class Player : MonoBehaviour, IGameSaveData
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

// 2. Scene object with transform saving
public class MovingPlatform : PersistentObject
{
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _moveDistance = 5f;
    [SaveGameData] private float _offset;
    [SaveGameData] private bool _isActive = true;

    private Vector3 _startPos;

    private void Start()
    {
        saveTransform = true; // Save position
        _startPos = transform.position;

        var saveManager = DependencyContainer.Instance.GetInstance<SaveGameManager>();
        saveManager.Register(this);
        saveManager.LoadData(this);
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
        var saveManager = DependencyContainer.Instance.GetInstance<SaveGameManager>();
        saveManager.SaveData(this);
    }
}

// 3. Spawnable enemy with unique ID
public class SaveableEnemy : PersistentObject
{
    private static int _nextId = 0;

    [SaveGameData] private float _health;
    [SaveGameData] private Vector3 _position;
    [SaveGameData] private string _enemyType;
    [SaveGameData] private bool _isAlive = true;

    public void Initialize(string type, Vector3 spawnPos)
    {
        spawnableObject = true;
        customId = "Enemy";
        SetCustomSuffix(_nextId++);

        _enemyType = type;
        _health = GetMaxHealthForType(type);
        _position = spawnPos;
        transform.position = spawnPos;
        _isAlive = true;

        var saveManager = DependencyContainer.Instance.GetInstance<SaveGameManager>();
        saveManager.Register(this);
        saveManager.LoadData(this);

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
        }

        var saveManager = DependencyContainer.Instance.GetInstance<SaveGameManager>();
        saveManager.SaveData(this);
    }

    private void Die()
    {
        _isAlive = false;
        gameObject.SetActive(false);
    }
}

// 4. Save slot manager with async support
public class SaveSlotUI : MonoBehaviour
{
    [Inject] private SaveGameManager _saveManager;

    [SerializeField] private GameObject _savingIcon;
    [SerializeField] private TMP_Text _statusText;

    public async void SaveToSlot(int slotIndex)
    {
        _savingIcon.SetActive(true);
        _statusText.text = "Saving...";

        _saveManager.SetCurrentSlot($"slot_{slotIndex}");

        // Run save on background thread
        await Task.Run(() =>
        {
            _saveManager.SaveAllData();
        });

        PlayerPrefs.SetInt("LastSlot", slotIndex);
        PlayerPrefs.Save();

        _savingIcon.SetActive(false);
        _statusText.text = "Game Saved!";
        await Task.Delay(2000);
        _statusText.text = "";
    }

    public async void LoadFromSlot(int slotIndex)
    {
        _savingIcon.SetActive(true);
        _statusText.text = "Loading...";

        _saveManager.SetCurrentSlot($"slot_{slotIndex}");

        await Task.Run(() =>
        {
            _saveManager.SaveAllData(); // Actually loads data
        });

        _savingIcon.SetActive(false);
        _statusText.text = "Game Loaded!";
        await Task.Delay(2000);
        _statusText.text = "";
    }

    public void NewGame()
    {
        _saveManager.SetCurrentSlot("slot_0");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
    }
}

// 5. Auto-save manager with coroutines
public class AutoSaveManager : MonoBehaviour
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

| Category         | Method                   | Description                          |
| ---------------- | ------------------------ | ------------------------------------ |
| **Registration** | `Register(component)`    | Register a component for saving      |
| **Loading**      | `LoadData(component)`    | Load data for a component            |
|                  | `SaveAllData()`          | Load data for all components         |
| **Saving**       | `SaveData(component)`    | Save a single component              |
|                  | `SaveAllData()`          | Save all components (synchronous)    |
| **Async Saving** | *Implement yourself*     | Use Task.Run or coroutines for async |
| **Deletion**     | `DeleteData(component)`  | Delete saved data for a component    |
| **Slot**         | `SetCurrentSlot(slotId)` | Change current save slot             |

### Key Points

| #   | Key Point                                                                          | Why It Matters                                            |
| --- | ---------------------------------------------------------------------------------- | --------------------------------------------------------- |
| 1   | **PersistentObject automatically saves transform** when `saveTransform` is enabled | No need to manually save position/rotation/scale          |
| 2   | **Scene objects use GUIDs** auto-generated from Build Settings                     | Unique identification without manual setup                |
| 3   | **Spawnable objects use customId + suffix**                                        | `customId` identifies the type, `suffix` separates copies |
| 4   | **GUIDs only generate for scenes in Build Settings**                               | Always add persistent scenes to Build Settings            |
| 5   | **Each spawnable copy needs a unique suffix**                                      | Prevents data conflicts between identical objects         |
| 6   | **Components must implement IGameSaveData**                                        | Receives lifecycle callbacks (OnBeforeLoad, etc.)         |
| 7   | **Mark fields with [SaveGameData]**                                                | Only marked fields are persisted                          |
| 8   | **Data models should use ModelService**                                            | SaveGameManager is for MonoBehaviour components           |
| 9   | **Default storage is synchronous**                                                 | Can cause frame drops with large save files               |
| 10  | **Async saving can be implemented by developer**                                   | Use Task.Run, coroutines, or threading for large games    |

### When to Use Synchronous vs Asynchronous Saving

| Game Size  | Save File Size | Recommended Approach               |
| ---------- | -------------- | ---------------------------------- |
| Small      | < 100KB        | Synchronous (default)              |
| Medium     | 100KB - 1MB    | Coroutine with yields              |
| Large      | > 1MB          | Async with Task.Run                |
| Very Large | > 10MB         | Background thread + chunked saving |

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
