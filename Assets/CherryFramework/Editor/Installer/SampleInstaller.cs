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
        public const string ProjectSettingsRelativePath = "ProjectSettings/ProjectSettings.asset";

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

        /// <summary>
        /// Switches the project to the new input handling. The Sample drives the
        /// player with the Input System package, which the old manager would
        /// starve of input. Unity exposes no API for this setting, so the
        /// project settings file is edited directly.
        /// </summary>
        public static bool SetActiveInputHandler(string projectRoot, int value)
        {
            var path = Path.Combine(
                projectRoot, ProjectSettingsRelativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(path))
            {
                return false;
            }

            var content = File.ReadAllText(path);
            var index = content.IndexOf("activeInputHandler:", StringComparison.Ordinal);

            if (index < 0)
            {
                File.WriteAllText(path, content.TrimEnd() + "\n  activeInputHandler: " + value + "\n");
                return true;
            }

            var lineEnd = content.IndexOf('\n', index);

            if (lineEnd < 0)
            {
                lineEnd = content.Length;
            }

            var lineStart = content.LastIndexOf('\n', index);
            lineStart = lineStart < 0 ? 0 : lineStart + 1;

            var indentation = content.Substring(lineStart, index - lineStart);
            var updated = content.Substring(0, lineStart) +
                          indentation + "activeInputHandler: " + value +
                          content.Substring(lineEnd);

            File.WriteAllText(path, updated);

            return true;
        }
    }
}
