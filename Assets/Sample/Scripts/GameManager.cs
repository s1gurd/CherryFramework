using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.StateService;
using DG.Tweening;
using GeneratedDataModels;
using Sample.Scripts.Settings;
using UnityEngine;


public class GameManager : BehaviourBase
{
    [Inject] private readonly GameSettings _gameSettings;
    [Inject] private readonly ModelService _modelService;
    [Inject] private readonly StateService _stateService;
    [Inject] private readonly InputSystem_Actions _inputSystem;
    [Inject] private readonly SaveGameManager _saveGame;

    private Sequence _speedUpTimer;
    private GameStateDataModel _gameState;
    
    void Start()
    {
        _gameState = _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
        _gameState.GameSpeed = _gameSettings.initialGameSpeed;
        
        Bindings.CreateBinding(_gameState.ReadyAccessor, ready =>
        {
            if (ready)
            {
                Bindings.CreateBinding(_gameState.PlayerDeadAccessor, dead =>
                {
                    if (dead)
                        GameOver();
                });
            }
        });
        _modelService.DataStorage.RegisterModelInStorage(_gameState);
        _modelService.DataStorage.LoadModelData(_gameState);
        
        StartGame();
    }
    
    public void StartGame()
    {
        _gameState.PlayerDead = false;
        _gameState.GameSpeedAccessor.RemoveAllProcessors();
        
        _speedUpTimer?.Kill();
        _speedUpTimer = DOTween.Sequence();
        _speedUpTimer.SetLoops(-1);
        _speedUpTimer.AppendInterval(_gameSettings.speedIncreasePeriod);
        _speedUpTimer.AppendCallback(() =>
        {
            _gameState.GameSpeed += _gameSettings.gameSpeedIncrease;
            _stateService.EmitEvent(EventKeys.SpeedUpGame);
        });
        
        _stateService.SetStatus(EventKeys.GameRunning);
        _inputSystem.Player.Enable();
    }
    
    public void GameOver(){
        _speedUpTimer?.Kill();
        _stateService.UnsetStatus(EventKeys.GameRunning);
        _gameState.GameSpeed = _gameSettings.initialGameSpeed;
        _inputSystem.Player.Disable();
    }
    
    private void OnApplicationQuit()
    {
        _saveGame.SaveAllData();
    }
}
