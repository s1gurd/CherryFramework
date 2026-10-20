# CherryFramework — dependency installer

**Date:** 2026-10-20
**Status:** approved
**Scope:** a self-contained Editor tool that installs every CherryFramework
dependency into a fresh Unity project, and that keeps working while the
framework's own code does not compile.

---

## Problem

Copying `Assets/CherryFramework` into a new Unity project leaves the project
broken: the framework references four packages and one binary plugin that are
not there yet.

| Dependency | Needed by |
| --- | --- |
| `com.unity.nuget.newtonsoft-json` 3.2.2 | `SaveGameManager`, `DataModels` |
| `com.unity.ugui` 2.0.0 (contains TextMeshPro) | `UnityEngine.UI`, `TMPro` |
| `com.v0lt.editor-attributes` (git) | the `EditorAttributes` attribute |
| `com.annulusgames.unity-codegen` (git) | the DataModels code generator |
| DOTween (binary plugin) | `DG.Tweening` across UI, SoundService, SaveGameManager |

The bootstrap is circular: the thing that would fix the project cannot live in
the code that is broken. Any installer placed inside the framework's own
assembly inherits the failure.

Two further traps make a naive copy-and-paste install unreliable:

1. **GUID references.** `Cherry.Framework.asmdef` and
   `Cherry.Framework.Editor.asmdef` reference their dependencies by GUID.
   A freshly downloaded package, and a freshly unzipped DOTween, carry no
   `.meta` files, so Unity assigns new GUIDs and every one of those references
   breaks. The project stays uncompilable even after every dependency is
   present.
2. **The missing module assembly.** The official DOTween archive ships
   `Modules/*.cs` but no assembly definition. Without one, the modules
   (`DOFade`, `DOScale`, …) compile into the predefined `Assembly-CSharp`
   and become invisible to `Cherry.Framework`, which is an assembly
   definition.

## Goals

- One action installs every dependency of the framework into a fresh project.
- The installer compiles and runs even when `Cherry.Framework` and
  `Cherry.Framework.Editor` fail to compile.
- The result is a project that compiles with no manual steps and no missing
  references.
- The demo Sample is optional and ships as an archive inside the framework.
- Re-running the installer is safe and never duplicates anything.

## Non-goals

- Supporting Unity versions other than 6000.3.x.
- Installing the framework itself into the target project — the user copies
  `Assets/CherryFramework` by hand.
- Keeping `Assets/ThirdParty/Plugins/DOTween` in the target project in sync
  with the copy that lives in the framework repository.
- Running DOTween's own "Setup DOTween" utility panel. The installer
  replicates the one part of it that the framework actually needs.

## Design

### 1. Where the installer lives

```
Assets/CherryFramework/Editor/Installer/
├── Cherry.Framework.Installer.asmdef
├── CherryDependencies.cs
├── CherryInstaller.cs
├── PackageInstaller.cs
├── DotweenInstaller.cs
├── SampleInstaller.cs
├── SamplePackageBuilder.cs
└── Payload/
    ├── dependencies.json
    └── CherryFrameworkSample.zip
```

`Cherry.Framework.Installer.asmdef`:

```jsonc
{
    "name": "Cherry.Framework.Installer",
    "includePlatforms": ["Editor"],
    "references": [],
    "autoReferenced": false
}
```

Why this survives compile errors:

- The nested assembly definition takes over the folder, so the installer is
  not part of `Cherry.Framework`.
- `references` is empty. Unity compiles assemblies along the dependency
  graph: a failed assembly only blocks the assemblies that depend on it.
  Nothing depends on the installer, and the installer depends on nothing but
  the engine.
- `autoReferenced: false` keeps the user's `Assembly-CSharp` independent of
  the installer, in both directions.
- No framework type appears anywhere in the installer's code.

### 2. What gets installed

| Dependency | Source | Destination |
| --- | --- | --- |
| `com.unity.nuget.newtonsoft-json` 3.2.2 | UPM registry | `Packages/manifest.json` |
| `com.unity.ugui` 2.0.0 | UPM registry | `Packages/manifest.json` |
| `com.v0lt.editor-attributes` | `https://github.com/v0lt13/EditorAttributes.git` | `Packages/manifest.json` |
| `com.annulusgames.unity-codegen` | `https://github.com/AnnulusGames/UnityCodeGen.git?path=/Assets/UnityCodeGen` | `Packages/manifest.json` |
| DOTween 1.3.045 | `https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip` | `Assets/ThirdParty/Plugins/DOTween` |
| Sample *(optional)* | `Payload/CherryFrameworkSample.zip` | `Assets/Sample`, `Assets/Scripts/GeneratedDataModels` |
| `com.unity.inputsystem` 1.20.0 *(with Sample only)* | UPM registry | `Packages/manifest.json` |

