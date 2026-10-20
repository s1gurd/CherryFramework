# CherryFramework framework tests — Design

**Date:** 2026-10-20
**Status:** approved
**Branch:** `feature/framework-tests`
**Scope:** an automated test suite for the CherryFramework runtime, built to
give an AI assistant a fast, unambiguous signal about whether the framework
still behaves as documented.

---

## Problem

CherryFramework has no automated tests. The README says so plainly, and
until now it was true of the whole repository: a clean compile was as far as
the framework took you. The only suite that exists covers the dependency
installer that was added alongside this work, and it covers the installer,
not the framework.

Two consequences:

1. A change to the runtime systems - the dependency container, the state
   service, the save system, the data models - cannot be checked
   automatically. Only a human playing the sample will notice a regression.
2. An AI assistant working on the framework has no way to verify its work.
   It writes code, waits for a compile, and hopes. The framework's own
   README already flags this as the weak point of AI-assisted development
   here.

## Goals

- Every runtime system that can be tested without a scene has a test suite.
- The suite runs in seconds and produces a result an agent can read without
  interpreting prose.
- The tests are isolated from each other, so a failure always points at one
  behaviour.
- Running the suite needs one command, and an agent needs to know that
  command without asking.
- A bug found while writing the tests is fixed in the same branch, with a
  regression test pinning the fix.

## Non-goals

- Testing the UI system (`PresenterBase`, `WidgetBase`, `ViewService`) or the
  sound system. Both need prefabs, scenes and audio assets; the tests would
  be brittle and slow for little gain.
- Testing the code generator - it is a tool, not runtime behaviour.
- Shipping the tests to users of the framework. The tests live in the
  repository only; the distributed framework stays lean.
- Replacing the sample as a demonstration of the framework.

## Design

### 1. Layout

```
Assets/Tests/
├── Editor/                              already exists - installer tests
├── Framework/                           new - EditMode tests of the runtime
│   ├── Cherry.Framework.Tests.asmdef
│   ├── Fakes/
│   │   ├── FakePlayerPrefs.cs
│   │   ├── TestDataModel.cs
│   │   └── TestGameSaveData.cs
│   ├── DependencyContainerTests.cs
│   ├── StateServiceTests.cs
│   ├── SaveGameManagerTests.cs
│   ├── DataModelTests.cs
│   ├── UtilsTests.cs
│   └── SimplePoolTests.cs
└── PlayMode/                            new - MonoBehaviour lifecycle
    ├── Cherry.Framework.PlayMode.Tests.asmdef
    ├── InstallerBehaviourTests.cs
    ├── InjectMonoBehaviourTests.cs
    ├── TickerBehaviourTests.cs
    └── PersistentObjectTests.cs
```

`Assets/Tests/Editor/Framework/` carries its own assembly definition, so it
becomes a separate assembly from the installer tests. `Assets/Tests/PlayMode/`
is deliberately not under an `Editor` folder - play mode tests must run in a
player.

Both assemblies reference `Cherry.Framework` by name, the same way the
framework itself references its dependencies.

### 2. What gets tested

| System | Assembly | Coverage |
| --- | --- | --- |
| `DependencyContainer` | EditMode | singleton and transient binding, field and property injection, inherited fields, removal, disposal |
| `StateService` | EditMode | events and their lifetime, payload events, statuses, conditional subscriptions, one-shot subscriptions, unsubscription, error handling |
| `SaveGameManager` | EditMode | registration, save and load round trip, key format, slots, forced reset, deletion, bulk save |
| `DataModels` | EditMode | accessor values, downward bindings and their activation modes, value processors, singleton and transient models, storage round trip |
| `Utils` | EditMode | `DataUtils.CreateKey`, `StringUtils`, `MathUtils`, `EnumerableUtils` |
| `SimplePool` | EditMode | creation, reuse of inactive instances, transform assignment, active object listing, clearing |
| `InstallerBehaviourBase` | PlayMode | `Awake` installs the bindings, `OnDestroy` removes them |
| `InjectMonoBehaviour`, `BehaviourBase` | PlayMode | injection on enable, cleanup on destroy |
| `TickerBehaviour` | PlayMode | a registered tickable is ticked, unregistering stops it |
| `PersistentObject` | PlayMode | registration and load on start, save on destroy |

