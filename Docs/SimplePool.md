# CherryFramework SimplePool Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [SimplePool&lt;T&gt;](#simplepoolt)
4. [Usage Patterns](#usage-patterns)
5. [Performance Considerations](#performance-considerations)
6. [Common Issues and Solutions](#common-issues-and-solutions)
7. [Best Practices](#best-practices)
8. [Examples](#examples)
9. [Summary](#summary)

---

## Overview

The CherryFramework `SimplePool<T>` is a small object pool parameterised by a component type `T`. It reuses objects instead of creating and destroying them repeatedly, which matters for anything spawned often - bullets, enemies, visual effects.

One thing to know up front: the pool is keyed by **reference**, not by type. Passing the same prefab field twice gives one pool; passing `Instantiate(prefab)` gives a brand new pool.

### Why Use Object Pooling?

| Without Pooling                                        | With Pooling             |
| ------------------------------------------------------ | ------------------------ |
| `Instantiate()` and `Destroy()` called constantly      | Objects reused from pool |
| Frequent garbage collection                            | Minimal GC overhead      |
| Performance spikes during instantiation and GC cleanup | Consistent performance   |
| No object limit                                        | Pool grows on demand (no built-in limit) |

---

## Core Concepts

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      SimplePool<T>                          │
├─────────────────────────────────────────────────────────────┤
│ - Dictionary<T, List<T>> _pool                              │
│                                                             │
│ + Get(T sample)                                             │
│ + Get(T sample, Vector3 position, Quaternion rotation)      │
│ + Get(T sample, position, rotation, Transform parent)       │
│ + List<T> ActiveObjects(T sample)                           │
│ + void Clear()                                              │
└─────────────────────────────────────────────────────────────┘
                              │
            ┌─────────────────┼─────────────────┐
            ▼                 ▼                 ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │   Pool A      │ │   Pool B      │ │   Pool C      │
    │ (BulletPrefab)│ │ (EnemyPrefab) │ │ (EffectPrefab)│
    └───────────────┘ └───────────────┘ └───────────────┘
            │                 │                 │
            ▼                 ▼                 ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │ [Inactive]    │ │ [Inactive]    │ │ [Inactive]    │
    │ [Inactive]    │ │ [Active]      │ │ [Inactive]    │
    │ [Active]      │ │ [Inactive]    │ │ [Active]      │
    │ [Active]      │ │ [Active]      │ │ [Inactive]    │
    └───────────────┘ └───────────────┘ └───────────────┘
```

### How It Works

1. **First Request**: pool has no entry for this sample -> a new object is
   instantiated from it (and it comes back **active**, because
   `Object.Instantiate` keeps the prefab's state)
2. **Subsequent Requests**: the first **inactive** instance is returned
   **as-is**. `Get` never calls `SetActive(true)` — activating the object is
   your job. See [Activation](#activating-objects-after-get).
3. **Object Return**: there is no `Release()`. The pool considers an object
   free the moment you call `gameObject.SetActive(false)` on it.
4. **Per-Sample Pools**: one pool per **component reference**. The same prefab
   passed as two different references produces two separate pools.
5. **Null Cleanup**: destroyed objects are pruned from the list only on the
   next `Get()` call. `ActiveObjects()` and `Clear()` merely skip them.

#### Activating objects after Get

Because `Get` returns inactive objects untouched, forgetting to activate is
the single most common way to get "the pool returned something but nothing
happened":

```csharp
var bullet = bulletPool.Get(bulletPrefab, firePoint.position, firePoint.rotation);
bullet.gameObject.SetActive(true); // required - Get does not do this
```

The framework follows the same rule internally: `AudioEmitter` activates
itself when it starts playing (see `Assets/CherryFramework/SoundService/AudioEmitter.cs`).

### Key Components

| Component         | Purpose                                        |
| ----------------- | ---------------------------------------------- |
| `SimplePool<T>`   | Generic pool class for any Component type      |
| `Get()`           | Retrieves an object from pool (or creates new) |
| `ActiveObjects()` | Returns all currently active objects           |
| `Clear()`         | Destroys all pooled objects                    |

---

## SimplePool&lt;T&gt;

**Namespace**: `CherryFramework.SimplePool`

### Class Definition

```csharp
public class SimplePool<T> where T : Component
{
    // Constructor
    public SimplePool();

    // Methods
    public T Get(T sample, Vector3 position, Quaternion rotation, Transform parent = null);
    public T Get(T sample);
    public List<T> ActiveObjects(T sample);
    public void Clear();
}
```

### Methods

#### Get (with transform)

```csharp
public T Get(T sample, Vector3 position, Quaternion rotation, Transform parent = null)
```

Retrieves an object from the pool, positions it, and returns it.
The pool reuses the first **inactive** instance of the sample (setting its
position, rotation, and parent); if none is available it instantiates a new
one from the sample. Already-active instances are left untouched, so a pool
call never hijacks a running object.

**Example**:

```csharp
var bullet = bulletPool.Get(bulletPrefab, firePoint.position, firePoint.rotation);
```

#### Get (simple)

```csharp
public T Get(T sample)
```

Retrieves an object without changing its transform - the first inactive
instance is reused as-is, or a new one is instantiated from the sample.

**Example**:

```csharp
var effect = effectPool.Get(effectPrefab);
effect.transform.position = spawnPoint;
```

#### ActiveObjects

```csharp
public List<T> ActiveObjects(T sample)
```

Returns the pooled instances whose GameObject is currently active (destroyed
Unity references are skipped). Returns an empty list for unknown samples.

**Example**:

```csharp
int activeCount = enemyPool.ActiveObjects(enemyPrefab).Count;
Debug.Log($"Active enemies: {activeCount}");
```

#### Clear

```csharp
public void Clear()
```

Destroys the GameObjects of **all** pooled instances via `Object.Destroy`
(already-destroyed references are skipped) and empties the pool.

**Example**:

```csharp
private void OnDestroy()
{
    bulletPool.Clear();
}
```

---

## Usage Patterns

### Basic Setup

```csharp
public class Weapon : MonoBehaviour
{
    private SimplePool<Bullet> _bulletPool;
    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private Transform _firePoint;

    private void Awake()
    {
        _bulletPool = new SimplePool<Bullet>();
    }

    public void Shoot()
    {
        var bullet = _bulletPool.Get(_bulletPrefab, _firePoint.position, _firePoint.rotation);
        bullet.Initialize();
    }
}
```

### Bullet Implementation

```csharp
public class Bullet : MonoBehaviour
{
    private float _speed = 20f;

    private void OnEnable()
    {
        // Auto-return after 3 seconds
        Invoke(nameof(ReturnToPool), 3f);
    }

    public void Initialize()
    {
        gameObject.SetActive(true);
        // Setup bullet
    }

    private void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<Enemy>().Die();
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false); // Returns to pool
    }
}
```

### Enemy Spawning

```csharp
public class EnemySpawner : MonoBehaviour
{
    private SimplePool<Enemy> _enemyPool;
    [SerializeField] private Enemy _enemyPrefab;
    [SerializeField] private Transform[] _spawnPoints;

    private void Awake()
    {
        _enemyPool = new SimplePool<Enemy>();
        PrewarmPool(10);
    }

    private void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var enemy = _enemyPool.Get(_enemyPrefab);
            enemy.gameObject.SetActive(false);
        }
    }

    public void SpawnEnemy()
    {
        var point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        var enemy = _enemyPool.Get(_enemyPrefab, point.position, point.rotation);
        enemy.Initialize();
    }

    public int ActiveEnemyCount => _enemyPool.ActiveObjects(_enemyPrefab).Count;
}
```

### Effect Pooling

```csharp
public class EffectManager : MonoBehaviour
{
    private SimplePool<ParticleSystem> _explosionPool;
    [SerializeField] private ParticleSystem _explosionPrefab;

    private void Awake()
    {
        _explosionPool = new SimplePool<ParticleSystem>();
    }

    public void SpawnExplosion(Vector3 position)
    {
        var explosion = _explosionPool.Get(_explosionPrefab, position, Quaternion.identity);
        explosion.gameObject.SetActive(true);
        explosion.Play();
        StartCoroutine(DeactivateAfterSeconds(explosion, explosion.main.duration));
    }

    private IEnumerator DeactivateAfterSeconds(ParticleSystem ps, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ps.gameObject.SetActive(false);
    }
}
```

### Multiple Pool Management

```csharp
public class PoolManager : MonoBehaviour
{
    private SimplePool<Bullet> _bulletPool;
    private SimplePool<Enemy> _enemyPool;
    private SimplePool<ParticleSystem> _effectPool;

    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private Enemy _enemyPrefab;
    [SerializeField] private ParticleSystem _effectPrefab;

    private void Awake()
    {
        _bulletPool = new SimplePool<Bullet>();
        _enemyPool = new SimplePool<Enemy>();
        _effectPool = new SimplePool<ParticleSystem>();

        PrewarmPools();
    }

    private void PrewarmPools()
    {
        // Prewarm bullets
        for (int i = 0; i < 20; i++)
        {
            var bullet = _bulletPool.Get(_bulletPrefab);
            bullet.gameObject.SetActive(false);
        }

        // Prewarm enemies
        for (int i = 0; i < 10; i++)
        {
            var enemy = _enemyPool.Get(_enemyPrefab);
            enemy.gameObject.SetActive(false);
        }

        // Prewarm effects
        for (int i = 0; i < 5; i++)
        {
            var effect = _effectPool.Get(_effectPrefab);
            effect.gameObject.SetActive(false);
        }
    }
}
```

---

## Performance Considerations

### Prewarm Pools

```csharp
private void PrewarmPool(int count)
{
    for (int i = 0; i < count; i++)
    {
        var obj = _pool.Get(_prefab);
        obj.gameObject.SetActive(false); // Return to pool
    }
    Debug.Log($"Prewarmed pool with {count} objects");
}
```

### Monitor Pool Usage

```csharp
private void Update()
{
    // Check pool usage every few seconds
    if (Time.frameCount % 300 == 0)
    {
        int active = _pool.ActiveObjects(_prefab).Count;
        Debug.Log($"Active objects: {active}");

        if (active > _warningThreshold)
        {
            Debug.LogWarning($"High pool usage: {active}");
        }
    }
}
```

### Clean Up Pools

```csharp
private void OnDestroy()
{
    _bulletPool.Clear();
    _enemyPool.Clear();
    _effectPool.Clear();
}

// Or when changing scenes
private void OnLevelWasLoaded()
{
    _bulletPool.Clear();
    _enemyPool.Clear();
    _effectPool.Clear();
}
```

### Pool Size Limits

`SimplePool<T>` has **no built-in limit** — it grows until you call `Clear()`.
If you need a cap (for example, a hard limit on simultaneous bullets), you
have to enforce it yourself. Count the *active* objects and recycle the oldest
one instead of creating another:

```csharp
using CherryFramework.SimplePool;
using UnityEngine;

// Composition, not inheritance: SimplePool keeps its internal list private,
// and its Get methods are not virtual, so neither can be reached from a
// derived class.
public class LimitedPool<T> where T : Component
{
    private readonly SimplePool<T> _pool = new();
    private readonly int _maxSize;

    public LimitedPool(int maxSize) => _maxSize = maxSize;

    public T Get(T sample, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var active = _pool.ActiveObjects(sample);
        if (active.Count >= _maxSize)
        {
            // Recycle the oldest active object - this is what returns it to
            // the pool, because there is no Release()
            active[0].gameObject.SetActive(false);
        }

        var obj = _pool.Get(sample, position, rotation, parent);
        obj.gameObject.SetActive(true); // Get does not activate for you
        return obj;
    }

    public List<T> ActiveObjects(T sample) => _pool.ActiveObjects(sample);

    public void Clear() => _pool.Clear();
}
```

Note that this caps **simultaneously active** objects, not the total number
of instances ever created. To bound memory as well, call `Clear()` yourself.

---

## Common Issues and Solutions

### Issue 1: Objects Not Returning to Pool

**Symptoms**: Pool creates new objects endlessly, memory usage grows

**Solution**: Always deactivate when done

```csharp
private void ReturnToPool()
{
    gameObject.SetActive(false); // Returns to pool
}

private void OnDisable()
{
    CancelInvoke(); // Clean up any pending calls
    StopAllCoroutines();
}
```

### Issue 2: Objects Not Resetting State

**Symptoms**: Reused objects retain old health, position, or visual state

**Solution**: Reset in OnEnable or with initialize method

```csharp
private void OnEnable()
{
    _health = 100;
    _speed = 5f;
    transform.localScale = Vector3.one;
    GetComponent<Renderer>().material.color = Color.white;
}

public void Initialize(Vector3 position, int health)
{
    transform.position = position;
    _health = health;
}
```

### Issue 3: Wrong Sample Reference

**Symptoms**: `Get()` always creates new objects even when pool has inactive ones

**Solution**: Always use the same reference

```csharp
// GOOD
private SimplePool<Bullet> _pool;
[SerializeField] private Bullet _prefab; // Inspector reference

private void Shoot()
{
    var bullet = _pool.Get(_prefab); // Use same reference
}

// BAD
var bullet = _pool.Get(Instantiate(_prefab)); // Different reference!
```

### Issue 4: Pool Grows Too Large

**Symptoms**: Pool size increases indefinitely, memory usage high

**Solution**: Implement size monitoring and limits

```csharp
// Composition again: SimplePool's internal list is private, so a derived
// class cannot measure the total instance count - and its Get methods are
// not virtual, so 'new Get' would not intercept calls made through a
// SimplePool<T> reference anyway.
public class MonitoredPool<T> where T : Component
{
    private readonly SimplePool<T> _pool = new();

    public int PeakActive { get; private set; }

    public T Get(T sample, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var obj = _pool.Get(sample, position, rotation, parent);

        int active = _pool.ActiveObjects(sample).Count;
        PeakActive = Mathf.Max(PeakActive, active);

        return obj;
    }

    public void LogStats(T sample)
    {
        int active = _pool.ActiveObjects(sample).Count;

        // The framework does not expose the total instance count, only the
        // active ones - track the total yourself if you need it.
        Debug.Log($"Pool stats - Active: {active}, Peak: {PeakActive}");
    }
}
```

### Issue 5: Memory Leaks from Uncleared Pools

**Symptoms**: Objects remain after scene change

**Solution**: Clear pools when no longer needed

```csharp
public class SceneCleanup : BehaviourBase
{
    [Inject] private PoolManager _poolManager;

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        _poolManager.ClearAllPools();
    }
}
```

---

## Best Practices

### 1. Always Deactivate When Done

```csharp
private void OnTriggerEnter(Collider other)
{
    // Handle collision
    gameObject.SetActive(false); // Return to pool
}

private void Update()
{
    if (transform.position.y < -10)
    {
        gameObject.SetActive(false); // Return when out of bounds
    }
}
```

### 2. Reset State in OnEnable

```csharp
private void OnEnable()
{
    _health = 100;
    _ammo = 30;
    _trail?.Clear();
    _rigidbody.velocity = Vector3.zero;
    _rigidbody.angularVelocity = Vector3.zero;
}
```

### 3. Prewarm Important Pools

```csharp
private void Start()
{
    // Prewarm pools at start to avoid runtime spikes
    PrewarmPool(_bulletPool, _bulletPrefab, 20);
    PrewarmPool(_enemyPool, _enemyPrefab, 10);
    PrewarmPool(_effectPool, _effectPrefab, 5);
}

private void PrewarmPool<T>(SimplePool<T> pool, T prefab, int count) where T : Component
{
    for (int i = 0; i < count; i++)
    {
        var obj = pool.Get(prefab);
        obj.gameObject.SetActive(false);
    }
}
```

### 4. Clear Pools When Changing Scenes

```csharp
private void OnDestroy()
{
    _bulletPool.Clear();
    _enemyPool.Clear();
    _effectPool.Clear();
}

// Or in a persistent manager
DontDestroyOnLoad(gameObject);
```

---

## Examples

### Complete Weapon System

```csharp
// Bullet.cs
public class Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 20f;
    [SerializeField] private float _lifetime = 3f;
    [SerializeField] private int _damage = 10;

    private void OnEnable()
    {
        Invoke(nameof(ReturnToPool), _lifetime);
    }

    public void Initialize(Vector3 direction)
    {
        gameObject.SetActive(true);
        GetComponent<Rigidbody>().velocity = direction * _speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            other.GetComponent<Enemy>().TakeDamage(_damage);
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }
}

