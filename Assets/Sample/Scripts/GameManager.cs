using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.SaveGameManager;
using CherryFramework.SoundService;
using CherryFramework.StateService;
using CherryFramework.UI.Views;
using DG.Tweening;
using GeneratedDataModels;
using Sample.Scripts.Settings;
using Sample.Settings;
using Sample.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sample
{
    // Behaviours inherited from BehaviourBase manage their subscriptions to different services automatically
    // No need to unsubscribe in OnDestroy
    public class GameManager : BehaviourBase
    {
        // Get the dependencies. They are injected in OnEnable 
        [Inject] private readonly GameSettings _gameSettings;
        [Inject] private readonly ModelService _modelService;
        [Inject] private readonly StateService _stateService;
        [Inject] private readonly InputSystem_Actions _inputSystem;
        [Inject] private readonly SaveGameManager _saveGame;
        [Inject] private readonly ViewService _viewService;
        [Inject] private readonly SoundService _soundService;

        private Sequence _speedUpTimer;
        private GameStateDataModel _gameState;
        private GameStatisticsModel _gameStatistics;

        private bool _reset;
        
        private void Start()
        {
            // Setup some models. Method names are self-explanatory
            _gameState = _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
            _modelService.DataStorage.RegisterModelInStorage(_gameState);
            var runStarted = _modelService.DataStorage.LoadModelData(_gameState);
            //If there was no saved model, set initial values from settings
            if (!runStarted)
            {
                _gameState.GameSpeed = _gameSettings.initialGameSpeed;
                _gameState.JumpForce = _gameSettings.jumpForce;
            }
        
            _gameStatistics = _modelService.GetOrCreateSingletonModel<GameStatisticsModel>();
            _modelService.DataStorage.RegisterModelInStorage(_gameStatistics);
            _modelService.DataStorage.LoadModelData(_gameStatistics);
        
            // The default  state of input system
            _inputSystem.UI.Enable();
            _inputSystem.Player.Disable();
        
            // Show the UI at startup.
            // If player is dead (remember, all the data was loaded) PlayerDead screen will be shown by binding
            if (!_gameState.PlayerDead)
            {
                // Check if the scene is reloaded, or game just started
                // Of course, this can be done using DontDestroyOnLoad and many other ways 
                if (!_gameStatistics.GameRunning)
                {
                    _viewService.PopView<GamePaused>(out var view);
                    view.SetMenuState(runStarted);
                }
                else
                {
                    _stateService.SetStatus(EventKeys.GameRunning);
                    StartGame();
                }
            }

            // Bind some actions to inputs
            _inputSystem.Player.Back.started += _ =>
            {
                _viewService.PopView<GamePaused>(out var view);
                view.SetMenuState(true);
            };
            _inputSystem.UI.Back.started += _ =>
            {
                if (_viewService.ActiveView is not PlayerDead && (!_viewService.IsLastView || _gameStatistics.GameRunning)) 
                    _viewService.Back();
            };
            
            // If UI is shown, set the game status to "not running". When it is hidden, run game back
            _viewService.OnAllViewsBecameInactive += () =>
            {
                _inputSystem.UI.Disable();
                _stateService.SetStatus(EventKeys.GameRunning);
            };
            _viewService.OnAnyViewBecameActive += () =>
            {
                _inputSystem.UI.Enable();
                _stateService.UnsetStatus(EventKeys.GameRunning);
            };
        
            // Switch input schemes when status is set or unset. We are doing it here because we want that game can be paused
            // or unpaused not only when UI is shown. For example, we may want to show some death animations
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameActive(EventKeys.GameRunning), () =>
            {
                _inputSystem.Player.Enable();
            });
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameInactive(EventKeys.GameRunning), () =>
            {
                _inputSystem.Player.Disable();
            });
        
            // Bind some logic to the event when the player is dead
            // Whenever _gameState.PlayerDead is set, it's accessor is called 
            Bindings.CreateBinding(_gameState.PlayerDeadAccessor, dead =>
            {
                if (dead)
                    OnPlayerDead();
            });
        }

        public void NewGame()
        {
            _gameStatistics.TriesNum++;
            StartGame();
        }
        
        public void StartGame()
        {
            _gameStatistics.GameRunning = true;
            
            // Set up the speed up infinite timer
            _speedUpTimer?.Kill();
            _speedUpTimer = DOTween.Sequence();
            _speedUpTimer.SetLoops(-1);
            _speedUpTimer.AppendInterval(_gameSettings.speedIncreasePeriod);
            _speedUpTimer.AppendCallback(() =>
            {
                _gameState.GameSpeed += _gameSettings.gameSpeedIncrease;
                _stateService.EmitEvent(EventKeys.SpeedUpGame);
            });
        }

        // THIS IS JUST FOR DEMO PURPOSES!!! Please implement more convenient logic in your game 
        // When we wabt to restart the game, just delete game data from storage and restart the level
        public void RestartGame()
        {
            _gameStatistics.GameRunning = true;
            _gameStatistics.TriesNum++;
            ClearData();
            _modelService.DataStorage.SaveModelToStorage(_gameStatistics);
            DOTween.KillAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    
        public void OnPlayerDead()
        {
           _speedUpTimer?.Kill();
           _viewService.PopView<PlayerDead>();
           _soundService.Play(EventKeys.GameOver, transform); 
        }
    
        private void OnApplicationQuit()
        {
            _gameStatistics.GameRunning = false;
        
            // Clear from storage all the loaded data for this session, just to remove possible leftovers
            // so the obsolete data from previous runs won't be stored
            // GameStatistics model will never be reset, so no need to clear it
            ClearData();

            // If the game is quitting, save all the current session data
            _saveGame.SaveAllData();
            _modelService.DataStorage.SaveModelToStorage(_gameState);
            _modelService.DataStorage.SaveModelToStorage(_gameStatistics);
            // Alternatively, you can reset models manually, but save all models automatically using ModelsSaver component  
        }

        private void ClearData()
        {
            foreach (var component in _saveGame.RegisteredComponents)
            {
                //Please note that DeleteData does not affect current loaded registered components, it only deletes data from storage
                _saveGame.DeleteData(component);
            }

            _modelService.DataStorage.DeleteModelFromStorage(_gameState);
        }
    }
}
