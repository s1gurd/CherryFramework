using System;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;

namespace CherryFramework.BaseClasses
{
    public class GeneralClassBase : InjectClass, IDisposable, IBindingsContainer, IUnsubscriber
    {
        private Action _onDestroy;
        
        public Bindings Bindings { get; } = new();

        public void AddUnsubscription(Action action)
        {
            _onDestroy += action;
        }
        
        public virtual void Dispose()
        {
            Bindings.ReleaseAllBindings();
            _onDestroy?.Invoke();
        }
    }
}