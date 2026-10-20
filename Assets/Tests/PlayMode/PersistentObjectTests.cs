using System.Collections;
using System.Text.RegularExpressions;
using CherryFramework.ModelService;
using CherryFramework.ModelService.ModelDataStorageBridges;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.Tests.Fakes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SaveGameManagerService = CherryFramework.SaveGameManager.SaveGameManager;

namespace CherryFramework.PlayMode.Tests
{
    public class PersistentObjectTests
    {
        private const string Guid = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            // PersistentObject injects both of them, so they have to be bound
            // before the component is created.
            var playerPrefs = new FakePlayerPrefs();
            DependencyContainer.Instance.BindAsSingleton(new SaveGameManagerService(playerPrefs, false));
            DependencyContainer.Instance.BindAsSingleton(new DataModelService(new PlayerPrefsBridge(playerPrefs), false));
        }

        [TearDown]
        public void TearDown()
        {
            if (_gameObject != null)
            {
                Object.DestroyImmediate(_gameObject);
            }

            if (DependencyContainer.Instance.HasDependency<SaveGameManagerService>())
            {
                DependencyContainer.Instance.RemoveDependency(typeof(SaveGameManagerService));
            }

            if (DependencyContainer.Instance.HasDependency<DataModelService>())
            {
                DependencyContainer.Instance.RemoveDependency(typeof(DataModelService));
            }
        }

        [UnityTest]
        public IEnumerator PersistentObject_WithoutAGuid_ReportsNoId()
        {
            LogAssert.Expect(LogType.Error, new Regex("guid is missing"));

            _gameObject = new GameObject("TestPersistentObject");
            var persistentObject = _gameObject.AddComponent<PersistentObject>();

            Assert.IsNull(persistentObject.GetObjectId());

            yield return null;
        }

        [UnityTest]
        public IEnumerator PersistentObject_WithAGuid_ReportsTheSceneId()
        {
            _gameObject = new GameObject("TestPersistentObject");
            var persistentObject = _gameObject.AddComponent<PersistentObject>();
            persistentObject.guid = Guid;

            Assert.AreEqual("SceneId:" + _gameObject.scene.buildIndex + "." + Guid, persistentObject.GetObjectId());

            yield return null;
        }
    }
}
