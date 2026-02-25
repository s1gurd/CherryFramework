using CherryFramework.DependencyManager;
using CherryFramework.StateService;
using CherryFramework.UI.InteractiveElements.Widgets;
using Sample.Settings;

namespace Sample.UI
{
    // Classes derived from WidgetElement have Show() and Hide() methods that use UI animations
    //See CherryFramework/UI/UiAnimation/Animators
    public class PowerUpNotification : WidgetElement
    {
        [Inject] private readonly StateService _stateService;

        private void Start()
        {
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameActive(EventKeys.RocketPowerUp), () => Show());
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameInactive(EventKeys.RocketPowerUp), () => Hide());
        }
    }
}