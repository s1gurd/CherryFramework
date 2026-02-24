using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.TickDispatcher;
using Sample.Scripts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : BehaviourBase, IFixedTickable, IGameSaveData
{
    [Inject] private readonly GameSettings _gameSettings;
    [Inject] private readonly Ticker _ticker;
    [Inject] private readonly InputSystem_Actions _inputSystem;
    [Inject] private readonly SaveGameManager _saveGameManager;
    
    private CharacterController _character;
    [SaveGameData] private Vector3 _direction;
    [SaveGameData] private JumpState _jumpState;

    private void Start()
    {
        _character = GetComponent<CharacterController>();
        _inputSystem.Player.Jump.started += OnJump;
        _jumpState = JumpState.Idle;
        
        _saveGameManager.Register(this);
        _saveGameManager.LoadData(this);
        
        _ticker.Register(this);
    }

    private void OnJump(InputAction.CallbackContext obj)
    {
        if (_jumpState != JumpState.Idle) return;
        
        _direction = Vector3.up * _gameSettings.jumpForce;
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


