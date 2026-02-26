using System;
using System.Collections.Generic;
using UnityEngine;

namespace CherryFramework.DependencyManager
{
    // We need the installer to initialize before any other objects in scene
    [DefaultExecutionOrder(-10000)]
    public abstract class InstallerBehaviourBase : MonoBehaviour
    {
        private DependencyContainer Container => DependencyContainer.Instance;
        
        private HashSet<Type> _installedDependencies = new HashSet<Type>();
        
        protected abstract void Install();

        private void Awake()
        {
            Install();
        }

        protected void BindAsSingleton<TService>(TService instance) 
            where TService : class
        {
            Container.BindAsSingleton<TService>(instance);
            _installedDependencies.Add(typeof(TService));
        }

        protected void BindAsSingleton<TService>()
            where TService : class, new()
        {
            Container.BindAsSingleton<TService>();
            _installedDependencies.Add(typeof(TService));
        }
        
        protected void BindAsSingleton(Type typeService, object instance)
        {
            Container.BindAsSingleton(typeService, instance);
            _installedDependencies.Add(typeService);
        }

        protected void Bind<TService>(BindingType bindType) where TService : class, new()
        {
            Container.Bind<TService>(bindType);
            _installedDependencies.Add(typeof(TService));
        }
        
        protected void Bind<TImpl, TService>(BindingType bindType) 
            where TImpl : class, new() 
            where TService : class
        {
            Container.Bind<TImpl, TService>(bindType);
            _installedDependencies.Add(typeof(TService));
        }
        
        protected void BindAsSingleton<TImpl, TService>(TImpl instance) 
            where TImpl : class 
            where TService : class
        {
            Container.BindAsSingleton<TImpl, TService>(instance);
            _installedDependencies.Add(typeof(TService));
        }

        private void OnDestroy()
        {
            foreach (var type in _installedDependencies)
            {
                Container.RemoveDependency(type);
            }
        }
    }
}