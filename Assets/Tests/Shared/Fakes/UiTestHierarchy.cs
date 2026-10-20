using System.Collections.Generic;
using CherryFramework.UI.InteractiveElements.Presenters;
using CherryFramework.UI.Views;
using UnityEngine;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// Builds a presenter hierarchy the way the framework expects it: a root
    /// presenter with a canvas as its children container, and the child
    /// presenters registered in it.
    ///
    /// The root starts inactive, so that the test can fill the wiring before
    /// the presenters initialise themselves.
    /// </summary>
    public class UiTestHierarchy
    {
        private readonly List<GameObject> _objects = new();

        public RootPresenterBase Root { get; private set; }
        public Canvas ChildrenContainer { get; private set; }

        public UiTestHierarchy(string rootName = "TestRoot")
        {
            var rootObject = new GameObject(rootName);
            rootObject.SetActive(false);
            _objects.Add(rootObject);

            Root = rootObject.AddComponent<RootPresenterBase>();

            var containerObject = new GameObject("ChildrenContainer");
            containerObject.transform.SetParent(rootObject.transform, false);
            _objects.Add(containerObject);

            ChildrenContainer = containerObject.AddComponent<Canvas>();

            SerializedFields.SetField(Root, "childrenContainer", ChildrenContainer);
        }

        /// <summary>
        /// Creates a child presenter, registers it in the root and leaves it
        /// inactive. The view service activates it when the view is shown.
        /// </summary>
        public T AddChild<T>(string name = null) where T : PresenterBase
        {
            var childObject = new GameObject(name ?? typeof(T).Name);
            childObject.transform.SetParent(Root.transform, false);
            childObject.SetActive(false);
            _objects.Add(childObject);

            var presenter = childObject.AddComponent<T>();

            if (!Root.ChildPresenters.Contains(presenter))
            {
                Root.ChildPresenters.Add(presenter);
            }

            return presenter;
        }

        public void SetLoadingScreen(PresenterLoadingBase loadingScreen)
        {
            SerializedFields.SetField(Root, "loadingScreen", loadingScreen);

            if (!Root.ChildPresenters.Contains(loadingScreen))
            {
                Root.ChildPresenters.Add(loadingScreen);
            }
        }

        public void Activate()
        {
            Root.gameObject.SetActive(true);
        }

        public void Destroy()
        {
            foreach (var gameObject in _objects)
            {
                if (gameObject != null)
                {
                    Object.DestroyImmediate(gameObject);
                }
            }

            _objects.Clear();
        }
    }
}
