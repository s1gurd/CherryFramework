# Sample Game Scene Structure: `dinoscene.unity`

## Overview

The `dinoscene.unity` scene is the main gameplay scene for the CherryFramework sample endless runner game. It demonstrates the integration of all framework systems in a complete, playable game. The scene is organized into logical groups of GameObjects that work together to create the game experience.

**Scene Location**: `Assets/Sample/Scenes/dinoscene.unity`

---

## Scene Hierarchy

```
📦 dinoscene (Root)
├── 📷 Main Camera
├── 🌍 Ground
├── 👤 Player
├── 🎮 GameRoot (Empty GameObject with GameInstaller)
├── 🎲 Spawner
├── 🖥️ Canvas (UI)
│   ├── 🎯 HUD (WidgetBase)
│   │   ├── 🔔 SpeedUpNotification (WidgetElement)
│   │   ├── 🚀 PowerUpNotification (WidgetElement)
│   │   ├── 🔘 JumpButton (WidgetElement)
│   │   └── 📊 UpperPart (WidgetElement)
│   │       ├── 📏 DistanceText (TMP)
│   │       ├── ⏱️ RunTimeText (TMP)
│   │       └── ⏸️ PauseButton
│   └── 🏠 UIRoot (RootPresenterBase - container for presenters)
└── 🎮 EventSystem
```

---

## Detailed Object Descriptions

### Core Game Objects

#### 📷 Main Camera

| Property       | Value           |
| -------------- | --------------- |
| **Position**   | (-0.86, 2, -10) |
| **Projection** | Orthographic    |
| **Size**       | 4               |
| **Tag**        | MainCamera      |

**Purpose**: Provides a fixed, orthographic view of the gameplay area. The camera is positioned to show the player, ground, and spawning area. It includes an AudioListener for 3D audio positioning.

#### 🌍 Ground

| Component       | Purpose                                                         |
| --------------- | --------------------------------------------------------------- |
| `Transform`     | Position (0,0,0), Scale (25,1,1)                                |
| `BoxCollider`   | Provides physics collision for player landing                   |
| `MeshRenderer`  | Displays ground texture with scrolling material                 |
| `MeshFilter`    | Basic cube mesh                                                 |
| `Ground` script | Handles texture scrolling, distance tracking, and time tracking |

**Purpose**: The endless runner surface. The ground scrolls to create the illusion of movement. It also tracks:

- Distance traveled (updates GameState model)
- Run time (updates GameState model)
- Statistics (updates GameStatistics model)

#### 👤 Player

| Component               | Purpose                                                                |
| ----------------------- | ---------------------------------------------------------------------- |
| `Transform`             | Position (-6,0,0)                                                      |
| `SpriteRenderer`        | Player visual                                                          |
| `CharacterController`   | Physics-based movement                                                 |
| `Player` script         | Main player logic (jumping, input handling)                            |
| `AnimatedSprite` script | Sprite animation                                                       |
| `PersistentObject`      | Save system integration (GUID: `7abb6373-7d8c-43dc-9c12-6c6114f78bba`) |

**Purpose**: The player character. Features:

- Jump mechanics with physics
- Sprite animation
- Save/load of jump state
- Integration with input system
- Collision detection with obstacles

#### 🎲 Spawner

| Component          | Purpose                                                                |
| ------------------ | ---------------------------------------------------------------------- |
| `Transform`        | Position (8.18,0,0)                                                    |
| `Spawner` script   | Main spawning logic                                                    |
| `PersistentObject` | Save system integration (GUID: `58f6ad04-11e7-41f9-86cf-a87ef00b0a3b`) |

**Purpose**: Manages obstacle spawning:

- Uses object pooling (`SimplePool<PersistentObject>`)
- Spawns random obstacles based on weighted chances
- Tracks spawned objects for save/load
- Responds to game state (spawns only when game is running)

#### 🎮 GameRoot

| Component              | Purpose                    |
| ---------------------- | -------------------------- |
| `GameInstaller` script | DI container configuration |

