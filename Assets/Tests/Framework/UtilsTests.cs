using CherryFramework.Utils;
using NUnit.Framework;

namespace Cherry.Framework.Tests
{
    public class UtilsTests
    {
        [Test]
        public void DataUtils_CreateKey_JoinsTheValuesWithADash()
        {
            Assert.AreEqual("a-b-c", DataUtils.CreateKey("a", "b", "c"));
        }

        [Test]
        public void DataUtils_CreateKey_SkipsTheEmptyValues()
        {
            Assert.AreEqual("a-b", DataUtils.CreateKey("a", string.Empty, "b"));
        }
    }
}
