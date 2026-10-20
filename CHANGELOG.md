# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1] - 2026-10-20

The dependency installer, an automated test suite for the runtime, the binding
activation modes of the data models, a code generator that writes the models
next to their templates, and the runtime bugs that the tests found.

### Added

- **Dependency installer** (`Assets/CherryFramework/Editor/Installer/`). A
  separate assembly, `Cherry.Framework.Installer`, that references nothing of
  the framework, so it compiles and runs while the framework itself does not.
  It adds the four UPM packages to `Packages/manifest.json`, downloads DOTween
  from demigiant.com and unpacks it, and unpacks the Sample archive.
- **Automated test suite**, 145 tests in three assemblies:
  `Cherry.Framework.Tests` (edit mode), `Cherry.Framework.PlayMode.Tests`
  (play mode) and `Cherry.Framework.TestSupport` (the shared fakes). Every
  fixture is built in code - no prefab, no scene, no audio asset.
- **`Tools -> CherryFramework -> Run All Tests`** runs the edit mode tests and
  then the play mode tests and prints one summary per run.
  `Run EditMode Tests` runs only the fast half.
- **`AGENTS.md`** in the repository root: how to build the project, how to run
  the suite from the command line and from the editor, and the conventions a
  change has to follow.
- **Data models: binding activation modes.** The new `BindingActivation` enum
  (`InvokeImmediate`, `ActivateImmediate`, `InvokeOnReady`, `ActivateOnReady`)
  replaces the boolean `invokeImmediate` parameter of `Accessor.BindDownwards`,
  `Bindings.CreateBinding` and `DownwardBindingHandler`. A model that is not
  ready yet no longer delivers values to the handlers that wait for it.
- **Data models: `ReadyMode`** and the `Ready` flag with `ReadyAccessor`, plus
  the `OnReady` callbacks that are flushed when the model becomes ready.
- **Data models: transient models.** `DataModelService.GetOrCreateTransientModel<T>(id)`
  and `DataModelService.ReleaseTransientModel(...)` for models that belong to one
  object instead of to the whole project.
- **Save system: `UnRegister<T>`, `UnRegisterObject` and `SaveDataForObject`**
  on the `SaveGameManager`.
- **The Sample ships as an archive** inside the framework
  (`Payload/CherryFrameworkSample.zip`) and is unpacked only when the user
  asks for it.
- **Reflection results are cached**: the injection cache of the dependency
  container and the fill-from cache of the data models are built once per type
  instead of on every call.
- **The generator finds the source file of a template through the
  AssetDatabase**, so the model of a template lands in the `generated` folder
  next to that template, one `generated` folder per folder with templates.
  `Assets/Tests/Framework/ModelsGeneratorTests.cs` pins the rule.

### Fixed

- `DependencyContainer.Dispose()` did not clear the registry, so the container
  kept handing out disposed instances and refused every new binding of the
  same type.
- `StateService.RemoveSubscription` and `RemoveAllSubscriptions` ignored a
  subscription that was added in the same tick, so a callback ran after it had
  been unsubscribed.
- `ViewService.Back()` did not update `ActiveView`, so the view that was left
  was still reported as the active one.
- `OnAllViewsBecameInactive` never fired with `skipAnimation: true`, because
  the callback was appended to an already completed sequence.
- `InteractiveElementBase.animators` was null on a presenter that was added at
  runtime, and `InitializePresenter()` threw on it.
- `ViewUtils.GetPresenterType` only searched the framework assembly and
  required an exact base class, so a presenter of the game code, or one
  deriving from `PresenterLoadingBase`, was never found by name.
- `ViewService.PopView` dropped the `readyAccessor` instead of forwarding it.
- `Ticker.UnRegister` removed the object from the first registered phase only,
  and `Ticker.checkActivity` looked the object up under a different key than
  the one it was stored with.
- `SoundService.FadeOut` called the `(delay)` overload instead of the
  `(duration, delay)` one, so the fade waited instead of fading.
- `SaveGameManager.SaveData` did not null check the object id, so every
  component of a type without a guid overwrote the same storage slot.
- `PersistentObject` keeps its transform fields protected, so the reflection
  sees them on derived classes.
- The download of DOTween no longer deadlocks the editor. The await captured
  the main thread context while the main thread waited for the task.
- `IInjectTarget` and `DependencyUtils` are gone: the marker was never checked
  by the container and the extension method was never used.

### Changed

- **The assembly definitions reference their dependencies by name instead of
  by GUID.** A freshly imported package or a freshly unpacked DOTween carries
  no GUID, so the old references broke on every fresh install.
- **The code generator supports generic template models.** The model class
  gets a `<T1, T2>` suffix, the generic constraints are propagated, and the
  generated file becomes `BaseModelName.T1,T2.Generated.cs`.
- **The generated models are placed next to their templates.** The model of a
  template goes into the `generated` folder of the folder the template file
  was found in, and its namespace is the namespace of the template with a
  `generated` suffix. The namespace of a template has to end with `DataModels`.
- The generator no longer deletes the folder it writes to. It removes only the
  models of the templates that are gone, so the guids of the models that stay
  do not change on every run.
- **The data models of the framework live in `CherryFramework.ModelService`
  now.** The namespace `CherryFramework.DataModels` is gone, so the class
  `ModelService` is now `DataModelService`
  (`CherryFramework.ModelService.DataModelService`) and the storage bridges
  live in `CherryFramework.ModelService.ModelDataStorageBridges`.
- The `using` statements of the generated models changed with it. The key a
  model is stored under in `PlayerPrefs` carries the namespace of the model,
  so the data of the old keys is not found anymore.
- Setting `Ready` to false on a model that is already ready is a warning
  instead of an error, and deactivating a ready model warns as well.
- **The documentation was rewritten.** About 150 examples did not compile -
  private or internal members called from outside, wrong signatures, types
  that never existed. The behaviours that silently did nothing are documented
  now as well: `SimplePool` never activates an object, `GetEmitters("")`
  matches nothing, and the `StateService` only evaluates its conditions in
  the frames where something was emitted.
- Every system document in `Docs/` ends with a `Tests` section that names the
  test file pinning the behaviour of that system.

### Removed

- The folder `Assets/Scripts/GeneratedDataModels` and the demo template
  `ExampleData` of the framework. The Sample shows how a template is written.

[1.1]: https://github.com/s1gurd/CherryFramework/compare/1.0...1.1
[1.0]: https://github.com/s1gurd/CherryFramework/releases/tag/1.0
