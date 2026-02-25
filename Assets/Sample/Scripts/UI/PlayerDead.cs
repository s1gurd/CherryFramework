using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.UI.InteractiveElements.Presenters;
using CherryFramework.UI.Views;
using DG.Tweening;
using GeneratedDataModels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sample.UI
{
    // Presenters must derive from PresenterBase and their objects must be placed in Child Presenters
    // list or a Root Presenter
    // Classes derived from PresenterBase have ShowFrom() and HideTo() methods that use UI animations
    // See CherryFramework/UI/UiAnimation/Animators
    // Also, PresenterBase classes can be shown and navigated through Model Service 
    public class PlayerDead : PresenterBase
    {
        [SerializeField] private Button statisticsBtn;
        [SerializeField] private Button restartBtn;
        [SerializeField] private TMP_Text resultTxt;
        
        [Inject] private readonly GameManager _gameManager;
        [Inject] private readonly ModelService _modelService;

        private GameStateDataModel _gameState;
        private string _resultTemplate;
        
        private void Start()
        {
            statisticsBtn.onClick.AddListener(() => ViewService.PopView<PlayerStats>());
            restartBtn.onClick.AddListener(() => _gameManager.RestartGame());
        }

        protected override void OnShowStart()
        {
            _resultTemplate ??= resultTxt.text;
            _gameState ??= _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
            resultTxt.text = string.Format(_resultTemplate, _gameState.DistanceTraveled, _gameState.RunTime);
        }
    }
}