The list lives in `Payload/dependencies.json` next to the code, so it can be
edited without touching C#.

### 3. Mechanisms

**Packages.** The installer reads `dependencies.json`, opens
`Packages/manifest.json`, and adds or replaces the entries while leaving
every unrelated entry untouched. It then calls
`UnityEditor.PackageManager.Client.Resolve()` to make the package manager
pick the manifest up.

**DOTween.** The archive is downloaded to a temporary file, its `DOTween/**`
entries are extracted into `Assets/ThirdParty/Plugins/DOTween`, and
`Modules/DOTween.Modules.asmdef` is created with `{"name": "DOTween.Modules"}`.
That assembly definition is what makes the module extension methods visible
to `Cherry.Framework`. The official archive does not contain it, and DOTween's
own setup panel would have to be run by hand to create it.

**Sample.** `Payload/CherryFrameworkSample.zip` is extracted, `.meta` files
included, into `Assets/Sample` and `Assets/Scripts/GeneratedDataModels`. The
archive keeps the original GUIDs, so the links between the scene, the
prefabs and the assets survive the round trip. Installing the Sample also
adds `com.unity.inputsystem` and sets `activeInputHandler` to `2` (Both) in
`ProjectSettings.asset`, which the Sample's `Player` script needs.

### 4. Assembly definitions switch from GUIDs to names

`Cherry.Framework.asmdef`:

```jsonc
"references": [
    "Unity.TextMeshPro",
    "EditorAttributes",
    "DOTween.Modules",
    "UnityEngine.UI"
]
```

`Cherry.Framework.Editor.asmdef`:

```jsonc
"references": [ "AnnulusGames.UnityCodeGen.Editor" ]
```

Names survive a re-import, a different package version and a fresh unzip;
GUIDs do not. `UnityEngine.UI` is added explicitly because it currently
arrives only transitively through TextMeshPro.

### 5. Trigger

`[InitializeOnLoadMethod]` runs when the domain loads. It checks the state —
are the packages in the manifest, is DOTween in place — and, if anything is
missing, schedules an `EditorApplication.delayCall` that asks:

> CherryFramework dependencies are not installed. Install them now?

`Install` runs the installer, `Later` skips. The prompt is shown at most once
per editor session, tracked through `SessionState`. A manual entry point,
`Tools → CherryFramework → Install Dependencies`, does the same thing on
demand.

### 6. Idempotency and error handling

Each step is wrapped in its own `try`/`catch`. The steps collect a report
that is shown when the run finishes, listing what was installed, what was
already present, and what failed.

- A package already in the manifest is left alone.
- DOTween already present is not downloaded again.
- An already extracted Sample is not extracted again.
- No network access fails only the DOTween step. The packages are still
  installed and the report says so explicitly.

### 7. The Sample archive

`Payload/CherryFrameworkSample.zip` holds `Assets/Sample` and
`Assets/Scripts/GeneratedDataModels`, `.meta` files included.

The framework repository keeps the Sample as live files, because the
repository *is* the demo project. The archive is the distribution form.
`Tools → CherryFramework → Rebuild Sample Package` regenerates it from the
live files, so the two cannot drift apart.

### 8. Repository changes

1. `Assets/CherryFramework/Cherry.Framework.asmdef` — references by name.
2. `Assets/CherryFramework/DataModels/Editor/Cherry.Framework.Editor.asmdef` —
   references by name.
3. New folder `Assets/CherryFramework/Editor/Installer/`.
4. `Payload/CherryFrameworkSample.zip` — the Sample archive.
5. `README.md` — the installation section is rewritten around the new tool.

## User flow

```
1. Create a new Unity 6000.3 project
2. Copy Assets/CherryFramework (with its .meta files) into Assets/
3. Open the project
   → the framework does not compile yet, the installer does
4. The prompt appears → Install
5. Packages resolve, DOTween downloads and unpacks, the Sample is unpacked
   if it was requested
6. Unity recompiles → no errors
```

## Verification

- A clean recipient project: copy `Assets/CherryFramework`, open it, run the
  installer, and check that `Packages/manifest.json` holds the four packages,
  that `Assets/ThirdParty/Plugins/DOTween/DOTween.dll` and
  `Modules/DOTween.Modules.asmdef` exist, and that the Console is free of
  compile errors.
- The same project, without installing anything, proves the installer
  compiles while the framework does not — the primary scenario.
- Re-running the installer must produce no changes.
