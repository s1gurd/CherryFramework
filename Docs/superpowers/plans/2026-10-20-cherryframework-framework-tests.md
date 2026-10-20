# CherryFramework Framework Tests — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** an automated test suite for the CherryFramework runtime, plus a documented way to run it, so an AI assistant can verify framework changes without a human playing the sample.

**Architecture:** two new test assemblies under `Assets/Tests/` - an edit mode assembly for the logic that needs no scene, and a play mode assembly for the MonoBehaviour lifecycle. The framework's statics and singletons are cleaned up in `[TearDown]` so tests stay isolated. A single editor command runs both assemblies through `TestRunnerApi` and prints one summary.

**Tech Stack:** Unity 6000.3.25f1, NUnit via `com.unity.test-framework` 1.6.0, `UnityEditor.TestTools.TestRunner.Api`.

**Spec:** `Docs/superpowers/specs/2026-10-20-cherryframework-framework-tests-design.md`

## Global Constraints

- Target Unity version is 6000.3.25f1.
- The tests live in the repository only. Nothing under `Assets/CherryFramework/` changes except the one bug fix in Task 2.
- Every test name is `System_Behaviour_ExpectedResult` and asserts exactly one behaviour.
- A test that exercises an error path must use `LogAssert.Expect`. A test that is not about an error path must not log an error at all - the Unity Test Framework fails a test on any unexpected `Debug.LogError`.
- No test may touch the real `PlayerPrefs`. Storage is faked with `FakePlayerPrefs`.
- No test may leave a `GameObject` behind. Everything created is destroyed in `[TearDown]`.
- The `DependencyContainer` is a singleton with static state. Every test that binds anything removes its bindings in `[TearDown]`.
- Test assemblies reference `Cherry.Framework` by assembly name.
- The fakes live in `Cherry.Framework.TestSupport`, an assembly with `includePlatforms: []`, because a play mode test assembly cannot reference an editor only one.

---

## File Structure

**New - shared test support** (`Assets/Tests/Shared/`):

| File | Responsibility |
| --- | --- |
| `Cherry.Framework.TestSupport.asmdef` | The fakes, usable from edit mode and play mode |
| `Fakes/FakePlayerPrefs.cs` | In-memory `IPlayerPrefs` |
| `Fakes/TestDataModel.cs` | A concrete `DataModelBase` with two members |
| `Fakes/TestGameSaveData.cs` | A plain `IGameSaveData` with `[SaveGameData]` members |
| `Fakes/TestGameObjects.cs` | Creates and destroys the GameObjects the tests need |

**New - edit mode tests** (`Assets/Tests/Framework/`):

| File | Responsibility |
| --- | --- |
| `Cherry.Framework.Tests.asmdef` | The edit mode test assembly |
| `TestRunnerCommand.cs` | `Tools -> CherryFramework -> Run All Tests` |
| `DependencyContainerTests.cs` | The container |
| `StateServiceTests.cs` | Events, statuses, subscriptions |
| `SaveGameManagerTests.cs` | Registration, save, load, delete |
| `DataModelTests.cs` | Accessors, bindings, processors, models, storage |
| `UtilsTests.cs` | The utility classes |
| `SimplePoolTests.cs` | The object pool |

**New - play mode tests** (`Assets/Tests/PlayMode/`):

| File | Responsibility |
| --- | --- |
| `Cherry.Framework.PlayMode.Tests.asmdef` | The play mode test assembly |
| `InstallerBehaviourTests.cs` | Install on awake, unbind on destroy |
| `InjectMonoBehaviourTests.cs` | Injection on enable, cleanup on destroy |
| `TickerBehaviourTests.cs` | Ticking and unregistering |
| `PersistentObjectTests.cs` | Register, load, save on destroy |

**Modified:**

| File | Change |
| --- | --- |
| `Assets/CherryFramework/DependencyManager/DependencyContainer.cs` | `Dispose` clears the registry (bug fix) |
| `Assets/Tests/Editor/EditorTestRunner.cs` | Deleted, replaced by `TestRunnerCommand` |
| `README.md` | The caveat about missing tests is corrected |
| `AGENTS.md` | New - how an agent builds and tests this project |

---

### Task 1: The test infrastructure

**Files:**
- Create: `Assets/Tests/Framework/Cherry.Framework.Tests.asmdef`
- Create: `Assets/Tests/Framework/TestRunnerCommand.cs`
- Create: `Assets/Tests/Framework/UtilsTests.cs` (a first, small test file - it proves the infrastructure works)
- Delete: `Assets/Tests/Editor/EditorTestRunner.cs`

**Interfaces:**
- Consumes: `com.unity.test-framework` 1.6.0, the existing installer tests.
- Produces:
  - `Cherry.Framework.Tests` - the edit mode test assembly.
  - `Cherry.Framework.Tests.TestRunnerCommand.RunAll()` - runs the edit mode tests and then the play mode tests, and logs one summary per run.
  - `Tools -> CherryFramework -> Run All Tests` - the menu item.

- [ ] **Step 1: Create the test support assembly, the fakes and the edit mode test assembly**

**a. The shared support assembly.** `Assets/Tests/Shared/Cherry.Framework.TestSupport.asmdef`:

```json
{
    "name": "Cherry.Framework.TestSupport",
    "rootNamespace": "",
    "references": [
        "Cherry.Framework"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`includePlatforms` is empty on purpose: a play mode test assembly cannot reference an editor only assembly, and both test assemblies need the fakes.

**b. The fakes.**

`Assets/Tests/Shared/Fakes/FakePlayerPrefs.cs`:

```csharp
using System.Collections.Generic;
using CherryFramework.Utils.PlayerPrefsWrapper;

namespace Cherry.Framework.Tests.Fakes
{
    /// <summary>
    /// Keeps the saved values in memory. The tests never touch the real
    /// PlayerPrefs, so they cannot leak into a real save file.
    /// </summary>
    public class FakePlayerPrefs : IPlayerPrefs
    {
        private readonly Dictionary<string, string> _values = new();

        public IReadOnlyDictionary<string, string> Values => _values;

        public void SetString(string key, string value) => _values[key] = value;
        public string GetString(string key) => _values.TryGetValue(key, out var value) ? value : null;
        public bool HasKey(string key) => _values.ContainsKey(key);
        public void DeleteKey(string key) => _values.Remove(key);
        public void DeleteAll() => _values.Clear();
        public void Save()
        {
        }
    }
}
```

`Assets/Tests/Shared/Fakes/TestGameSaveData.cs`:

```csharp
using CherryFramework.SaveGameManager;

