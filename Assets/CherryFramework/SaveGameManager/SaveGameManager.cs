using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CherryFramework.DataModels;
using CherryFramework.Utils;
using CherryFramework.Utils.PlayerPrefsWrapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CherryFramework.SaveGameManager
{
    public class SaveGameManager
    { 
        protected bool DebugMessages;
        
        private readonly IPlayerPrefs _playerPrefs;
        private readonly Dictionary<IGameSaveData, PersistentObject> _persistentComponents =  new ();
        private static readonly ConcurrentDictionary<Type, SaveDataCache> _saveDataCache = new();

        public string SlotId { get; private set; } = "";
        public IGameSaveData[] RegisteredComponents => _persistentComponents.Keys.ToArray();
        public PersistentObject[]  RegisteredObjects => _persistentComponents.Values.Distinct().ToArray();

        public SaveGameManager(IPlayerPrefs playerPrefs, bool debugMessages)
        {
            _playerPrefs = playerPrefs;
            DebugMessages = debugMessages;
        }

        public virtual bool Register<T>(T component, PersistentObject persistentObj = null) where T : IGameSaveData
        {
            if (!persistentObj)
            {
                if (component is MonoBehaviour monoBehaviour)
                {
                    persistentObj = monoBehaviour.gameObject.GetComponent<PersistentObject>();
                    
                    if (!persistentObj)
                    {
                        Debug.LogError(
                            $"[Save Game Manager] Tried register component {component}, whose game object does not have PersistentObject component!",
                            monoBehaviour);
                        return false;
                    }
                }
            }
             
            if (!persistentObj)
            {
                Debug.LogError($"[Save Game Manager] Tried register component {component}, which is not MonoBehaviour and PersistentObject is NULL");
                return false;
            }

            if (!_persistentComponents.TryAdd(component, persistentObj))
            {
                Debug.LogError($"[Save Game Manager] Tried to register component {component}, which is already registered!");
                return false;
            }

            return true;
        }

        public virtual bool UnRegister<T>(T component) where T : IGameSaveData
        {
            if (!_persistentComponents.ContainsKey(component))
            {
                Debug.LogError($"[Save Game Manager] Tried to unregister component {component}, which is not registered!");
                return false;
            }
            _persistentComponents.Remove(component);
            return true;
        }

        public virtual bool UnRegisterObject(PersistentObject persistentObj)
        {
            if (!RegisteredObjects.Contains(persistentObj))
            {
                Debug.LogError($"[Save Game Manager] Tried to unregister object {persistentObj}, but there are no registered components!");
                return false;
            }

            var result = true;
            var kvps = _persistentComponents.Where(kvp => kvp.Value == persistentObj).ToArray();
            foreach (var kvp in kvps)
            {
                result &= UnRegister(kvp.Key);
            }
            return result;
        }

        public virtual bool LoadData<T>(T component) where T : IGameSaveData
        {
            if (!_persistentComponents.TryGetValue(component, out var persistentObj))
            {
                Debug.LogError($"[Save Game Manager] Tried to load data for component {component}, but it is not registered!");
                return false;
            }
            
            component.OnBeforeLoad();
            
            var id = persistentObj.GetObjectId();
            if (id == null) 
                return false;
            
            var key = DataUtils.CreateKey(id, SlotId, component.GetType().ToString());
            
            var cache = GetSaveDataCache(component.GetType());
            var props = cache.Props;
            var fields = cache.Fields;

            if (!props.Any() && !fields.Any())
            {
                Debug.LogError($"[Save Game Manager] No data found to link in component {component}");
                return false;
            }

            foreach (var field in fields)
            {
                if (typeof(DataModelBase).IsAssignableFrom(field.FieldType))
                {
                    Debug.LogError($"[Save Game Manager] \"{field.FieldType}\" Data models are not supported by Save Game Manager! Use {nameof(ModelService.DataStorage.RegisterModelInStorage)} instead.");
                }
            }

            foreach (var prop in props)
            {
                if (typeof(DataModelBase).IsAssignableFrom(prop.PropertyType))
                {
                    Debug.LogError($"[Save Game Manager] \"{prop.PropertyType}\" Data models are not supported by Save Game Manager! Use {nameof(ModelService.DataStorage.RegisterModelInStorage)} instead.");
                }
            }
            
            if (!_playerPrefs.HasKey(key))
            {
                if (DebugMessages) Debug.Log($"[Save Game Manager] Not found data for component {component.GetType()} with key {key}");
                return false;
            }
            
            var str = _playerPrefs.GetString(key);
            if (DebugMessages) Debug.Log($"[Save Game Manager] Loaded component {component.GetType()} with key {key} found data: {str}");
            var json = (JObject)JsonConvert.DeserializeObject(str);

            if (json != null)
            {
                foreach (var data in json)
                {
                    if (data.Value == null) continue;
                    
                    if (cache.FieldsByName.TryGetValue(data.Key, out var field))
                    {
                        if (!persistentObj.ForceReset)
                        {
                            field.SetValue(component, data.Value.ToObject(field.FieldType));
                        }
                        continue;
                    }
                    
                    if (cache.PropsByName.TryGetValue(data.Key, out var prop))
                    {
                        if (!persistentObj.ForceReset)
                        {
                            prop.SetValue(component, data.Value.ToObject(prop.PropertyType));
                        }
                        continue;
                    }
                }
            }
            component.OnAfterLoad();
            return true;
        }

        public virtual void SaveDataForObject(PersistentObject persistentObj)
        {
            if (!RegisteredObjects.Contains(persistentObj))
            {
                Debug.LogError($"[Save Game Manager] Tried to save data for object {persistentObj}, but there are no registered components!");
                return;
            }

            foreach (var component in _persistentComponents.Where(kvp => kvp.Value == persistentObj))
            {
                SaveData(component.Key);
            }
        } 

        public virtual void SaveData<T>(T component) where T : IGameSaveData
        {
            if (!_persistentComponents.TryGetValue(component, out var persistentObj))
            {
                Debug.LogError($"[Save Game Manager] Tried to save data for component {component}, but it is not registered!");
                return;
            }
            
            component.OnBeforeSave();
            
            var cache = GetSaveDataCache(component.GetType());
            var props = cache.Props;
            var fields = cache.Fields;

            if (!props.Any() && !fields.Any())
            {
                Debug.LogError($"[Save Game Manager] No data found to save in component {component}");
                return;
            }
            
            var id = _persistentComponents[component].GetObjectId();
            if (id == null)
                return;
            
            var key = DataUtils.CreateKey(id, SlotId, component.GetType().ToString());
            
            var saveObject = new JObject();
            
            var serializer = new JsonSerializer
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Culture = CultureInfo.InvariantCulture,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            foreach (var field in fields)
            {
                if (typeof(DataModelBase).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                var obj = field.GetValue(component);
                
                var token = JToken.FromObject(obj, serializer);
                
                saveObject.Add(field.Name, token);
            }
            
            foreach (var prop in props)
            {
                if (typeof(DataModelBase).IsAssignableFrom(prop.PropertyType))
                {
                    continue;
                }
                
                var obj = prop.GetValue(component);
                
                var token = JToken.FromObject(obj, serializer);
                
                saveObject.Add(prop.Name, token);
            }
            
            _playerPrefs.SetString(key, saveObject.ToString());
            _playerPrefs.Save();
            component.OnAfterSave();
            
            if (DebugMessages) Debug.Log($"[Save Game Manager] Saved key {key} with {saveObject}");
        }

        public void SaveAllData()
        {
            foreach (var component in _persistentComponents.Keys)
            {
                SaveData(component);
            }
        }

        public virtual bool DeleteData<T>(T component) where T : IGameSaveData
        {
            if (!_persistentComponents.TryGetValue(component, out var persistentObj))
            {
                Debug.LogError(
                    $"[Save Game Manager] Tried to load data for component {component}, but it is not registered!");
                return false;
            }

            var id = persistentObj.GetObjectId();
            if (id == null) 
                return false;

            var key = DataUtils.CreateKey(id, SlotId, component.GetType().ToString());
            
            if (!_playerPrefs.HasKey(key))
            {
                if (DebugMessages) Debug.Log($"[Save Game Manager] Not found data to delete for component {component.GetType()} with key {key}");
                return false;
            }
            
            _playerPrefs.DeleteKey(key);
            if (DebugMessages) Debug.Log($"[Save Game Manager] Deleted data for component {component.GetType()} with key {key}");
            return true;
        }

        public void SetCurrentSlot(string slotId)
        {
            SlotId = slotId;
        }
        
        private static SaveDataCache GetSaveDataCache(Type type)
        {
            return _saveDataCache.GetOrAdd(type, static t =>
            {
                const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                var props = t.GetProperties(bindingFlags).Where(p =>
                    p.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any() && p.CanWrite).ToList();
                var fields = t.GetFields(bindingFlags).Where(f =>
                    f.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any()).ToList();
                return new SaveDataCache(props, fields);
            });
        }
        
        private class SaveDataCache
        {
            public readonly List<PropertyInfo> Props;
            public readonly List<FieldInfo> Fields;
            public readonly Dictionary<string, FieldInfo> FieldsByName;
            public readonly Dictionary<string, PropertyInfo> PropsByName;
            
            public SaveDataCache(List<PropertyInfo> props, List<FieldInfo> fields)
            {
                Props = props;
                Fields = fields;
                FieldsByName = fields.ToDictionary(f => f.Name);
                PropsByName = props.ToDictionary(p => p.Name);
            }
        }
    }
}