// Weapon.cs
public class Weapon : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private float _fireRate = 0.1f;

    private SimplePool<Bullet> _bulletPool;
    private float _nextFireTime;

    private void Awake()
    {
        _bulletPool = new SimplePool<Bullet>();
        PrewarmPool(20);
    }

    private void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var bullet = _bulletPool.Get(_bulletPrefab);
            bullet.gameObject.SetActive(false);
        }
    }

    public void Fire()
    {
        if (Time.time < _nextFireTime) return;

        _nextFireTime = Time.time + _fireRate;

        var bullet = _bulletPool.Get(_bulletPrefab, _firePoint.position, _firePoint.rotation);
        bullet.Initialize(_firePoint.forward);
    }
}
```

### Complete Enemy System

```csharp
// Enemy.cs
public class Enemy : MonoBehaviour
{
    [SerializeField] private int _health = 3;
    [SerializeField] private float _speed = 2f;
    [SerializeField] private int _scoreValue = 100;

    private Transform _player;

    private void OnEnable()
    {
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        _health = 3;
    }

    private void Update()
    {
        if (_player == null) return;

        var direction = (_player.position - transform.position).normalized;
        transform.Translate(direction * _speed * Time.deltaTime, Space.World);
    }

    public void TakeDamage(int damage)
    {
        _health -= damage;

        if (_health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Add score
        ScoreManager.Instance.AddScore(_scoreValue);

        // Play death effect.
        // FindObjectOfType is [Obsolete] in Unity 6 - use FindFirstObjectByType.
        // Better still, [Inject] the manager: BehaviourBase fills it in OnEnable.
        FindFirstObjectByType<EffectManager>()?.SpawnExplosion(transform.position);

        gameObject.SetActive(false); // Return to pool
    }
}

// EnemySpawner.cs
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Enemy _enemyPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _spawnInterval = 2f;
    [SerializeField] private int _maxEnemies = 20;

