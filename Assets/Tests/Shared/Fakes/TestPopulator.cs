using CherryFramework.UI.InteractiveElements.Populators;
using CherryFramework.UI.UiAnimation;
using UnityEngine;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// The data a test populator holds.
    /// </summary>
    public class TestItem
    {
        public string Name;
    }

    /// <summary>
    /// An element of a dynamic list.
    ///
    /// The element is not a generic type on purpose: Unity cannot add a
    /// generic MonoBehaviour to a GameObject.
    /// </summary>
    public class TestPopulatorElement : PopulatorElementBase<TestItem>
    {
        public TestPopulatorElement()
        {
            animators = new System.Collections.Generic.List<UiAnimationSettings>();
        }
    }

    /// <summary>
    /// A dynamic list of items.
    /// </summary>
    public class TestPopulator : PopulatorBase<TestItem>
    {
        public TestPopulator(PopulatorElementBase<TestItem> elementSample, Transform root) : base(elementSample, root)
        {
        }
    }
}
