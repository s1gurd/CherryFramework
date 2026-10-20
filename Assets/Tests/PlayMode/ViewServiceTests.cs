using System.Collections;
using System.Text.RegularExpressions;
using CherryFramework.DependencyManager;
using CherryFramework.Tests.Fakes;
using CherryFramework.UI.InteractiveElements.Presenters;
using CherryFramework.UI.Views;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CherryFramework.PlayMode.Tests
{
    public class ViewServiceTests
    {
        private UiTestHierarchy _hierarchy;
        private ViewService _viewService;

        [SetUp]
        public void SetUp()
        {
            _hierarchy = new UiTestHierarchy();

            // The view service reads the loading screen from the root, so it
            // has to be in place before the service is created.
            var loading = _hierarchy.AddChild<TestLoadingPresenter>();
            _hierarchy.SetLoadingScreen(loading);

            _viewService = new ViewService(_hierarchy.Root, false);

            // The presenters inject the view service, so it has to be in the
            // container before they are enabled.
            DependencyContainer.Instance.BindAsSingleton(_viewService);
            _hierarchy.Activate();
        }

        [TearDown]
        public void TearDown()
        {
            _hierarchy.Destroy();
            DependencyContainer.Instance.RemoveDependency(typeof(ViewService));
        }

        [UnityTest]
        public IEnumerator PopView_SetsTheActiveView()
        {
            var view = _hierarchy.AddChild<TestPresenter>();

            _viewService.PopView(view, skipAnimation: true);

            Assert.AreSame(view, _viewService.ActiveView);
            Assert.IsTrue(_viewService.IsViewActive);
            Assert.IsTrue(view.gameObject.activeSelf);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PopView_OfASecondView_KeepsTheFirstOneInTheHistory()
        {
            var first = _hierarchy.AddChild<TestPresenter>();
            var second = _hierarchy.AddChild<TestPresenter>();

            _viewService.PopView(first, skipAnimation: true);
            _viewService.PopView(second, skipAnimation: true);

            Assert.AreSame(second, _viewService.ActiveView);
            Assert.IsFalse(_viewService.IsLastView);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Back_ReturnsToThePreviousView()
        {
            var first = _hierarchy.AddChild<TestPresenter>();
            var second = _hierarchy.AddChild<TestPresenter>();

            _viewService.PopView(first, skipAnimation: true);
            _viewService.PopView(second, skipAnimation: true);

            _viewService.Back(skipAnimation: true);

            Assert.AreSame(first, _viewService.ActiveView);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Back_FromTheOnlyView_ClearsTheActiveView()
        {
            var view = _hierarchy.AddChild<TestPresenter>();
            _viewService.PopView(view, skipAnimation: true);

            _viewService.Back(skipAnimation: true);

            Assert.IsNull(_viewService.ActiveView);
            Assert.IsFalse(_viewService.IsViewActive);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PopView_OfTheFirstView_RaisesOnAnyViewBecameActive()
        {
            var raised = 0;
            _viewService.OnAnyViewBecameActive += () => raised++;

            var view = _hierarchy.AddChild<TestPresenter>();
            _viewService.PopView(view, skipAnimation: true);

            // The event is raised from the show sequence, which plays on the
            // next update.
            yield return null;

            Assert.AreEqual(1, raised);
        }

        [UnityTest]
        public IEnumerator Back_FromTheOnlyView_RaisesOnAllViewsBecameInactive()
        {
            var raised = 0;
            _viewService.OnAllViewsBecameInactive += () => raised++;

            var view = _hierarchy.AddChild<TestPresenter>();
            _viewService.PopView(view, skipAnimation: true);
            _viewService.Back(skipAnimation: true);

            yield return null;

            Assert.AreEqual(1, raised);
        }

        [UnityTest]
        public IEnumerator PopView_WhileAModalViewIsOpen_KeepsTheModalView()
        {
            var modal = _hierarchy.AddChild<TestModalPresenter>();
            var other = _hierarchy.AddChild<TestPresenter>();

            _viewService.PopView(modal, skipAnimation: true);
            _viewService.PopView(other, skipAnimation: true);

            Assert.AreSame(modal, _viewService.ActiveView);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Back_WhileAModalViewIsOpen_KeepsTheModalView()
        {
            var modal = _hierarchy.AddChild<TestModalPresenter>();

            _viewService.PopView(modal, skipAnimation: true);
            _viewService.Back(skipAnimation: true);

            Assert.AreSame(modal, _viewService.ActiveView);
            Assert.IsTrue(_viewService.IsViewActive);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PopView_OfAnUnregisteredType_LogsAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("not registered in View Container"));

            var view = _hierarchy.AddChild<TestPresenter>();
            var stranger = new GameObject("Stranger").AddComponent<TestPresenter>();

            _viewService.PopView(stranger, skipAnimation: true);

            Object.DestroyImmediate(stranger.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PopView_OfAnUnknownTypeName_LogsAnError()
        {
            LogAssert.Expect(LogType.Error, new Regex("not found"));

            _viewService.PopView("ThereIsNoSuchPresenter", skipAnimation: true);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ClearHistory_KeepsOnlyTheCurrentView()
        {
            var first = _hierarchy.AddChild<TestPresenter>();
            var second = _hierarchy.AddChild<TestPresenter>();

            _viewService.PopView(first, skipAnimation: true);
            _viewService.PopView(second, skipAnimation: true);

            _viewService.ClearHistory();

            Assert.IsTrue(_viewService.IsLastView);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HideAndReset_ClearsTheActiveView()
        {
            var view = _hierarchy.AddChild<TestPresenter>();
            _viewService.PopView(view, skipAnimation: true);

            _viewService.HideAndReset(skipAnimation: true);

            Assert.IsNull(_viewService.ActiveView);
            Assert.IsFalse(_viewService.IsViewActive);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PopLoadingView_MakesTheLoadingScreenTheActiveView()
        {
            var view = _hierarchy.AddChild<TestPresenter>();
            _viewService.PopView(view, skipAnimation: true);

            _viewService.PopLoadingView();

            yield return null;

            Assert.AreSame(_hierarchy.Root.LoadingScreen, _viewService.ActiveView);
        }
    }
}
