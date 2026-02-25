using CherryFramework.DataModels;
using CherryFramework.DataModels.ModelDataStorageBridges;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.StateService;
using CherryFramework.TickDispatcher;
using CherryFramework.UI.Views;
using CherryFramework.Utils.PlayerPrefsWrapper;
using Sample.Scripts.Settings;
using Sample.Settings;
using UnityEngine;

namespace Sample
{
    // We need the installer to initialize before any other objects in scene
    [DefaultExecutionOrder(-10000)]
    public class GameInstaller : InstallerBehaviourBase
    {
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private RootPresenterBase uiRoot;
        
        protected override void Install()
        {
            // Here we bind some objects for injection in dependent classes using [Inject] attribute
            // Notice that binding can be called by multiple ways - generic or by instance
            BindAsSingleton<Ticker>();
            BindAsSingleton(new StateService(false));
            BindAsSingleton(new SaveGameManager(new PlayerPrefsData(), true));
            BindAsSingleton(new ModelService(new PlayerPrefsBridge<PlayerPrefsData>(), true));
            BindAsSingleton(new InputSystem_Actions());
            BindAsSingleton(gameSettings);
            BindAsSingleton(Camera.main);
            BindAsSingleton(new ViewService(uiRoot, true));
            BindAsSingleton(gameObject.AddComponent<GameManager>());
            // When this component is destroyed, all the dependencies it binded are cleared
            // So, if some class tries to receive an injection of an object binded here, exception will occur
        }
    }
}