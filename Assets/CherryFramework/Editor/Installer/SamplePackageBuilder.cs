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
                var absolute = Path.Combine(
                    projectRoot, source.sourceDirectory.Replace('/', Path.DirectorySeparatorChar));

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
