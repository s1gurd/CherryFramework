using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class DotweenInstallerTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-dotween-" + System.Guid.NewGuid().ToString("N"));
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
        public void WriteModulesAssembly_CreatesTheAssemblyTheFrameworkReferences()
        {
            var installDirectory = Path.Combine(_root, "DOTween");
            var definition = new DotweenDefinition
            {
                downloadUrl = "https://example.invalid/DOTween.zip",
                version = "1.3.045",
                installPath = "Assets/ThirdParty/Plugins/DOTween",
                modulesAssemblyName = "DOTween.Modules",
                modulesAssemblyFileName = "DOTween.Modules.asmdef"
            };

            DotweenInstaller.WriteModulesAssembly(installDirectory, definition);

            var path = Path.Combine(installDirectory, "Modules", "DOTween.Modules.asmdef");

            Assert.IsTrue(File.Exists(path));

            var json = File.ReadAllText(path);

            StringAssert.Contains("\"name\": \"DOTween.Modules\"", json);
        }

        [Test]
        public void Install_ExtractsTheArchiveAndCreatesTheModulesAssembly()
        {
            var zip = Path.Combine(_root, "dotween.zip");

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Write(archive, "DOTween/DOTween.dll", "dll");
                Write(archive, "DOTween/Modules/DOTweenModuleUI.cs", "code");
            }

            var definition = new DotweenDefinition
            {
                downloadUrl = "file:///" + zip.Replace('\\', '/'),
                version = "1.3.045",
                installPath = "Assets/ThirdParty/Plugins/DOTween",
                modulesAssemblyName = "DOTween.Modules",
                modulesAssemblyFileName = "DOTween.Modules.asmdef"
            };

            var result = DotweenInstaller.Install(_root, definition, Path.Combine(_root, "work"));

            Assert.IsTrue(result.Installed, result.Error);
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween", "DOTween.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween", "Modules", "DOTweenModuleUI.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween", "Modules", "DOTween.Modules.asmdef")));
        }

        [Test]
        public void Install_WhenDotweenIsAlreadyThere_Skips()
        {
            var definition = new DotweenDefinition
            {
                downloadUrl = "https://example.invalid/DOTween.zip",
                version = "1.3.045",
                installPath = "Assets/ThirdParty/Plugins/DOTween",
                modulesAssemblyName = "DOTween.Modules",
                modulesAssemblyFileName = "DOTween.Modules.asmdef"
            };

            var dll = Path.Combine(_root, "Assets", "ThirdParty", "Plugins", "DOTween", "DOTween.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(dll));
            File.WriteAllText(dll, "dll");

            var modules = Path.Combine(
                _root, "Assets", "ThirdParty", "Plugins", "DOTween", "Modules", "DOTween.Modules.asmdef");
            Directory.CreateDirectory(Path.GetDirectoryName(modules));
            File.WriteAllText(modules, "{}");

            var result = DotweenInstaller.Install(_root, definition, _root);

            Assert.IsTrue(result.Skipped);
            Assert.IsFalse(result.Installed);
        }

        [Test]
        public void Install_WithoutADownloadUrl_ReportsTheFailure()
        {
            var result = DotweenInstaller.Install(_root, new DotweenDefinition(), _root);

            Assert.IsFalse(result.Installed);
            Assert.IsNotNull(result.Error);
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
