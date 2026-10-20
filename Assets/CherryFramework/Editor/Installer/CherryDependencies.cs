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
            catch (Exception exception)
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
