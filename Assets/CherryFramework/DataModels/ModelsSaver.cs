using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using TriInspector;
using UnityEngine;

namespace CherryFramework.DataModels
{
    [DefaultExecutionOrder(10000)]
    public class ModelsSaver : BehaviourBase
    {
        [InfoBox("Select events when all models to be saved")]
        [SerializeField] private bool onDestroyThis;
        [SerializeField] private bool onApplicationLostFocus;
        [SerializeField] private bool onApplicationPause;
        [SerializeField] private bool onApplicationQuit = true;
        
        [Inject] private readonly ModelService _modelService;

        protected override void OnDestroy()
        {
            if (onDestroyThis)
                _modelService.DataStorage.SaveAllModels();
            base.OnDestroy();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && onApplicationPause)
                _modelService.DataStorage.SaveAllModels();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && onApplicationPause)
                _modelService.DataStorage.SaveAllModels();
        }

        private void OnApplicationQuit()
        {
            if (onApplicationQuit)
                _modelService.DataStorage.SaveAllModels();
        }
    }
}