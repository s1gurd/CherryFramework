using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.StateService;
using CherryFramework.TickDispatcher;
using GeneratedDataModels;
using Sample.Scripts.Settings;
using UnityEngine;

public class Obstacle : BehaviourBase, ITickable
{
    [Inject] private readonly ModelService _modelService;
    [Inject] private readonly StateService _stateService;
    [Inject] private readonly Ticker _ticker;
    [Inject] private readonly Camera _camera;
    
    private GameStateDataModel _gameState;
    private float _leftEdge;
    private PersistentObject _persistentObject;

    void Start()
    {
        _leftEdge = _camera.ScreenToWorldPoint(Vector3.zero).x - 2f;
    }

    // Here we get the reference to the data model in OnEnable because Tick can be called before Start
    // (that's the difference between classic Update and Tick) 
    // We register Tick here instead of Start, because Obstacle objects are pooled, and we want turn tick back on 
    // when object is pulled from the pool
    protected override void OnEnable()
    {
        base.OnEnable();
        _gameState ??= _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
        _ticker.Register(this);
    }

    // Obstacle objects are pooled, so we want to unregister tick when an obstacle gets inactive
    private void OnDisable()
    {
        _ticker.UnRegister(this);
    }

    public void Tick(float deltaTime)
    {
        // If performance is not a concern, you may skip Register/UnRegister mess with ticker in OnEnable/OnDisable
        // callbacks and simply check here for object activity and return; when inactive
        if (!_stateService.IsStatusActive(EventKeys.GameRunning))
        {
            return;
        }
        
        transform.position += Vector3.left * (_gameState.GameSpeedAccessor.ProcessedValue * deltaTime);
        if(transform.position.x < _leftEdge){
            gameObject.SetActive(false);
        }
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     if (other.CompareTag("Player")) 
    //         _gameState.PlayerDead = true;
    // }
}
