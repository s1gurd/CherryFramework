using CherryFramework.UI.InteractiveElements.Presenters;
using UnityEngine;

namespace CherryFramework.UI.Views
{
    public class RootPresenterBase : PresenterBase
    {
        [SerializeField] private PresenterLoadingBase loadingScreen;
        [SerializeField] private PresenterErrorBase errorScreen;
    
        public PresenterLoadingBase LoadingScreen => loadingScreen;
        public PresenterErrorBase ErrorScreen => errorScreen;

        protected override void OnEnable()
        {
            base.OnEnable();
            
            if (loadingScreen != null)
            {
                childPresenters.Add(loadingScreen);
            }
            if (errorScreen != null)
            {
                childPresenters.Add(errorScreen);
            }
        }
    }
}
