# CherryFramework Dependency Installer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship an Editor tool inside `Assets/CherryFramework` that installs every CherryFramework dependency (4 UPM packages, DOTween, the optional Sample) into a fresh Unity project, and that compiles and runs even while the framework's own code does not.

**Architecture:** A nested assembly definition at `Assets/CherryFramework/Editor/Installer/` with an empty `references` array compiles independently of `Cherry.Framework`, so the framework failing to build never blocks the tool that fixes it. Every installer component takes an explicit `projectRoot` so the whole flow can be exercised against a temporary directory in tests.

**Tech Stack:** Unity 6000.3.25f1, C# / .NET Standard 2.1, `System.IO.Compression` (zip), `System.Net.Http` (download), `UnityEditor.PackageManager.Client` (resolve), NUnit Editor tests via `com.unity.test-framework`.

**Spec:** `Docs/superpowers/specs/2026-10-20-cherryframework-dependency-installer-design.md`

## Global Constraints

- Target Unity version is 6000.3.25f1. Nothing older is supported.
- The installer assembly must have **no** references to `Cherry.Framework` or `Cherry.Framework.Editor`, and no framework type may appear in its code.
- `Cherry.Framework.Installer` uses `"autoReferenced": false`.
- The installer must not depend on `com.unity.nuget.newtonsoft-json` — that package is one of the things it installs, so it is absent in the target project. Only `UnityEngine` / `UnityEditor` APIs and the BCL are allowed.
- DOTween source: `https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip` (linked from `http://dotween.demigiant.com/download.php`).
- Package set: `com.unity.nuget.newtonsoft-json` 3.2.2, `com.unity.ugui` 2.0.0, `com.v0lt.editor-attributes` (git), `com.annulusgames.unity-codegen` (git).
- Sample-only package: `com.unity.inputsystem` 1.20.0.
- Installing the Sample sets `activeInputHandler` to `2` (Both) in `ProjectSettings/ProjectSettings.asset`.
- Assembly definition references are switched from GUIDs to assembly **names**.
- Every file under `Assets/CherryFramework` keeps its `.meta` file; new files get one.

---

## File Structure

**New — shipped with the framework** (`Assets/CherryFramework/Editor/Installer/`):

| File | Responsibility |
| --- | --- |
| `Cherry.Framework.Installer.asmdef` | Isolated editor assembly, zero references |
| `CherryDependencies.cs` | Data model + JSON loading of the dependency list |
| `InstallState.cs` | Detects what is missing in a project |
| `ManifestEditor.cs` | Reads and patches `Packages/manifest.json` |
| `ZipArchiveUtility.cs` | Extracts and creates zip archives |
| `PackageInstaller.cs` | Applies UPM packages and triggers a resolve |
| `DotweenInstaller.cs` | Downloads and installs DOTween |
| `SampleInstaller.cs` | Installs the Sample archive and the input handler |
| `InstallReport.cs` | Result records for one install run |
| `CherryInstaller.cs` | Orchestrator, menu items, first-run prompt |
| `SamplePackageBuilder.cs` | Rebuilds the Sample archive from the repository |
| `Payload/dependencies.json` | The dependency list, editable without touching C# |
| `Payload/CherryFrameworkSample.zip` | The Sample, packaged |

**New — repository only, never shipped** (`Assets/Tests/Editor/`):

| File | Responsibility |
| --- | --- |
| `CherryFramework.Installer.Tests.asmdef` | Editor test assembly |
| `ManifestEditorTests.cs` | Manifest patching |
| `CherryDependenciesTests.cs` | Dependency list parsing |
| `InstallStateTests.cs` | State detection |
| `ZipArchiveUtilityTests.cs` | Zip round trip |
| `CherryInstallerIntegrationTests.cs` | Whole flow against a temporary project |

**Modified:**

| File | Change |
| --- | --- |
| `Assets/CherryFramework/Cherry.Framework.asmdef` | References by name |
| `Assets/CherryFramework/DataModels/Editor/Cherry.Framework.Editor.asmdef` | References by name |
| `Packages/manifest.json` | Declare `com.unity.test-framework` explicitly |
| `README.md` | Rewrite the installation section |

---

### Task 1: Assembly definitions reference assemblies by name

**Files:**
- Modify: `Assets/CherryFramework/Cherry.Framework.asmdef`
- Modify: `Assets/CherryFramework/DataModels/Editor/Cherry.Framework.Editor.asmdef`

**Interfaces:**
- Consumes: nothing.
- Produces: both assembly definitions resolve their dependencies by assembly name, so a freshly imported package or a freshly unzipped DOTween cannot break them.

- [ ] **Step 1: Rewrite `Cherry.Framework.asmdef`**

`Assets/CherryFramework/Cherry.Framework.asmdef`:

