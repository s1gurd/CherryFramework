using System.Collections.Generic;
using CherryFramework.UI.InteractiveElements;
using CherryFramework.UI.InteractiveElements.Widgets;
using CherryFramework.UI.UiAnimation;
using UnityEngine;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// A widget whose states the test fills in.
    /// </summary>
    public class TestWidget : WidgetBase
    {
        public TestWidget()
        {
            animators = new List<UiAnimationSettings>();
        }

        public void SetStates(List<WidgetState> states)
        {
            widgetStates = states;
        }
    }
}
