using CherryFramework.DependencyManager;
using CherryFramework.StateService;
using CherryFramework.UI.InteractiveElements.Widgets;
using DG.Tweening;
using Sample.Settings;

namespace Sample.UI
{
    // Classes derived from WidgetElement have Show() and Hide() methods that use UI animations
    //See CherryFramework/UI/UiAnimation/Animators
    public class SpeedUpNotification : WidgetElement
    {
        [Inject] private readonly StateService _stateService;
        [Inject] private readonly GameSettings _gameSettings;

        private Sequence _sequence;
        private void Start()
        {
            _sequence = DOTween.Sequence();
            _stateService.AddStateSubscription(s => s.IsEventActive(EventKeys.SpeedUpGame), () =>
            {
                _sequence.Kill(true);
                _sequence = Show();
                _sequence.AppendInterval(_gameSettings.notificationShowTime);
                _sequence.AppendCallback(() => Hide());
            });
        }
    }
}