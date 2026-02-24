using CherryFramework.DependencyManager;
using CherryFramework.StateService;
using CherryFramework.UI.InteractiveElements.Widgets;
using Sample.Scripts.Settings;
using UnityEngine;

namespace Sample.Scripts.UI
{
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