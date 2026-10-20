using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;

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
                if (!string.Equals(Path.GetFullPath(uri.LocalPath), Path.GetFullPath(destination), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(uri.LocalPath, destination, true);
                }

                return;
            }

            var task = DownloadAsync(url, destination);

            EditorUtility.DisplayProgressBar("CherryFramework", "Downloading DOTween...", 0.5f);

            try
            {
                if (!task.Wait(TimeSpan.FromMinutes(5)))
                {
                    throw new TimeoutException("The DOTween download timed out");
                }

                task.GetAwaiter().GetResult();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static async Task DownloadAsync(string url, string destination)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                // ConfigureAwait(false) keeps the continuation off the main
                // thread. Without it the editor's synchronization context is
                // captured, the continuation waits for the main thread, and
                // the main thread waits for the continuation - a deadlock.
                var response = await client
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var target = File.Create(destination))
                {
                    await source.CopyToAsync(target).ConfigureAwait(false);
                }
            }
        }
    }
}