**Purpose**: Empty GameObject that holds the main installer. This is where all dependencies are registered at startup:

- `Ticker` (update manager)
- `StateService` (event system)
- `SaveGameManager` (persistence)
- `ModelService` (data models)
- `SoundService` (audio)
- `ViewService` (UI navigation)
- `InputSystem_Actions` (input)
- `GameSettings` (configuration)
- Camera reference
- `GameManager` (core game logic)

---

### UI System

#### 🖥️ Canvas

| Component          | Purpose                                         |
| ------------------ | ----------------------------------------------- |
| `Canvas`           | Root UI container                               |
| `CanvasScaler`     | Scales UI with screen size (reference: 800x600) |
| `GraphicRaycaster` | Handles UI input                                |

**Children**:

- `HUD` - In-game heads-up display
- `UIRoot` - Container for presenters (menus, screens)

#### 🎯 HUD (WidgetBase)

| Property   | Value                            |
| ---------- | -------------------------------- |
| **Type**   | `WidgetBase` (stateful UI)       |
| **States** | 2 states (0: hidden, 1: visible) |

**Purpose**: Main in-game UI that appears during gameplay. The widget switches between states based on game running status.

**Contains**:

- `SpeedUpNotification` - Shows when game speed increases
- `PowerUpNotification` - Shows when rocket power-up is collected
- `JumpButton` - On-screen jump button for touch controls
- `UpperPart` - Top bar with game stats

#### 🔔 SpeedUpNotification (WidgetElement)

| Components             | Purpose             |
| ---------------------- | ------------------- |
| `CanvasGroup`          | For fade animations |
| `UiFade`               | Fade animation      |
| `UiScale`              | Scale animation     |
| `WidgetElement` script | Show/hide logic     |

**Purpose**: Displays "Speeding up!" message when game speed increases. Uses multiple animations (fade + scale) for visual effect.

#### 🚀 PowerUpNotification (WidgetElement)

| Components             | Purpose             |
| ---------------------- | ------------------- |
| `CanvasGroup`          | For fade animations |
| `UiSlide`              | Slide animation     |
| `UiFade`               | Fade animation      |
| `UiScale`              | Scale animation     |
| `WidgetElement` script | Show/hide logic     |

**Purpose**: Displays "Rocket Jump!!!" message when rocket power-up is collected. Uses three simultaneous animations for dramatic effect.

#### 🔘 JumpButton (WidgetElement)

| Components              | Purpose                      |
| ----------------------- | ---------------------------- |
| `Image`                 | Button background            |
| `Button` with `UIPress` | Input handling for space key |
| `UiSlide`               | Slide animation              |
| `WidgetElement` script  | Show/hide logic              |

**Purpose**: On-screen jump button. Also responds to spacebar input. Only visible when game is running.

#### 📊 UpperPart (WidgetElement)

| Components             | Purpose                |
| ---------------------- | ---------------------- |
| `UiSlide`              | Slide animation        |
| `WidgetElement` script | Show/hide logic        |
| `GameStatsHUD` script  | Updates stats displays |

**Contains**:

- `DistanceText` - Shows current distance
- `RunTimeText` - Shows current run time
- `PauseButton` - Pause menu button

**Purpose**: Top bar with game statistics. Slides in/out with game state.

##### 📏 DistanceText (TMP)

| Text        | `Distance traveled: {0} units`                     |
| ----------- | -------------------------------------------------- |
| **Binding** | Updates from `GameStateDataModel.DistanceTraveled` |

##### ⏱️ RunTimeText (TMP)

| Text        | `Run time: {0} sec`                       |
| ----------- | ----------------------------------------- |
| **Binding** | Updates from `GameStateDataModel.RunTime` |

##### ⏸️ PauseButton

| Components | Purpose             |
| ---------- | ------------------- |
| `Image`    | Button background   |
| `Button`   | Click handling      |
| `UIPress`  | Escape key handling |

**Purpose**: Opens pause menu. Also responds to Escape key.

