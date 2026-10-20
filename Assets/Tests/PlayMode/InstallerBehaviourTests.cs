using System.Collections;
using CherryFramework.DependencyManager;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CherryFramework.PlayMode.Tests
{
    public class InstallerBehaviourTests
    {
        private class Service
        {
        }

        private class TestInstaller : InstallerBehaviourBase
        {
            protected override void Install()
            {
                BindAsSingleton(new Service());
            }
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
        public IEnumerator Installer_Awake_BindsTheServices()
        {
            var gameObject = new GameObject("TestInstaller");
            gameObject.AddComponent<TestInstaller>();

            yield return null;

            Assert.IsTrue(DependencyContainer.Instance.HasDependency<Service>());

            Object.DestroyImmediate(gameObject);
        }

        [UnityTest]
        public IEnumerator Installer_OnDestroy_RemovesTheServices()
        {
            var gameObject = new GameObject("TestInstaller");
            gameObject.AddComponent<TestInstaller>();

            yield return null;

            Object.DestroyImmediate(gameObject);
            yield return null;

            Assert.IsFalse(DependencyContainer.Instance.HasDependency<Service>());
        }
    }
}
