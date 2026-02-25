using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.TickDispatcher;
using GeneratedDataModels;
using Sample.Scripts.Settings;
using Sample.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sample
{
    // Behaviours inherited from BehaviourBase manage their subscriptions to different services automatically
    // No need to unsubscribe in OnDestroy
    
    // To use SaveGameManager, class must implement IGameSaveData
    // fields and properties with [SaveGameData] gets their data loaded when (Save Game Manager instance).LoadData(this) is called
    
    // To use Ticker Service instead of ordinary Update(), a class must derive from ITickable, IFixedTickable
    // or ILateTickable interfaces in any combination
    // To start receiving ticks just call (Ticker instance).Register(this, (optional tick period)). To stop - call Unregister
    public class Player : BehaviourBase, IFixedTickable, IGameSaveData
    {
        [Inject] private readonly ModelService _modelService;
        [Inject] private readonly GameSettings _gameSettings;
        [Inject] private readonly Ticker _ticker;
        [Inject] private readonly InputSystem_Actions _inputSystem;
        [Inject] private readonly SaveGameManager _saveGameManager;
    
        private CharacterController _character;
        private GameStateDataModel _gameState;
        
        [SaveGameData] private Vector3 _direction;
        [SaveGameData] private JumpState _jumpState;

        private void Start()
        {
            _character = GetComponent<CharacterController>();
            _gameState = _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
            _inputSystem.Player.Jump.started += OnJump;
            _jumpState = JumpState.Idle;
        
            _saveGameManager.Register(this);
            _saveGameManager.LoadData(this);
        
            _ticker.Register(this);
        }

        private void OnJump(InputAction.CallbackContext obj)
        {
            if (_jumpState != JumpState.Idle) return;
        
            //Notice that here we get Processed Value, so RocketPowerUp effect is used
            _direction = Vector3.up * _gameState.JumpForceAccessor.ProcessedValue;
            _jumpState = JumpState.Pending;
        }

        public void FixedTick(float deltaTime)
        {
            if(_jumpState == JumpState.Jumping && _character.isGrounded){
                _direction=Vector3.zero;
                _jumpState = JumpState.Idle;
            }

            if (_jumpState == JumpState.Pending)
            {
                _jumpState = JumpState.Jumping;
            }
        
            if (_jumpState == JumpState.Jumping)
            {
                _direction += Vector3.down * (_gameSettings.gravity * deltaTime);
            }
            _character.Move(_direction * deltaTime);
        }
    
        private enum JumpState
        {
            Idle,
            Pending,
            Jumping
        }
    }
}


