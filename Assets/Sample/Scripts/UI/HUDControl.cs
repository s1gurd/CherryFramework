using CherryFramework.DependencyManager;
using CherryFramework.StateService;
using CherryFramework.UI.InteractiveElements.Widgets;
using Sample.Settings;

namespace Sample.UI
{
    // Classes derived from WidgetBase can switch between states with animation
    public class HUDControl : WidgetBase
    {
        [Inject] private readonly StateService _stateService;

        private void Start()
        {
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameActive(EventKeys.GameRunning), () => SetState(1));
            _stateService.AddStateSubscription(s => s.IsStatusJustBecameInactive(EventKeys.GameRunning), () => SetState(0));
        }
    }
}