using CherryFramework.BaseClasses;
using CherryFramework.ModelService;
using CherryFramework.DependencyManager;
using Sample.DataModels.generated;
using TMPro;
using UnityEngine;

namespace Sample.UI
{
    public class GameStatsHUD : BehaviourBase
    {
        [SerializeField] private TMP_Text distanceText;
        [SerializeField] private TMP_Text timeText;
        
        [Inject] private readonly DataModelService _modelService;

        private string _distanceTemplate;
        private string _timeTemplate;
        private void Start()
        {
            _distanceTemplate = distanceText.text;
            _timeTemplate = timeText.text;
            
            var gameState = _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
            Bindings.CreateBinding(gameState.DistanceTraveledAccessor, d =>
            {
                distanceText.text = string.Format(_distanceTemplate, d);
            });
            Bindings.CreateBinding(gameState.RunTimeAccessor, t =>
            {
                timeText.text = string.Format(_timeTemplate, t);
            });
        }
    }
}