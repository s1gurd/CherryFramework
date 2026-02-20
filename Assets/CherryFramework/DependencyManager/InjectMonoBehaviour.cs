using UnityEngine;

namespace CherryFramework.DependencyManager
{
    public abstract class InjectMonoBehaviour : MonoBehaviour, IInjectTarget
    {
        protected bool Injected { get; private set; }

        protected virtual void OnEnable() => Inject();

        protected void Inject()
        {
            if (Injected) 
                return;
            
            DependencyContainer.Instance.InjectDependencies(this);
            Injected = true;
        }
    }
}