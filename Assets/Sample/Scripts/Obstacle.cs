using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
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
    public class Obstacle : BehaviourBase, ITickable
    {
        [Inject] protected readonly StateService StateService;
        [Inject] private readonly ModelService _modelService;
        [Inject] private readonly Ticker _ticker;
        [Inject] private readonly Camera _camera;
    
        protected GameStateDataModel GameState;
        protected float LeftEdge;
        
        private PersistentObject _persistentObject;
        
        private void Start()
        {
            LeftEdge = _camera.ScreenToWorldPoint(Vector3.zero).x - 2f;
        }

        // Here we get the reference to the data model in OnEnable because Tick can be called before Start
        // (that's the difference between classic Update and Tick) 
        // We register Tick here instead of Start, because Obstacle objects are pooled, and we want turn tick back on 
        // when object is pulled from the pool
        protected override void OnEnable()
        {
            base.OnEnable();
            GameState ??= _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
            _ticker.Register(this);
        }

        private void OnDisable()
        {
            _ticker.UnRegister(this);
        }

        public void Tick(float deltaTime)
        {
            // If performance is not a concern, you may skip Register/UnRegister mess with ticker in OnEnable/OnDisable
            // callbacks and simply check here for object activity and return; when inactive
            if (!StateService.IsStatusActive(EventKeys.GameRunning))
            {
                return;
            }
        
            transform.position += Vector3.left * (GameState.GameSpeed * deltaTime);
            if(transform.position.x < LeftEdge){
                gameObject.SetActive(false);
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) 
                GameState.PlayerDead = true;
        }
    }
}
