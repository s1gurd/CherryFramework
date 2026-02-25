using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.StateService;
using CherryFramework.TickDispatcher;
using GeneratedDataModels;
using Sample.Settings;
using UnityEngine;

namespace Sample
{
    // Behaviours inherited from BehaviourBase manage their subscriptions to different services automatically
    // No need to unsubscribe in OnDestroy
    
    // To use Ticker Service instead of ordinary Update(), a class must derive from ITickable, IFixedTickable
    // or ILateTickable interfaces in any combination
    // To start receiving ticks just call (Ticker instance).Register(this, (optional tick period)). To stop - call Unregister
    public class Ground : BehaviourBase, ITickable
    {
        [Inject] private readonly StateService _stateService;
        [Inject] private readonly ModelService _modelService;
        [Inject] private readonly Ticker _ticker;
    
        private MeshRenderer _meshRenderer;
        private GameStateDataModel _gameState;
        private GameStatisticsModel _gameStatistics;

        private float _travelTimeFraction;
        private float _travelDistanceFraction;
    
        private void Start()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            _gameState = _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
            _gameStatistics = _modelService.GetOrCreateSingletonModel<GameStatisticsModel>();
        
            // We want the game run only when iti is running 
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameActive(EventKeys.GameRunning), () =>
            {
                _ticker.Register(this);
            });
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameInactive(EventKeys.GameRunning), () =>
            {
                _ticker.UnRegister(this);
            });
        }

        public void Tick(float deltaTime)
        {
            var speed = _gameState.GameSpeed / transform.localScale.x;
            _meshRenderer.material.mainTextureOffset += Vector2.right * (speed * deltaTime);
        
            _travelDistanceFraction += _gameState.GameSpeed * deltaTime;
            if (_travelDistanceFraction >= 1f)
            {
                var units = (int)_travelDistanceFraction;
                _gameState.DistanceTraveled += units;
                _gameStatistics.TotalDistance += units;
                _travelDistanceFraction -= units;
                
                if (_gameState.DistanceTraveled > _gameStatistics.MaxDistance)
                {
                    _gameStatistics.MaxDistance = _gameState.DistanceTraveled;
                }
            }
        
            _travelTimeFraction += deltaTime;
            if (_travelTimeFraction >= 1f)
            {
                var seconds = (int)_travelTimeFraction;
                _gameState.RunTime += seconds;
                _gameStatistics.TotalRunTime += seconds;
                _travelTimeFraction -= seconds;
            }
        
        }
    }
}
