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
