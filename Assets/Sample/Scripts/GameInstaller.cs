using CherryFramework.DataModels;
using CherryFramework.DataModels.ModelDataStorageBridges;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.StateService;
using CherryFramework.TickDispatcher;
using CherryFramework.Utils.PlayerPrefsWrapper;
using Sample.Scripts.Settings;
using UnityEngine;

namespace Sample.Scripts
{
    // We need the installer to initialize before any other objects in scene
    [DefaultExecutionOrder(-10000)]
    public class GameInstaller : InstallerBehaviourBase
    {
        [SerializeField] private GameSettings gameSettings;
        
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
        }
    }
}