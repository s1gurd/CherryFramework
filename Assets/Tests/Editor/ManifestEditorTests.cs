using NUnit.Framework;

namespace CherryFramework.Installer.Tests
{
    public class ManifestEditorTests
    {
        private const string Manifest = @"{
  ""dependencies"": {
    ""com.unity.ugui"": ""2.0.0"",
    ""com.unity.timeline"": ""1.8.7""
  },
  ""scopedRegistries"": [
    {
      ""name"": ""Unity"",
      ""url"": ""https://packages.unity.com"",
      ""scopes"": [
        ""com.unity""
      ]
    }
  ]
}";

        [Test]
        public void ReadPackages_ReturnsEveryDependency()
        {
            var packages = ManifestEditor.ReadPackages(Manifest);

            Assert.AreEqual(2, packages.Count);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
            Assert.AreEqual("1.8.7", packages["com.unity.timeline"]);
        }

        [Test]
        public void SetPackages_AddsAMissingPackage()
        {
            var result = ManifestEditor.SetPackages(
                Manifest,
                new[] { new PackageEntry { id = "com.unity.nuget.newtonsoft-json", version = "3.2.2" } });

            var packages = ManifestEditor.ReadPackages(result);

            Assert.AreEqual("3.2.2", packages["com.unity.nuget.newtonsoft-json"]);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
            Assert.AreEqual("1.8.7", packages["com.unity.timeline"]);
            Assert.IsTrue(result.Contains("scopedRegistries"), "unrelated sections must survive");
        }

        [Test]
        public void SetPackages_ReplacesAnExistingPackage()
        {
            var result = ManifestEditor.SetPackages(
                Manifest,
                new[] { new PackageEntry { id = "com.unity.ugui", version = "2.0.1" } });

            var packages = ManifestEditor.ReadPackages(result);

            Assert.AreEqual("2.0.1", packages["com.unity.ugui"]);
            Assert.AreEqual(2, packages.Count, "the package must be replaced, not duplicated");
        }

        [Test]
        public void SetPackages_KeepsTheFormattingOfUntouchedEntries()
        {
            var result = ManifestEditor.SetPackages(
                Manifest,
                new[] { new PackageEntry { id = "com.unity.nuget.newtonsoft-json", version = "3.2.2" } });

            StringAssert.Contains("\"com.unity.timeline\": \"1.8.7\"", result);
        }

        [Test]
        public void SetPackages_HandlesAnEmptyDependencyBlock()
        {
            var result = ManifestEditor.SetPackages(
                "{ \"dependencies\": {} }",
                new[] { new PackageEntry { id = "com.unity.ugui", version = "2.0.0" } });

            var packages = ManifestEditor.ReadPackages(result);

            Assert.AreEqual(1, packages.Count);
            Assert.AreEqual("2.0.0", packages["com.unity.ugui"]);
        }

        [Test]
        public void ReadPackages_OnGarbage_ReturnsNothing()
        {
            Assert.AreEqual(0, ManifestEditor.ReadPackages("not json at all").Count);
        }
    }
}
