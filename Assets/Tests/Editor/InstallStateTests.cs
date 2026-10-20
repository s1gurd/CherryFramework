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
