using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using CherryFramework.TickDispatcher;
using Sample.Scripts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : BehaviourBase, ITickable
{
    [Inject] private readonly GameSettings _gameSettings;
    [Inject] private readonly Ticker _ticker;
    [Inject] private readonly InputSystem_Actions _inputSystem;
    
    private CharacterController _character;
    private Vector3 _direction;
    private bool _jumpQueued;

    private void Start()
    {
        _character = GetComponent<CharacterController>();
        _inputSystem.Player.Jump.started += OnJump;
        _ticker.Register(this);
    }

    private void OnJump(InputAction.CallbackContext obj)
    {
        if (_jumpQueued) return;
        
        _direction = Vector3.up * _gameSettings.jumpForce;
        _jumpQueued = true;
    }

    public void Tick(float deltaTime)
    {
        if(_character.isGrounded){
            _direction=Vector3.zero;
            _jumpQueued=false;
        }
        else
        {
            _direction += Vector3.down * (_gameSettings.gravity * deltaTime);
        }
        _character.Move(_direction * deltaTime);
    }
}
