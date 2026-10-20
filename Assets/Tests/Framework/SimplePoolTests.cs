using CherryFramework.SimplePool;
using NUnit.Framework;
using UnityEngine;

namespace Cherry.Framework.Tests
{
    public class SimplePoolTests
    {
        private GameObject _sample;
        private SimplePool<Transform> _pool;

        [SetUp]
        public void SetUp()
        {
            _sample = new GameObject("PoolSample");
            _pool = new SimplePool<Transform>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var active in _pool.ActiveObjects(_sample.transform))
            {
                Object.DestroyImmediate(active.gameObject);
            }

            Object.DestroyImmediate(_sample);
        }

        [Test]
        public void Get_OnAnEmptyPool_CreatesANewInstance()
        {
            var instance = _pool.Get(_sample.transform);

            Assert.IsNotNull(instance);
            Assert.AreNotSame(_sample.transform, instance);
        }

        [Test]
        public void Get_AfterTheInstanceWasDeactivated_ReusesIt()
        {
            var first = _pool.Get(_sample.transform);
            first.gameObject.SetActive(false);

            var second = _pool.Get(_sample.transform);

            Assert.AreSame(first, second);
        }

        [Test]
        public void Get_WhileTheInstanceIsActive_CreatesANewOne()
        {
            var first = _pool.Get(_sample.transform);

            var second = _pool.Get(_sample.transform);

            Assert.AreNotSame(first, second);
        }

        [Test]
        public void Get_WithATransform_AppliesThePositionAndTheRotation()
        {
            var position = new Vector3(1f, 2f, 3f);
            var rotation = Quaternion.Euler(0f, 90f, 0f);

            var instance = _pool.Get(_sample.transform, position, rotation);

            Assert.AreEqual(position, instance.position);
            Assert.Less(Quaternion.Angle(rotation, instance.rotation), 0.01f);
        }

        [Test]
        public void ActiveObjects_ReturnsOnlyTheActiveInstances()
        {
            var first = _pool.Get(_sample.transform);
            var second = _pool.Get(_sample.transform);
            second.gameObject.SetActive(false);

            var active = _pool.ActiveObjects(_sample.transform);

            Assert.AreEqual(1, active.Count);
            Assert.AreSame(first, active[0]);
        }

        [Test]
        public void Clear_EmptiesThePool()
        {
            var instance = _pool.Get(_sample.transform);

            _pool.Clear();

            Assert.AreEqual(0, _pool.ActiveObjects(_sample.transform).Count);
            Assert.IsTrue(instance == null, "the pooled object is destroyed");
        }
    }
}
