using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.StateService;
using CherryFramework.TickDispatcher;
using GeneratedDataModels;
using Sample.Scripts.Settings;
using UnityEngine;

public class Ground : BehaviourBase, ITickable
{
    [Inject] private readonly StateService _stateService;
    [Inject] private readonly ModelService _modelService;
    [Inject] private readonly Ticker _ticker;
    
    private MeshRenderer _meshRenderer;
    private GameStateDataModel _gameStateDataModel;
    
    private void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _gameStateDataModel = _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
        
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
        var speed = _gameStateDataModel.GameSpeedAccessor.ProcessedValue / transform.localScale.x;
        _meshRenderer.material.mainTextureOffset += Vector2.right * (speed * deltaTime);
    }
}
