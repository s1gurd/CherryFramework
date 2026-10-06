# CherryFramework SoundService Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [SoundService](#soundservice)
4. [AudioEvent](#audioevent)
5. [AudioEmitter](#audioemitter)
6. [GlobalAudioSettings](#globalaudiosettings)
7. [AudioEventsCollection](#audioeventscollection)
8. [Performance Considerations](#performance-considerations)
9. [Common Issues and Solutions](#common-issues-and-solutions)
10. [Best Practices](#best-practices)
11. [Examples](#examples)
12. [Summary](#summary)

---

## Overview

The CherryFramework SoundService provides a lightweight, event-based audio system for Unity applications. It's designed as a simple yet powerful alternative when integrating full-featured audio middleware like Wwise or FMOD would be overkill or too complex for your project.

### When to Use SoundService

| Scenario                                     | Recommendation                             |
| -------------------------------------------- | ------------------------------------------ |
| **Small to medium projects**                 | Perfect fit - quick to set up, easy to use |
| **Prototyping**                              | Ideal - get audio working in minutes       |
| **Mobile games**                             | Great - lightweight with good performance  |
| **Projects with simple audio needs**         | Excellent - covers most common use cases   |
| **Projects requiring complex audio routing** | Consider Wwise/FMOD instead                |

### Key Features

- **Event-Based Audio**: Play sounds using string-based event keys
- **Automatic Pooling**: Audio emitters are pooled for performance
- **3D Spatial Audio**: Full support for positional audio
- **Advanced Positioning**: Blend between emitter and camera positions/orientations
- **Fading**: Smooth volume fades for both playing and stopping
- **Multiple Playback Control**: Per-sound handler system for individual control
- **ScriptableObject Configuration**: Easy setup through asset files
- **Resource-Based Audio**: Uses Unity's AudioResource system

---

## Core Concepts

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      SoundService                           │
├─────────────────────────────────────────────────────────────┤
│ - Dictionary<string, AudioEvent> _events                    │
│ - SimplePool<AudioEmitter> _emitters                        │
│ - ListenerCamera _camera                                    │
│                                                             │
│ + Play(string eventName, Transform emitter)                 │
│ + FadeIn() / FadeOut()                                      │
│ + Stop() / StopAll()                                        │
│ + GetEmitter(uint handler)                                  │
└─────────────────────────────────────────────────────────────┘
                              │
            ┌─────────────────┼─────────────────┐
            ▼                 ▼                 ▼
    ┌───────────────┐ ┌───────────────┐ ┌───────────────┐
    │  AudioEvent   │ │ AudioEmitter  │ │    Camera     │
    │ (Definition)  │ │  (Instance)   │ │  (Listener)   │
    └───────────────┘ └───────────────┘ └───────────────┘
            │                 │                 │
            ▼                 ▼                 │
    ┌───────────────┐ ┌───────────────┐         │
    │ - eventKey    │ │ - PlayEvent() │         │
    │ - volume      │ │ - Stop()      │         │
    │ - pitch       │ │ - FadeIn/Out()│         │
    │ - spatialBlend│ │ - Tick()      │         │
    │ - rolloffMode │ │ - Follow mode │         │
    └───────────────┘ └───────────────┘         │
            │                 │                 │
            └─────────────────┼─────────────────┘
                              ▼
                    ┌─────────────────┐
                    │  AudioSource    │
                    │(Unity Component)│
                    └─────────────────┘
```

### Key Components

| Component               | Purpose                                                        |
| ----------------------- | -------------------------------------------------------------- |
| `SoundService`          | Central audio service for playing and managing sounds          |
| `AudioEvent`            | Definition of a sound (pitch, volume, spatial settings)        |
| `AudioEmitter`          | Runtime component that plays sounds with pooling support       |
| `GlobalAudioSettings`   | Global configuration for audio system                          |
| `AudioEventsCollection` | ScriptableObject container for multiple AudioEvent definitions |
| `ListenerCamera`        | Wrapper for the audio listener camera                          |

### AudioEvent Configuration

AudioEvents are **not** created as individual ScriptableObjects. Instead, they are configured within an `AudioEventsCollection` ScriptableObject, which contains a list of AudioEvent definitions. This approach keeps related sounds organized together.

```
AudioEventsCollection (ScriptableObject)
├── List<AudioEvent>
│   ├── AudioEvent (eventKey: "player_shoot", volume: 0.8, ...)
│   ├── AudioEvent (eventKey: "player_jump", volume: 0.6, ...)
│   └── AudioEvent (eventKey: "player_hit", volume: 0.7, ...)
```

### Audio Identification

Sounds are played using string-based event keys defined in the AudioEventsCollection:

```csharp
// Play a sound by event name (must match an event in a collection)
soundService.Play("player_shoot", transform);
soundService.Play("explosion_large", explosionPosition, delay: 0.5f);
```

### Handler System

Each played sound receives a unique handler ID for individual control:

```csharp
// The emitter Transform is REQUIRED - passing null throws
// NullReferenceException inside AudioEmitter.SetTransformation().
uint handler = soundService.Play("music_background", transform);
// Later...
soundService.FadeOut(handler, duration: 2f); // fades over 2 seconds
```

For a 2D sound - music, UI clicks - pass the transform of the thing that owns
it (the camera, a UI panel, or `this.gameObject.transform`). Set
`spatialBlend = 0` on the event so Unity ignores the distance, and
`positionToListener` / `orientToListener` to `0` to keep the source glued to
that transform.

**`emitter` may not be null.** `AudioEmitter` stores it and immediately
dereferences `_emitterTransform.position` in `SetTransformation()`, with no null
guard. There is no "no position" option.

### Positioning Modes

| Ratio | Position Behavior | Orientation Behavior |
| ----- | ----------------- | -------------------- |
| 0     | At emitter object | As emitter object    |
| 0.5   | Halfway to camera | Blended orientation  |
| 1     | At camera         | Blended orientation  |

The ratios are `Vector3.Lerp` weights between the emitter and the
`ListenerCamera`. Two special cases short-circuit the per-frame update and
re-parent the source outright:

- both ratios `0` → the source is parented to the emitter
- both ratios `1` → the source is parented to the camera

Orientation blends towards the camera's **rotation**, so at ratio 1 the source
takes the camera's orientation rather than turning to face it.

---

## SoundService

**Namespace**: `CherryFramework.SoundService`

**Purpose**: Central service that manages all audio playback, pooling, and event handling.

### Class Definition

```csharp
public class SoundService : GeneralClassBase
{
    // Constructor
    public SoundService(GlobalAudioSettings globalAudioSettings, 
                        AudioEventsCollection audioEvents, 
                        Camera camera = null);

    public SoundService(GlobalAudioSettings globalAudioSettings,
                        List<AudioEventsCollection> settingsCollection,
                        Camera camera = null);

    // Playback Methods
    public uint Play(string eventName, Transform emitter, float delay = 0f, Action onPlayEnd = null);
    public uint FadeIn(string eventName, Transform emitter, float fadeDuration = 0f, 
                       float delay = 0f, Action onPlayEnd = null);

    // Control Methods
    public void Stop(uint handler);
    public void FadeOut(uint handler, float duration = 0f);
    public void StopAll();

    // Query Methods
    public AudioEmitter GetEmitter(uint handler);
    public IEnumerable<AudioEmitter> GetEmitters(string eventKey);
    public bool IsPlaying(uint handler);
}
```

### Constructor

```csharp
// Single collection
var soundService = new SoundService(settings, sfxCollection, Camera.main);

// Multiple collections
var collections = new List<AudioEventsCollection> { sfxCollection, musicCollection };
var soundService = new SoundService(settings, collections, Camera.main);
```

### Playback Methods

#### Play

```csharp
public uint Play(string eventName, Transform emitter, float delay = 0f, Action onPlayEnd = null)
```

**Example**:

```csharp
uint handler = soundService.Play("explosion", explosionTransform, 0.2f, () => {
    Debug.Log("Explosion finished");
});
```

#### FadeIn

```csharp
public uint FadeIn(string eventName, Transform emitter, float fadeDuration = 0f, 
                   float delay = 0f, Action onPlayEnd = null)
```

**Example**:

```csharp
// Fade in music over 3 seconds
uint musicHandler = soundService.FadeIn("background_music", null, 3f);
```

### Control Methods

#### Stop

```csharp
public void Stop(uint handler)
```

**Example**:

```csharp
soundService.Stop(musicHandler);
```

#### FadeOut

```csharp
public void FadeOut(uint handler, float duration = 0f)
```

**Example**:

```csharp
soundService.FadeOut(musicHandler, 2f); // Fade out over 2 seconds
```

#### StopAll

```csharp
public void StopAll()
```

**Example**:

```csharp
private void OnDestroy()
{
    soundService.StopAll();
}
```

### Query Methods

#### GetEmitter

```csharp
public AudioEmitter GetEmitter(uint handler)
```

**Example**:

```csharp
var emitter = soundService.GetEmitter(handler);
if (emitter != null)
{
    emitter.Source.pitch = 1.5f; // Modify pitch dynamically
}
```

#### GetEmitters

```csharp
public IEnumerable<AudioEmitter> GetEmitters(string eventKey)
```

**Example**:

```csharp
int explosionCount = soundService.GetEmitters("explosion").Count();
Debug.Log($"Active explosions: {explosionCount}");
```

#### IsPlaying

```csharp
public bool IsPlaying(uint handler)
```

**Example**:

```csharp
if (soundService.IsPlaying(handler))
{
    // Wait for sound to finish
}
```

---

## AudioEvent

**Namespace**: `CherryFramework.SoundService`

**Purpose**: Serializable definition of an audio event, containing all parameters needed to play a sound. AudioEvents are configured inside an AudioEventsCollection ScriptableObject.

### Class Definition

```csharp
[Serializable]
public class AudioEvent
{
    // Core Identification
    public string eventKey;
    public AudioResource audioResource;

    // Positioning
    [Range(0f, 1f)] public float positionToListener;
    [Range(0f, 1f)] public float orientToListener;
    public bool freezeTransform;
    public bool doNotDeactivateOnStop;

    // Audio Source Settings
    public AudioMixerGroup output;
    public bool mute;
    public bool bypassEffects;
    public bool bypassListenerEffects;
    public bool bypassReverbZones;
    public bool loop;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0f, 3f)] public float pitch = 1f;
    [Range(-1f, 1f)] public float panStereo;
    [Range(0f, 1f)] public float spatialBlend;
    [Range(0f, 1.1f)] public float reverbZoneMix = 1f;
    [Range(0f, 5f)] public float dopplerLevel = 1f;
    [Range(0f, 360f)] public float spread;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    [Range(0, 500f)] public float minDistance = 1f;
    [Range(0, 500f)] public float maxDistance = 100f;
    public AnimationCurve volumeCurve;
}
```

---

## AudioEmitter

**Namespace**: `CherryFramework.SoundService`

**Purpose**: Runtime component that handles actual audio playback. Instances are pooled and reused.

### Class Definition

```csharp
public class AudioEmitter : BehaviourBase, ITickable
{
    public uint CurrentHandler { get; private set; }
    public string EventKey { get; private set; }
    public AudioSource Source => source;

    public void PlayEvent(AudioEvent evt, Transform emitter, float delay, 
                          uint handler, Action onPlayEnd = null);
    public void FadeIn(AudioEvent evt, Transform emitter, float delay, uint handler, 
                       float fadeInDuration, Action onPlayEnd = null);
    public void FadeOut(float fadeOutDuration, float delay);
    public void Stop(float delay = 0f);
    public void Pause(float delay = 0f);
    public void Resume(float delay = 0f);

    // On-stop callback management (onPlayEnd is stored and invoked when the sound ends)
    public void AppendOnStopCallback(Action callback);
    public void RemoveOnStopCallback(Action callback);
    public void ClearOnStopCallback();
}
```

### Deactivation Behaviour

`AudioEmitter` objects are pooled, so the emitter takes care of its own
dormant state:

- When the sound **finishes naturally** (the `AudioSource` is no longer
  playing, nothing is waiting, and the event does **not** set
  `doNotDeactivateOnStop`), the emitter invokes its on-stop callback and
  deactivates its own GameObject - it then sits inactive in the pool until
  the next `PlayEvent` reactivates it.
- If the event's `doNotDeactivateOnStop` is set, the emitter **stays active**
  after stopping (useful for emitters that must keep playing again, e.g.
  ambient or looping sources).
- If the emitter's GameObject is **not active in the hierarchy** when an
  event is started, the play is aborted and the emitter is deactivated.

### Key Methods

#### PlayEvent

```csharp
public void PlayEvent(AudioEvent evt, Transform emitter, float delay, uint handler, Action onPlayEnd = null)
```

**Example** (internal usage):

```csharp
// Called by SoundService
emitter.PlayEvent(audioEvent, transform, 0f, handlerId, onComplete);
```

#### FadeOut

```csharp
public void FadeOut(float fadeOutDuration, float delay)
public void FadeOut(float delay = 0f)   // <-- easy to bind by mistake
```

**Example**:

```csharp
emitter.FadeOut(1.5f, 0f); // fade out over 1.5 seconds
emitter.FadeOut(0.5f);      // wait 0.5 s, then use the event's fade duration
```

#### Stop

```csharp
public void Stop(float delay = 0f)
```

**Example**:

```csharp
emitter.Stop(0.5f); // Stop after 0.5 seconds delay
```

---

## GlobalAudioSettings

**Namespace**: `CherryFramework.SoundService`

**Purpose**: ScriptableObject containing global audio configuration.

### Class Definition

```csharp
[CreateAssetMenu(menuName = "Audio/Sound Service/Settings", fileName = "AudioSettings")]
public class GlobalAudioSettings : ScriptableObject
{
    public AudioEmitter emitterSample;
    public float defaultFadeDuration = 0.5f;
}
```

### Setup

```csharp
// Create in project:
// Right-click → Audio/Sound Service/Settings
// Assign AudioEmitter prefab
// Set default fade duration (e.g., 0.5 seconds)
```

---

## AudioEventsCollection

**Namespace**: `CherryFramework.SoundService`

**Purpose**: ScriptableObject container for multiple AudioEvent definitions. This is where all audio events are configured and organized.

### Class Definition

```csharp
[CreateAssetMenu(menuName = "Audio/Sound Service/Audio Events Collection", fileName = "AudioEventsCollection")]
public class AudioEventsCollection : ScriptableObject
{
    public List<AudioEvent> audioEvents;
}
```

### Creating an AudioEvents Collection

1. **Right-click in Project window** → **Audio/Sound Service/Audio Events Collection**
2. **Name it** (e.g., "SFX_Collection", "Music_Collection", "UI_Collection")
3. **Add AudioEvents** to the list in the Inspector
4. **Configure each event** with:
   - Unique event key (e.g., "player_shoot", "explosion")
   - AudioResource (assign your audio clip)
   - Volume, pitch, spatial settings
   - 3D positioning options

### Example Project Structure

```
Assets/
└── Audio/
    ├── Settings/
    │   └── GlobalAudioSettings.asset
    ├── Collections/
    │   ├── SFX_Collection.asset
    │   │   ├── player_shoot
    │   │   ├── player_jump
    │   │   ├── player_hit
    │   │   └── explosion
    │   ├── Music_Collection.asset
    │   │   ├── bgm_main
    │   │   └── bgm_battle
    │   └── UI_Collection.asset
    │       ├── ui_click
    │       ├── ui_hover
    │       └── ui_error
    └── AudioResources/
        ├── gun_shot.audioclip
        ├── explosion.audioclip
        └── background_music.audioclip
```

---

## Performance Considerations

**Emitters are pooled, and they deactivate themselves.** `SoundService`
keeps a `SimplePool<AudioEmitter>` and `AudioEmitter` sets its GameObject
inactive when the clip finishes. A pooled instance is therefore reused for the
next `Play` - you do not create or destroy emitters by hand.

**`Get` does not activate.** The pooled emitter comes back **inactive**. If you
ever call `GetEmitter(...)` / `GetEmitters(...)` directly and then expect it to
play, you must call `gameObject.SetActive(true)` yourself. Going through
`SoundService.Play` handles this for you.

**Keep the pool bounded.** `SimplePool` has no built-in limit - it grows to the
high-water mark of simultaneously playing sounds and keeps them. For a busy
game that is fine; if you need a cap, count your own handlers and stop the
oldest (see `SimplePool.md` for the wrapper pattern).

**Looping sounds never return to the pool** until you stop them - an emitter
with `loop = true` stays active for as long as it runs. Make sure every looping
sound is stopped when it should end, or the pool will hold on to it forever.

**Check the handler.** `Play` returns `0` when the event key was not found, and
`_currentHandler` starts at `0` too, so `if (handler != 0)` guards against
silently doing nothing.

---

## Common Issues and Solutions

### Issue 1: Sound Not Playing, Error Are Firing in Console

**Solution**: Verify event exists in a collection and AudioResource is assigned

```csharp
public uint SafePlay(string eventName, Transform emitter)
{
    if (!_events.ContainsKey(eventName))
    {
        Debug.LogError($"Sound event '{eventName}' not found! Check your AudioEventsCollections.");
        return 0;
    }
    return Play(eventName, emitter);
}
```

### Issue 2: 3D Positioning Not Working, or Incorrect Volume

**Solution**: Configure AudioEvent correctly in your collection

```csharp
// In your AudioEventsCollection
new AudioEvent
{
    eventKey = "explosion",
    spatialBlend = 1f,      // Full 3D
    positionToListener = 0f, // At emitter
    rolloffMode = AudioRolloffMode.Linear,
    minDistance = 5f,
    maxDistance = 30f
};
```

### Issue 3: Sounds Overlapping

**Solution**: Stop previous instances

```csharp
public void PlayOneShot(string eventName, Transform emitter)
{
    // Stop any existing instances
    foreach (var e in GetEmitters(eventName))
    {
        if (e.transform.parent == emitter)
        {
            e.Stop();
        }
    }
    Play(eventName, emitter);
}
```

### Issue 4: Emitters Left Alive

`SoundService.Dispose()` is `override` and does **not** stop sounds - it only
unbinds `GlobalAudioSettings` and `ListenerCamera` from the DI container and
calls `base.Dispose()`. The `_emitters` pool is private, so the documented
"fix" would not compile even if it were inside `SoundService`:

```csharp
// WRONG - _emitters is private, and Dispose does not call StopAll()
public override void Dispose()
{
    StopAll();
    _emitters.Clear();   // CS0122
    base.Dispose();
}
```

What to do instead: call `StopAll()` yourself before disposing, and clear the
pool through the public `Clear()` if you hold a reference to it.

```csharp
// On your own component, when tearing down
_soundService.StopAll();          // public, safe
_soundService.Dispose();          // unbinds DI
```

`StopAll()` also does not free memory permanently - it stops the sounds, and
`SimplePool.Clear()` is what destroys the pooled GameObjects. Both are
reversible: stopped emitters return to the pool for reuse.

### Issue 5: Fading Not Working

**Solution**: Ensure minimum duration and kill existing tweens

```csharp
public void SafeFadeOut(uint handler, float duration)
{
    if (duration < 0.1f) duration = 0.1f;

    var emitter = GetEmitter(handler);
    if (emitter != null)
    {
        DOTween.Kill(emitter.Source);      // kill conflicting tweens
        // Two arguments on purpose - see the overload note below
        emitter.FadeOut(duration, 0f);
    }
}
```

**`AudioEmitter` has two `FadeOut` overloads, and the short one bites:**

| Call                       | Binds to                | Meaning                        |
| -------------------------- | ----------------------- | ------------------------------ |
| `FadeOut(duration, delay)` | two-argument overload   | fade for `duration`, after `delay` |
| `FadeOut(delay = 0f)`      | one-argument overload   | wait `delay`, then use the event's fade duration |

`emitter.FadeOut(duration)` binds to the **delay** overload, so it waits and
then fades with the default duration. `SoundService.FadeOut` used to have this
exact bug; always pass both arguments when you mean the duration.

---

## Best Practices

### 1. Organize Audio Events in Collections by Category

```csharp
// Create separate collections for different sound types
// - SFX_Collection.asset (all sound effects)
// - Music_Collection.asset (all background music)
// - UI_Collection.asset (all UI sounds)

public static class AudioKeys
{
    public static class Sfx
    {
        public const string PlayerShoot = "player_shoot";
        public const string PlayerJump = "player_jump";
        public const string PlayerHit = "player_hit";
    }

    public static class Music
    {
        public const string MainTheme = "music_main";
        public const string BattleTheme = "music_battle";
    }

    public static class Ui
    {
        public const string ButtonClick = "ui_click";
        public const string ButtonHover = "ui_hover";
        public const string Error = "ui_error";
    }
}

// Usage
soundService.Play(AudioKeys.Sfx.PlayerShoot, transform);
```

### 2. Configure 3D Sounds Properly in Collections

```csharp
// In your SFX_Collection.asset:
// 3D explosion
new AudioEvent
{
    eventKey = "explosion",
    spatialBlend = 1f,
    positionToListener = 0f,
    rolloffMode = AudioRolloffMode.Logarithmic,
    minDistance = 5f,
    maxDistance = 30f
};

// In your UI_Collection.asset:
// 2D UI sound
new AudioEvent
{
    eventKey = "ui_click",
    spatialBlend = 0f,
    positionToListener = 1f
};

// In your Music_Collection.asset:
// Ambient music
new AudioEvent
{
    eventKey = "ambient_music",
    spatialBlend = 0f,
    positionToListener = 1f,
    loop = true
};
```

### 3. Use Fading for Music

```csharp
public class MusicManager : BehaviourBase
{
    [Inject] private SoundService _soundService;

    private uint _currentMusicHandler;

    public void PlayMusic(string musicKey, float fadeDuration = 2f)
    {
        if (_currentMusicHandler != 0)
        {
            _soundService.FadeOut(_currentMusicHandler, fadeDuration);
        }

        _currentMusicHandler = _soundService.FadeIn(musicKey, null, fadeDuration);
    }

    public void StopMusic(float fadeDuration = 2f)
    {
        if (_currentMusicHandler != 0)
        {
            _soundService.FadeOut(_currentMusicHandler, fadeDuration);
            _currentMusicHandler = 0;
        }
    }
}
```

### 4. Handle Sound Completion

```csharp
public void PlayOneShotWithCallback(string eventName, Transform emitter, Action onComplete)
{
    _soundService.Play(eventName, emitter, 0f, onComplete);
}

// Example: Destroy object after explosion sound
void ExplodeAndDestroy()
{
    _soundService.Play("explosion", transform, 0f, () => {
        Destroy(gameObject);
    });
}
```

### 5. Pool Management

```csharp
public class AudioPoolMonitor : BehaviourBase
// Monitor pool usage.
// GetEmitters(eventKey) filters emitters by their event key, so an empty
// string matches NOTHING and always reports 0. There is no "all emitters"
// overload - track the handlers you started instead.
public class AudioMonitor : BehaviourBase
{
    [Inject] private SoundService _soundService;
    private readonly List<uint> _tracked = new();

    public uint Play(string eventKey)
    {
        var handler = _soundService.Play(eventKey, transform);
        if (handler != 0) _tracked.Add(handler);
        return handler;
    }

    private void Update()
    {
        if (Time.frameCount % 300 != 0) return;

        int stillPlaying = _tracked.Count(h => _soundService.IsPlaying(h));
        Debug.Log($"Tracked sounds still playing: {stillPlaying}");
        _tracked.RemoveAll(h => !_soundService.IsPlaying(h));
    }
}
```

A handler of `0` means the event key was not found - check for it before
adding to the list.

### 6. Dynamic Audio Parameters

```csharp
public class EngineSound : BehaviourBase
{
    [Inject] private SoundService _soundService;
    private uint _engineHandler;

    public void StartEngine()
    {
        _engineHandler = _soundService.Play("engine_loop", transform);
    }

    public void UpdateEnginePitch(float rpm)
    {
        var emitter = _soundService.GetEmitter(_engineHandler);
        if (emitter != null)
        {
            // Map RPM (0-1) to pitch (0.8-2.0)
            emitter.Source.pitch = Mathf.Lerp(0.8f, 2.0f, rpm);
        }
    }
}
```

### 7. Validate Audio Collections

```csharp
#if UNITY_EDITOR
public static void ValidateAudioCollection(AudioEventsCollection collection)
{
    foreach (var evt in collection.audioEvents)
    {
        if (string.IsNullOrEmpty(evt.eventKey))
            Debug.LogError($"Empty event key in {collection.name}");

        if (evt.audioResource == null)
            Debug.LogError($"Missing AudioResource for {evt.eventKey} in {collection.name}");
    }

    // Check for duplicate keys
    var duplicates = collection.audioEvents
        .GroupBy(e => e.eventKey)
        .Where(g => g.Count() > 1);

    foreach (var dup in duplicates)
    {
        Debug.LogError($"Duplicate event key '{dup.Key}' in {collection.name}");
    }
}
#endif
```

### 8. Volume Control by Category

First, why the obvious code does not work: `GetEmitters(eventKey)` filters by
an **exact** key match and there is no "give me every emitter" overload, so
`GetEmitters("")` always yields an empty sequence. A loop over it can never do
anything, no matter how the condition inside is written.

Writing `emitter.Source.volume` directly is also a poor fit - it overwrites the
volume already stored in the `AudioEvent`, and you need to know every key.

**Recommended: an AudioMixerGroup per category.** Note that the Sample project
ships **without** an `.mixer` asset, so you create one yourself:

1. Create an AudioMixer asset, add a group per category (Music, SFX, UI).
2. Expose a Volume parameter on each group.
3. Assign the group to each event's **`output`** field (`AudioEvent.output` is
   an `AudioMixerGroup`, and `AudioEmitter` applies it to the source when the
   sound starts).

```csharp
using UnityEngine.Audio;

public class AudioSettings : BehaviourBase
{
    [SerializeField] private AudioMixer _mixer;

    public void SetMusicVolume(float linear01) => _mixer.SetFloat("MusicVol", LinearToDb(linear01));
    public void SetSFXVolume(float linear01)   => _mixer.SetFloat("SfxVol", LinearToDb(linear01));
    public void SetUIVolume(float linear01)    => _mixer.SetFloat("UiVol", LinearToDb(linear01));

    private static float LinearToDb(float linear)
        => linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
}
```

**Without a mixer**, keep your own registry of the handlers you started and act
on those. `GetEmitters` works fine as long as you pass a real key:

```csharp
public class CategoryVolume : BehaviourBase
{
    [Inject] private SoundService _soundService;
    private readonly Dictionary<string, List<uint>> _byCategory = new();

    public uint Play(string eventKey, string category)
    {
        var handler = _soundService.Play(eventKey, transform);
        if (handler == 0) return handler; // key not found

        if (!_byCategory.TryGetValue(category, out var list))
            _byCategory[category] = list = new List<uint>();
        list.Add(handler);
        return handler;
    }

    public void SetCategoryVolume(string category, float volume01)
    {
        if (!_byCategory.TryGetValue(category, out var handlers)) return;

        foreach (var handler in handlers)
        {
            var emitter = _soundService.GetEmitter(handler);
            if (emitter == null) continue;

            // AudioSource.volume is linear 0..1, so scaling it directly is fine
            emitter.Source.volume = emitter.Source.volume * volume01;
        }
    }
}
```

---

## Examples

### Complete Audio System Setup

```csharp
// 1. Create your AudioEventsCollection assets in the Editor
// Assets/Audio/Collections/SFX_Collection.asset
// Assets/Audio/Collections/Music_Collection.asset
// Assets/Audio/Collections/UI_Collection.asset

// 2. Installer
[DefaultExecutionOrder(-10000)]
public class AudioInstaller : InstallerBehaviourBase
{
    [SerializeField] private GlobalAudioSettings _audioSettings;
    [SerializeField] private List<AudioEventsCollection> _audioCollections;
    [SerializeField] private Camera _audioListenerCamera;

    protected override void Install()
    {
        var soundService = new SoundService(
            _audioSettings,
            _audioCollections,
            _audioListenerCamera
        );

        BindAsSingleton(soundService);
    }
}

// 3. Audio Manager
public class AudioManager : BehaviourBase
{
    [Inject] private SoundService _soundService;

    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private Dictionary<string, uint> _loopingSounds = new();

    private void Start()
    {
        ApplyVolumes();
        PlayMusic("music_main");
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = masterVolume;
    }

    public void PlayMusic(string musicKey)
    {
        if (_loopingSounds.ContainsKey("music"))
        {
            _soundService.FadeOut(_loopingSounds["music"], 1f);
        }

        uint handler = _soundService.FadeIn(musicKey, null, 2f);
        _loopingSounds["music"] = handler;
    }

    public void PlaySFX(string eventName, Transform source = null)
    {
        _soundService.Play(eventName, source ?? transform);
    }

    public void PlayOneShot(string eventName, Vector3 position)
    {
        var tempGO = new GameObject("TempAudioSource");
        tempGO.transform.position = position;
        _soundService.Play(eventName, tempGO.transform, 0f, () => {
            Destroy(tempGO);
        });
    }
}

// 4. Player Audio
public class PlayerAudio : BehaviourBase
{
    [Inject] private AudioManager _audio;

    public void Shoot()
    {
        _audio.PlaySFX("player_shoot", transform);
    }

    public void Jump()
    {
        _audio.PlaySFX("player_jump", transform);
    }

    public void TakeDamage()
    {
        _audio.PlaySFX("player_hit", transform);
    }

    public void Die()
    {
        _audio.PlayOneShot("player_death", transform.position);
    }
}

// 5. Enemy Audio
public class EnemyAudio : BehaviourBase
{
    [Inject] private AudioManager _audio;

    public void OnSpawn()
    {
        _audio.PlaySFX("enemy_spawn", transform);
    }

    public void OnDamage()
    {
        _audio.PlaySFX("enemy_hit", transform.position);
    }

    public void OnDeath()
    {
        _audio.PlayOneShot("enemy_death", transform.position);
    }
}

// 6. UI Audio
public class UIAudio : BehaviourBase
{
    [Inject] private AudioManager _audio;

    public void OnButtonClick()
    {
        _audio.PlaySFX("ui_click");
    }

    public void OnButtonHover()
    {
        _audio.PlaySFX("ui_hover");
    }

    public void OnError()
    {
        _audio.PlaySFX("ui_error");
    }
}
```

### 3D Sound Example

```csharp
public class Explosion : BehaviourBase
{
    [Inject] private AudioManager _audio;

    public void Explode()
    {
        // Play 3D explosion at this position
        _audio.PlayOneShot("explosion", transform.position);

        // Camera shake effect
        StartCoroutine(ShakeCamera());
    }
}

// In SFX_Collection.asset, configure:
// eventKey: "explosion"
// spatialBlend: 1
// positionToListener: 0
// minDistance: 5
// maxDistance: 50
// rolloffMode: Logarithmic
```

### Background Music with Crossfade

```csharp
public class BackgroundMusic : BehaviourBase
{
    [Inject] private SoundService _soundService;

    private uint _currentTrack;
    private string _currentTrackName;

    public void CrossfadeTo(string newTrack, float fadeDuration = 3f)
    {
        if (!string.IsNullOrEmpty(_currentTrackName))
        {
            _soundService.FadeOut(_currentTrack, fadeDuration);
        }

        _currentTrack = _soundService.FadeIn(newTrack, null, fadeDuration);
        _currentTrackName = newTrack;
    }

    public void SetVolume(float volume)
    {
        var emitter = _soundService.GetEmitter(_currentTrack);
        if (emitter != null)
        {
            emitter.Source.volume = volume;
        }
    }
}

// In Music_Collection.asset:
// eventKey: "music_main" (looping, 2D)
// eventKey: "music_battle" (looping, 2D)
```

---

## Summary

### Architecture Diagram Recap

```
┌─────────────────────────────────────────────────────────────┐
│                      SoundService                           │
├─────────────────────────────────────────────────────────────┤
│ - Dictionary<string, AudioEvent> _events  ←─ Loaded from    │
│ - SimplePool<AudioEmitter> _emitters          Collections   │
│ - ListenerCamera _camera                                    │
└─────────────────────────────────────────────────────────────┘
         │                          │
         │ Plays using              │ Manages pool of
         │ event keys               │ AudioEmitters
         ▼                          ▼
┌─────────────────┐        ┌─────────────────┐
│  AudioEvent     │        │  AudioEmitter   │
│(from Collection)│        │   (Instance)    │
└─────────────────┘        └─────────────────┘
```

### Method Summary

| Method                                 | Description                   | Example                                   |
| -------------------------------------- | ----------------------------- | ----------------------------------------- |
| `Play(eventName, emitter)`             | Play a sound                  | `Play("explosion", transform)`            |
| `FadeIn(eventName, emitter, duration)` | Fade in a sound               | `FadeIn("music", null, 2f)`               |
| `Stop(handler)`                        | Stop a specific sound         | `Stop(musicHandler)`                      |
| `FadeOut(handler, duration)`           | Fade out a sound              | `FadeOut(musicHandler, 2f)`               |
| `GetEmitter(handler)`                  | Get emitter by handler        | `GetEmitter(handler).Source.pitch = 1.5f` |
| `GetEmitters(eventKey)`                | Get all emitters for an event | `GetEmitters("explosion").Count()`        |

### Key Points

| #   | Key Point                                                                 | Why It Matters                                         |
| --- | ------------------------------------------------------------------------- | ------------------------------------------------------ |
| 1   | **AudioEvents are configured in AudioEventsCollection ScriptableObjects** | Organizes sounds by category, easy to manage in Editor |
| 2   | **Each AudioEvent has a unique eventKey**                                 | Used to play sounds throughout your code               |
| 3   | **AudioEmitters are automatically pooled**                                | Reduces garbage collection and improves performance    |
| 4   | **Each play call returns a unique handler**                               | Allows individual control of each sound instance       |
| 5   | **Position blending (0-1) between emitter and camera**                    | Flexible 3D audio positioning                          |
| 6   | **FadeIn/FadeOut methods**                                                | Smooth volume transitions                              |
| 7   | **Multiple AudioEventsCollections can be loaded**                         | Organize sounds by category (SFX, Music, UI)           |
| 8   | **Callback on sound completion**              | Chain actions after sounds finish                      |

### When to Use SoundService

| Use SoundService         | Consider Wwise/FMOD            |
| ------------------------ | ------------------------------ |
| Small to medium projects | Large AAA titles               |
| Prototyping              | Complex audio routing          |
| Mobile games             | Advanced DSP effects           |
| Simple 2D/3D audio needs | Multi-platform audio profiling |
| Quick implementation     | Dedicated audio team           |

The SoundService provides a robust, easy-to-use audio system that integrates seamlessly with the CherryFramework, giving you professional-quality audio without the complexity of full-featured audio middleware.
