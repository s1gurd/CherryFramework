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
| `Assets/Tests/Shared/` | the fakes both test assemblies use |
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
into the Console. `Tools -> CherryFramework -> Run EditMode Tests` runs only
the fast half.

The editor has to be closed for the command line runs - Unity locks the
project folder. On this machine Unity 6000.3.25f1 lives in
`C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`.

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
- A bug the tests find is fixed in the same branch, with the test that found
  it as the regression.
