using System;
using CherryFramework.ModelService.Editor;
using NUnit.Framework;

namespace CherryFramework.Tests
{
    public class ModelsGeneratorTests
    {
        private const string SampleTemplate = "Sample.DataModels.GameStateData, Assembly-CSharp";

        private static Type GetSampleTemplate()
        {
            var template = Type.GetType(SampleTemplate);

            Assert.IsNotNull(template, "The template of the Sample was not found: " + SampleTemplate);

            return template;
        }

        [Test]
        public void Generator_PutsTheModelIntoTheGeneratedFolderNextToTheTemplate()
        {
            var folder = ModelsGenerator.GetOutputFolder(GetSampleTemplate());

            Assert.AreEqual("Assets/Sample/Scripts/DataModels/generated", folder);
        }

        [Test]
        public void Generator_UsesTheNamespaceOfTheTemplateWithTheGeneratedSuffix()
        {
            var modelNamespace = ModelsGenerator.GetModelNamespace(GetSampleTemplate());

            Assert.AreEqual("Sample.DataModels.generated", modelNamespace);
        }

        [Test]
        public void Generator_NamesTheFileAfterTheModel()
        {
            var fileName = ModelsGenerator.GetModelFileName(GetSampleTemplate());

            Assert.AreEqual("GameStateDataModel.Generated.cs", fileName);
        }
    }
}
