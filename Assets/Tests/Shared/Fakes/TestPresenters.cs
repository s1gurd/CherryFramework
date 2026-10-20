using System.Collections.Generic;
using CherryFramework.UI.InteractiveElements;
using CherryFramework.UI.InteractiveElements.Presenters;
using CherryFramework.UI.UiAnimation;
using UnityEngine;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// A plain presenter. The framework only ships abstract presenters, so
    /// the tests need a concrete one to build a view hierarchy with.
    /// </summary>
    public class TestPresenter : PresenterBase
    {
        public int InitializedCount { get; private set; }

        public TestPresenter()
        {
            // CreateSequence walks that list, a null one would throw.
            animators = new List<UiAnimationSettings>();
        }

        protected override void OnPresenterInitialized()
        {
            InitializedCount++;
        }
    }

    /// <summary>
    /// A presenter that blocks the navigation while it is on top.
    /// </summary>
    public class TestModalPresenter : TestPresenter
    {
        public override bool Modal => true;
    }

    /// <summary>
    /// A presenter that is replaced by the next one instead of stacking.
    /// </summary>
    public class TestPopupPresenter : TestPresenter, IPopUp
    {
    }

    /// <summary>
    /// The loading screen of the root presenter.
    /// </summary>
    public class TestLoadingPresenter : PresenterLoadingBase
    {
        public TestLoadingPresenter()
        {
            animators = new List<UiAnimationSettings>();
        }
    }
}
