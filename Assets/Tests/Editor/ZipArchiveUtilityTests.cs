using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class ZipArchiveUtilityTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "cherry-zip-" + Guid.NewGuid().ToString("N"));
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
        public void Extract_StripsTheRootPrefix()
        {
            var zip = Path.Combine(_root, "sample.zip");

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Write(archive, "DOTween/DOTween.dll", "dll");
                Write(archive, "DOTween/Modules/DOTweenModuleUI.cs", "code");
                Write(archive, "other/ignored.txt", "ignored");
            }

            var target = Path.Combine(_root, "out");
            ZipArchiveUtility.Extract(zip, target, "DOTween/");

            Assert.IsTrue(File.Exists(Path.Combine(target, "DOTween.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(target, "Modules", "DOTweenModuleUI.cs")));
            Assert.IsFalse(File.Exists(Path.Combine(target, "ignored.txt")), "entries outside the prefix are skipped");
            Assert.IsFalse(File.Exists(Path.Combine(target, "other", "ignored.txt")));
        }

        [Test]
        public void Extract_OverwritesExistingFiles()
        {
            var zip = Path.Combine(_root, "sample.zip");

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Write(archive, "root/file.txt", "new");
            }

            var target = Path.Combine(_root, "out");
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "file.txt"), "old");

            ZipArchiveUtility.Extract(zip, target, "root/");

            Assert.AreEqual("new", File.ReadAllText(Path.Combine(target, "file.txt")));
        }

        [Test]
        public void Extract_WithoutAPrefix_KeepsTheWholeLayout()
        {
            var zip = Path.Combine(_root, "sample.zip");

            using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            {
                Write(archive, "Assets/Sample/Scenes/dinoscene.unity", "scene");
            }

            var target = Path.Combine(_root, "out");
            ZipArchiveUtility.Extract(zip, target, string.Empty);

            Assert.IsTrue(File.Exists(Path.Combine(target, "Assets", "Sample", "Scenes", "dinoscene.unity")));
        }

        [Test]
        public void Create_KeepsTheEntryPrefixAndTheMetaFiles()
        {
            var source = Path.Combine(_root, "source");
            Directory.CreateDirectory(Path.Combine(source, "Sub"));
            File.WriteAllText(Path.Combine(source, "a.cs"), "code");
            File.WriteAllText(Path.Combine(source, "a.cs.meta"), "meta");
            File.WriteAllText(Path.Combine(source, "Sub", "b.png"), "png");

            var zip = Path.Combine(_root, "out.zip");

            ZipArchiveUtility.Create(zip, new List<ZipSourceFolder>
            {
                new ZipSourceFolder { sourceDirectory = source, entryPrefix = "Assets/Sample" }
            });

            using (var archive = ZipFile.OpenRead(zip))
            {
                var names = new List<string>();
                foreach (var entry in archive.Entries)
                {
                    names.Add(entry.FullName);
                }

                CollectionAssert.Contains(names, "Assets/Sample/a.cs");
                CollectionAssert.Contains(names, "Assets/Sample/a.cs.meta");
                CollectionAssert.Contains(names, "Assets/Sample/Sub/b.png");
            }
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
