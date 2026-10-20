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
