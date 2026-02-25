using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.UI.InteractiveElements.Presenters;
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
    public class PlayerStats : PresenterBase
    {
        [SerializeField] private Button okayBtn;
        [SerializeField] private TMP_Text statTxt;
        
        [Inject] private readonly ModelService _modelService;

        private GameStateDataModel _gameState;
        private GameStatisticsModel _gameStatistics;
        private string _textTemplate;

        private void Start()
        {
            okayBtn.onClick.AddListener(() => ViewService.Back());
        }

        protected override void OnShowStart()
        {
            _textTemplate ??= statTxt.text;
            _gameState ??= _modelService.GetOrCreateSingletonModel<GameStateDataModel>();
            _gameStatistics ??= _modelService.GetOrCreateSingletonModel<GameStatisticsModel>();
            statTxt.text = string.Format(_textTemplate, _gameState.DistanceTraveled, _gameState.RunTime,
                _gameStatistics.TriesNum, _gameStatistics.TotalDistance, _gameStatistics.TotalRunTime,
                _gameStatistics.MaxDistance);
        }
    }
}