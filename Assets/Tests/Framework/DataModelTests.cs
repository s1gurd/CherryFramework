using CherryFramework.DataModels;
using CherryFramework.DataModels.ModelDataStorageBridges;
using CherryFramework.Tests.Fakes;
using NUnit.Framework;

namespace CherryFramework.Tests
{
    public class DataModelTests
    {
        private FakePlayerPrefs _playerPrefs;
        private ModelService _modelService;

        [SetUp]
        public void SetUp()
        {
            _playerPrefs = new FakePlayerPrefs();
            _modelService = new ModelService(new PlayerPrefsBridge(_playerPrefs), false);
        }

        [Test]
        public void Accessor_Value_ReturnsTheValueOfTheModel()
        {
            var model = new TestDataModel();

            Assert.AreEqual(100, model.HealthAccessor.Value);
            Assert.AreEqual("untitled", model.TitleAccessor.Value);
        }

        [Test]
        public void Accessor_BindDownwards_WithInvokeImmediate_CallsTheCallbackRightAway()
        {
            var model = new TestDataModel();
            var received = 0;

            model.HealthAccessor.BindDownwards(value => received = value);

            Assert.AreEqual(100, received);
        }

        [Test]
        public void Accessor_BindDownwards_CallsTheCallbackWhenTheValueChanges()
        {
            var model = new TestDataModel();
            var received = 0;
            model.HealthAccessor.BindDownwards(value => received = value);

            model.Health = 55;

            Assert.AreEqual(55, received);
        }

        [Test]
        public void Accessor_BindDownwards_WithActivateOnReady_WaitsForTheReadyFlag()
        {
            var model = new TestDataModel();
            var received = 0;
            model.HealthAccessor.BindDownwards(value => received = value, BindingActivation.ActivateOnReady);

            model.Health = 55;

            Assert.AreEqual(0, received, "the binding is not active before the model is ready");

            model.Ready = true;
            model.Health = 70;

            Assert.AreEqual(70, received, "an active binding delivers the next value");
        }

        [Test]
        public void Accessor_BindDownwards_WithInvokeOnReady_CallsTheCallbackWhenTheModelBecomesReady()
        {
            var model = new TestDataModel();
            var received = 0;
            model.HealthAccessor.BindDownwards(value => received = value, BindingActivation.InvokeOnReady);

            model.Health = 55;

            Assert.AreEqual(0, received, "the binding waits for the model to be ready");

            model.Ready = true;

            Assert.AreEqual(55, received, "the callback runs with the current value");
        }

        [Test]
        public void Accessor_AddProcessor_ProcessedValue_IsTransformed()
        {
            var model = new TestDataModel();
            model.HealthAccessor.AddProcessor(value => value / 2);

            Assert.AreEqual(50, model.HealthAccessor.ProcessedValue);
            Assert.AreEqual(100, model.HealthAccessor.Value);
        }

        [Test]
        public void Accessor_RemoveProcessor_ProcessedValue_IsTheRawValueAgain()
        {
            var model = new TestDataModel();
            var processor = model.HealthAccessor.AddProcessor(value => value / 2);

            model.HealthAccessor.RemoveProcessor(processor);

            Assert.AreEqual(100, model.HealthAccessor.ProcessedValue);
        }

        [Test]
        public void Bindings_ReleaseAllBindings_StopsTheCallbacks()
        {
            var model = new TestDataModel();
            var received = 0;
            var bindings = new Bindings();
            bindings.CreateBinding(model.HealthAccessor, value => received = value);

            model.Health = 55;
            bindings.ReleaseAllBindings();
            model.Health = 70;

            Assert.AreEqual(55, received);
        }

        [Test]
        public void ModelService_GetOrCreateSingletonModel_ReturnsTheSameInstance()
        {
            var first = _modelService.GetOrCreateSingletonModel<TestDataModel>();
            var second = _modelService.GetOrCreateSingletonModel<TestDataModel>();

            Assert.AreSame(first, second);
        }

        [Test]
        public void ModelService_GetOrCreateTransientModel_WithTheSameId_ReturnsTheSameInstance()
        {
            var first = _modelService.GetOrCreateTransientModel<TestDataModel>("enemy-1");
            var second = _modelService.GetOrCreateTransientModel<TestDataModel>("enemy-1");

            Assert.AreSame(first, second);
            Assert.AreEqual("enemy-1", first.Id);
        }

        [Test]
        public void ModelService_GetOrCreateTransientModel_WithADifferentId_ReturnsADifferentInstance()
        {
            var first = _modelService.GetOrCreateTransientModel<TestDataModel>("enemy-1");
            var second = _modelService.GetOrCreateTransientModel<TestDataModel>("enemy-2");

            Assert.AreNotSame(first, second);
        }

        [Test]
        public void ModelService_ReleaseTransientModel_RemovesTheModel()
        {
            var model = _modelService.GetOrCreateTransientModel<TestDataModel>("enemy-1");

            Assert.IsTrue(_modelService.ReleaseTransientModel(model));
            Assert.IsFalse(_modelService.ReleaseTransientModel(model));
        }

        [Test]
        public void PlayerPrefsBridge_SaveModel_ThenLoadModel_RestoresTheValues()
        {
            var model = _modelService.GetOrCreateSingletonModel<TestDataModel>();
            _modelService.DataStorage.RegisterModelInStorage(model);

            model.Health = 42;
            model.Title = "boss";
            _modelService.DataStorage.SaveModelToStorage(model);

            var restored = _modelService.GetOrCreateSingletonModel<TestDataModel>();
            _modelService.DataStorage.RegisterModelInStorage(restored);
            _modelService.DataStorage.LoadModelData(restored);

            Assert.AreEqual(42, restored.Health);
            Assert.AreEqual("boss", restored.Title);
        }
    }
}