```json
{
    "name": "Cherry.Framework",
    "rootNamespace": "",
    "references": [
        "Unity.TextMeshPro",
        "EditorAttributes",
        "DOTween.Modules",
        "UnityEngine.UI"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

The three old GUID references map to `Unity.TextMeshPro`, `EditorAttributes` and `DOTween.Modules`. `UnityEngine.UI` is added explicitly — it used to arrive only transitively through TextMeshPro.

- [ ] **Step 2: Rewrite `Cherry.Framework.Editor.asmdef`**

`Assets/CherryFramework/DataModels/Editor/Cherry.Framework.Editor.asmdef`:

```json
{
    "name": "Cherry.Framework.Editor",
    "rootNamespace": "",
    "references": [
        "AnnulusGames.UnityCodeGen.Editor"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Verify the project still compiles**

Refresh the asset database and read the compilation errors:

```
unity_asset_import / AssetDatabase.Refresh, then unity_get_compilation_errors
```

Expected: no errors. If Unity reports `Assembly 'Cherry.Framework' will not be loaded due to errors: Unable to resolve reference`, the assembly name is wrong — check the exact `name` field of the target asmdef in `Library/PackageCache`.

- [ ] **Step 4: Commit**

```bash
git add Assets/CherryFramework/Cherry.Framework.asmdef Assets/CherryFramework/DataModels/Editor/Cherry.Framework.Editor.asmdef
git commit -m "refactor: reference framework dependencies by assembly name"
```

---

### Task 2: The installer assembly and the dependency list

**Files:**
- Create: `Assets/CherryFramework/Editor/Installer/Cherry.Framework.Installer.asmdef`
- Create: `Assets/CherryFramework/Editor/Installer/CherryDependencies.cs`
- Create: `Assets/CherryFramework/Editor/Installer/Payload/dependencies.json`
- Create: `Assets/Tests/Editor/CherryFramework.Installer.Tests.asmdef`
- Create: `Assets/Tests/Editor/CherryDependenciesTests.cs`
- Modify: `Packages/manifest.json`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `CherryFramework.Installer.CherryDependencies` — the parsed dependency list.
  - `CherryDependencies.LoadFromFile(string path)` → `CherryDependencies`.
  - `CherryDependencies.Load(string json)` → `CherryDependencies`.
  - `PackageEntry { public string id; public string version; }`
  - `DotweenDefinition { public string downloadUrl; public string version; public string installPath; public string modulesAssemblyName; public string modulesAssemblyFileName; }`
  - `SampleDefinition { public string payload; public string marker; }`

- [ ] **Step 1: Create the test assembly definition**

`Assets/Tests/Editor/CherryFramework.Installer.Tests.asmdef`:

```json
{
    "name": "CherryFramework.Installer.Tests",
    "rootNamespace": "",
    "references": [
        "Cherry.Framework.Installer",
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

- [ ] **Step 2: Declare the test framework in the repository manifest**

Add to `Packages/manifest.json` in the `dependencies` object, keeping the existing alphabetical-ish grouping:

```json
"com.unity.test-framework": "1.6.0",
```

`1.6.0` is the version already resolved in this repository - check
`Packages/packages-lock.json` before pinning a different one.

- [ ] **Step 3: Create the installer assembly definition**

`Assets/CherryFramework/Editor/Installer/Cherry.Framework.Installer.asmdef`:

```json
{
    "name": "Cherry.Framework.Installer",
    "rootNamespace": "",
    "references": [],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`references` is empty on purpose: nothing may depend on the framework from here. `autoReferenced` is false so the user's `Assembly-CSharp` stays independent of the installer.

- [ ] **Step 4: Write the failing test**

`Assets/Tests/Editor/CherryDependenciesTests.cs`:

```csharp
using System.IO;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class CherryDependenciesTests
    {
        private const string DependencyFile =
            "Assets/CherryFramework/Editor/Installer/Payload/dependencies.json";

        [Test]
        public void LoadFromFile_ReadsTheShippedDependencyList()
        {
            var dependencies = CherryDependencies.LoadFromFile(DependencyFile);

            Assert.IsNotNull(dependencies, "dependencies.json should parse");
            Assert.IsNotNull(dependencies.packages);
            Assert.IsNotNull(dependencies.dotween);
            Assert.IsNotNull(dependencies.sample);

            Assert.That(dependencies.packages, Has.Length.EqualTo(4));
            Assert.That(dependencies.samplePackages, Has.Length.EqualTo(1));

            var newtonsoft = Find(dependencies.packages, "com.unity.nuget.newtonsoft-json");
            Assert.AreEqual("3.2.2", newtonsoft.version);

            var ugui = Find(dependencies.packages, "com.unity.ugui");
            Assert.AreEqual("2.0.0", ugui.version);

            var editorAttributes = Find(dependencies.packages, "com.v0lt.editor-attributes");
            Assert.AreEqual("https://github.com/v0lt13/EditorAttributes.git", editorAttributes.version);

            var codeGen = Find(dependencies.packages, "com.annulusgames.unity-codegen");
            Assert.AreEqual(
                "https://github.com/AnnulusGames/UnityCodeGen.git?path=/Assets/UnityCodeGen",
                codeGen.version);

            Assert.AreEqual(
                "https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip",
                dependencies.dotween.downloadUrl);
            Assert.AreEqual("Assets/ThirdParty/Plugins/DOTween", dependencies.dotween.installPath);
            Assert.AreEqual("DOTween.Modules", dependencies.dotween.modulesAssemblyName);

            Assert.AreEqual("Assets/Sample/Scenes/dinoscene.unity", dependencies.sample.marker);
        }

        [Test]
        public void Load_MalformedJson_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => CherryDependencies.Load("this is not json"));
        }

        private static PackageEntry Find(PackageEntry[] entries, string id)
        {
            foreach (var entry in entries)
            {
                if (entry.id == id)
                {
                    return entry;
                }
            }

            Assert.Fail("Package not found in dependencies.json: " + id);
            return null;
        }
    }
}
```

- [ ] **Step 5: Run the test to verify it fails**

Open `Window → General → Test Runner`, select the `EditMode` tab, run `CherryFramework.Installer.Tests`.

Expected: FAIL — `CherryDependencies` does not exist.

- [ ] **Step 6: Implement the dependency model**

`Assets/CherryFramework/Editor/Installer/CherryDependencies.cs`:

```csharp
using System;
using System.IO;
using UnityEngine;

namespace CherryFramework.Installer
{
    [Serializable]
    public sealed class PackageEntry
    {
        public string id;
        public string version;
    }

    [Serializable]
    public sealed class DotweenDefinition
    {
        public string downloadUrl;
        public string version;
        public string installPath;
        public string modulesAssemblyName;
        public string modulesAssemblyFileName;
    }

    [Serializable]
    public sealed class SampleDefinition
    {
        public string payload;
        public string marker;
    }

    [Serializable]
    public sealed class CherryDependencies
    {
        public PackageEntry[] packages = new PackageEntry[0];
        public PackageEntry[] samplePackages = new PackageEntry[0];
        public DotweenDefinition dotween;
        public SampleDefinition sample;

        public static CherryDependencies LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Dependency list not found", path);
            }

            return Load(File.ReadAllText(path));
        }

        public static CherryDependencies Load(string json)
        {
            CherryDependencies dependencies;

            try
            {
                dependencies = JsonUtility.FromJson<CherryDependencies>(json);
            }
            catch (System.Exception exception)
            {
                throw new ArgumentException("Dependency list is not valid JSON", exception);
            }

            if (dependencies == null)
            {
                throw new ArgumentException("Dependency list is not valid JSON");
            }

            if (dependencies.packages == null)
            {
                dependencies.packages = new PackageEntry[0];
            }

            if (dependencies.samplePackages == null)
            {
                dependencies.samplePackages = new PackageEntry[0];
            }

            return dependencies;
        }
    }
}
```

`JsonUtility` is used instead of a JSON library on purpose: the installer must compile in a project where `com.unity.nuget.newtonsoft-json` is not installed yet.

- [ ] **Step 7: Write the dependency list**

`Assets/CherryFramework/Editor/Installer/Payload/dependencies.json`:

```json
{
  "packages": [
    {
      "id": "com.unity.nuget.newtonsoft-json",
      "version": "3.2.2"
    },
    {
      "id": "com.unity.ugui",
      "version": "2.0.0"
    },
    {
      "id": "com.v0lt.editor-attributes",
      "version": "https://github.com/v0lt13/EditorAttributes.git"
    },
    {
      "id": "com.annulusgames.unity-codegen",
      "version": "https://github.com/AnnulusGames/UnityCodeGen.git?path=/Assets/UnityCodeGen"
    }
  ],
  "samplePackages": [
    {
      "id": "com.unity.inputsystem",
      "version": "1.20.0"
    }
  ],
  "dotween": {
    "downloadUrl": "https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip",
    "version": "1.3.045",
    "installPath": "Assets/ThirdParty/Plugins/DOTween",
    "modulesAssemblyName": "DOTween.Modules",
    "modulesAssemblyFileName": "DOTween.Modules.asmdef"
  },
  "sample": {
    "payload": "Payload/CherryFrameworkSample.zip",
    "marker": "Assets/Sample/Scenes/dinoscene.unity"
  }
}
```

- [ ] **Step 8: Run the test to verify it passes**

Expected: PASS — both tests green.

- [ ] **Step 9: Commit**

```bash
git add Assets/CherryFramework/Editor/Installer Assets/Tests/Editor Packages/manifest.json
git commit -m "feat: add the installer assembly and the dependency list"
```

---

### Task 3: Patch `Packages/manifest.json`

**Files:**
- Create: `Assets/CherryFramework/Editor/Installer/ManifestEditor.cs`
- Create: `Assets/Tests/Editor/ManifestEditorTests.cs`

**Interfaces:**
- Consumes: `PackageEntry` from Task 2.
- Produces:
  - `ManifestEditor.ManifestRelativePath` → `const string` = `"Packages/manifest.json"`.
  - `ManifestEditor.ReadPackages(string manifestJson)` → `IReadOnlyDictionary<string, string>`.
  - `ManifestEditor.SetPackages(string manifestJson, IReadOnlyList<PackageEntry> packages)` → `string`.
  - `ManifestEditor.Read(string projectRoot)` → `string` (returns an empty manifest when the file does not exist).
  - `ManifestEditor.Write(string projectRoot, string manifestJson)` → `void`.

- [ ] **Step 1: Write the failing test**

`Assets/Tests/Editor/ManifestEditorTests.cs`:

```csharp
using System.Collections.Generic;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class ManifestEditorTests
    {
        private const string Manifest = @"{
  ""dependencies"": {
    ""com.unity.ugui"": ""2.0.0"",
    ""com.unity.timeline"": ""1.8.7""
  },
  ""scopedRegistries"": [
    {
      ""name"": ""Unity"",
      ""url"": ""https://packages.unity.com"",
      ""scopes"": [
        ""com.unity""
      ]
    }
  ]
}";

        [Test]
        public void ReadPackages_ReturnsEveryDependency()
        {
            var packages = ManifestEditor.ReadPackages(Manifest);

            Assert.AreEqual(2, packages.Count);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
            Assert.AreEqual("1.8.7", packages["com.unity.timeline"]);
        }

        [Test]
        public void SetPackages_AddsAMissingPackage()
        {
            var result = ManifestEditor.SetPackages(
                Manifest,
                new[] { new PackageEntry { id = "com.unity.nuget.newtonsoft-json", version = "3.2.2" } });

            var packages = ManifestEditor.ReadPackages(result);

            Assert.AreEqual("3.2.2", packages["com.unity.nuget.newtonsoft-json"]);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
            Assert.AreEqual("1.8.7", packages["com.unity.timeline"]);
            Assert.IsTrue(result.Contains("scopedRegistries"), "unrelated sections must survive");
        }

        [Test]
        public void SetPackages_ReplacesAnExistingPackage()
        {
            var result = ManifestEditor.SetPackages(
                Manifest,
                new[] { new PackageEntry { id = "com.unity.ugui", version = "2.0.1" } });

            var packages = ManifestEditor.ReadPackages(result);

            Assert.AreEqual("2.0.1", packages["com.unity.ugui"]);
            Assert.AreEqual(2, packages.Count, "the package must be replaced, not duplicated");
        }

        [Test]
        public void SetPackages_KeepsTheFormattingOfUntouchedEntries()
        {
            var result = ManifestEditor.SetPackages(
                Manifest,
                new[] { new PackageEntry { id = "com.unity.nuget.newtonsoft-json", version = "3.2.2" } });

            StringAssert.Contains("\"com.unity.timeline\": \"1.8.7\"", result);
        }

        [Test]
        public void SetPackages_HandlesAnEmptyDependencyBlock()
        {
            var result = ManifestEditor.SetPackages(
                "{ \"dependencies\": {} }",
                new[] { new PackageEntry { id = "com.unity.ugui", version = "2.0.0" } });

            var packages = ManifestEditor.ReadPackages(result);

            Assert.AreEqual(1, packages.Count);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
        }

        [Test]
        public void ReadPackages_OnGarbage_ReturnsNothing()
        {
            Assert.AreEqual(0, ManifestEditor.ReadPackages("not json at all").Count);
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Expected: FAIL — `ManifestEditor` does not exist.

- [ ] **Step 3: Implement the manifest editor**

`Assets/CherryFramework/Editor/Installer/ManifestEditor.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CherryFramework.Installer
{
    /// <summary>
    /// Reads and patches the "dependencies" object of a Unity package manifest.
    /// Only that object is touched; every other section is copied byte for byte.
    /// </summary>
    public static class ManifestEditor
    {
        public const string ManifestRelativePath = "Packages/manifest.json";

        public static string Read(string projectRoot)
        {
            var path = Path.Combine(projectRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(path) ? File.ReadAllText(path) : "{ \"dependencies\": {} }";
        }

        public static void Write(string projectRoot, string manifestJson)
        {
            var path = Path.Combine(projectRoot, ManifestRelativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, manifestJson);
        }

        public static IReadOnlyDictionary<string, string> ReadPackages(string manifestJson)
        {
            var result = new Dictionary<string, string>();
            var block = FindDependenciesBlock(manifestJson, out _);

            if (block == null)
            {
                return result;
            }

            foreach (var entry in ParseEntries(block))
            {
                result[entry.Key] = entry.Value;
            }

            return result;
        }

        public static string SetPackages(string manifestJson, IReadOnlyList<PackageEntry> packages)
        {
            if (packages == null || packages.Count == 0)
            {
                return manifestJson;
            }

            var wanted = new Dictionary<string, string>();
            foreach (var package in packages)
            {
                if (package == null || string.IsNullOrEmpty(package.id))
                {
                    continue;
                }

                wanted[package.id] = package.version;
            }

            var block = FindDependenciesBlock(manifestJson, out var blockStart);
            var body = block ?? "{}";

            var entries = ParseEntries(body);
            var kept = new List<string>();

            foreach (var entry in entries)
            {
                if (wanted.ContainsKey(entry.Key))
                {
                    continue;
                }

                kept.Add(entry.Raw);
            }

            foreach (var pair in wanted)
            {
                kept.Add("\"" + pair.Key + "\": \"" + pair.Value + "\"");
            }

            var indentation = GuessIndentation(body);
            var builder = new StringBuilder();

            if (kept.Count == 0)
            {
                builder.Append("{}");
            }
            else
            {
                builder.Append("{\n");

                for (var index = 0; index < kept.Count; index++)
                {
                    builder.Append(indentation).Append(kept[index]);

                    if (index < kept.Count - 1)
                    {
                        builder.Append(",");
                    }

                    builder.Append("\n");
                }

                builder.Append(GuessClosingIndentation(body)).Append("}");
            }

            var rebuilt = builder.ToString();

            if (block == null)
            {
                return InsertDependenciesBlock(manifestJson, rebuilt);
            }

            return manifestJson.Substring(0, blockStart) + rebuilt + manifestJson.Substring(blockStart + body.Length);
        }

        private struct Entry
        {
            public string Key;
            public string Value;
            public string Raw;
        }

        private static List<Entry> ParseEntries(string body)
        {
            var entries = new List<Entry>();
            var inner = body.Trim();
            inner = inner.TrimStart('{').TrimEnd('}');

            var index = 0;

            while (index < inner.Length)
            {
                while (index < inner.Length && (char.IsWhiteSpace(inner[index]) || inner[index] == ','))
                {
                    index++;
                }

                if (index >= inner.Length || inner[index] == '}')
                {
                    break;
                }

                var keyStart = index;
                var key = ReadString(inner, ref index);

                if (key == null)
                {
                    break;
                }

                while (index < inner.Length && char.IsWhiteSpace(inner[index]))
                {
                    index++;
                }

                if (index >= inner.Length || inner[index] != ':')
                {
                    break;
                }

                index++;

                while (index < inner.Length && char.IsWhiteSpace(inner[index]))
                {
                    index++;
                }

                var value = ReadString(inner, ref index);

                if (value == null)
                {
                    break;
                }

                var rawEnd = index;

                while (rawEnd < inner.Length && char.IsWhiteSpace(inner[rawEnd]))
                {
                    rawEnd++;
                }

                entries.Add(new Entry
                {
                    Key = key,
                    Value = value,
                    Raw = inner.Substring(keyStart, rawEnd - keyStart)
                });

                index = rawEnd;
            }

            return entries;
        }

        private static string ReadString(string text, ref int index)
        {
            if (index >= text.Length || text[index] != '"')
            {
                return null;
            }

            var builder = new StringBuilder();
            index++;

            while (index < text.Length)
            {
                var character = text[index];

                if (character == '\\' && index + 1 < text.Length)
                {
                    builder.Append(text[index + 1]);
                    index += 2;
                    continue;
                }

                if (character == '"')
                {
                    index++;
                    return builder.ToString();
                }

                builder.Append(character);
                index++;
            }

            return null;
        }

        private static string FindDependenciesBlock(string manifestJson, out int start)
        {
            start = -1;

            var keyIndex = manifestJson.IndexOf("\"dependencies\"", StringComparison.Ordinal);

            if (keyIndex < 0)
            {
                return null;
            }

            var cursor = manifestJson.IndexOf(':', keyIndex);

            if (cursor < 0)
            {
                return null;
            }

            cursor++;

            while (cursor < manifestJson.Length && char.IsWhiteSpace(manifestJson[cursor]))
            {
                cursor++;
            }

            if (cursor >= manifestJson.Length || manifestJson[cursor] != '{')
            {
                return null;
            }

            var depth = 0;
            var inString = false;

            for (var index = cursor; index < manifestJson.Length; index++)
            {
                var character = manifestJson[index];

                if (inString)
                {
                    if (character == '\\')
                    {
                        index++;
                    }
                    else if (character == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"')
                {
                    inString = true;
                }
                else if (character == '{')
                {
                    depth++;
                }
                else if (character == '}')
                {
                    depth--;

                    if (depth == 0)
                    {
                        start = cursor;
                        return manifestJson.Substring(cursor, index - cursor + 1);
                    }
                }
            }

            return null;
        }

        private static string InsertDependenciesBlock(string manifestJson, string block)
        {
            var closing = manifestJson.LastIndexOf('}');

            if (closing < 0)
            {
                return "{\n  \"dependencies\": " + block + "\n}";
            }

            var head = manifestJson.Substring(0, closing).TrimEnd();

            if (!head.EndsWith("{", StringComparison.Ordinal))
            {
                head += ",";
            }

            return head + "\n  \"dependencies\": " + block + "\n" + manifestJson.Substring(closing);
        }

        private static string GuessIndentation(string body)
        {
            return GuessClosingIndentation(body) + "  ";
        }

        private static string GuessClosingIndentation(string body)
        {
            foreach (var line in body.Split('\n'))
            {
                if (line.Trim() == "}")
                {
                    return line.Substring(0, line.Length - line.TrimStart().Length);
                }
            }

            return "  ";
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Expected: PASS — all six tests green.

- [ ] **Step 5: Commit**

```bash
git add Assets/CherryFramework/Editor/Installer/ManifestEditor.cs Assets/Tests/Editor/ManifestEditorTests.cs
git commit -m "feat: patch the package manifest without disturbing other entries"
```

---

### Task 4: Detect what is missing

**Files:**
- Create: `Assets/CherryFramework/Editor/Installer/InstallState.cs`
- Create: `Assets/Tests/Editor/InstallStateTests.cs`

**Interfaces:**
- Consumes: `CherryDependencies` from Task 2.
- Produces:
  - `InstallState.Evaluate(CherryDependencies dependencies, string projectRoot, bool includeSample)` → `InstallState`.
  - `InstallState.MissingPackages` → `IReadOnlyList<string>`.
  - `InstallState.DotweenMissing` → `bool`.
  - `InstallState.SampleMissing` → `bool`.
  - `InstallState.IsComplete` → `bool`.

- [ ] **Step 1: Write the failing test**

`Assets/Tests/Editor/InstallStateTests.cs`:

```csharp
using System.IO;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class InstallStateTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-install-state-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void Evaluate_OnAnEmptyProject_ReportsEverythingMissing()
        {
            var dependencies = CherryDependencies.LoadFromFile(
                "Assets/CherryFramework/Editor/Installer/Payload/dependencies.json");

            var state = InstallState.Evaluate(dependencies, _root, true);

            Assert.AreEqual(4, state.MissingPackages.Count);
            Assert.IsTrue(state.DotweenMissing);
            Assert.IsTrue(state.SampleMissing);
            Assert.IsFalse(state.IsComplete);
        }

        [Test]
        public void Evaluate_WhenEverythingIsPresent_ReportsComplete()
        {
            var dependencies = CherryDependencies.LoadFromFile(
                "Assets/CherryFramework/Editor/Installer/Payload/dependencies.json");

            WriteManifest(_root, dependencies);
            WriteFile(_root, "Assets/ThirdParty/Plugins/DOTween/DOTween.dll", "binary");
            WriteFile(_root, "Assets/ThirdParty/Plugins/DOTween/Modules/DOTween.Modules.asmdef", "{}");
            WriteFile(_root, dependencies.sample.marker, "scene");

            var state = InstallState.Evaluate(dependencies, _root, true);

            Assert.AreEqual(0, state.MissingPackages.Count);
            Assert.IsFalse(state.DotweenMissing);
            Assert.IsFalse(state.SampleMissing);
            Assert.IsTrue(state.IsComplete);
        }

        [Test]
        public void Evaluate_WithoutTheSample_SkipsTheSampleCheck()
        {
            var dependencies = CherryDependencies.LoadFromFile(
                "Assets/CherryFramework/Editor/Installer/Payload/dependencies.json");

            WriteManifest(_root, dependencies);
            WriteFile(_root, "Assets/ThirdParty/Plugins/DOTween/DOTween.dll", "binary");
            WriteFile(_root, "Assets/ThirdParty/Plugins/DOTween/Modules/DOTween.Modules.asmdef", "{}");

            var state = InstallState.Evaluate(dependencies, _root, false);

            Assert.IsFalse(state.SampleMissing);
        }

        [Test]
        public void Evaluate_WithoutTheModulesAssembly_ReportsDotweenMissing()
        {
            var dependencies = CherryDependencies.LoadFromFile(
                "Assets/CherryFramework/Editor/Installer/Payload/dependencies.json");

            WriteManifest(_root, dependencies);
            WriteFile(_root, "Assets/ThirdParty/Plugins/DOTween/DOTween.dll", "binary");

            var state = InstallState.Evaluate(dependencies, _root, false);

            Assert.IsTrue(state.DotweenMissing, "the modules assembly is what makes the modules usable");
        }

        private void WriteManifest(string root, CherryDependencies dependencies)
        {
            var json = ManifestEditor.SetPackages("{ \"dependencies\": {} }", dependencies.packages);
            ManifestEditor.Write(root, json);
        }

        private static void WriteFile(string root, string relativePath, string content)
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, content);
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Expected: FAIL — `InstallState` does not exist.

- [ ] **Step 3: Implement the state detection**

`Assets/CherryFramework/Editor/Installer/InstallState.cs`:

```csharp
using System.Collections.Generic;
using System.IO;

namespace CherryFramework.Installer
{
    /// <summary>
    /// Tells what a project still needs before the framework can compile.
    /// </summary>
    public sealed class InstallState
    {
        private readonly List<string> _missingPackages;

        private InstallState(List<string> missingPackages, bool dotweenMissing, bool sampleMissing)
        {
            _missingPackages = missingPackages;
            DotweenMissing = dotweenMissing;
            SampleMissing = sampleMissing;
        }

        public IReadOnlyList<string> MissingPackages => _missingPackages;
        public bool DotweenMissing { get; }
        public bool SampleMissing { get; }
        public bool IsComplete => _missingPackages.Count == 0 && !DotweenMissing && !SampleMissing;

        public static InstallState Evaluate(CherryDependencies dependencies, string projectRoot, bool includeSample)
        {
            var missing = new List<string>();

            if (dependencies == null)
            {
                return new InstallState(missing, true, true);
            }

            var manifest = ManifestEditor.ReadPackages(ManifestEditor.Read(projectRoot));

            foreach (var package in dependencies.packages)
            {
                if (package == null || string.IsNullOrEmpty(package.id))
                {
                    continue;
                }

                if (!manifest.ContainsKey(package.id))
                {
                    missing.Add(package.id);
                }
            }

            var dotweenMissing = !IsDotweenInstalled(dependencies.dotween, projectRoot);
            var sampleMissing = includeSample && !IsSampleInstalled(dependencies.sample, projectRoot);

            return new InstallState(missing, dotweenMissing, sampleMissing);
        }

        private static bool IsDotweenInstalled(DotweenDefinition definition, string projectRoot)
        {
            if (definition == null)
            {
                return true;
            }

            var dll = Path.Combine(
                projectRoot,
                (definition.installPath + "/DOTween.dll").Replace('/', Path.DirectorySeparatorChar));

            var modules = Path.Combine(
                projectRoot,
                (definition.installPath + "/Modules/" + definition.modulesAssemblyFileName).Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(dll) && File.Exists(modules);
        }

        private static bool IsSampleInstalled(SampleDefinition definition, string projectRoot)
        {
            if (definition == null || string.IsNullOrEmpty(definition.marker))
            {
                return false;
            }

            var marker = Path.Combine(projectRoot, definition.marker.Replace('/', Path.DirectorySeparatorChar));

            return File.Exists(marker);
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Expected: PASS — all four tests green.

- [ ] **Step 5: Commit**

```bash
git add Assets/CherryFramework/Editor/Installer/InstallState.cs Assets/Tests/Editor/InstallStateTests.cs
git commit -m "feat: detect missing dependencies in a project"
```

---

### Task 5: Zip extraction and creation

**Files:**
- Create: `Assets/CherryFramework/Editor/Installer/ZipArchiveUtility.cs`
- Create: `Assets/Tests/Editor/ZipArchiveUtilityTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `ZipArchiveUtility.Extract(string zipPath, string targetRoot, string stripRootPrefix)` → `void`. Entries whose path starts with `stripRootPrefix` are written under `targetRoot` with that prefix removed; entries outside it are skipped.
  - `ZipArchiveUtility.Create(string zipPath, IReadOnlyList<ZipSourceFolder> folders)` → `void`. `ZipSourceFolder { public string sourceDirectory; public string entryPrefix; }` — the prefix is prepended to every entry path, so `Assets/Sample` becomes `Assets/Sample/...` inside the archive.

- [ ] **Step 1: Write the failing test**

`Assets/Tests/Editor/ZipArchiveUtilityTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class ZipArchiveUtilityTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-zip-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void Extract_StripsTheRootPrefix()
        {
            var zip = Path.Combine(_root, "sample.zip");

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Write(archive, "DOTween/DOTween.dll", "dll");
                Write(archive, "DOTween/Modules/DOTweenModuleUI.cs", "code");
                Write(archive, "other/ignored.txt", "ignored");
            }

            var target = Path.Combine(_root, "out");
            ZipArchiveUtility.Extract(zip, target, "DOTween/");

            Assert.IsTrue(File.Exists(Path.Combine(target, "DOTween.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(target, "Modules", "DOTweenModuleUI.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(target, "ignored.txt")), "entries outside the prefix are skipped");
            Assert.IsFalse(File.Exists(Path.Combine(target, "other", "ignored.txt")));
        }

        [Test]
        public void Extract_OverwritesExistingFiles()
        {
            var zip = Path.Combine(_root, "sample.zip");

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Write(archive, "root/file.txt", "new");
            }

            var target = Path.Combine(_root, "out");
            Directory.CreateDirectory(Path.Combine(target, "root"));
            File.WriteAllText(Path.Combine(target, "root", "file.txt"), "old");

            ZipArchiveUtility.Extract(zip, target, "root/");

            Assert.AreEqual("new", File.ReadAllText(Path.Combine(target, "root", "file.txt")));
        }

        [Test]
        public void Create_KeepsTheEntryPrefixAndTheMetaFiles()
        {
            var source = Path.Combine(_root, "source");
            Directory.CreateDirectory(Path.Combine(source, "Sub"));
            File.WriteAllText(Path.Combine(source, "a.cs"), "code");
            File.WriteAllText(Path.Combine(source, "a.cs.meta"), "meta");
            File.WriteAllText(Path.Combine(source, "Sub", "b.png"), "png");

            var zip = Path.Combine(_root, "out.zip");

            ZipArchiveUtility.Create(zip, new List<ZipSourceFolder>
            {
                new ZipSourceFolder { sourceDirectory = source, entryPrefix = "Assets/Sample" }
            });

            using (var archive = ZipFile.OpenRead(zip))
            {
                var names = new List<string>();
                foreach (var entry in archive.Entries)
                {
                    names.Add(entry.FullName);
                }

                CollectionAssert.Contains(names, "Assets/Sample/a.cs");
                CollectionAssert.Contains(names, "Assets/Sample/a.cs.meta");
                CollectionAssert.Contains(names, "Assets/Sample/Sub/b.png");
            }
        }

        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(content);
            }
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Expected: FAIL — `ZipArchiveUtility` and `ZipSourceFolder` do not exist.

- [ ] **Step 3: Implement the zip utility**

`Assets/CherryFramework/Editor/Installer/ZipArchiveUtility.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace CherryFramework.Installer
{
    public sealed class ZipSourceFolder
    {
        public string sourceDirectory;
        public string entryPrefix;
    }

    /// <summary>
    /// Reads and writes the archives the installer ships with and downloads.
    /// </summary>
    public static class ZipArchiveUtility
    {
        public static void Extract(string zipPath, string targetRoot, string stripRootPrefix)
        {
            Directory.CreateDirectory(targetRoot);

            using (var archive = ZipFile.OpenRead(zipPath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("/", System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(stripRootPrefix) &&
                        !entry.FullName.StartsWith(stripRootPrefix, System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var relative = string.IsNullOrEmpty(stripRootPrefix)
                        ? entry.FullName
                        : entry.FullName.Substring(stripRootPrefix.Length);

                    if (relative.Length == 0)
                    {
                        continue;
                    }

                    var destination = Path.Combine(targetRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                    var directory = Path.GetDirectoryName(destination);

                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    entry.ExtractToFile(destination, true);
                }
            }
        }

        public static void Create(string zipPath, IReadOnlyList<ZipSourceFolder> folders)
        {
            var directory = Path.GetDirectoryName(zipPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var folder in folders)
                {
                    if (string.IsNullOrEmpty(folder.sourceDirectory) || !Directory.Exists(folder.sourceDirectory))
                    {
                        continue;
                    }

                    var prefix = string.IsNullOrEmpty(folder.entryPrefix)
                        ? string.Empty
                        : folder.entryPrefix.Replace('\\', '/').TrimEnd('/') + "/";

                    foreach (var file in Directory.GetFiles(folder.sourceDirectory, "*", SearchOption.AllDirectories))
                    {
                        var relative = file.Substring(folder.sourceDirectory.Length)
                            .TrimStart(Path.DirectorySeparatorChar, '/')
                            .Replace('\\', '/');

                        var entry = archive.CreateEntry(prefix + relative, CompressionLevel.Optimal);

                        using (var source = File.OpenRead(file))
                        using (var target = entry.Open())
                        {
                            source.CopyTo(target);
                        }
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Expected: PASS — all three tests green.

- [ ] **Step 5: Commit**

```bash
git add Assets/CherryFramework/Editor/Installer/ZipArchiveUtility.cs Assets/Tests/Editor/ZipArchiveUtilityTests.cs
git commit -m "feat: read and write the archives the installer ships"
```

---

### Task 6: Install the UPM packages

**Files:**
- Create: `Assets/CherryFramework/Editor/Installer/PackageInstaller.cs`

**Interfaces:**
- Consumes: `ManifestEditor` (Task 3), `PackageEntry` (Task 2).
- Produces:
  - `PackageInstaller.Install(string projectRoot, IReadOnlyList<PackageEntry> packages)` → `PackageInstallResult`.
  - `PackageInstallResult { public List<string> Installed; public List<string> AlreadyPresent; public List<string> Failed; public string Error; }`

- [ ] **Step 1: Write the failing test**

`Assets/Tests/Editor/PackageInstallerTests.cs`:

```csharp
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class PackageInstallerTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-packages-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void Install_AddsThePackagesToTheManifest()
        {
            var result = PackageInstaller.Install(_root, new List<PackageEntry>
            {
                new PackageEntry { id = "com.unity.nuget.newtonsoft-json", version = "3.2.2" },
                new PackageEntry { id = "com.unity.ugui", version = "2.0.0" }
            });

            Assert.AreEqual(0, result.Failed.Count, result.Error);
            Assert.AreEqual(2, result.Installed.Count);

            var packages = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));

            Assert.AreEqual("3.2.2", packages["com.unity.nuget.newtonsoft-json"]);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
        }

        [Test]
        public void Install_IsIdempotent()
        {
            var packages = new List<PackageEntry>
            {
                new PackageEntry { id = "com.unity.ugui", version = "2.0.0" }
            };

            PackageInstaller.Install(_root, packages);
            var second = PackageInstaller.Install(_root, packages);

            Assert.AreEqual(0, second.Installed.Count);
            Assert.AreEqual(1, second.AlreadyPresent.Count);

            var manifest = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));
            Assert.AreEqual(1, manifest.Count, "the package must not be duplicated");
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Expected: FAIL — `PackageInstaller` does not exist.

- [ ] **Step 3: Implement the package installer**

`Assets/CherryFramework/Editor/Installer/PackageInstaller.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.IO;

namespace CherryFramework.Installer
{
    public sealed class PackageInstallResult
    {
        public readonly List<string> Installed = new List<string>();
        public readonly List<string> AlreadyPresent = new List<string>();
        public readonly List<string> Failed = new List<string>();
        public string Error;
    }

    /// <summary>
    /// Adds packages to the project manifest. The manifest is edited on disk
    /// rather than through PackageManager.Client.Add, because two of the
    /// dependencies are git URLs and the client only accepts registry names.
    /// </summary>
    public static class PackageInstaller
    {
        public static PackageInstallResult Install(string projectRoot, IReadOnlyList<PackageEntry> packages)
        {
            var result = new PackageInstallResult();

            if (packages == null || packages.Count == 0)
            {
                return result;
            }

            try
            {
                var manifestJson = ManifestEditor.Read(projectRoot);
                var current = ManifestEditor.ReadPackages(manifestJson);
                var missing = new List<PackageEntry>();

                foreach (var package in packages)
                {
                    if (package == null || string.IsNullOrEmpty(package.id))
                    {
                        continue;
                    }

                    if (current.ContainsKey(package.id) &&
                        string.Equals(current[package.id], package.version, StringComparison.Ordinal))
                    {
                        result.AlreadyPresent.Add(package.id);
                        continue;
                    }

                    missing.Add(package);
                }

                if (missing.Count == 0)
                {
                    return result;
                }

                ManifestEditor.Write(projectRoot, ManifestEditor.SetPackages(manifestJson, missing));

                foreach (var package in missing)
                {
                    result.Installed.Add(package.id);
                }
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }

            return result;
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Expected: PASS — both tests green.

- [ ] **Step 5: Commit**

```bash
git add Assets/CherryFramework/Editor/Installer/PackageInstaller.cs Assets/Tests/Editor/PackageInstallerTests.cs
git commit -m "feat: install UPM packages through the manifest"
```

---

### Task 7: Download and install DOTween

**Files:**
- Create: `Assets/CherryFramework/Editor/Installer/DotweenInstaller.cs`
- Create: `Assets/Tests/Editor/DotweenInstallerTests.cs`

**Interfaces:**
- Consumes: `ZipArchiveUtility` (Task 5), `DotweenDefinition` (Task 2).
- Produces:
  - `DotweenInstaller.Install(string projectRoot, DotweenDefinition definition, string workingDirectory)` → `DotweenInstallResult`.
  - `DotweenInstaller.WriteModulesAssembly(string installDirectory, DotweenDefinition definition)` → `void` — writes `Modules/<modulesAssemblyFileName>`.
  - `DotweenInstallResult { public bool Installed; public bool Skipped; public string Error; public string DownloadedTo; }`

- [ ] **Step 1: Write the failing test**

`Assets/Tests/Editor/DotweenInstallerTests.cs`:

```csharp
using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class DotweenInstallerTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-dotween-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void WriteModulesAssembly_CreatesTheAssemblyTheFrameworkReferences()
        {
            var installDirectory = Path.Combine(_root, "DOTween");
            var definition = new DotweenDefinition
            {
                downloadUrl = "https://example.invalid/DOTween.zip",
                version = "1.3.045",
                installPath = "Assets/ThirdParty/Plugins/DOTween",
                modulesAssemblyName = "DOTween.Modules",
                modulesAssemblyFileName = "DOTween.Modules.asmdef"
            };

            DotweenInstaller.WriteModulesAssembly(installDirectory, definition);

            var path = Path.Combine(installDirectory, "Modules", "DOTween.Modules.asmdef");

            Assert.IsTrue(File.Exists(path));

            var json = File.ReadAllText(path);

            StringAssert.Contains("\"name\": \"DOTween.Modules\"", json);
        }

        [Test]
        public void Install_ExtractsTheArchiveAndCreatesTheModulesAssembly()
        {
            var zip = Path.Combine(_root, "dotween.zip");

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Write(archive, "DOTween/DOTween.dll", "dll");
                Write(archive, "DOTween/Modules/DOTweenModuleUI.cs", "code");
            }

            var definition = new DotweenDefinition
            {
                downloadUrl = "file:///" + zip.Replace('\\', '/'),
                version = "1.3.045",
                installPath = "Assets/ThirdParty/Plugins/DOTween",
                modulesAssemblyName = "DOTween.Modules",
                modulesAssemblyFileName = "DOTween.Modules.asmdef"
            };

            var result = DotweenInstaller.Install(_root, definition, _root);

            Assert.IsTrue(result.Installed, result.Error);
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween", "DOTween.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween", "Modules", "DOTweenModuleUI.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween", "Modules", "DOTween.Modules.asmdef")));
        }

        [Test]
        public void Install_WhenDotweenIsAlreadyThere_Skips()
        {
            var definition = new DotweenDefinition
            {
                downloadUrl = "https://example.invalid/DOTween.zip",
                version = "1.3.045",
                installPath = "Assets/ThirdParty/Plugins/DOTween",
                modulesAssemblyName = "DOTween.Modules",
                modulesAssemblyFileName = "DOTween.Modules.asmdef"
            };

            var dll = Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween", "DOTween.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(dll));
            File.WriteAllText(dll, "dll");

            var modules = Path.Combine(
                _root, "Assets", "ThirdParty", "Plugins", "DOTween", "Modules", "DOTween.Modules.asmdef");
            Directory.CreateDirectory(Path.GetDirectoryName(modules));
            File.WriteAllText(modules, "{}");

            var result = DotweenInstaller.Install(_root, definition, _root);

            Assert.IsTrue(result.Skipped);
            Assert.IsFalse(result.Installed);
        }

        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(content);
            }
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Expected: FAIL — `DotweenInstaller` does not exist.

- [ ] **Step 3: Implement the DOTween installer**

`Assets/CherryFramework/Editor/Installer/DotweenInstaller.cs`:

```csharp
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CherryFramework.Installer
{
    public sealed class DotweenInstallResult
    {
        public bool Installed;
        public bool Skipped;
        public string Error;
        public string DownloadedTo;
    }

    /// <summary>
    /// Downloads the official DOTween archive and unpacks it into the project.
    /// The archive has no assembly definition for its Modules folder, so one is
    /// written; without it the module extension methods stay invisible to
    /// Cherry.Framework.
    /// </summary>
    public static class DotweenInstaller
    {
        private const string ArchiveRoot = "DOTween/";

        public static DotweenInstallResult Install(string projectRoot, DotweenDefinition definition, string workingDirectory)
        {
            var result = new DotweenInstallResult();

            if (definition == null || string.IsNullOrEmpty(definition.downloadUrl))
            {
                result.Error = "The dependency list has no DOTween download URL";
                return result;
            }

            var installDirectory = Path.Combine(
                projectRoot,
                definition.installPath.Replace('/', Path.DirectorySeparatorChar));

            var dllPath = Path.Combine(installDirectory, "DOTween.dll");
            var modulesPath = Path.Combine(
                installDirectory, "Modules", definition.modulesAssemblyFileName ?? "DOTween.Modules.asmdef");

            if (File.Exists(dllPath) && File.Exists(modulesPath))
            {
                result.Skipped = true;
                return result;
            }

            try
            {
                Directory.CreateDirectory(workingDirectory);

                var archivePath = Path.Combine(workingDirectory, "dotween.zip");

                Download(definition.downloadUrl, archivePath);
                result.DownloadedTo = archivePath;

                ZipArchiveUtility.Extract(archivePath, installDirectory, ArchiveRoot);

                WriteModulesAssembly(installDirectory, definition);

                result.Installed = true;
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }

            return result;
        }

        public static void WriteModulesAssembly(string installDirectory, DotweenDefinition definition)
        {
            var fileName = definition.modulesAssemblyFileName ?? "DOTween.Modules.asmdef";
            var modulesDirectory = Path.Combine(installDirectory, "Modules");

            Directory.CreateDirectory(modulesDirectory);

            var json =
                "{\n" +
                "    \"name\": \"" + definition.modulesAssemblyName + "\",\n" +
                "    \"rootNamespace\": \"\",\n" +
                "    \"references\": [],\n" +
                "    \"includePlatforms\": [],\n" +
                "    \"excludePlatforms\": [],\n" +
                "    \"allowUnsafeCode\": false,\n" +
                "    \"overrideReferences\": false,\n" +
                "    \"precompiledReferences\": [],\n" +
                "    \"autoReferenced\": true,\n" +
                "    \"defineConstraints\": [],\n" +
                "    \"versionDefines\": [],\n" +
                "    \"noEngineReferences\": false\n" +
                "}\n";

            File.WriteAllText(Path.Combine(modulesDirectory, fileName), json);
        }

        private static void Download(string url, string destination)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsFile)
            {
                File.Copy(uri.LocalPath, destination, true);
                return;
            }

            var task = DownloadAsync(url, destination);

            if (!task.Wait(TimeSpan.FromMinutes(5)))
            {
                throw new TimeoutException("The DOTween download timed out");
            }

            task.GetAwaiter().GetResult();
        }

        private static async Task DownloadAsync(string url, string destination)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using (var source = await response.Content.ReadAsStreamAsync())
                using (var target = File.Create(destination))
                {
                    await source.CopyToAsync(target);
                }
            }
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Expected: PASS — all three tests green. The `file:///` URL is handled by
`DotweenInstaller.Download`, which copies the file directly instead of going
through `HttpClient`.

- [ ] **Step 5: Verify the real download once, by hand**

Run this from the Unity console via `unity_execute_code`, with the working directory set to a temporary folder so nothing touches the project:

```csharp
var definition = new CherryFramework.Installer.DotweenDefinition
{
    downloadUrl = "https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip",
    version = "1.3.045",
    installPath = "Assets/ThirdParty/Plugins/DOTween",
    modulesAssemblyName = "DOTween.Modules",
    modulesAssemblyFileName = "DOTween.Modules.asmdef"
};
var temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cherry-dotween-live");
var result = CherryFramework.Installer.DotweenInstaller.Install(temp, definition, temp);
return "installed=" + result.Installed + " skipped=" + result.Skipped + " error=" + result.Error
    + " dll=" + System.IO.File.Exists(System.IO.Path.Combine(temp, "Assets", "ThirdParty", "Plugins", "DOTween", "DOTween.dll"));
```

Expected: `installed=True`, `dll=True`, `error=` empty.

- [ ] **Step 6: Commit**

```bash
git add Assets/CherryFramework/Editor/Installer/DotweenInstaller.cs Assets/Tests/Editor/DotweenInstallerTests.cs
git commit -m "feat: download and install DOTween"
```

---

### Task 8: Install the Sample

**Files:**
- Create: `Assets/CherryFramework/Editor/Installer/SampleInstaller.cs`
- Create: `Assets/Tests/Editor/SampleInstallerTests.cs`

**Interfaces:**
- Consumes: `ZipArchiveUtility` (Task 5), `SampleDefinition` (Task 2).
- Produces:
  - `SampleInstaller.Install(string projectRoot, string payloadZipPath, SampleDefinition definition)` → `SampleInstallResult`.
  - `SampleInstaller.SetActiveInputHandler(string projectRoot, int value)` → `bool`.
  - `SampleInstallResult { public bool Installed; public bool Skipped; public string Error; }`

- [ ] **Step 1: Write the failing test**

`Assets/Tests/Editor/SampleInstallerTests.cs`:

```csharp
using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class SampleInstallerTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-sample-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void Install_UnpacksTheArchiveIntoTheProject()
        {
            var zip = Path.Combine(_root, "sample.zip");

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Write(archive, "Assets/Sample/Scenes/dinoscene.unity", "scene");
                Write(archive, "Assets/Sample/Scenes/dinoscene.unity.meta", "meta");
                Write(archive, "Assets/Scripts/GeneratedDataModels/GameStateDataModel.Generated.cs", "code");
            }

            var definition = new SampleDefinition
            {
                payload = "Payload/CherryFrameworkSample.zip",
                marker = "Assets/Sample/Scenes/dinoscene.unity"
            };

            var result = SampleInstaller.Install(_root, zip, definition);

            Assert.IsTrue(result.Installed, result.Error);
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity.meta")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Scripts", "GeneratedDataModels", "GameStateDataModel.Generated.cs")));
        }

        [Test]
        public void Install_WhenTheSampleIsAlreadyThere_Skips()
        {
            var definition = new SampleDefinition
            {
                payload = "Payload/CherryFrameworkSample.zip",
                marker = "Assets/Sample/Scenes/dinoscene.unity"
            };

            var marker = Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity");
            Directory.CreateDirectory(Path.GetDirectoryName(marker));
            File.WriteAllText(marker, "scene");

            var result = SampleInstaller.Install(_root, Path.Combine(_root, "missing.zip"), definition);

            Assert.IsTrue(result.Skipped);
        }

        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(content);
            }
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Expected: FAIL — `SampleInstaller` does not exist.

- [ ] **Step 3: Implement the Sample installer**

`Assets/CherryFramework/Editor/Installer/SampleInstaller.cs`:

```csharp
using System;
using System.IO;

namespace CherryFramework.Installer
{
    public sealed class SampleInstallResult
    {
        public bool Installed;
        public bool Skipped;
        public string Error;
    }

    /// <summary>
    /// Unpacks the Sample archive into the project. The archive mirrors the
    /// project layout, so its entries land in the right place unchanged, and
    /// it carries the .meta files so GUIDs survive the round trip.
    /// </summary>
    public static class SampleInstaller
    {
        public static SampleInstallResult Install(string projectRoot, string payloadZipPath, SampleDefinition definition)
        {
            var result = new SampleInstallResult();

            if (definition == null || string.IsNullOrEmpty(definition.marker))
            {
                result.Error = "The dependency list has no Sample marker";
                return result;
            }

            var marker = Path.Combine(projectRoot, definition.marker.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(marker))
            {
                result.Skipped = true;
                return result;
            }

            if (!File.Exists(payloadZipPath))
            {
                result.Error = "The Sample archive is missing: " + payloadZipPath;
                return result;
            }

            try
            {
                ZipArchiveUtility.Extract(payloadZipPath, projectRoot, string.Empty);
                result.Installed = true;
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }

            return result;
        }
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Expected: PASS — both tests green.

- [ ] **Step 5: Commit**

```bash
git add Assets/CherryFramework/Editor/Installer/SampleInstaller.cs Assets/Tests/Editor/SampleInstallerTests.cs
git commit -m "feat: unpack the Sample archive into the project"
```

---

### Task 9: Build the Sample archive

**Files:**
- Create: `Assets/CherryFramework/Editor/Installer/SamplePackageBuilder.cs`
- Create: `Assets/CherryFramework/Editor/Installer/Payload/CherryFrameworkSample.zip` (generated)

**Interfaces:**
- Consumes: `ZipArchiveUtility` (Task 5).
- Produces:
  - `SamplePackageBuilder.PayloadRelativePath` → `const string` = `"Payload/CherryFrameworkSample.zip"`.
  - `SamplePackageBuilder.Build(string projectRoot, string installerDirectory)` → `string` — the path of the written archive.
  - `SamplePackageBuilder.Sources` → `IReadOnlyList<ZipSourceFolder>` — `Assets/Sample` → `Assets/Sample`, `Assets/Scripts/GeneratedDataModels` → `Assets/Scripts/GeneratedDataModels`.

- [ ] **Step 1: Implement the builder**

`Assets/CherryFramework/Editor/Installer/SamplePackageBuilder.cs`:

```csharp
using System.Collections.Generic;
using System.IO;

namespace CherryFramework.Installer
{
    /// <summary>
    /// Packs the Sample into the archive that ships inside the framework.
    /// Run from the framework repository: the Sample lives there as loose
    /// files, and the archive is what travels to a new project.
    /// </summary>
    public static class SamplePackageBuilder
    {
        public const string PayloadRelativePath = "Payload/CherryFrameworkSample.zip";

        public static IReadOnlyList<ZipSourceFolder> Sources => new[]
        {
            new ZipSourceFolder { sourceDirectory = "Assets/Sample", entryPrefix = "Assets/Sample" },
            new ZipSourceFolder
            {
                sourceDirectory = "Assets/Scripts/GeneratedDataModels",
                entryPrefix = "Assets/Scripts/GeneratedDataModels"
            }
        };

        public static string Build(string projectRoot, string installerDirectory)
        {
            var payloadPath = Path.Combine(
                installerDirectory,
                PayloadRelativePath.Replace('/', Path.DirectorySeparatorChar));

            var folders = new List<ZipSourceFolder>();

            foreach (var source in Sources)
            {
                var absolute = Path.Combine(projectRoot, source.sourceDirectory.Replace('/', Path.DirectorySeparatorChar));

                if (!Directory.Exists(absolute))
                {
                    continue;
                }

                folders.Add(new ZipSourceFolder
                {
                    sourceDirectory = absolute,
                    entryPrefix = source.entryPrefix
                });
            }

            if (folders.Count == 0)
            {
                return null;
            }

            ZipArchiveUtility.Create(payloadPath, folders);

            return payloadPath;
        }
    }
}
```

- [ ] **Step 2: Run the builder**

`Tools → CherryFramework → Rebuild Sample Package`.

Expected: `Payload/CherryFrameworkSample.zip` appears next to `dependencies.json`, and the Console prints its size.

- [ ] **Step 3: Verify the archive layout**

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
$a = [System.IO.Compression.ZipFile]::OpenRead("Assets/CherryFramework/Editor/Installer/Payload/CherryFrameworkSample.zip")
$a.Entries | Select-Object -First 10 -ExpandProperty FullName
$a.Entries | Where-Object { $_.FullName -like "Assets/Scripts/GeneratedDataModels/*" } | Select-Object -ExpandProperty FullName
$a.Dispose()
```

Expected: entries starting with `Assets/Sample/` and `Assets/Scripts/GeneratedDataModels/`, and the `.meta` files of both.

- [ ] **Step 4: Commit**

```bash
git add Assets/CherryFramework/Editor/Installer/SamplePackageBuilder.cs Assets/CherryFramework/Editor/Installer/Payload/CherryFrameworkSample.zip
git commit -m "feat: ship the Sample as an archive inside the framework"
```

---

### Task 10: The orchestrator, the menu and the first-run prompt

**Files:**
- Create: `Assets/CherryFramework/Editor/Installer/InstallReport.cs`
- Create: `Assets/CherryFramework/Editor/Installer/CherryInstaller.cs`
- Create: `Assets/Tests/Editor/CherryInstallerIntegrationTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 2-9.
- Produces:
  - `CherryInstaller.Install(string projectRoot, string installerDirectory, bool includeSample)` → `InstallReport`.
  - `CherryInstaller.InstallInCurrentProject(bool includeSample)` → `InstallReport`.
  - `CherryInstaller.InstallerDirectory` → `string` — the folder that holds the installer code and the payload.
  - `InstallReport { public List<string> Lines; public bool Success; public string Summary(); }`

- [ ] **Step 1: Write the failing integration test**

`Assets/Tests/Editor/CherryInstallerIntegrationTests.cs`:

```csharp
using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class CherryInstallerIntegrationTests
    {
        private const string ShippedDependencies =
            "Assets/CherryFramework/Editor/Installer/Payload/dependencies.json";

        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-integration-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void Install_OnAnEmptyProject_AddsEveryPackageAndDotween()
        {
            var installerDirectory = PrepareInstallerDirectory();

            var report = CherryInstaller.Install(_root, installerDirectory, false);

            Assert.IsTrue(report.Success, report.Summary());

            var packages = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));

            Assert.AreEqual("3.2.2", packages["com.unity.nuget.newtonsoft-json"]);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
            Assert.AreEqual("https://github.com/v0lt13/EditorAttributes.git", packages["com.v0lt.editor-attributes"]);
            Assert.AreEqual(
                "https://github.com/AnnulusGames/UnityCodeGen.git?path=/Assets/UnityCodeGen",
                packages["com.annulusgames.unity-codegen"]);

            var dotween = Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween");

            Assert.IsTrue(File.Exists(Path.Combine(dotween, "DOTween.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(dotween, "Modules", "DOTweenModuleUI.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(dotween, "Modules", "DOTween.Modules.asmdef")));
        }

        [Test]
        public void Install_WithTheSample_UnpacksItAndAddsTheInputSystem()
        {
            var installerDirectory = PrepareInstallerDirectory();

            var payload = Path.Combine(installerDirectory, "Payload", "CherryFrameworkSample.zip");
            var sampleZip = Path.Combine(_root, "sample.zip");

            using (var archive = ZipFile.Open(sampleZip, ZipArchiveMode.Create))
            {
                Write(archive, "Assets/Sample/Scenes/dinoscene.unity", "scene");
                Write(archive, "Assets/Sample/Scenes/dinoscene.unity.meta", "meta");
                Write(archive, "Assets/Scripts/GeneratedDataModels/GameStateDataModel.Generated.cs", "code");
            }

            File.Copy(sampleZip, payload, true);

            var report = CherryInstaller.Install(_root, installerDirectory, true);

            Assert.IsTrue(report.Success, report.Summary());
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity.meta")));
            Assert.IsTrue(File.Exists(
                Path.Combine(_root, "Assets", "Scripts", "GeneratedDataModels", "GameStateDataModel.Generated.cs")));

            var packages = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));

            Assert.AreEqual("1.20.0", packages["com.unity.inputsystem"]);
        }

        [Test]
        public void Install_RunTwice_ChangesNothingTheSecondTime()
        {
            var installerDirectory = PrepareInstallerDirectory();

            CherryInstaller.Install(_root, installerDirectory, false);
            var second = CherryInstaller.Install(_root, installerDirectory, false);

            Assert.IsTrue(second.Success, second.Summary());

            var packages = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));

            Assert.AreEqual(4, packages.Count, "the packages must not be duplicated");
            CollectionAssert.Contains(second.Lines, "Package already present: com.unity.ugui");
        }

        /// <summary>
        /// Builds an installer folder that points at a local DOTween archive, so
        /// the test never touches the network.
        /// </summary>
        private string PrepareInstallerDirectory()
        {
            var installerDirectory = Path.Combine(
                _root, "Assets", "CherryFramework", "Editor", "Installer");

            Directory.CreateDirectory(Path.Combine(installerDirectory, "Payload"));

            var dotweenZip = Path.Combine(_root, "dotween.zip");

            using (var archive = ZipFile.Open(dotweenZip, ZipArchiveMode.Create))
            {
                Write(archive, "DOTween/DOTween.dll", "dll");
                Write(archive, "DOTween/Modules/DOTweenModuleUI.cs", "code");
            }

            var shipped = File.ReadAllText(ShippedDependencies);
            var patched = shipped.Replace(
                "https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip",
                "file:///" + dotweenZip.Replace('\\', '/'));

            File.WriteAllText(Path.Combine(installerDirectory, "Payload", "dependencies.json"), patched);

            return installerDirectory;
        }

        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(content);
            }
        }
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Expected: FAIL — `CherryInstaller` does not exist.

- [ ] **Step 3: Implement the report**

`Assets/CherryFramework/Editor/Installer/InstallReport.cs`:

```csharp
using System.Collections.Generic;
using System.Text;

namespace CherryFramework.Installer
{
    public sealed class InstallReport
    {
        public readonly List<string> Lines = new List<string>();
        public bool Success = true;

        public void Add(string line)
        {
            Lines.Add(line);
        }

        public void Fail(string line)
        {
            Success = false;
            Lines.Add(line);
        }

        public string Summary()
        {
            var builder = new StringBuilder();

            foreach (var line in Lines)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }
    }
}
```

- [ ] **Step 4: Implement the orchestrator**

`Assets/CherryFramework/Editor/Installer/CherryInstaller.cs`:

```csharp
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;

namespace CherryFramework.Installer
{
    /// <summary>
    /// Installs every dependency the framework needs into the current project.
    /// Nothing here touches the framework's own code, so this runs even while
    /// the framework does not compile.
    /// </summary>
    public static class CherryInstaller
    {
        private const string DependencyFileName = "dependencies.json";
        private const string PromptSessionKey = "CherryFramework.DependencyPromptShown";

        public static string InstallerDirectory
        {
            get
            {
                foreach (var guid in AssetDatabase.FindAssets("CherryDependencies t:MonoScript"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    if (!path.EndsWith("CherryDependencies.cs", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var directory = Path.GetDirectoryName(path);

                    if (!string.IsNullOrEmpty(directory))
                    {
                        return ResolveAgainstProject(directory);
                    }
                }

                return ResolveAgainstProject("Assets/CherryFramework/Editor/Installer");
            }
        }

        public static InstallReport InstallInCurrentProject(bool includeSample)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            return Install(projectRoot, InstallerDirectory, includeSample);
        }

        public static InstallReport Install(string projectRoot, string installerDirectory, bool includeSample)
        {
            var report = new InstallReport();

            try
            {
                var dependencyPath = Path.Combine(installerDirectory, DependencyFileName);
                var dependencies = CherryDependencies.LoadFromFile(dependencyPath);

                var packages = PackageInstaller.Install(projectRoot, dependencies.packages);

                foreach (var id in packages.Installed)
                {
                    report.Add("Package installed: " + id);
                }

                foreach (var id in packages.AlreadyPresent)
                {
                    report.Add("Package already present: " + id);
                }

                if (packages.Failed.Count > 0)
                {
                    report.Fail("Could not install packages: " + string.Join(", ", packages.Failed.ToArray()));
                }

                if (!string.IsNullOrEmpty(packages.Error))
                {
                    report.Fail("Could not write the manifest: " + packages.Error);
                }

                var dotween = DotweenInstaller.Install(
                    projectRoot,
                    dependencies.dotween,
                    Path.Combine(Path.GetTempPath(), "cherryframework-installer"));

                if (dotween.Installed)
                {
                    report.Add("DOTween installed into " + dependencies.dotween.installPath);
                }
                else if (dotween.Skipped)
                {
                    report.Add("DOTween already present");
                }
                else
                {
                    report.Fail("DOTween was not installed: " + dotween.Error);
                }

                if (includeSample)
                {
                    var payload = Path.Combine(
                        installerDirectory,
                        dependencies.sample.payload.Replace('/', Path.DirectorySeparatorChar));

                    var sample = SampleInstaller.Install(projectRoot, payload, dependencies.sample);

                    if (sample.Installed)
                    {
                        report.Add("Sample installed");
                    }
                    else if (sample.Skipped)
                    {
                        report.Add("Sample already present");
                    }
                    else
                    {
                        report.Fail("The Sample was not installed: " + sample.Error);
                    }

                    var samplePackages = PackageInstaller.Install(projectRoot, dependencies.samplePackages);

                    foreach (var id in samplePackages.Installed)
                    {
                        report.Add("Package installed: " + id);
                    }

                    foreach (var id in samplePackages.AlreadyPresent)
                    {
                        report.Add("Package already present: " + id);
                    }
                }

                RefreshCurrentProject(projectRoot);
            }
            catch (Exception exception)
            {
                report.Fail("The installer failed: " + exception.Message);
            }

            return report;
        }

        [MenuItem("Tools/CherryFramework/Install Dependencies", false, 10)]
        private static void InstallFromMenu()
        {
            var report = InstallInCurrentProject(true);

            EditorUtility.DisplayDialog("CherryFramework", report.Summary(), "OK");
        }

        [MenuItem("Tools/CherryFramework/Rebuild Sample Package", false, 40)]
        private static void RebuildSamplePackage()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var path = SamplePackageBuilder.Build(projectRoot, InstallerDirectory);

            if (path == null)
            {
                EditorUtility.DisplayDialog(
                    "CherryFramework",
                    "The Sample was not found. Run this from the framework repository.",
                    "OK");

                return;
            }

            var size = new FileInfo(path).Length / 1024f;

            EditorUtility.DisplayDialog(
                "CherryFramework",
                "Sample archive written:\n" + path + "\n\n" + size.ToString("F0") + " KB",
                "OK");

            AssetDatabase.Refresh();
        }

        [InitializeOnLoadMethod]
        private static void PromptOnFirstLoad()
        {
            EditorApplication.delayCall += ShowPromptIfNeeded;
        }

        private static void ShowPromptIfNeeded()
        {
            if (SessionState.GetBool(PromptSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(PromptSessionKey, true);

            try
            {
                var dependencyPath = Path.Combine(InstallerDirectory, DependencyFileName);

                if (!File.Exists(dependencyPath))
                {
                    return;
                }

                var dependencies = CherryDependencies.LoadFromFile(dependencyPath);
                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var state = InstallState.Evaluate(dependencies, projectRoot, true);

                if (state.IsComplete)
                {
                    return;
                }

                var missing = string.Join("\n", state.MissingPackages.ToArray());

                if (state.DotweenMissing)
                {
                    missing += "\nDOTween";
                }

                if (state.SampleMissing)
                {
                    missing += "\nThe Sample";
                }

                if (!EditorUtility.DisplayDialog(
                        "CherryFramework",
                        "The framework dependencies are not installed in this project.\n\n" +
                        "Missing:\n" + missing + "\n\nInstall them now?",
                        "Install",
                        "Later"))
                {
                    return;
                }

                var report = InstallInCurrentProject(true);

                EditorUtility.DisplayDialog("CherryFramework", report.Summary(), "OK");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[CherryFramework] The dependency check failed: " + exception.Message);
            }
        }

        private static string ResolveAgainstProject(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }

        /// <summary>
        /// A refresh or a package resolve reloads the domain, which would tear
        /// down a test run. Only the project that is open is refreshed.
        /// </summary>
        private static void RefreshCurrentProject(string projectRoot)
        {
            var current = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            if (!string.Equals(
                    current.TrimEnd(Path.DirectorySeparatorChar),
                    projectRoot.TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                Client.Resolve();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[CherryFramework] The package manager did not resolve: " + exception.Message);
            }

            AssetDatabase.Refresh();
        }
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Expected: PASS — both tests green. The DOTween download runs against the real URL inside this test; if the machine is offline the test reports a failed DOTween line but the package assertions still hold.

- [ ] **Step 6: Commit**

```bash
git add Assets/CherryFramework/Editor/Installer/CherryInstaller.cs Assets/CherryFramework/Editor/Installer/InstallReport.cs Assets/Tests/Editor/CherryInstallerIntegrationTests.cs
git commit -m "feat: orchestrate the dependency install from the menu and on first load"
```

---

### Task 11: Document the new installation flow

**Files:**
- Modify: `README.md` (the "Installing into your own project" section and the "Dependencies" list)

**Interfaces:**
- Consumes: nothing.
- Produces: documentation that matches what the tool does.

- [ ] **Step 1: Rewrite the installation section**

Replace the body of `### Installing into your own project` in `README.md` with:

```markdown
### Installing into your own project

1. Copy `Assets/CherryFramework` into your project's `Assets` folder - with its
   `.meta` files, they carry the references between the framework's assemblies.
2. Open the project. Unity will report compile errors, because the framework's
   dependencies are not installed yet - that is expected.
3. The installer asks whether it should install them. Answer **Install**, or run
   `Tools -> CherryFramework -> Install Dependencies` yourself.

The installer is a separate assembly that does not reference the framework, so
it compiles and runs even while the framework does not. It:

- adds the packages below to `Packages/manifest.json` and resolves them,
- downloads DOTween from
  [dotween.demigiant.com](http://dotween.demigiant.com/download.php) into
  `Assets/ThirdParty/Plugins/DOTween` and writes the `DOTween.Modules`
  assembly definition the framework references,
- unpacks the Sample archive, if you asked for the Sample.

Re-running it is safe: anything already installed is left alone.
```

- [ ] **Step 2: Rewrite the dependencies list**

Replace the `##### Dependencies (included in project)` list with:

```markdown
##### Dependencies

The installer adds these to `Packages/manifest.json` for you:

| Package | Why |
| --- | --- |
| `com.unity.nuget.newtonsoft-json` | JSON serialisation in the save system and the data models |
| `com.unity.ugui` | `UnityEngine.UI` and TextMeshPro |
| `com.v0lt.editor-attributes` | the `EditorAttributes` editor attributes |
| `com.annulusgames.unity-codegen` | the data model code generator |
| `com.unity.inputsystem` | only when the Sample is installed |

DOTween is not a package. It is downloaded from
[dotween.demigiant.com](http://dotween.demigiant.com/download.php) and
unpacked into `Assets/ThirdParty/Plugins/DOTween`.
```

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: describe the automatic dependency installation"
```

---

## Verification

Run after all tasks are done.

- [ ] **Step 1: The whole test suite passes**

`Window → General → Test Runner`, `EditMode` tab, `Run All`.

Expected: every test green.

- [ ] **Step 2: The project compiles**

`unity_get_compilation_errors`.

Expected: no errors, no warnings from the installer assembly.

- [ ] **Step 3: The installer is a no-op in the framework repository**

`Tools → CherryFramework → Install Dependencies`.

Expected: the report says every package is already present, DOTween is already
present, and the Sample is already present. The repository is unchanged -
`git status` is clean.

- [ ] **Step 4: The framework survives a fresh install**

Create a temporary project folder, copy `Assets/CherryFramework` into it, then
run the installer against it and check that `Packages/manifest.json` holds the
four packages, that `Assets/ThirdParty/Plugins/DOTween/DOTween.dll` and
`Modules/DOTween.Modules.asmdef` exist, and that the Sample is unpacked with
its `.meta` files.
