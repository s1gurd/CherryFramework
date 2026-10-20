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
                (definition.installPath + "/Modules/" + definition.modulesAssemblyFileName)
                    .Replace('/', Path.DirectorySeparatorChar));

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