    private SimplePool<Enemy> _enemyPool;
    private float _spawnTimer;

    private void Awake()
    {
        _enemyPool = new SimplePool<Enemy>();
        PrewarmPool(_maxEnemies);
    }

    private void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var enemy = _enemyPool.Get(_enemyPrefab);
            enemy.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;

            if (_enemyPool.ActiveObjects(_enemyPrefab).Count < _maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    private void SpawnEnemy()
    {
        var point = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        var enemy = _enemyPool.Get(_enemyPrefab, point.position, point.rotation);
        enemy.gameObject.SetActive(true);
        // Enemy automatically starts due to OnEnable
    }

    public int ActiveEnemyCount => _enemyPool.ActiveObjects(_enemyPrefab).Count;
}
```

### Complete Effect System

```csharp
// EffectManager.cs
public class EffectManager : MonoBehaviour
{
    [System.Serializable]
    public class EffectConfig
    {
        public string name;
        public ParticleSystem prefab;
        public int prewarmCount = 5;
        public float duration = -1; // -1 = use particle system duration
    }

    [SerializeField] private List<EffectConfig> _effects;
    [SerializeField] private Transform _effectParent;

    private Dictionary<string, SimplePool<ParticleSystem>> _pools = new();

    private void Awake()
    {
        // Create parent if not assigned
        if (_effectParent == null)
        {
            var parentObj = new GameObject("EffectPool");
            _effectParent = parentObj.transform;
            _effectParent.SetParent(transform);
        }

        // Initialize pools
        foreach (var effect in _effects)
        {
            var pool = new SimplePool<ParticleSystem>();

            // Prewarm
            for (int i = 0; i < effect.prewarmCount; i++)
            {
                var instance = pool.Get(effect.prefab);
                instance.transform.SetParent(_effectParent);
                instance.gameObject.SetActive(false);
            }

            _pools[effect.name] = pool;
        }
    }

