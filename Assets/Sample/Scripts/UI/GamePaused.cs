using CherryFramework.DependencyManager;
using CherryFramework.UI.InteractiveElements.Presenters;
using CherryFramework.UI.Views;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Sample.UI
{
    // Main Presenter class for game menu
    // Presenters must derive from PresenterBase and their objects must be placed in Child Presenters
    // list or a Root Presenter
    // Classes derived from PresenterBase have ShowFrom() and HideTo() methods that use UI animations
    // See CherryFramework/UI/UiAnimation/Animators
    // Also, PresenterBase classes can be shown and navigated through Model Service 
    public class GamePaused : PresenterBase
    {
        [Inject] private readonly GameManager _gameManager;
        
        [SerializeField] private Button statisticsBtn;
        [SerializeField] private Button restartBtn;
        [SerializeField] private Button continueBtn;
        [SerializeField] private Button newGameBtn;

        // Switch the state depending on the previous game was found or not 
        public void SetMenuState(bool gameStarted)
        {
            if (gameStarted)
            {
                restartBtn.gameObject.SetActive(true);
                continueBtn.gameObject.SetActive(true);
                newGameBtn.gameObject.SetActive(false);
            }
            else
            {
                restartBtn.gameObject.SetActive(false);
                continueBtn.gameObject.SetActive(false);
                newGameBtn.gameObject.SetActive(true);
            }
        }
        private void Start()
        {
            statisticsBtn.onClick.AddListener(() => ViewService.PopView<PlayerStats>());
            restartBtn.onClick.AddListener(() => _gameManager.RestartGame());
            continueBtn.onClick.AddListener(() =>
            {
                ViewService.HideAndReset().AppendCallback(() => _gameManager.StartGame());
            });
            newGameBtn.onClick.AddListener(() =>
            {
                ViewService.HideAndReset().AppendCallback(() => _gameManager.NewGame());
            });
        }
    }
}