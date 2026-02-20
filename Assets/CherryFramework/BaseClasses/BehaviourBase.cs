using System;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;

namespace CherryFramework.BaseClasses
{
    public abstract class BehaviourBase : InjectMonoBehaviour, IBindingsContainer, IUnsubscriber
    {
        private Action _onDestroy;  
        
        public Bindings Bindings { get; } = new();

        public void AddUnsubscription(Action action)
        {
            _onDestroy += action;
        }
        
        protected virtual void OnDestroy()
        {
            Bindings.ReleaseAllBindings();
            _onDestroy?.Invoke();
        }
    }
}