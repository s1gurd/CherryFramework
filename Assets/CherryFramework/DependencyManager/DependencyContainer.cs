using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace CherryFramework.DependencyManager
{
    [Preserve]
    public sealed class DependencyContainer : IDisposable
    {
        public static DependencyContainer Instance => Lazy.Value;
        private static readonly Lazy<DependencyContainer> Lazy = new(() => new DependencyContainer());

        private DependencyContainer()
        {
        }

        private readonly Dictionary<Type, Dependency> _dependencies = new ();

        public void BindAsSingleton<TService>(TService instance) 
            where TService : class
        {
            BindAsSingleton(typeof(TService), instance);
        }

        public void BindAsSingleton<TService>()
            where TService : class, new()
        {
            var typeService = typeof(TService);
            var dep = new Dependency
            {
                Factory = () => InjectDependencies(new TService()),
                BindType = BindingType.Singleton
            };
            
            if (!_dependencies.TryAdd(typeService, dep))
            {
                Debug.LogError($"[Dependency Container] Could not add binding of type {typeService} as it is already installed!");
            }
        }
        
        public void BindAsSingleton(Type typeService, object instance)
        {
            if (instance == null)
                throw new NullReferenceException($"[Dependency Container] Could not add binding of type {typeService} the instance is missing.");

            var dep = new Dependency
            {
                BindedInstance = instance,
                BindType = BindingType.Singleton
            };
            
            if (!_dependencies.TryAdd(typeService, dep))
            {
                Debug.LogError($"[Dependency Container] Could not add binding of type {typeService} as it is already installed!");
            }
        }

        public void Bind<TService>(BindingType bindType) where TService : class, new()
        {
            var typeService = typeof(TService);
            var dep = new Dependency
            {
                Factory = () => InjectDependencies(new TService()),
                BindType = bindType
            };
            
            if (!_dependencies.TryAdd(typeService, dep))
            {
                Debug.LogError($"[Dependency Container] Could not add binding of type {typeService} as it is already installed!");
            }
        }
        
        public void Bind<TImpl, TService>(BindingType bindType) 
            where TImpl : class, new() 
            where TService : class
        {
            var typeService = typeof(TService);
            var typeImpl = typeof(TImpl);
            if (!typeService.IsAssignableFrom(typeImpl))
            {
                Debug.LogError($"[Dependency Container] Could not add binding of object with type {typeImpl} as it is not assignable from {typeImpl}!");
                return;
            }

            var dep = new Dependency
            {
                Factory = () => InjectDependencies(new TImpl() as TService),
                BindType = bindType
            };
            
            if (!_dependencies.TryAdd(typeService, dep))
            {
                Debug.LogError($"[Dependency Container] Could not add binding of type {typeImpl} as it is already installed!");
            }
        }
        
        public void BindAsSingleton<TImpl, TService>(TImpl instance) 
            where TImpl : class 
            where TService : class
        {
            var typeService = typeof(TService);
            if (instance == null)
                throw new NullReferenceException($"[Dependency Container] Could not add binding of type {typeService} the instance is missing.");
            
            var typeImpl = typeof(TImpl);
            if (!typeService.IsAssignableFrom(typeImpl))
            {
                Debug.LogError($"[Dependency Container] Could not add binding of object with type {typeImpl} as it is not assignable from {typeImpl}!");
                return;
            }

            var dep = new Dependency
            {
                BindedInstance = instance,
                BindType = BindingType.Singleton
            };
            
            if (!_dependencies.TryAdd(typeService, dep))
            {
                Debug.LogError($"[Dependency Container] Could not add binding of type {typeImpl} as it is already installed!");
            }
        }

        public T InjectDependencies<T>(T target)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            
            var fields = new List<FieldInfo>();
            var currentType = target.GetType();
            while (currentType != null)
            {
                fields.AddRange( currentType.GetFields(flags)
                    .Where(f => f.GetCustomAttributes(typeof(InjectAttribute)).Any()).ToList());
                currentType = currentType.BaseType;
            }
            
            var props = new List<PropertyInfo>();
            currentType = target.GetType();
            while (currentType != null)
            {
                props.AddRange( currentType.GetProperties(flags)
                    .Where(p => p.GetCustomAttributes(typeof(InjectAttribute)).Any() && p.CanWrite).ToList());
                currentType = currentType.BaseType;
            }

            foreach (var field in fields)
            {
                InjectFieldValue(field);
            }

            foreach (var prop in props)
            {
                InjectPropValue(prop);
            }
            
            return target;
            
            void InjectFieldValue(FieldInfo field)
            {
                if (_dependencies.TryGetValue(field.FieldType, out var dep))
                {
                    switch (dep.BindType)
                    {
                        case BindingType.Singleton:
                            dep.BindedInstance ??= dep.Factory();
                            field.SetValue(target, dep.BindedInstance);
                            break;
                        case BindingType.Transient:
                            field.SetValue(target, dep.Factory());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(dep.BindType.ToString());
                    }
                }
                else
                {
                    Debug.LogError($"[Dependency Container] {target.GetType()} tried to receive field injection of type {field.FieldType} which is not registered in the container!");
                }
            }
            
            void InjectPropValue(PropertyInfo prop)
            {
                if (_dependencies.TryGetValue(prop.PropertyType, out var dep))
                {
                    switch (dep.BindType)
                    {
                        case BindingType.Singleton:
                            dep.BindedInstance ??= dep.Factory();
                            prop.SetValue(target, dep.BindedInstance);
                            break;
                        case BindingType.Transient:
                            prop.SetValue(target, dep.Factory());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(dep.BindType.ToString());
                    }
                }
                else
                {
                    Debug.LogError($"[Dependency Container] {target.GetType()} tried to receive property injection of type {prop.PropertyType} which is not registered in the container!");
                }
            }
        }

        public void RemoveDependency(Type type)
        {
            var dep = _dependencies[type];
            if (dep.BindedInstance is IDisposable disposable)
                disposable.Dispose();
            
            _dependencies.Remove(type);
        }
        
        public bool HasDependency<T>() => HasDependency(typeof(T));

        public bool HasDependency(Type type)
        {
            return _dependencies.ContainsKey(type);
        }

        internal T GetInstance<T>()
        {
            if (_dependencies.TryGetValue(typeof(T), out var dep))
            {
                return (T)dep.BindedInstance;
            }
            Debug.LogError($"[Dependency Container] Tried to get dependency of type {typeof(T)} which is not registered in the container!");
            return default;
        }
        
        private class Dependency
        {
            public object BindedInstance;
            public Func<object> Factory;
            public BindingType BindType;
        }

        public void Dispose()
        {
            foreach (var dep in _dependencies.Values)
            {
                if (dep.BindedInstance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
    
    public enum BindingType
    {
        Singleton = 0,
        Transient = 1
    }
}