    public void PlayEffect(string name, Vector3 position)
    {
        PlayEffect(name, position, Quaternion.identity);
    }

    public void PlayEffect(string name, Vector3 position, Quaternion rotation)
    {
        if (!_pools.TryGetValue(name, out var pool)) return;

        var config = _effects.Find(e => e.name == name);
        if (config == null) return;

        var effect = pool.Get(config.prefab, position, rotation, _effectParent);
        effect.gameObject.SetActive(true);
        effect.Play();

        float duration = config.duration > 0 ? config.duration : effect.main.duration;
        StartCoroutine(DeactivateAfterSeconds(effect, duration));
    }

    private IEnumerator DeactivateAfterSeconds(ParticleSystem ps, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ps.Stop();
        ps.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (var pool in _pools.Values)
        {
            pool.Clear();
        }
        _pools.Clear();
    }
}

// Usage
public class GameEvents : MonoBehaviour
{
    [Inject] private EffectManager _effects;

    public void OnPlayerShoot()
    {
        _effects.PlayEffect("MuzzleFlash", firePoint.position);
    }

    public void OnEnemyDeath(Vector3 position)
    {
        _effects.PlayEffect("Explosion", position);
        _effects.PlayEffect("BloodSplat", position);
    }

    public void OnPowerupCollected(Vector3 position)
    {
        _effects.PlayEffect("PowerupGlow", position);
    }
}
```

---

## Summary

### Method Summary

| Method                            | Description                | Example                              |
| --------------------------------- | -------------------------- | ------------------------------------ |
| `Get(T sample)`                   | Get object from pool       | `pool.Get(bulletPrefab)`             |
| `Get(T sample, pos, rot)`         | Get and position object    | `pool.Get(prefab, pos, rot)`         |
| `Get(T sample, pos, rot, parent)` | Get, position, and parent  | `pool.Get(prefab, pos, rot, parent)` |
| `ActiveObjects(T sample)`         | Get all active objects     | `pool.ActiveObjects(prefab).Count`   |
| `Clear()`                         | Destroy all pooled objects | `pool.Clear()`                       |

### Key Points

| #   | Key Point                                        | Why It Matters                                                 |
| --- | ------------------------------------------------ | -------------------------------------------------------------- |
| 1   | **Always deactivate objects when done**          | This is the return mechanism — there is no `Release()` (`gameObject.SetActive(false)`) |
| 2   | **Activate the object yourself after `Get`**     | `Get` never calls `SetActive(true)`; an inactive instance does nothing |
| 3   | **Reset state in `OnEnable`**                    | Ensures fresh state when object is reused                      |
| 4   | **Clear pools when changing scenes**             | `Clear()` destroys the pooled objects; prevents leaks          |
| 5   | **Use the same sample reference**                | The pool key is the component reference, so a different reference creates a different pool |
| 6   | **Check `ActiveObjects` for limits**             | The pool has no built-in cap — count active objects yourself   |

### When to Use SimplePool

| Use Pooling                           | Don't Pool                                       |
| ------------------------------------- | ------------------------------------------------ |
| Bullets / Projectiles                 | Boss enemies (rare)                              |
| Enemies in waves                      | Unique quest items                               |
| Particle effects                      | Level geometry                                   |
| UI elements that appear frequently    | Objects created once |
| Anything created/destroyed frequently | Objects with complex setup                       |

SimplePool provides an efficient, easy-to-use object pooling solution that integrates seamlessly with the CherryFramework, helping you write high-performance Unity games with minimal garbage collection overhead.