namespace CherryFramework.Tests.Fakes
{
    public class TestGameSaveData : IGameSaveData
    {
        [SaveGameData] public int Health;
        [SaveGameData] public string Title = string.Empty;

        public int LoadCount { get; private set; }
        public int SaveCount { get; private set; }

        public void OnAfterLoad() => LoadCount++;
        public void OnAfterSave() => SaveCount++;
    }
}
```

`Assets/Tests/Shared/Fakes/TestGameObjects.cs`:

```csharp
using CherryFramework.SaveGameManager;
using UnityEngine;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// Creates the GameObjects the tests need and takes them down again.
    /// </summary>
    public static class TestGameObjects
    {
        public static PersistentObject CreatePersistentObject(string guid)
        {
            var gameObject = new GameObject("TestPersistentObject");
            var persistentObject = gameObject.AddComponent<PersistentObject>();
            persistentObject.guid = guid;

            return persistentObject;
        }

        public static void Destroy(GameObject gameObject)
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
```

`Assets/Tests/Shared/Fakes/TestDataModel.cs`:

```csharp
using System;
using CherryFramework.DataModels;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// A data model in the shape the code generator produces: a getter, a
    /// setter that sends the value downwards, and an accessor.
    /// </summary>
    public class TestDataModel : DataModelBase
    {
        private int _health = 100;
        private string _title = "untitled";

        public TestDataModel()
        {
            Getters.Add(nameof(Health), new Func<int>(() => Health));
            Setters.Add(nameof(Health), new Action<int>(value => Health = value));
            HealthAccessor = new Accessor<int>(this, nameof(Health));

            Getters.Add(nameof(Title), new Func<string>(() => Title));
            Setters.Add(nameof(Title), new Action<string>(value => Title = value));
            TitleAccessor = new Accessor<string>(this, nameof(Title));
        }

        public int Health
        {
            get => _health;
            set
            {
                _health = value;
                Send(nameof(Health), value);
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                Send(nameof(Title), value);
            }
        }

        public Accessor<int> HealthAccessor;
        public Accessor<string> TitleAccessor;
    }
}
```

**c. The edit mode test assembly.** `Assets/Tests/Framework/Cherry.Framework.Tests.asmdef`:

```json
{
    "name": "Cherry.Framework.Tests",
    "rootNamespace": "",
    "references": [
        "Cherry.Framework",
        "Cherry.Framework.TestSupport",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the first test file**

`Assets/Tests/Framework/UtilsTests.cs`:

```csharp
using CherryFramework.Utils;
using NUnit.Framework;

namespace Cherry.Framework.Tests
{
    public class UtilsTests
    {
        [Test]
        public void DataUtils_CreateKey_JoinsTheValuesWithADash()
        {
            Assert.AreEqual("a-b-c", DataUtils.CreateKey("a", "b", "c"));
        }

        [Test]
        public void DataUtils_CreateKey_SkipsTheEmptyValues()
        {
            Assert.AreEqual("a-b", DataUtils.CreateKey("a", string.Empty, "b"));
        }
    }
}
```

- [ ] **Step 3: Delete the reflection based runner**

Delete `Assets/Tests/Editor/EditorTestRunner.cs`. The `TestRunnerApi` command in step 4 replaces it.

- [ ] **Step 4: Write the test runner command**

`Assets/Tests/Framework/TestRunnerCommand.cs`:

```csharp
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Cherry.Framework.Tests
{
    /// <summary>
    /// Runs the whole suite and prints a summary per run. The Test Runner
    /// window is the proper way to run the tests; this is the shortcut for an
    /// agent that wants one command and one log.
    /// </summary>
    public static class TestRunnerCommand
    {
        private const string MenuPath = "Tools/CherryFramework/Run All Tests";

        [MenuItem(MenuPath, false, 50)]
        public static void RunAll()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new RunnerCallbacks(api));
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        private class RunnerCallbacks : ICallbacks
        {
            private readonly TestRunnerApi _api;
            private bool _playModeStarted;

            public RunnerCallbacks(TestRunnerApi api)
            {
                _api = api;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus != TestStatus.Failed)
                {
                    return;
                }

                Debug.Log("[CherryFramework] FAILED " + result.FullName + "\n" + result.Message);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Debug.Log("[CherryFramework] " + Describe(result));

                if (_playModeStarted)
                {
                    Debug.Log("[CherryFramework] The test run is finished.");
                    return;
                }

                _playModeStarted = true;
                _api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode }));
            }

            private static string Describe(ITestResultAdaptor result)
            {
                var builder = new StringBuilder();

                builder.Append(result.Name);
                builder.Append(" - passed: ").Append(result.PassCount);
                builder.Append(", failed: ").Append(result.FailCount);
                builder.Append(", skipped: ").Append(result.SkipCount);
                builder.Append(", inconclusive: ").Append(result.InconclusiveCount);
                builder.Append(", total: ").Append(result.TestCount);

                return builder.ToString();
            }
        }
    }
}
```

- [ ] **Step 5: Refresh Unity and check for compile errors**

Run `AssetDatabase.Refresh()` through the Unity editor, then read the compilation errors.

Expected: no errors. `Cherry.Framework.Tests` appears in the list of compiled assemblies with two source files.

- [ ] **Step 6: Run the suite and verify it works**

Run the menu item `Tools/CherryFramework/Run All Tests`, then read the Console.

Expected in the log:

```
[CherryFramework] EditMode - passed: 2, failed: 0, skipped: 0, inconclusive: 0, total: 2
[CherryFramework] PlayMode - passed: 0, failed: 0, skipped: 0, inconclusive: 0, total: 0
[CherryFramework] The test run is finished.
```

The installer tests must be part of the edit mode count as well - the count is higher than 2.

- [ ] **Step 7: Commit**

```bash
git add Assets/Tests/Framework Assets/Tests/Editor/EditorTestRunner.cs
git commit -m "test: add the framework test assembly and the test runner command"
```

---

### Task 2: The dependency container, and the bug it finds

**Files:**
- Create: `Assets/Tests/Framework/DependencyContainerTests.cs`
- Modify: `Assets/CherryFramework/DependencyManager/DependencyContainer.cs` (the `Dispose` method)

**Interfaces:**
- Consumes: `CherryFramework.DependencyManager.DependencyContainer`, `InjectAttribute`, `BindingType`.
- Produces: `DependencyContainer.Dispose()` also clears the registry.

- [ ] **Step 1: Write the failing test**

`Assets/Tests/Framework/DependencyContainerTests.cs`:

```csharp
using CherryFramework.DependencyManager;
using NUnit.Framework;

namespace Cherry.Framework.Tests
{
    public class DependencyContainerTests
    {
        private class Service
        {
        }

        private class OtherService
        {
        }

        private class Consumer : InjectClass
        {
            [Inject] private Service _service;
            [Inject] public OtherService Other { get; set; }

            public Service Service => _service;
        }

        private class DerivedConsumer : Consumer
        {
            [Inject] private OtherService _inherited;
        }

        [TearDown]
        public void TearDown()
        {
            DependencyContainer.Instance.RemoveDependency(typeof(Service));
            DependencyContainer.Instance.RemoveDependency(typeof(OtherService));
        }

        [Test]
        public void BindAsSingleton_Instance_IsInjectedIntoTheField()
        {
            var service = new Service();
            DependencyContainer.Instance.BindAsSingleton(service);

            var consumer = new Consumer();

            Assert.AreSame(service, consumer.Service);
        }

        [Test]
        public void BindAsSingleton_WithoutAnInstance_ReusesOneInstance()
        {
            DependencyContainer.Instance.BindAsSingleton<Service>();

            var first = new Consumer();
            var second = new Consumer();

            Assert.AreNotNull(first.Service);
            Assert.AreSame(first.Service, second.Service);
        }

        [Test]
        public void Bind_Transient_CreatesANewInstanceForEveryInjection()
        {
            DependencyContainer.Instance.Bind<Service>(BindingType.Transient);

            var first = new Consumer();
            var second = new Consumer();

            Assert.AreNotNull(first.Service);
            Assert.AreNotSame(first.Service, second.Service);
        }

        [Test]
        public void BindAsSingleton_InterfaceBinding_ResolvesByTheServiceType()
        {
            var service = new OtherService();
            DependencyContainer.Instance.BindAsSingleton<OtherService, OtherService>(service);

            var consumer = new Consumer();

            Assert.AreSame(service, consumer.Other);
        }

        [Test]
        public void InjectDependencies_PropertyWithTheAttribute_IsSet()
        {
            var other = new OtherService();
            DependencyContainer.Instance.BindAsSingleton(other);

            var consumer = new Consumer();

            Assert.AreSame(other, consumer.Other);
        }

        [Test]
        public void InjectDependencies_FieldsOfTheBaseClass_AreInjected()
        {
            var service = new Service();
            var other = new OtherService();
            DependencyContainer.Instance.BindAsSingleton(service);
            DependencyContainer.Instance.BindAsSingleton(other);

            var consumer = new DerivedConsumer();

            Assert.AreSame(service, consumer.Service);
            Assert.AreSame(other, consumer.Other);
        }

        [Test]
        public void RemoveDependency_ThenHasDependency_IsFalse()
        {
            DependencyContainer.Instance.BindAsSingleton(new Service());
            DependencyContainer.Instance.RemoveDependency(typeof(Service));

            Assert.IsFalse(DependencyContainer.Instance.HasDependency<Service>());
        }

        [Test]
        public void Dispose_ClearsTheRegistrations()
        {
            DependencyContainer.Instance.BindAsSingleton(new Service());

            DependencyContainer.Instance.Dispose();

            Assert.IsFalse(DependencyContainer.Instance.HasDependency<Service>());
        }
    }
}
```

- [ ] **Step 2: Run the tests and verify the last one fails**

Run the menu item `Tools/CherryFramework/Run All Tests` and read the Console.

Expected: `DependencyContainerTests.Dispose_ClearsTheRegistrations` fails with
`Expected: False But was: True`. Every other test passes.

- [ ] **Step 3: Fix `Dispose`**

In `Assets/CherryFramework/DependencyManager/DependencyContainer.cs`, replace:

```csharp
public void Dispose()
{
    foreach (var dep in _dependencies.Values)
    {
        DisposeInstance(dep.BindInstance);
    }
}
```

with:

```csharp
public void Dispose()
{
    foreach (var dep in _dependencies.Values)
    {
        DisposeInstance(dep.BindInstance);
    }

    // Disposing the instances is not enough: the registrations have to go as
    // well, otherwise the container keeps handing out disposed instances and
    // refuses every new binding of the same type.
    _dependencies.Clear();
    _disposedInstances.Clear();
}
```

- [ ] **Step 4: Run the tests and verify they pass**

Expected: every dependency container test passes, and the edit mode count grew by eight.

- [ ] **Step 5: Commit**

```bash
git add Assets/Tests/Framework/DependencyContainerTests.cs Assets/CherryFramework/DependencyManager/DependencyContainer.cs
git commit -m "fix: clear the container registrations when it is disposed"
```

---

### Task 3: The state service

**Files:**
- Create: `Assets/Tests/Framework/StateServiceTests.cs`

**Interfaces:**
- Consumes: `CherryFramework.StateService.StateService`, `StateAccessor`, `StateSubscription`.
- Produces: nothing.

- [ ] **Step 1: Write the tests**

`Assets/Tests/Framework/StateServiceTests.cs`:

```csharp
using System.Text.RegularExpressions;
using CherryFramework.StateService;
using CherryFramework.TickDispatcher;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Cherry.Framework.Tests
{
    public class StateServiceTests
    {
        private StateService _stateService;

        [SetUp]
        public void SetUp()
        {
            _stateService = new StateService(false);
        }

        [TearDown]
        public void TearDown()
        {
            // Every StateService builds its own Ticker, and the Ticker builds
            // its own GameObject.
            foreach (var behaviour in Object.FindObjectsByType<TickerBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(behaviour.gameObject);
            }
        }

        [Test]
        public void EmitEvent_IsEventActive_UntilTheNextTick()
        {
            _stateService.EmitEvent("GameStarted");

            Assert.IsTrue(_stateService.IsEventActive("GameStarted"));

            _stateService.LateTick(0f);

            Assert.IsFalse(_stateService.IsEventActive("GameStarted"));
            Assert.IsTrue(_stateService.EventPassed("GameStarted"));
        }

        [Test]
        public void EmitEvent_WithAPayload_TryGetPayloadReturnsIt()
        {
            _stateService.EmitEvent("PlayerDied", 42);

            Assert.IsTrue(_stateService.TryGetPayload("PlayerDied", out int payload));
            Assert.AreEqual(42, payload);
        }

        [Test]
        public void GetPayload_OfAnUnknownKey_ReturnsTheDefault()
        {
            Assert.AreEqual(0, _stateService.GetPayload("Nothing"));
        }

        [Test]
        public void SetStatus_IsStatusActive()
        {
            _stateService.SetStatus("IsPaused");

            Assert.IsTrue(_stateService.IsStatusActive("IsPaused"));
            Assert.IsTrue(_stateService.IsStatusJustBecameActive("IsPaused"));
        }

        [Test]
        public void UnsetStatus_AfterSetStatus_IsStatusInactive()
        {
            _stateService.SetStatus("IsPaused");
            _stateService.LateTick(0f);

            _stateService.UnsetStatus("IsPaused");

            Assert.IsFalse(_stateService.IsStatusActive("IsPaused"));
            Assert.IsTrue(_stateService.IsStatusInactive("IsPaused"));
        }

        [Test]
        public void AddStateSubscription_WithAMetCondition_RunsTheCallbackOnTick()
        {
            var calls = 0;
            _stateService.AddStateSubscription(_ => true, () => calls++);

            _stateService.LateTick(0f);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void AddStateSubscription_WithAnUnmetCondition_DoesNotRunTheCallback()
        {
            var calls = 0;
            _stateService.AddStateSubscription(accessor => accessor.IsEventActive("Ready"), () => calls++);

            _stateService.LateTick(0f);

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void AddStateSubscription_SeesTheEventsOfTheSameTick()
        {
            var calls = 0;
            _stateService.AddStateSubscription(accessor => accessor.IsEventActive("Ready"), () => calls++);

            _stateService.EmitEvent("Ready");
            _stateService.LateTick(0f);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void AddStateSubscription_WithDestroyAfterInvoke_RunsOnlyOnce()
        {
            var calls = 0;
            _stateService.AddStateSubscription(_ => true, () => calls++, null, true);

            _stateService.LateTick(0f);
            _stateService.LateTick(0f);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void RemoveSubscription_StopsTheCallback()
        {
            var calls = 0;
            var subscription = _stateService.AddStateSubscription(_ => true, () => calls++);

            _stateService.RemoveSubscription(subscription);
            _stateService.LateTick(0f);

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void RemoveAllSubscriptions_StopsTheCallback()
        {
            var calls = 0;
            var subscriber = new object();
            _stateService.AddStateSubscription(_ => true, () => calls++, subscriber);

            _stateService.RemoveAllSubscriptions(subscriber);
            _stateService.LateTick(0f);
            _stateService.LateTick(0f);

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void AddStateSubscription_ThatThrows_IsLoggedAndTheRestStillRuns()
        {
            LogAssert.Expect(LogType.Error, new Regex("Exception in subscription"));

            _stateService.AddStateSubscription(_ => true, () => throw new System.InvalidOperationException("boom"));
            _stateService.AddStateSubscription(_ => true, () => { });

            Assert.DoesNotThrow(() => _stateService.LateTick(0f));
        }
    }
}
```

- [ ] **Step 2: Run the tests**

Expected: the count grows by twelve. If a test fails, read the failure message - the most likely cause is a leftover `GameObject` from the `Ticker`, which the tear down removes.

- [ ] **Step 3: Commit**

```bash
git add Assets/Tests/Framework/StateServiceTests.cs
git commit -m "test: cover the state service"
```

---

### Task 4: The save game manager

**Files:**
- Create: `Assets/Tests/Framework/SaveGameManagerTests.cs`

**Interfaces:**
- Consumes: `CherryFramework.SaveGameManager.SaveGameManager`, `PersistentObject`, `IGameSaveData`, `CherryFramework.Utils.PlayerPrefsWrapper.IPlayerPrefs`, and the fakes from `Cherry.Framework.Tests.Fakes`.
- Produces: nothing.

- [ ] **Step 1: Write the tests**

`Assets/Tests/Framework/SaveGameManagerTests.cs`:

```csharp
using CherryFramework.DataModels;
using CherryFramework.DataModels.ModelDataStorageBridges;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;

namespace CherryFramework.Tests
{
    public class SaveGameManagerTests
    {
        private const string Guid = "11111111-2222-3333-4444-555555555555";

        private ModelService _modelService;

        private FakePlayerPrefs _playerPrefs;
        private SaveGameManager _saveGameManager;
        private PersistentObject _persistentObject;
        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            _playerPrefs = new FakePlayerPrefs();
            _saveGameManager = new SaveGameManager(_playerPrefs, false);
            _modelService = new ModelService(new PlayerPrefsBridge(_playerPrefs), false);

            // PersistentObject injects both of them. They are bound so that
            // the component never logs an injection error, no matter whether
            // the editor calls OnEnable on a component added at edit time.
            DependencyContainer.Instance.BindAsSingleton(_saveGameManager);
            DependencyContainer.Instance.BindAsSingleton(_modelService);

            _persistentObject = TestGameObjects.CreatePersistentObject(Guid);
            _gameObject = _persistentObject.gameObject;
        }

        [TearDown]
        public void TearDown()
        {
            TestGameObjects.Destroy(_gameObject);
            DependencyContainer.Instance.RemoveDependency(typeof(SaveGameManager));
            DependencyContainer.Instance.RemoveDependency(typeof(ModelService));
        }

        [Test]
        public void Register_ThenRegisteredComponents_ContainsTheComponent()
        {
            var component = new TestGameSaveData();

            Assert.IsTrue(_saveGameManager.Register(component, _persistentObject));
            Assert.AreEqual(1, _saveGameManager.RegisteredComponents.Length);
        }

        [Test]
        public void Register_TheSameComponentTwice_Fails()
        {
            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);

            Assert.IsFalse(_saveGameManager.Register(component, _persistentObject));
        }

        [Test]
        public void LoadData_OfAnUnregisteredComponent_Fails()
        {
            Assert.IsFalse(_saveGameManager.LoadData(new TestGameSaveData()));
        }

        [Test]
        public void SaveData_ThenLoadData_RestoresTheFieldValues()
        {
            var component = new TestGameSaveData { Health = 75, Title = "hero" };
            _saveGameManager.Register(component, _persistentObject);

            _saveGameManager.SaveData(component);

            var restored = new TestGameSaveData();
            _saveGameManager.Register(restored, _persistentObject);

            Assert.IsTrue(_saveGameManager.LoadData(restored));
            Assert.AreEqual(75, restored.Health);
            Assert.AreEqual("hero", restored.Title);
            Assert.AreEqual(1, restored.LoadCount);
        }

        [Test]
        public void SaveData_WritesTheKeyOfTheComponent()
        {
            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);

            _saveGameManager.SaveData(component);

            var expectedKey = "SceneId:" + _gameObject.scene.buildIndex + "." + Guid + "-" + typeof(TestGameSaveData);
            Assert.IsTrue(_playerPrefs.HasKey(expectedKey), "expected key " + expectedKey);
        }

        [Test]
        public void LoadData_WithoutSavedData_Fails()
        {
            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);

            Assert.IsFalse(_saveGameManager.LoadData(component));
        }

        [Test]
        public void DeleteData_RemovesTheSavedKey()
        {
            var component = new TestGameSaveData { Health = 10 };
            _saveGameManager.Register(component, _persistentObject);
            _saveGameManager.SaveData(component);

            Assert.IsTrue(_saveGameManager.DeleteData(component));
            Assert.AreEqual(0, _playerPrefs.Values.Count);
        }

        [Test]
        public void SetCurrentSlot_ChangesTheKey()
        {
            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);
            _saveGameManager.SetCurrentSlot("slot2");

            _saveGameManager.SaveData(component);

            Assert.IsTrue(_playerPrefs.HasKey("slot2-SceneId:" + _gameObject.scene.buildIndex + "." + Guid + "-" + typeof(TestGameSaveData)));
        }

        [Test]
        public void UnRegister_RemovesTheComponent()
        {
            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);

            Assert.IsTrue(_saveGameManager.UnRegister(component));
            Assert.AreEqual(0, _saveGameManager.RegisteredComponents.Length);
        }

        [Test]
        public void SaveAllData_SavesEveryRegisteredComponent()
        {
            var first = new TestGameSaveData { Health = 1 };
            var second = new TestGameSaveData { Health = 2 };
            _saveGameManager.Register(first, _persistentObject);
            _saveGameManager.Register(second, _persistentObject);

            _saveGameManager.SaveAllData();

            Assert.AreEqual(2, _playerPrefs.Values.Count);
        }
    }
}
```

- [ ] **Step 2: Run the tests**

Expected: the count grows by ten. The `PersistentObject` needs the
`SaveGameManager` and the `ModelService` in the container, which the set up
binds - without that the component logs an injection error and the test
fails.

- [ ] **Step 3: Commit**

```bash
git add Assets/Tests/Framework/SaveGameManagerTests.cs
git commit -m "test: cover the save game manager"
```

---

### Task 5: The data models

**Files:**
- Create: `Assets/Tests/Framework/Fakes/TestDataModel.cs`
- Create: `Assets/Tests/Framework/DataModelTests.cs`

**Interfaces:**
- Consumes: `CherryFramework.DataModels.DataModelBase`, `Accessor`, `Bindings`, `BindingActivation`, `ModelService`, `PlayerPrefsBridge`, `ReadyMode`.
- Produces: `Cherry.Framework.Tests.Fakes.TestDataModel` - a `DataModelBase` with a `Health` int and a `Title` string, and their accessors.

- [ ] **Step 1: Write the tests**

`Assets/Tests/Framework/DataModelTests.cs`:

```csharp
using CherryFramework.DataModels;
using CherryFramework.DataModels.ModelDataStorageBridges;
using CherryFramework.Tests.Fakes;
using NUnit.Framework;

namespace CherryFramework.Tests
{
    public class DataModelTests
    {
        private FakePlayerPrefs _playerPrefs;
        private ModelService _modelService;

        [SetUp]
        public void SetUp()
        {
            _playerPrefs = new FakePlayerPrefs();
            _modelService = new ModelService(new PlayerPrefsBridge(_playerPrefs), false);
        }

        [Test]
        public void Accessor_Value_ReturnsTheValueOfTheModel()
        {
            var model = new TestDataModel();

            Assert.AreEqual(100, model.HealthAccessor.Value);
            Assert.AreEqual("untitled", model.TitleAccessor.Value);
        }

        [Test]
        public void Accessor_BindDownwards_WithInvokeImmediate_CallsTheCallbackRightAway()
        {
            var model = new TestDataModel();
            var received = 0;

            model.HealthAccessor.BindDownwards(value => received = value);

            Assert.AreEqual(100, received);
        }

        [Test]
        public void Accessor_BindDownwards_CallsTheCallbackWhenTheValueChanges()
        {
            var model = new TestDataModel();
            var received = 0;
            model.HealthAccessor.BindDownwards(value => received = value);

            model.Health = 55;

            Assert.AreEqual(55, received);
        }

        [Test]
        public void Accessor_BindDownwards_WithActivateOnReady_WaitsForTheReadyFlag()
        {
            var model = new TestDataModel();
            var received = 0;
            model.HealthAccessor.BindDownwards(value => received = value, BindingActivation.ActivateOnReady);

            model.Health = 55;

            Assert.AreEqual(0, received, "the binding is not active before the model is ready");

            model.Ready = true;
            model.Health = 70;

            Assert.AreEqual(70, received, "an active binding delivers the next value");
        }

        [Test]
        public void Accessor_BindDownwards_WithInvokeOnReady_CallsTheCallbackWhenTheModelBecomesReady()
        {
            var model = new TestDataModel();
            var received = 0;
            model.HealthAccessor.BindDownwards(value => received = value, BindingActivation.InvokeOnReady);

            model.Health = 55;

            Assert.AreEqual(0, received, "the binding waits for the model to be ready");

            model.Ready = true;

            Assert.AreEqual(55, received, "the callback runs with the current value");
        }

        [Test]
        public void Accessor_AddProcessor_ProcessedValue_IsTransformed()
        {
            var model = new TestDataModel();
            model.HealthAccessor.AddProcessor(value => value / 2);

            Assert.AreEqual(50, model.HealthAccessor.ProcessedValue);
            Assert.AreEqual(100, model.HealthAccessor.Value);
        }

        [Test]
        public void Accessor_RemoveProcessor_ProcessedValue_IsTheRawValueAgain()
        {
            var model = new TestDataModel();
            var processor = model.HealthAccessor.AddProcessor(value => value / 2);

            model.HealthAccessor.RemoveProcessor(processor);

            Assert.AreEqual(100, model.HealthAccessor.ProcessedValue);
        }

        [Test]
        public void Bindings_ReleaseAllBindings_StopsTheCallbacks()
        {
            var model = new TestDataModel();
            var received = 0;
            var bindings = new Bindings();
            bindings.CreateBinding(model.HealthAccessor, value => received = value);

            model.Health = 55;
            bindings.ReleaseAllBindings();
            model.Health = 70;

            Assert.AreEqual(55, received);
        }

        [Test]
        public void ModelService_GetOrCreateSingletonModel_ReturnsTheSameInstance()
        {
            var first = _modelService.GetOrCreateSingletonModel<TestDataModel>();
            var second = _modelService.GetOrCreateSingletonModel<TestDataModel>();

            Assert.AreSame(first, second);
        }

        [Test]
        public void ModelService_GetOrCreateTransientModel_WithTheSameId_ReturnsTheSameInstance()
        {
            var first = _modelService.GetOrCreateTransientModel<TestDataModel>("enemy-1");
            var second = _modelService.GetOrCreateTransientModel<TestDataModel>("enemy-1");

            Assert.AreSame(first, second);
            Assert.AreEqual("enemy-1", first.Id);
        }

        [Test]
        public void ModelService_ReleaseTransientModel_RemovesTheModel()
        {
            var model = _modelService.GetOrCreateTransientModel<TestDataModel>("enemy-1");

            Assert.IsTrue(_modelService.ReleaseTransientModel(model));
            Assert.IsFalse(_modelService.ReleaseTransientModel(model));
        }

        [Test]
        public void PlayerPrefsBridge_SaveModel_ThenLoadModel_RestoresTheValues()
        {
            var model = _modelService.GetOrCreateSingletonModel<TestDataModel>();
            _modelService.DataStorage.RegisterModelInStorage(model);

            model.Health = 42;
            model.Title = "boss";
            _modelService.DataStorage.SaveModelToStorage(model);

            var restored = _modelService.GetOrCreateSingletonModel<TestDataModel>();
            _modelService.DataStorage.RegisterModelInStorage(restored);
            _modelService.DataStorage.LoadModelData(restored);

            Assert.AreEqual(42, restored.Health);
            Assert.AreEqual("boss", restored.Title);
        }
    }
}
```

- [ ] **Step 2: Run the tests**

Expected: the count grows by twelve.

- [ ] **Step 3: Commit**

```bash
git add Assets/Tests/Framework/DataModelTests.cs
git commit -m "test: cover the data models"
```

---

### Task 6: The utility classes

**Files:**
- Modify: `Assets/Tests/Framework/UtilsTests.cs`

**Interfaces:**
- Consumes: `CherryFramework.Utils.DataUtils`, `StringUtils`, `MathUtils`, `EnumerableUtils`.
- Produces: nothing.

- [ ] **Step 1: Extend the tests**

Replace `Assets/Tests/Framework/UtilsTests.cs` with:

```csharp
using System;
using System.Collections.Generic;
using CherryFramework.Utils;
using NUnit.Framework;
using UnityEngine;

namespace CherryFramework.Tests
{
    public class UtilsTests
    {
        private enum Sample
        {
            First,
            Second
        }

        [Test]
        public void DataUtils_CreateKey_JoinsTheValuesWithADash()
        {
            Assert.AreEqual("a-b-c", DataUtils.CreateKey("a", "b", "c"));
        }

        [Test]
        public void DataUtils_CreateKey_SkipsTheEmptyValues()
        {
            Assert.AreEqual("a-b", DataUtils.CreateKey("a", string.Empty, "b"));
        }

        [Test]
        public void StringUtils_IsNullOrWhiteSpace_MatchesTheStringClass()
        {
            Assert.IsTrue(((string)null).IsNullOrWhiteSpace());
            Assert.IsTrue("  ".IsNullOrWhiteSpace());
            Assert.IsFalse("text".IsNullOrWhiteSpace());
        }

        [Test]
        public void MathUtils_InRange_IncludesTheBounds()
        {
            Assert.IsTrue(5f.InRange(0f, 10f));
            Assert.IsTrue(0f.InRange(0f, 10f));
            Assert.IsFalse(11f.InRange(0f, 10f));
        }

        [Test]
        public void MathUtils_InRange_Exclusive_ExcludesTheBounds()
        {
            Assert.IsTrue(5f.InRange(0f, 10f, true));
            Assert.IsFalse(0f.InRange(0f, 10f, true));
        }

        [Test]
        public void MathUtils_InRange_WorksWithSwappedBounds()
        {
            Assert.IsTrue(5f.InRange(10f, 0f));
        }

        [Test]
        public void MathUtils_RotateByDegrees_TurnsTheVector()
        {
            var rotated = new Vector2(1f, 0f).RotateByDegrees(90f);

            Assert.AreEqual(0f, rotated.x, 0.0001f);
            Assert.AreEqual(1f, rotated.y, 0.0001f);
        }

        [Test]
        public void MathUtils_ArrayToVector2AndBack_KeepsTheValues()
        {
            var vector = MathUtils.ArrayToVector2(new[] { 1f, 2f });

            Assert.AreEqual(new Vector2(1f, 2f), vector);
            CollectionAssert.AreEqual(new[] { 1f, 2f }, MathUtils.Vector2ToArray(vector));
        }

        [Test]
        public void EnumerableUtils_GetAllEnumValues_ReturnsEveryValue()
        {
            CollectionAssert.AreEqual(new[] { Sample.First, Sample.Second }, Sample.First.GetAllEnumValues());
        }

        [Test]
        public void EnumerableUtils_SelectWhere_KeepsOnlyTheValidResults()
        {
            var result = new List<int>(new[] { 1, 2, 3, 4 }.SelectWhere(value => (value % 2 == 0, value * 10)));

            CollectionAssert.AreEqual(new[] { 20, 40 }, result);
        }

        [Test]
        public void EnumerableUtils_IndexOf_ReturnsThePositionOrMinusOne()
        {
            var source = new[] { "a", "b", "c" };

            Assert.AreEqual(1, source.IndexOf("b"));
            Assert.AreEqual(-1, source.IndexOf("z"));
        }

        [Test]
        public void EnumerableUtils_TryGet_FindsTheFirstMatch()
        {
            var source = new[] { 1, 2, 3 };

            Assert.IsTrue(source.TryGet(value => value > 1, out int found));
            Assert.AreEqual(2, found);
            Assert.IsFalse(source.TryGet(value => value > 5, out _));
        }

        [Test]
        public void EnumerableUtils_Each_PassesTheIndex()
        {
            var indexes = new List<int>();
            new[] { "a", "b", "c" }.Each((value, index) => indexes.Add(index));

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indexes);
        }
    }
}
```

- [ ] **Step 2: Run the tests**

Expected: the count grows by twelve.

- [ ] **Step 3: Commit**

```bash
git add Assets/Tests/Framework/UtilsTests.cs
git commit -m "test: cover the utility classes"
```

---

### Task 7: The object pool

**Files:**
- Create: `Assets/Tests/Framework/SimplePoolTests.cs`

**Interfaces:**
- Consumes: `CherryFramework.SimplePool.SimplePool`.
- Produces: nothing.

- [ ] **Step 1: Write the tests**

`Assets/Tests/Framework/SimplePoolTests.cs`:

```csharp
using CherryFramework.SimplePool;
using NUnit.Framework;
using UnityEngine;

namespace CherryFramework.Tests
{
    public class SimplePoolTests
    {
        private GameObject _sample;
        private SimplePool<Transform> _pool;

        [SetUp]
        public void SetUp()
        {
            _sample = new GameObject("PoolSample");
            _pool = new SimplePool<Transform>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var active in _pool.ActiveObjects(_sample.transform))
            {
                Object.DestroyImmediate(active.gameObject);
            }

            Object.DestroyImmediate(_sample);
        }

        [Test]
        public void Get_OnAnEmptyPool_CreatesANewInstance()
        {
            var instance = _pool.Get(_sample.transform);

            Assert.IsNotNull(instance);
            Assert.AreNotSame(_sample.transform, instance);
        }

        [Test]
        public void Get_AfterTheInstanceWasDeactivated_ReusesIt()
        {
            var first = _pool.Get(_sample.transform);
            first.gameObject.SetActive(false);

            var second = _pool.Get(_sample.transform);

            Assert.AreSame(first, second);
        }

        [Test]
        public void Get_WhileTheInstanceIsActive_CreatesANewOne()
        {
            var first = _pool.Get(_sample.transform);

            var second = _pool.Get(_sample.transform);

            Assert.AreNotSame(first, second);
        }

        [Test]
        public void Get_WithATransform_AppliesThePositionAndTheRotation()
        {
            var position = new Vector3(1f, 2f, 3f);
            var rotation = Quaternion.Euler(0f, 90f, 0f);

            var instance = _pool.Get(_sample.transform, position, rotation);

            Assert.AreEqual(position, instance.position);
            Assert.AreEqual(rotation, instance.rotation);
        }

        [Test]
        public void ActiveObjects_ReturnsOnlyTheActiveInstances()
        {
            var first = _pool.Get(_sample.transform);
            var second = _pool.Get(_sample.transform);
            second.gameObject.SetActive(false);

            var active = _pool.ActiveObjects(_sample.transform);

            Assert.AreEqual(1, active.Count);
            Assert.AreSame(first, active[0]);
        }

        [Test]
        public void Clear_EmptiesThePool()
        {
            _pool.Get(_sample.transform);

            _pool.Clear();

            Assert.AreEqual(0, _pool.ActiveObjects(_sample.transform).Count);
        }
    }
}
```

- [ ] **Step 2: Run the tests**

Expected: the count grows by six.

- [ ] **Step 3: Commit**

```bash
git add Assets/Tests/Framework/SimplePoolTests.cs
git commit -m "test: cover the object pool"
```

---

### Task 8: The MonoBehaviour lifecycle

**Files:**
- Create: `Assets/Tests/PlayMode/Cherry.Framework.PlayMode.Tests.asmdef`
- Create: `Assets/Tests/PlayMode/InstallerBehaviourTests.cs`
- Create: `Assets/Tests/PlayMode/InjectMonoBehaviourTests.cs`
- Create: `Assets/Tests/PlayMode/TickerBehaviourTests.cs`
- Create: `Assets/Tests/PlayMode/PersistentObjectTests.cs`

**Interfaces:**
- Consumes: `InstallerBehaviourBase`, `InjectMonoBehaviour`, `BehaviourBase`, `Ticker`, `TickerBehaviour`, `PersistentObject`, `SaveGameManager`.
- Produces: nothing.

- [ ] **Step 1: Create the play mode assembly**

`Assets/Tests/PlayMode/Cherry.Framework.PlayMode.Tests.asmdef`:

```json
{
    "name": "Cherry.Framework.PlayMode.Tests",
    "rootNamespace": "",
    "references": [
        "Cherry.Framework",
        "Cherry.Framework.TestSupport",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Write the installer test**

`Assets/Tests/PlayMode/InstallerBehaviourTests.cs`:

```csharp
using CherryFramework.DependencyManager;
using NUnit.Framework;
using UnityEngine;

namespace Cherry.Framework.PlayMode.Tests
{
    public class InstallerBehaviourTests
    {
        private class Service
        {
        }

        private class TestInstaller : InstallerBehaviourBase
        {
            protected override void Install()
            {
                BindAsSingleton(new Service());
            }
        }

        [TearDown]
        public void TearDown()
        {
            DependencyContainer.Instance.RemoveDependency(typeof(Service));
        }

        [UnityTest]
        public IEnumerator Installer_Awake_BindsTheServices()
        {
            var gameObject = new GameObject("TestInstaller");
            gameObject.AddComponent<TestInstaller>();

            yield return null;

            Assert.IsTrue(DependencyContainer.Instance.HasDependency<Service>());

            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator Installer_OnDestroy_RemovesTheServices()
        {
            var gameObject = new GameObject("TestInstaller");
            gameObject.AddComponent<TestInstaller>();

            yield return null;

            Object.DestroyImmediate(gameObject);
            yield return null;

            Assert.IsFalse(DependencyContainer.Instance.HasDependency<Service>());
        }
    }
}
```

- [ ] **Step 3: Write the injection test**

`Assets/Tests/PlayMode/InjectMonoBehaviourTests.cs`:

```csharp
using System.Collections;
using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Cherry.Framework.PlayMode.Tests
{
    public class InjectMonoBehaviourTests
    {
        private class Service
        {
        }

        private class TestComponent : BehaviourBase
        {
            [Inject] private Service _service;

            public Service Service => _service;
        }

        [TearDown]
        public void TearDown()
        {
            DependencyContainer.Instance.RemoveDependency(typeof(Service));
        }

        [UnityTest]
        public IEnumerator Component_OnEnable_ReceivesTheInjectedService()
        {
            DependencyContainer.Instance.BindAsSingleton(new Service());

            var gameObject = new GameObject("TestComponent");
            var component = gameObject.AddComponent<TestComponent>();

            yield return null;

            Assert.IsNotNull(component.Service);

            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator Component_OnDestroy_ReleasesItsBindings()
        {
            var gameObject = new GameObject("TestComponent");
            gameObject.AddComponent<TestComponent>();

            yield return null;

            Object.DestroyImmediate(gameObject);
            yield return null;

            Assert.Pass("the component was destroyed without an exception");
        }
    }
}
```

- [ ] **Step 4: Write the ticker test**

`Assets/Tests/PlayMode/TickerBehaviourTests.cs`:

```csharp
using System.Collections;
using CherryFramework.TickDispatcher;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CherryFramework.PlayMode.Tests
{
    public class TickerBehaviourTests
    {
        private class CountingTickable : ITickable
        {
            public int Ticks;

            public void Tick(float deltaTime) => Ticks++;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var behaviour in Object.FindObjectsByType<TickerBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(behaviour.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator RegisteredTickable_IsTickedEveryFrame()
        {
            var ticker = new Ticker();
            var tickable = new CountingTickable();
            ticker.Register(tickable);

            yield return null;
            yield return null;

            Assert.GreaterOrEqual(tickable.Ticks, 1);

            ticker.UnRegister(tickable);
        }

        [UnityTest]
        public IEnumerator UnregisteredTickable_IsNotTicked()
        {
            var ticker = new Ticker();
            var tickable = new CountingTickable();
            ticker.Register(tickable);

            yield return null;

            ticker.UnRegister(tickable);
            var ticksBefore = tickable.Ticks;

            yield return null;
            yield return null;

            Assert.AreEqual(ticksBefore, tickable.Ticks);
        }
    }
}
```

- [ ] **Step 5: Write the persistent object test**

`Assets/Tests/PlayMode/PersistentObjectTests.cs`:

```csharp
using System.Collections;
using CherryFramework.DataModels;
using CherryFramework.DataModels.ModelDataStorageBridges;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CherryFramework.PlayMode.Tests
{
    public class PersistentObjectTests
    {
        private const string Guid = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            // PersistentObject injects both of them, so they have to be bound
            // before the component is created.
            var playerPrefs = new FakePlayerPrefs();
            DependencyContainer.Instance.BindAsSingleton(new SaveGameManager(playerPrefs, false));
            DependencyContainer.Instance.BindAsSingleton(new ModelService(new PlayerPrefsBridge(playerPrefs), false));
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }

            DependencyContainer.Instance.RemoveDependency(typeof(SaveGameManager));
            DependencyContainer.Instance.RemoveDependency(typeof(ModelService));
        }

        [UnityTest]
        public IEnumerator PersistentObject_WithoutAGuid_ReportsNoId()
        {
            _gameObject = new GameObject("TestPersistentObject");
            var persistentObject = _gameObject.AddComponent<PersistentObject>();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("guid is missing"));

            Assert.IsNull(persistentObject.GetObjectId());

            yield return null;
        }

        [UnityTest]
        public IEnumerator PersistentObject_WithAGuid_ReportsTheSceneId()
        {
            _gameObject = new GameObject("TestPersistentObject");
            var persistentObject = _gameObject.AddComponent<PersistentObject>();
            persistentObject.guid = Guid;

            Assert.AreEqual("SceneId:" + _gameObject.scene.buildIndex + "." + Guid, persistentObject.GetObjectId());

            yield return null;
        }
    }
}
```

- [ ] **Step 6: Refresh Unity and check for compile errors**

Expected: no errors. `Cherry.Framework.PlayMode.Tests` appears in the list of compiled assemblies.

- [ ] **Step 7: Run the suite and verify the play mode tests pass**

Run `Tools -> CherryFramework -> Run All Tests` and read the Console.

Expected: the play mode summary reports a passing count of six and no failures.

- [ ] **Step 8: Commit**

```bash
git add Assets/Tests/PlayMode
git commit -m "test: cover the MonoBehaviour lifecycle"
```

---

### Task 9: Document how to build and test

**Files:**
- Create: `AGENTS.md`
- Modify: `README.md`

**Interfaces:**
- Consumes: nothing.
- Produces: `AGENTS.md` - the instructions an agent needs to work in this repository.

- [ ] **Step 1: Write `AGENTS.md`**

`AGENTS.md`:

```markdown
# CherryFramework

Unity framework for game architecture: dependency injection, data models, a
save system, UI navigation, audio, a state service, a tick dispatcher and
object pooling.

## Layout

| Path | What it is |
| --- | --- |
| `Assets/CherryFramework/` | the framework, distributed to users as one folder |
| `Assets/Tests/Framework/` | edit mode tests of the framework |
| `Assets/Tests/PlayMode/` | play mode tests of the MonoBehaviour lifecycle |
| `Assets/Tests/Editor/` | tests of the dependency installer |
| `Assets/Sample/` | the demo game, not part of the framework |
| `Docs/` | one document per system, the contract the tests follow |

## Running the tests

Edit mode, the fast loop:

```
Unity.exe -batchmode -runTests -testPlatform EditMode \
  -projectPath X:/CherryFramework -testResults results.xml \
  -logFile test-editmode.log
```

Play mode:

```
Unity.exe -batchmode -runTests -testPlatform PlayMode \
  -projectPath X:/CherryFramework -testResults results-playmode.xml \
  -logFile test-playmode.log
```

Inside a running editor: `Tools -> CherryFramework -> Run All Tests`. It runs
the edit mode tests, then the play mode tests, and prints one summary per run
into the Console.

The editor has to be closed for the command line runs - Unity locks the
project folder.

## Conventions

- A test name is `System_Behaviour_ExpectedResult` and asserts one behaviour.
- A test that exercises an error path uses `LogAssert.Expect`. A test that is
  not about an error path must not log an error: the test framework fails a
  test on any unexpected `Debug.LogError`.
- The framework keeps state in statics and singletons. Every test cleans up
  after itself in `[TearDown]` - remove the bindings, destroy the
  GameObjects.
- Storage is faked with `FakePlayerPrefs`. Tests never touch the real
  `PlayerPrefs`.
- The framework references its dependencies by assembly name, never by GUID,
  so a package can be re-imported without breaking the references.
```

- [ ] **Step 2: Correct the README caveat**

In `README.md`, replace the paragraph that starts with `**The caveat, stated
plainly:**` with:

```markdown
**The caveat, stated plainly:** the runtime systems are covered by automated
tests - run them with `Tools -> CherryFramework -> Run All Tests`, or from the
command line, see `AGENTS.md`. What the tests cannot see is an installer's
`[SerializeField]` references: they are invisible to the compiler, so one
left unassigned stays `null` and throws at runtime, not at build time.
```

- [ ] **Step 3: Commit**

```bash
git add AGENTS.md README.md
git commit -m "docs: explain how to build and test the framework"
```

---

## Verification

Run after all tasks are done.

- [ ] **Step 1: The whole suite passes**

Run `Tools -> CherryFramework -> Run All Tests`.

Expected: the edit mode summary and the play mode summary both report zero
failures.

- [ ] **Step 2: The project compiles**

Read the compilation errors.

Expected: no errors.

- [ ] **Step 3: A deliberate break turns exactly one test red**

Add a broken line to `Assets/CherryFramework/Utils/MathUtils.cs`, for example
`this is not valid csharp;`, refresh, and run the suite.

Expected: `MathUtils_InRange_IncludesTheBounds` fails, and only that test.
Revert the change afterwards.

- [ ] **Step 4: The command line run reports the same result**

```
Unity.exe -batchmode -runTests -testPlatform EditMode \
  -projectPath X:/CherryFramework -testResults results.xml -logFile test.log
```

Expected: `results.xml` lists no failures. This needs the editor to be
closed.
