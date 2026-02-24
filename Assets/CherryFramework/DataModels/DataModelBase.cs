using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace CherryFramework.DataModels
{
    [Serializable]
    public abstract class DataModelBase
    {
        private readonly Dictionary<string, List<DownwardBindingHandler>> _handlers = new ();
        private bool _debugMode;
        private bool _bindingsOff;
        private bool _ready;
        
        [JsonIgnore] public string Id { get; private set; } = "";
        [JsonIgnore] public string SlotId { get; private set; } = "";

        
        protected Dictionary<string, Delegate> Getters = new ();
        protected Dictionary<string, Delegate> Setters = new ();
        
        
        
        [JsonIgnore]
        public bool Ready
        {
            get => _ready;
            set
            {
                if (_ready && value)
                    Debug.LogError($"[{GetType().Name}] Tried to set Ready for model which is already ready!");
                _ready = value; 
                Send(nameof(Ready), value);
            }
        }

        [JsonIgnore]
        public Accessor<bool> ReadyAccessor;

        protected DataModelBase()
        {
            Getters.Add(nameof(Ready), new Func<bool>(() => Ready));
            Setters.Add(nameof(Ready), new Action<bool>(o => Ready = o));
            ReadyAccessor = new Accessor<bool>(this, nameof(Ready));
        }

        public void AddBinding<T>(string memberName, DownwardBindingHandler handler, bool invokeImmediate)
        {
            if (!_handlers.ContainsKey(memberName))
            {
                _handlers.Add(memberName, new List<DownwardBindingHandler> { handler });
            } 
            else
            {
                _handlers[memberName].Add(handler);
            }
            
            if (invokeImmediate && Getters.TryGetValue(memberName, out var getter) && getter != null)
            {
                ((DownwardBindingHandler<T>)handler).DownwardCallback.Invoke(((Func<T>)getter).Invoke());
            }
        }

        public void RemoveBinding(DownwardBindingHandler handler)
        {
            var keys = _handlers.Where(kvp => kvp.Value != null && kvp.Value.Contains(handler)).Select(kvp => kvp.Key).ToList();
            
            for (var i=0; i < keys.Count; i++)
            {
                var handlersList = _handlers[keys[i]].Where(h => !ReferenceEquals(handler, h)).ToList();
                if (handlersList.Count == 0)
                {
                    _handlers.Remove(keys[i]);
                    continue;
                }
                
                _handlers[keys[i]] = handlersList;
            }
        }

        public T GetValue<T>(string memberName)
        {
            if (Getters.TryGetValue(memberName, out var getter))
            {
                return ((Func<T>)getter).Invoke();
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Not found member with name {memberName} while trying to invoke its downward bindings!");
                return default;
            }
        }

        public void SetValue<T>(string memberName, T value)
        {
            if (Setters.TryGetValue(memberName, out var action) && action != null)
            {
                if (_debugMode)
                {
                    Debug.Log($"[{GetType().Name}] Received upwards {memberName} = {value?.ToString()}");
                }
                (action as Action<T>)!.Invoke(value);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Not found member with name {memberName} while trying to set its value");
            }
        }

        public void InvokeBinding<T>(string memberName)
        {
            if (Getters.TryGetValue(memberName, out var getter))
            {
                var value = ((Func<T>)getter).Invoke();
                Send(memberName, value);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Not found member with name {memberName} while trying to invoke its downward bindings!");
            }
        }

        public void PauseBindings(bool pause)
        {
            _bindingsOff = pause;
        }

        public void SetDebugMode(bool value)
        {
            _debugMode = value;
        }
        
        public void FillFrom(object instance)
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var insType = instance.GetType();
            var fromProps = insType.GetProperties(bindingFlags).Where(p => p.CanWrite).ToDictionary(x => x.Name);
            var fromFields = insType.GetFields(bindingFlags).ToDictionary(x => x.Name);
            
            foreach (var thisProp in GetType().GetProperties(bindingFlags).Where(p => p.CanWrite))
            {
                if (fromProps.TryGetValue(thisProp.Name, out var fromProp) && thisProp.PropertyType == fromProp.PropertyType)
                    thisProp.SetValue(this, fromProp.GetValue(instance));
                
                if (fromFields.TryGetValue(thisProp.Name, out var fromField) && thisProp.PropertyType == fromField.FieldType)
                    thisProp.SetValue(this, fromField.GetValue(instance));
            }
        }

        public void SetId(string id)
        {
            Id = id;
        }
        
        public void SetSlotId(string slotId)
        {
            SlotId = slotId;
        }
        
        protected void Send<T>(string memberName, T value)
        {
            if (_bindingsOff) return;

            if (_debugMode)
            {
                Debug.Log($"[{GetType().Name}] Send downwards {memberName} = {value?.ToString()}");
            }
            if (!_handlers.TryGetValue(memberName, out var handlers)) return;

            foreach (var handler in handlers)
            {
                (handler as DownwardBindingHandler<T>)!.DownwardCallback.Invoke(value);
            }
        }
    }
}