using System;
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
        protected bool DebugMessages = false;
        
        private readonly IPlayerPrefs _playerPrefs;
        private readonly Dictionary<IGameSaveData, PersistentObject> _persistentComponents =  new ();

        public string SlotId { get; private set; } = "";
        public IGameSaveData[] RegisteredComponents => _persistentComponents.Keys.ToArray();
        public PersistentObject[]  RegisteredObjects => _persistentComponents.Values.Distinct().ToArray();

        public SaveGameManager(IPlayerPrefs playerPrefs, bool debugMessages = false)
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
            
            var props = component.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p =>
                    p.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any() && p.CanWrite).ToList();
            var fields = component.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(f =>
                    f.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any()).ToList();

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
                    
                    var field = fields.FirstOrDefault(f => f.Name == data.Key);
                    if (field != null)
                    {
                        if (!persistentObj.ForceReset)
                        {
                            field.SetValue(component, data.Value.ToObject(field.FieldType));
                        }
                        continue;
                    }
                    
                    var prop = props.FirstOrDefault(p => p.Name == data.Key);
                    if (prop != null)
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

        public virtual void SaveData(IGameSaveData component)
        {
            if (!_persistentComponents.TryGetValue(component, out var persistentObj))
            {
                Debug.LogError($"[Save Game Manager] Tried to save data for component {component}, but it is not registered!");
                return;
            }
            
            component.OnBeforeSave();
            
            var props = component.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p =>
                    p.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any() && p.CanWrite).ToList();
            var fields = component.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(f =>
                    f.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any()).ToList();

            if (!props.Any() && !fields.Any())
            {
                Debug.LogError($"[Save Game Manager] No data found to save in component {component}");
                return;
            }

            var id = _persistentComponents[component].GetObjectId();
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
    }
}