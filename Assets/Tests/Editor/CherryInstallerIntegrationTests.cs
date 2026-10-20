using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class CherryInstallerIntegrationTests
    {
        private const string ShippedDependencies =
            "Assets/CherryFramework/Editor/Installer/Payload/dependencies.json";

        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-integration-" + System.Guid.NewGuid().ToString("N"));
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
        public void Install_OnAnEmptyProject_AddsEveryPackageAndDotween()
        {
            var installerDirectory = PrepareInstallerDirectory();

            var report = CherryInstaller.Install(_root, installerDirectory, false);

            Assert.IsTrue(report.Success, report.Summary());

            var packages = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));

            Assert.AreEqual("3.2.2", packages["com.unity.nuget.newtonsoft-json"]);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
            Assert.AreEqual("https://github.com/v0lt13/EditorAttributes.git", packages["com.v0lt.editor-attributes"]);
            Assert.AreEqual(
                "https://github.com/AnnulusGames/UnityCodeGen.git?path=/Assets/UnityCodeGen",
                packages["com.annulusgames.unity-codegen"]);

            var dotween = Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween");

            Assert.IsTrue(File.Exists(Path.Combine(dotween, "DOTween.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(dotween, "Modules", "DOTweenModuleUI.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(dotween, "Modules", "DOTween.Modules.asmdef")));
        }

        [Test]
        public void Install_WithTheSample_UnpacksItAndAddsTheInputSystem()
        {
            var installerDirectory = PrepareInstallerDirectory();

            var payload = Path.Combine(installerDirectory, "Payload", "CherryFrameworkSample.zip");
            var sampleZip = Path.Combine(_root, "sample.zip");

            using (var archive = ZipFile.Open(sampleZip, ZipArchiveMode.Create))
            {
                Write(archive, "Assets/Sample/Scenes/dinoscene.unity", "scene");
                Write(archive, "Assets/Sample/Scenes/dinoscene.unity.meta", "meta");
                Write(archive, "Assets/Scripts/GeneratedDataModels/GameStateDataModel.Generated.cs", "code");
            }

            File.Copy(sampleZip, payload, true);

            var report = CherryInstaller.Install(_root, installerDirectory, true);

            Assert.IsTrue(report.Success, report.Summary());
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "Sample", "Scenes", "dinoscene.unity.meta")));
            Assert.IsTrue(File.Exists(
                Path.Combine(_root, "Assets", "Scripts", "GeneratedDataModels", "GameStateDataModel.Generated.cs")));

            var packages = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));

            Assert.AreEqual("1.20.0", packages["com.unity.inputsystem"]);
        }

        [Test]
        public void Install_RunTwice_ChangesNothingTheSecondTime()
        {
            var installerDirectory = PrepareInstallerDirectory();

            CherryInstaller.Install(_root, installerDirectory, false);
            var second = CherryInstaller.Install(_root, installerDirectory, false);

            Assert.IsTrue(second.Success, second.Summary());

            var packages = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));

            Assert.AreEqual(4, packages.Count, "the packages must not be duplicated");
            CollectionAssert.Contains(second.Lines, "Package already present: com.unity.ugui");
        }

        /// <summary>
        /// Builds an installer folder that points at a local DOTween archive, so
        /// the test never touches the network.
        /// </summary>
        private string PrepareInstallerDirectory()
        {
            var installerDirectory = Path.Combine(
                _root, "Assets", "CherryFramework", "Editor", "Installer");

            Directory.CreateDirectory(Path.Combine(installerDirectory, "Payload"));

            var dotweenZip = Path.Combine(_root, "dotween.zip");

            using (var archive = ZipFile.Open(dotweenZip, ZipArchiveMode.Create))
            {
                Write(archive, "DOTween/DOTween.dll", "dll");
                Write(archive, "DOTween/Modules/DOTweenModuleUI.cs", "code");
            }

            var shipped = File.ReadAllText(ShippedDependencies);
            var patched = shipped.Replace(
                "https://dotween.demigiant.com/downloads/DOTween_1_3_045.zip",
                "file:///" + dotweenZip.Replace('\\', '/'));

            File.WriteAllText(Path.Combine(installerDirectory, "Payload", "dependencies.json"), patched);

            return installerDirectory;
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
