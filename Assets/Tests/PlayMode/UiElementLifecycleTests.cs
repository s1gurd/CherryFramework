using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CherryFramework.DependencyManager;
using CherryFramework.Tests.Fakes;
using CherryFramework.UI.InteractiveElements.Widgets;
using CherryFramework.UI.UiAnimation;
using CherryFramework.UI.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CherryFramework.PlayMode.Tests
{
    public class UiElementLifecycleTests
    {
        private UiTestHierarchy _hierarchy;
        private ViewService _viewService;
        private GameObject _widgetObject;

        [SetUp]
        public void SetUp()
        {
            _hierarchy = new UiTestHierarchy();
            _viewService = new ViewService(_hierarchy.Root, false);
            DependencyContainer.Instance.BindAsSingleton(_viewService);
            _hierarchy.Activate();
        }

        [TearDown]
        public void TearDown()
        {
            if (_widgetObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_widgetObject);
            }

            _hierarchy.Destroy();
            DependencyContainer.Instance.RemoveDependency(typeof(ViewService));
        }

        private TestWidget CreateWidget(List<WidgetState> states)
        {
            _widgetObject = new GameObject("TestWidget");
            var widget = _widgetObject.AddComponent<TestWidget>();

            SerializedFields.SetField(widget, "startupBehaviour", WidgetStartupBehaviour.JustSetCurrentState);
            widget.SetStates(states);

            return widget;
        }

        private static WidgetState State(string name)
        {
            return new WidgetState { stateName = name, stateElements = new List<WidgetElement>() };
        }

        [UnityTest]
        public IEnumerator Presenter_InitialisesItselfWhenItIsEnabled()
        {
            var presenter = _hierarchy.AddChild<TestPresenter>();

            Assert.AreEqual(0, presenter.InitializedCount, "an inactive presenter is not initialised yet");

            presenter.gameObject.SetActive(true);

            Assert.AreEqual(1, presenter.InitializedCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Presenter_InitialisesItselfOnlyOnce()
        {
            var presenter = _hierarchy.AddChild<TestPresenter>();
            presenter.gameObject.SetActive(true);

            presenter.gameObject.SetActive(false);
            presenter.gameObject.SetActive(true);

            Assert.AreEqual(1, presenter.InitializedCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Presenter_WithAForeignChildrenContainer_Throws()
        {
            var presenter = _hierarchy.AddChild<TestPresenter>();
            var foreignContainer = new GameObject("ForeignContainer").AddComponent<Canvas>();

            SerializedFields.SetField(presenter, "childrenContainer", foreignContainer);

            Assert.Throws<Exception>(() => presenter.InitializePresenter());

            UnityEngine.Object.DestroyImmediate(foreignContainer.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Widget_InitialisesItselfWithTheFirstState()
        {
            var widget = CreateWidget(new List<WidgetState> { State("first"), State("second") });

            widget.gameObject.SetActive(true);

            Assert.IsTrue(widget.Inited);
            Assert.AreEqual(0, widget.CurrentState);
            Assert.AreEqual(2, widget.StatesCount);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Widget_SetState_ByIndex_ChangesTheState()
        {
            var widget = CreateWidget(new List<WidgetState> { State("first"), State("second") });
            widget.gameObject.SetActive(true);

            var started = string.Empty;
            widget.OnStartStateChange += name => started = name;

            widget.SetState(1);

            Assert.AreEqual(1, widget.CurrentState);
            Assert.AreEqual("second", started);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Widget_SetState_ByName_ChangesTheState()
        {
            var widget = CreateWidget(new List<WidgetState> { State("first"), State("second") });
            widget.gameObject.SetActive(true);

            widget.SetState("second");

            Assert.AreEqual(1, widget.CurrentState);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Widget_SetState_RaisesTheFinishEventAfterTheAnimation()
        {
            var widget = CreateWidget(new List<WidgetState> { State("first"), State("second") });
            widget.gameObject.SetActive(true);

            var finished = string.Empty;
            widget.OnFinishStateChange += name => finished = name;

            widget.SetState(1);
            yield return null;

            Assert.AreEqual("second", finished);
        }

        [UnityTest]
        public IEnumerator Widget_SetState_WithAnUnknownIndex_LogsAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("Not found requested state"));

            var widget = CreateWidget(new List<WidgetState> { State("first") });
            widget.gameObject.SetActive(true);

            widget.SetState(5);

            yield return null;
        }

        [UnityTest]
        public IEnumerator Widget_SetState_WithAnUnknownName_LogsAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("Not found requested state"));

            var widget = CreateWidget(new List<WidgetState> { State("first") });
            widget.gameObject.SetActive(true);

            widget.SetState("there_is_no_such_state");

            yield return null;
        }
    }
}
