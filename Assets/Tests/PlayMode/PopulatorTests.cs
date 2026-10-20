using System.Collections;
using System.Collections.Generic;
using CherryFramework.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CherryFramework.PlayMode.Tests
{
    public class PopulatorTests
    {
        private GameObject _root;
        private GameObject _sampleObject;
        private TestPopulator _populator;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("PopulatorRoot");

            _sampleObject = new GameObject("ElementSample");
            _sampleObject.transform.SetParent(_root.transform, false);
            _sampleObject.SetActive(false);

            var elementSample = _sampleObject.AddComponent<TestPopulatorElement>();
            _populator = new TestPopulator(elementSample, _root.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        private static List<TestItem> Items(int count)
        {
            var items = new List<TestItem>();

            for (var index = 0; index < count; index++)
            {
                items.Add(new TestItem { Name = "item" + index });
            }

            return items;
        }

        [UnityTest]
        public IEnumerator UpdateElements_CreatesOneElementPerItem()
        {
            _populator.UpdateElements(Items(3));

            yield return null;

            Assert.AreEqual(3, _populator.Active.Count);
        }

        [UnityTest]
        public IEnumerator UpdateElements_PassesTheDataToTheElements()
        {
            var items = Items(2);

            _populator.UpdateElements(items);

            yield return null;

            foreach (var element in _populator.Active)
            {
                Assert.IsNotNull(element.data);
            }
        }

        [UnityTest]
        public IEnumerator UpdateElements_WithFewerItems_ReleasesTheSurplusElements()
        {
            _populator.UpdateElements(Items(3));

            yield return null;

            _populator.UpdateElements(Items(1));

            yield return null;

            Assert.AreEqual(1, _populator.Active.Count);
        }

        [UnityTest]
        public IEnumerator UpdateElements_WithNoItems_LeavesNoActiveElement()
        {
            _populator.UpdateElements(Items(2));

            yield return null;

            _populator.UpdateElements(new List<TestItem>());

            yield return null;

            Assert.AreEqual(0, _populator.Active.Count);
        }
    }
}