### 3. Isolation

The framework keeps state in statics and singletons. Without cleanup, one
test poisons the next, and a failure points at nothing in particular. Every
test class therefore cleans up after itself:

- `DependencyContainer.Instance` is a singleton. Bindings are removed in
  `[TearDown]`.
- `new StateService()` creates a `Ticker`, which creates a
  `GameObject("Ticker")`. The leftover behaviour is destroyed in
  `[TearDown]` through `FindObjectsByType<TickerBehaviour>()`.
- `PersistentObject` is a `MonoBehaviour` and needs a `GameObject`. The test
  creates one and destroys it in `[TearDown]`.
- Storage is faked. `FakePlayerPrefs` implements `IPlayerPrefs` with an
  in-memory dictionary, so the tests never touch the real `PlayerPrefs`.

### 4. Error handling in tests

The framework reports failures with `Debug.LogError`. The Unity Test
Framework fails any test that logs an unexpected error, so a test that
exercises an error path has to say what it expects:

```csharp
LogAssert.Expect(LogType.Error, new Regex("already registered"));
```

Tests that are not about an error path must not log one at all. This is
worth the effort: an unexpected error in a test is a real finding, not noise.

### 5. Test names as documentation

An assistant that reads the framework needs to learn the contract without
reading the implementation. Every test is named
`System_Behaviour_ExpectedResult` and asserts exactly one behaviour:

```
StateService_EmitEvent_SubscriptionRunsOnLateTick
SaveGameManager_SaveThenLoad_RestoresFieldValues
DependencyContainer_BindAsSingleton_InjectsSameInstance
```

The behaviour under test is taken from the matching document in `Docs/`, so
the test file and the documentation describe the same contract.

### 6. Three ways to run the suite

| Way | How |
| --- | --- |
| Command line | `Unity -batchmode -runTests -testPlatform EditMode -projectPath <path> -testResults results.xml` |
| Editor menu | `Tools -> CherryFramework -> Run All Tests` |
| Agent | `AGENTS.md` in the repository root |

The menu item uses `TestRunnerApi` to run the edit mode tests and then the
play mode tests, and prints one summary with the counts and the names of the
failures. It replaces the reflection-based runner that the installer tests
use today: `TestRunnerApi` supports `[UnityTest]`, play mode and the full
NUnit semantics, so one mechanism covers everything.

`AGENTS.md` states how to build the project, how to run the suite in both
modes, where the tests live, and the conventions a change has to follow.

### 7. The bug the tests find

`DependencyContainer.Dispose()` disposes the registered instances but leaves
the registrations in place:

```csharp
public void Dispose()
{
    foreach (var dep in _dependencies.Values)
    {
        DisposeInstance(dep.BindInstance);
    }
}
```

Two consequences. The container keeps handing out disposed instances after
`Dispose`. And the registrations survive, so a second binding of the same
type fails with "already installed". The framework never calls `Dispose` on
the container itself, so nothing in the shipped framework depends on the
current behaviour.

The fix clears `_dependencies` and `_disposedInstances` as well. A regression
test pins it: after `Dispose`, the container holds no dependencies.

## Repository changes

1. New `Assets/Tests/Framework/` and `Assets/Tests/PlayMode/` assemblies.
2. `DependencyContainer.Dispose()` clears the registry (bug fix).
3. `Assets/Tests/Editor/EditorTestRunner.cs` is removed; the `TestRunnerApi`
   based command replaces it.
4. `AGENTS.md` in the repository root.
5. `README.md` - the caveat about the missing tests is corrected.

## Verification

- The whole suite passes, edit mode and play mode.
- A deliberate break in the framework turns the matching test red, and only
  that test.
- `Unity -batchmode -runTests` reports the same result as the menu item.
