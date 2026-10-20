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

                foreach (var package in packages)
                {
                    if (package != null && !string.IsNullOrEmpty(package.id))
                    {
                        result.Failed.Add(package.id);
                    }
                }
            }

            return result;
        }
    }
}
