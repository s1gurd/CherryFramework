using System.Collections;
using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CherryFramework.PlayMode.Tests
{
    public class InjectMonoBehaviourTests
    {
        private class Service
        {
        }

        private class TestComponent : BehaviourBase
        {
            [Inject] private Service _service;

            public Service Service => _service;
        }

        [TearDown]
        public void TearDown()
        {
            if (DependencyContainer.Instance.HasDependency<Service>())
            {
                DependencyContainer.Instance.RemoveDependency(typeof(Service));
            }
        }

        [UnityTest]
        public IEnumerator Component_OnEnable_ReceivesTheInjectedService()
        {
            DependencyContainer.Instance.BindAsSingleton(new Service());

            var gameObject = new GameObject("TestComponent");
            var component = gameObject.AddComponent<TestComponent>();

            yield return null;

            Assert.IsNotNull(component.Service);

            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator Component_OnDestroy_ReleasesItsBindings()
        {
            DependencyContainer.Instance.BindAsSingleton(new Service());

            var gameObject = new GameObject("TestComponent");
            gameObject.AddComponent<TestComponent>();

            yield return null;

            Object.DestroyImmediate(gameObject);
            yield return null;

            Assert.IsTrue(gameObject == null);
        }
    }
}
