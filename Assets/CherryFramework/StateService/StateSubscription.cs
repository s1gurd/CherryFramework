using System;

namespace CherryFramework.StateService
{
    public class StateSubscription
    {
        public readonly Predicate<StateAccessor> Condition;
        public readonly Action Callback;
        public readonly bool DestroyAfterInvoke;

        public StateSubscription(Predicate<StateAccessor> condition, Action callback, bool destroyAfterInvoke)
        {
            Condition = condition;
            Callback = callback;
            DestroyAfterInvoke = destroyAfterInvoke;
        }
    }
}