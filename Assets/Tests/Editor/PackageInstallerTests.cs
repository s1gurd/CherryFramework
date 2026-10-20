using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class PackageInstallerTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-packages-" + System.Guid.NewGuid().ToString("N"));
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
        public void Install_AddsThePackagesToTheManifest()
        {
            var result = PackageInstaller.Install(_root, new List<PackageEntry>
            {
                new PackageEntry { id = "com.unity.nuget.newtonsoft-json", version = "3.2.2" },
                new PackageEntry { id = "com.unity.ugui", version = "2.0.0" }
            });

            Assert.AreEqual(0, result.Failed.Count, result.Error);
            Assert.AreEqual(2, result.Installed.Count);

            var packages = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));

            Assert.AreEqual("3.2.2", packages["com.unity.nuget.newtonsoft-json"]);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
        }

        [Test]
        public void Install_IsIdempotent()
        {
            var packages = new List<PackageEntry>
            {
                new PackageEntry { id = "com.unity.ugui", version = "2.0.0" }
            };

            PackageInstaller.Install(_root, packages);
            var second = PackageInstaller.Install(_root, packages);

            Assert.AreEqual(0, second.Installed.Count);
            Assert.AreEqual(1, second.AlreadyPresent.Count);

            var manifest = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));
            Assert.AreEqual(1, manifest.Count, "the package must not be duplicated");
        }

        [Test]
        public void Install_KeepsThePackagesThatAreAlreadyThere()
        {
            var packages = new List<PackageEntry>
            {
                new PackageEntry { id = "com.unity.timeline", version = "1.8.7" }
            };

            PackageInstaller.Install(_root, packages);
            var result = PackageInstaller.Install(_root, new List<PackageEntry>
            {
                new PackageEntry { id = "com.unity.ugui", version = "2.0.0" }
            });

            Assert.AreEqual(1, result.Installed.Count);

            var manifest = ManifestEditor.ReadPackages(ManifestEditor.Read(_root));

            Assert.AreEqual("1.8.7", manifest["com.unity.timeline"]);
            Assert.AreEqual("2.0.0", manifest["com.unity.ugui"]);
        }
    }
}
