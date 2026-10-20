# CherryFramework UI and sound tests — Design

**Date:** 2026-10-20
**Status:** approved
**Branch:** `feature/framework-tests` (continues the test suite work)
**Scope:** automated tests for the UI navigation, the sound service and the
lifecycle of the UI elements.

---

## Problem

The test suite covers the dependency container, the state service, the save
system, the data models, the utilities and the object pool. The UI system
and the sound system have no tests at all, and they are exactly the parts an
assistant is most likely to break: both are built on MonoBehaviours, both
keep their state in serialized fields, and both fail at runtime rather than
at compile time when the wiring is wrong.

## Goals

- The navigation contracts of `ViewService` are pinned: the history, the
  active view, the modal block, the error paths.
- The contracts of `SoundService` are pinned: the event lookup, the handler
  bookkeeping, stopping and fading.
- The lifecycle of the UI elements is pinned: the presenter initialises
  itself, the widget switches its states.
- Every fixture is built in code. The tests do not depend on the sample game
  and keep working when it changes.
- The tests stay fast and deterministic. No real audio asset, no prefab, no
  scene.

## Non-goals

- The UI animations (`UiFade`, `UiScale`, `UiSlide`, `UiTextFade`). They are
  driven by DOTween and their timing is not a contract worth pinning.
- The playback internals of `AudioEmitter`.
- The error screen, which needs a `TMP_Text` and would drag the text mesh
  pro resources into the test.
- The sample game as a fixture.

## Design

### 1. Where the tests live

The UI and the sound need real MonoBehaviour lifecycle: `Awake`, `OnEnable`
and `Start` do not run in edit mode, and that lifecycle is the thing under
test. Both suites therefore live in the play mode assembly,
`Assets/Tests/PlayMode/`.

| File | Covers |
| --- | --- |
| `ViewServiceTests.cs` | navigation, history, modals, error paths |
| `SoundServiceTests.cs` | event lookup, handlers, stop, fade |
| `UiElementLifecycleTests.cs` | presenter initialisation, widget states |
| `PopulatorTests.cs` | dynamic lists and their pooling |

### 2. The test doubles

They live in `Cherry.Framework.TestSupport`, next to the other fakes, so both
test assemblies can use them.

| Double | Purpose |
| --- | --- |
| `TestPresenter` | a plain presenter |
| `TestModalPresenter` | a presenter with `Modal` on |
| `TestPopupPresenter` | a presenter that implements `IPopUp` |
| `TestLoadingPresenter` | the loading screen of the root presenter |
| `TestWidget` | a widget with a controllable state list |
| `TestPopulatorElement<T>` | an element of a dynamic list |
| `TestPopulator<T>` | a dynamic list |
| `UiTestHierarchy` | builds the presenter hierarchy and fills the private serialized fields |

`UiTestHierarchy` builds a root presenter with a `Canvas` children container
and the child presenters registered in it. The private serialized fields
(`childrenContainer`, `childPresenters`, `loadingScreen`, `errorScreen`,
`widgetStates`) are written through `SerializedObject`, which the play mode
assembly can use because it already references `UnityEditor.TestRunner`.

`animators` is `protected` on `InteractiveElementBase`. Every double sets it
to an empty list in its constructor, because `CreateSequence` walks that list
and a null one would throw.

### 3. The contracts

**`ViewService`**

- `PopView` sets the active view and marks a view active.
- `Back` returns to the previous view, and out of the last view it clears
  the active view.
- `OnAnyViewBecameActive` fires for the first view, `OnAllViewsBecameInactive`
  fires when the last view is left.
- A modal view blocks both `PopView` and `Back`.
- A view that is not registered in the container is refused with an error.
- `PopView` with an unknown type name is refused with an error. The lookup
  searches the framework assembly only, so a test presenter is never found by
  name - that path is an error path.
- `ClearHistory` keeps the current view and drops the rest.
- `HideAndReset` clears the active view.
- `PopLoadingView` makes the loading screen the active view.

**`SoundService`**

- `Play` with an unknown event name logs an error and returns handler zero.
- `Play` with a known event name returns a non zero handler, and the emitter
  is found by that handler and by the event key.
- `Stop` stops the emitter, and refuses an unknown handler with an error.
- `FadeOut` of a known handler does not throw, an unknown one logs an error.
- `StopAll` leaves no active emitters.
- The constructor binds the global audio settings and the listener camera
  into the container, and `Dispose` removes them again.

**UI elements**

- A presenter initialises itself when it is enabled, and calls
  `OnPresenterInitialized` once.
- A presenter whose children container is not its own child throws.
- A widget initialises on enable with the first state as the current one.
- `SetState` by index and by name both switch the state and raise the state
  change events.
- `SetState` with an out of range index and with an unknown name log errors.

**`PopulatorBase`**

- `UpdateElements` creates one element per item.
- `UpdateElements` with fewer items releases the surplus elements.
- `Clear` empties the pool.

### 4. Isolation

Every fixture is destroyed in `[TearDown]`. The dependency container is
cleaned up the same way the other suites do it. The sound service is disposed
so that it removes its own bindings.

## Verification

- The whole suite passes, edit mode and play mode.
- A deliberate break in `ViewService` turns exactly one test red.