#### 🏠 UIRoot (RootPresenterBase)

| Component                  | Purpose                          |
| -------------------------- | -------------------------------- |
| `RootPresenterBase` script | Root UI container for navigation |
| `Canvas`                   | Separate canvas for presenters   |

**Purpose**: Container for all presenter-based UI screens:

- Main menu
- Pause menu
- Game over screen
- Statistics screen
- Loading screen
- Error screen

These presenters are loaded as prefabs and instantiated when needed.

---

### System Objects

#### 🎮 EventSystem

| Component                  | Purpose                           |
| -------------------------- | --------------------------------- |
| `EventSystem`              | Unity UI event system             |
| `InputSystemUIInputModule` | Input module for new Input System |

**Purpose**: Required for UI interaction. Handles input routing to UI elements.

---

## Scene Statistics

Counted from `dinoscene.unity` itself (20 `GameObject` blocks, 36
`MonoBehaviour` blocks).

| Category                | Count                       |
| ----------------------- | --------------------------- |
| GameObjects             | 20                          |
| MonoBehaviour components| 36                          |
| PersistentObjects       | 2 (Player, Spawner)         |

---

## Key Scene Features

### 1. **Framework Integration**

- All major CherryFramework systems are demonstrated
- DI container configured in `GameInstaller`
- Models generated from templates
- Save/load working across sessions

### 2. **UI Hierarchy**

```
Canvas (Root UI)
├── HUD (Widget - shows during gameplay)
│   └── Various UI elements with animations
└── UIRoot (Presenter container)
    └── Presenter prefabs (instantiated at runtime)
```

### 3. **Game Flow Objects**

- **Player**: Core character logic
- **Ground**: World scrolling and tracking
- **Spawner**: Obstacle generation
- **GameManager**: Overall game state (attached to GameRoot)

### 4. **Save System Integration**

Exactly **two** objects carry a `PersistentObject` component (verified by
counting the script reference in the scene file):

- **Player** — guid `7abb6373-7d8c-43dc-9c12-6c6114f78bba`,
  `saveTransform: 1` (its position is restored, so a quit mid-run resumes
  where you were)
- **Spawner** — guid `58f6ad04-11e7-41f9-86cf-a87ef00b0a3b`,
  `saveTransform: 0` (only its own state is saved, not its position)

`Ground` has **no** `PersistentObject`. It tracks distance and time through the
`GameStateData` data model instead, which is handled by `ModelService` rather
than by `SaveGameManager`.

Both objects are non-spawnable (`spawnableObject: 0`), so their ids are
`SceneId:{buildIndex}.{guid}`. That is why `dinoscene.unity` must stay in Build
Settings: remove it and the scene's `buildIndex` becomes `-1`, changing every
key this scene has ever written.

### 5. **Animation Showcase**

Multiple animation types demonstrated:

- `UiSlide` - Jump button, UpperPart
- `UiFade` - Notifications
- `UiScale` - Notifications
- Combined animations - PowerUpNotification uses three animators

### 6. **Input Handling**

- Keyboard: Space (jump), Escape (pause)
- On-screen buttons for touch
- Input System integration with action maps

---

## Scene Setup Summary

To recreate or modify this scene:

1. **Core objects** must be in place:
   
   - Camera (orthographic, size 4)
   - Ground (scaled 25x1)
   - Player (with CharacterController)
   - Spawner (positioned at right edge)

2. **UI Canvas** needs:
   
   - Canvas Scaler (800x600 reference)
   - HUD widget with states
   - UIRoot presenter container

3. **Installers**:
   
   - `GameInstaller` on GameRoot object
   - Configure all settings assets

4. **EventSystem** with InputSystemUIInputModule

5. **Build Settings** must include this scene - the `PersistentObject` ids are `SceneId:{buildIndex}.{guid}`, so without it every key this scene has saved becomes unreachable.

The scene demonstrates a complete, functional game that showcases all major CherryFramework features in an integrated, playable experience.
