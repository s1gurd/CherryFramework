using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace CherryFramework.Installer
{
    /// <summary>
    /// Installs every dependency the framework needs into the current project.
    /// Nothing here touches the framework's own code, so this runs even while
    /// the framework does not compile.
    /// </summary>
    public static class CherryInstaller
    {
        private const string DependencyFileRelativePath = "Payload/dependencies.json";
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
                var dependencyPath = Path.Combine(
                    installerDirectory,
                    DependencyFileRelativePath.Replace('/', Path.DirectorySeparatorChar));

                var dependencies = CherryDependencies.LoadFromFile(dependencyPath);

                ReportPackages(report, PackageInstaller.Install(projectRoot, dependencies.packages));

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

                    ReportPackages(report, PackageInstaller.Install(projectRoot, dependencies.samplePackages));

                    if (!SampleInstaller.SetActiveInputHandler(projectRoot, 2))
                    {
                        report.Add("The input handling was left untouched, the project settings were not found");
                    }
                    else
                    {
                        report.Add("Active input handling switched to Both");
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
                var dependencyPath = Path.Combine(
                    InstallerDirectory,
                    DependencyFileRelativePath.Replace('/', Path.DirectorySeparatorChar));

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

        private static void ReportPackages(InstallReport report, PackageInstallResult result)
        {
            foreach (var id in result.Installed)
            {
                report.Add("Package installed: " + id);
            }

            foreach (var id in result.AlreadyPresent)
            {
                report.Add("Package already present: " + id);
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                report.Fail("Could not write the manifest: " + result.Error);
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
