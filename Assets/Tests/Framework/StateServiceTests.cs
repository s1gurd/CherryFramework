using System.Text.RegularExpressions;
using CherryFramework.StateService;
using CherryFramework.TickDispatcher;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Cherry.Framework.Tests
{
    public class StateServiceTests
    {
        private StateService _stateService;

        [SetUp]
        public void SetUp()
        {
            _stateService = new StateService(false);
        }

        [TearDown]
        public void TearDown()
        {
            // Every StateService builds its own Ticker, and the Ticker builds
            // its own GameObject.
            foreach (var behaviour in Object.FindObjectsByType<TickerBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(behaviour.gameObject);
            }
        }

        [Test]
        public void EmitEvent_IsEventActive_UntilTheNextTick()
        {
            _stateService.EmitEvent("GameStarted");

            Assert.IsTrue(_stateService.IsEventActive("GameStarted"));

            _stateService.LateTick(0f);

            Assert.IsFalse(_stateService.IsEventActive("GameStarted"));
            Assert.IsTrue(_stateService.EventPassed("GameStarted"));
        }

        [Test]
        public void EmitEvent_WithAPayload_TryGetPayloadReturnsIt()
        {
            _stateService.EmitEvent("PlayerDied", 42);

            Assert.IsTrue(_stateService.TryGetPayload("PlayerDied", out int payload));
            Assert.AreEqual(42, payload);
        }

        [Test]
        public void GetPayload_OfAnUnknownKey_ReturnsTheDefault()
        {
            Assert.AreEqual(0, _stateService.GetPayload<int>("Nothing"));
        }

        [Test]
        public void SetStatus_IsStatusActive()
        {
            _stateService.SetStatus("IsPaused");

            Assert.IsTrue(_stateService.IsStatusActive("IsPaused"));
            Assert.IsTrue(_stateService.IsStatusJustBecameActive("IsPaused"));
        }

        [Test]
        public void UnsetStatus_AfterSetStatus_IsStatusInactive()
        {
            _stateService.SetStatus("IsPaused");
            _stateService.LateTick(0f);

            _stateService.UnsetStatus("IsPaused");

            Assert.IsFalse(_stateService.IsStatusActive("IsPaused"));
            Assert.IsTrue(_stateService.IsStatusInactive("IsPaused"));
        }

        [Test]
        public void AddStateSubscription_WithAMetCondition_RunsTheCallbackOnTick()
        {
            var calls = 0;
            _stateService.AddStateSubscription(_ => true, () => calls++);

            _stateService.EmitEvent("Anything");
            _stateService.LateTick(0f);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void AddStateSubscription_WithAnUnmetCondition_DoesNotRunTheCallback()
        {
            var calls = 0;
            _stateService.AddStateSubscription(accessor => accessor.IsEventActive("Ready"), () => calls++);

            _stateService.EmitEvent("SomethingElse");
            _stateService.LateTick(0f);

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void AddStateSubscription_SeesTheEventsOfTheSameTick()
        {
            var calls = 0;
            _stateService.AddStateSubscription(accessor => accessor.IsEventActive("Ready"), () => calls++);

            _stateService.EmitEvent("Ready");
            _stateService.LateTick(0f);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void AddStateSubscription_WithoutAnEvent_IsNotInvoked()
        {
            var calls = 0;
            _stateService.AddStateSubscription(_ => true, () => calls++);

            _stateService.LateTick(0f);

            Assert.AreEqual(0, calls, "the subscriptions only run in a tick where something happened");
        }

        [Test]
        public void AddStateSubscription_WithDestroyAfterInvoke_RunsOnlyOnce()
        {
            var calls = 0;
            _stateService.AddStateSubscription(_ => true, () => calls++, null, true);

            _stateService.EmitEvent("First");
            _stateService.LateTick(0f);

            _stateService.EmitEvent("Second");
            _stateService.LateTick(0f);

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void RemoveSubscription_StopsTheCallback()
        {
            var calls = 0;
            var subscription = _stateService.AddStateSubscription(_ => true, () => calls++);

            _stateService.RemoveSubscription(subscription);

            _stateService.EmitEvent("Anything");
            _stateService.LateTick(0f);

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void RemoveAllSubscriptions_StopsTheCallback()
        {
            var calls = 0;
            var subscriber = new object();
            _stateService.AddStateSubscription(_ => true, () => calls++, subscriber);

            _stateService.RemoveAllSubscriptions(subscriber);

            _stateService.EmitEvent("Anything");
            _stateService.LateTick(0f);
            _stateService.LateTick(0f);

            Assert.AreEqual(0, calls);
        }

        [Test]
        public void AddStateSubscription_ThatThrows_IsLoggedAndTheRestStillRuns()
        {
            LogAssert.Expect(LogType.Error, new Regex("Exception in subscription"));

            var calls = 0;
            _stateService.AddStateSubscription(_ => true, () => throw new System.InvalidOperationException("boom"));
            _stateService.AddStateSubscription(_ => true, () => calls++);

            _stateService.EmitEvent("Anything");
            _stateService.LateTick(0f);

            Assert.AreEqual(1, calls);
        }
    }
}
