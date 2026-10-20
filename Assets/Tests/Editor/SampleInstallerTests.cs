using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class SampleInstallerTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-sample-" + System.Guid.NewGuid().ToString("N"));
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
        public void Install_UnpacksTheArchiveIntoTheProject()
        {
            var zip = Path.Combine(_root, "sample.zip");

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Write(archive, "Assets/Sample/Scenes/dinoscene.unity", "scene");
                Write(archive, "Assets/Sample/Scenes/dinoscene.unity.meta", "meta");
                Write(archive, "Assets/Scripts/GeneratedDataModels/GameStateDataModel.Generated.cs", "code");
            }

            var definition = new SampleDefinition
            {
                payload = "Payload/CherryFrameworkSample.zip",
                marker = "Assets/Sample/Scenes/dinoscene.unity"
            };

            var result = SampleInstaller.Install(_root, zip, definition);

            Assert.IsTrue(result.Installed, result.Error);
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity.meta")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Scripts", "GeneratedDataModels", "GameStateDataModel.Generated.cs")));
        }

        [Test]
        public void Install_WhenTheSampleIsAlreadyThere_Skips()
        {
            var definition = new SampleDefinition
            {
                payload = "Payload/CherryFrameworkSample.zip",
                marker = "Assets/Sample/Scenes/dinoscene.unity"
            };

            var marker = Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity");
            Directory.CreateDirectory(Path.GetDirectoryName(marker));
            File.WriteAllText(marker, "scene");

            var result = SampleInstaller.Install(_root, Path.Combine(_root, "missing.zip"), definition);

            Assert.IsTrue(result.Skipped);
        }

        [Test]
        public void Install_WithoutTheArchive_ReportsTheFailure()
        {
            var definition = new SampleDefinition
            {
                payload = "Payload/CherryFrameworkSample.zip",
                marker = "Assets/Sample/Scenes/dinoscene.unity"
            };

            var result = SampleInstaller.Install(_root, Path.Combine(_root, "missing.zip"), definition);

            Assert.IsFalse(result.Installed);
            Assert.IsNotNull(result.Error);
        }

        [Test]
        public void SetActiveInputHandler_SwitchesTheProjectToBoth()
        {
            var settings = Path.Combine(_root, "ProjectSettings", "ProjectSettings.asset");
            Directory.CreateDirectory(Path.GetDirectoryName(settings));
            File.WriteAllText(settings, "PlayerSettings:\n  activeInputHandler: 0\n  other: 1\n");

            Assert.IsTrue(SampleInstaller.SetActiveInputHandler(_root, 2));

            var content = File.ReadAllText(settings);

            StringAssert.Contains("activeInputHandler: 2", content);
            StringAssert.Contains("other: 1", content);
        }

        [Test]
        public void SetActiveInputHandler_WithoutTheSettingsFile_ReportsTheFailure()
        {
            Assert.IsFalse(SampleInstaller.SetActiveInputHandler(_root, 2));
        }

        private static void Write(ZipArchive archive, string name, string content)
        {
            var entry = archive.CreateEntry(name);
            using (var writer = new StreamWriter(entry.Open()))
            {
                writer.Write(content);
            }
        }
    }
}
