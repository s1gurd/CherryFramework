namespace CherryFramework.StateService
{
    public class StateAccessor
    {
        private StateService _stateService;

        public StateAccessor(StateService stateService)
        {
            _stateService = stateService;
        }
        
        public bool IsEventActive(string key) => _stateService.IsEventActive(key);
        public bool IsStatusJustBecameActive(string key) => _stateService.IsStatusJustBecameActive(key);
        public bool IsStatusJustBecameInactive(string key) => _stateService.IsStatusJustBecameInactive(key);
    }
}