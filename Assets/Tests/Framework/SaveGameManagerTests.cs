using System.Text.RegularExpressions;
using CherryFramework.ModelService;
using CherryFramework.ModelService.ModelDataStorageBridges;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.Tests.Fakes;
using SaveGameManagerService = CherryFramework.SaveGameManager.SaveGameManager;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CherryFramework.Tests
{
    public class SaveGameManagerTests
    {
        private const string Guid = "11111111-2222-3333-4444-555555555555";

        private FakePlayerPrefs _playerPrefs;
        private SaveGameManagerService _saveGameManager;
        private DataModelService _modelService;
        private PersistentObject _persistentObject;
        private GameObject _gameObject;

        [SetUp]
        public void SetUp()
        {
            _playerPrefs = new FakePlayerPrefs();
            _saveGameManager = new SaveGameManagerService(_playerPrefs, false);
            _modelService = new DataModelService(new PlayerPrefsBridge(_playerPrefs), false);

            // PersistentObject injects both of them. They are bound so that
            // the component never logs an injection error, no matter whether
            // the editor calls OnEnable on a component added at edit time.
            DependencyContainer.Instance.BindAsSingleton(_saveGameManager);
            DependencyContainer.Instance.BindAsSingleton(_modelService);

            _persistentObject = TestGameObjects.CreatePersistentObject(Guid);
            _gameObject = _persistentObject.gameObject;
        }

        [TearDown]
        public void TearDown()
        {
            TestGameObjects.Destroy(_gameObject);
            DependencyContainer.Instance.RemoveDependency(typeof(SaveGameManagerService));
            DependencyContainer.Instance.RemoveDependency(typeof(DataModelService));
        }

        [Test]
        public void Register_ThenRegisteredComponents_ContainsTheComponent()
        {
            var component = new TestGameSaveData();

            Assert.IsTrue(_saveGameManager.Register(component, _persistentObject));
            Assert.AreEqual(1, _saveGameManager.RegisteredComponents.Length);
        }

        [Test]
        public void Register_TheSameComponentTwice_Fails()
        {
            LogAssert.Expect(LogType.Error, new Regex("already registered"));

            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);

            Assert.IsFalse(_saveGameManager.Register(component, _persistentObject));
        }

        [Test]
        public void LoadData_OfAnUnregisteredComponent_Fails()
        {
            LogAssert.Expect(LogType.Error, new Regex("it is not registered"));

            Assert.IsFalse(_saveGameManager.LoadData(new TestGameSaveData()));
        }

        [Test]
        public void SaveData_ThenLoadData_RestoresTheFieldValues()
        {
            var component = new TestGameSaveData { Health = 75, Title = "hero" };
            _saveGameManager.Register(component, _persistentObject);

            _saveGameManager.SaveData(component);

            var restored = new TestGameSaveData();
            _saveGameManager.Register(restored, _persistentObject);

            Assert.IsTrue(_saveGameManager.LoadData(restored));
            Assert.AreEqual(75, restored.Health);
            Assert.AreEqual("hero", restored.Title);
            Assert.AreEqual(1, restored.LoadCount);
        }

        [Test]
        public void SaveData_WritesTheKeyOfTheComponent()
        {
            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);

            _saveGameManager.SaveData(component);

            var expectedKey = "SceneId:" + _gameObject.scene.buildIndex + "." + Guid + "-" + typeof(TestGameSaveData);
            Assert.IsTrue(_playerPrefs.HasKey(expectedKey), "expected key " + expectedKey);
        }

        [Test]
        public void LoadData_WithoutSavedData_Fails()
        {
            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);

            Assert.IsFalse(_saveGameManager.LoadData(component));
        }

        [Test]
        public void DeleteData_RemovesTheSavedKey()
        {
            var component = new TestGameSaveData { Health = 10 };
            _saveGameManager.Register(component, _persistentObject);
            _saveGameManager.SaveData(component);

            Assert.IsTrue(_saveGameManager.DeleteData(component));
            Assert.AreEqual(0, _playerPrefs.Values.Count);
        }

        [Test]
        public void SetCurrentSlot_ChangesTheKey()
        {
            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);
            _saveGameManager.SetCurrentSlot("slot2");

            _saveGameManager.SaveData(component);

            var expectedKey = "SceneId:" + _gameObject.scene.buildIndex + "." + Guid +
                              "-slot2-" + typeof(TestGameSaveData);
            Assert.IsTrue(_playerPrefs.HasKey(expectedKey), "expected key " + expectedKey);
        }

        [Test]
        public void UnRegister_RemovesTheComponent()
        {
            var component = new TestGameSaveData();
            _saveGameManager.Register(component, _persistentObject);

            Assert.IsTrue(_saveGameManager.UnRegister(component));
            Assert.AreEqual(0, _saveGameManager.RegisteredComponents.Length);
        }

        [Test]
        public void SaveAllData_SavesEveryRegisteredComponent()
        {
            var otherObject = TestGameObjects.CreatePersistentObject("99999999-8888-7777-6666-555555555555");

            try
            {
                var first = new TestGameSaveData { Health = 1 };
                var second = new TestGameSaveData { Health = 2 };
                _saveGameManager.Register(first, _persistentObject);
                _saveGameManager.Register(second, otherObject);

                _saveGameManager.SaveAllData();

                Assert.AreEqual(2, _playerPrefs.Values.Count);
            }
            finally
            {
                TestGameObjects.Destroy(otherObject.gameObject);
            }
        }
    }
}
