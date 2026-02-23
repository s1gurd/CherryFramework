using System.Collections.Generic;
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
        private readonly Dictionary<IGameSaveData, PersistentObject> _persistentObjects =  new ();

        public string SlotId { get; private set; } = "";

        public SaveGameManager(IPlayerPrefs playerPrefs, bool debugMessages = false)
        {
            _playerPrefs = playerPrefs;
            DebugMessages = debugMessages;
        }

        public virtual void Register<T>(T component) where T : MonoBehaviour, IGameSaveData
        {
            var persistentObj = component.gameObject.GetComponent<PersistentObject>();
            if (!persistentObj)
            {
                if (DebugMessages) Debug.Log($"[Save Game Manager] Tried to get save game data for component {component}, whose gameobject does not have PersistentObject component!", component);
                return;
            }

            if (_persistentObjects.ContainsKey(component))
            {
                Debug.LogError($"[Save Game Manager] Tried to register component {component}, which is already registered!");
                return;
            }
            
            _persistentObjects.Add(component, persistentObj);
            persistentObj.RegisterComponent(component);
        }

        public virtual void LoadData<T>(T component) where T : MonoBehaviour, IGameSaveData
        {
            if (!_persistentObjects.TryGetValue(component, out var persistentObj))
            {
                Debug.LogError($"[Save Game Manager] Tried to load data for component {component}, but it is not registered!");
                return;
            }
            
            var id = persistentObj.GetObjectId();
            if (id == null) return;
            
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
                return;
            }

            foreach (var field in fields)
            {
                if (typeof(DataModelBase).IsAssignableFrom(field.FieldType))
                {
                    Debug.LogError($"[Save Game Manager] \"{field.FieldType}\" Data models are not supported by Save Game Manager! Use {nameof(ModelService.DataStorage.LinkModelToStorage)} instead.");
                }
            }

            foreach (var prop in props)
            {
                if (typeof(DataModelBase).IsAssignableFrom(prop.PropertyType))
                {
                    Debug.LogError($"[Save Game Manager] \"{prop.PropertyType}\" Data models are not supported by Save Game Manager! Use {nameof(ModelService.DataStorage.LinkModelToStorage)} instead.");
                }
            }
            
            if (!_playerPrefs.HasKey(key))
            {
                if (DebugMessages) Debug.Log($"[Save Game Manager] Not found data for component {component.GetType()} with key {key}");
                return;
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
        }

        public virtual void SaveData(IGameSaveData component)
        {
            if (!_persistentObjects.TryGetValue(component, out var persistentObj))
            {
                Debug.LogError($"[Save Game Manager] Tried to save data for component {component}, but it is not registered!");
                return;
            }
            
            var props = component.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p =>
                    p.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any()).ToList();
            var fields = component.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(f =>
                    f.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any()).ToList();

            if (!props.Any() && !fields.Any())
            {
                Debug.LogError($"[Save Game Manager] No data found to save in component {component}");
                return;
            }

            var id = _persistentObjects[component].GetObjectId();
            var key = DataUtils.CreateKey(id, SlotId, component.GetType().ToString());
            
            var saveObject = new JObject();
            
            foreach (var field in fields)
            {
                if (typeof(DataModelBase).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                var obj = field.GetValue(component);

                var token = JToken.FromObject(obj);
                saveObject.Add(field.Name, token);
            }
            
            foreach (var prop in props)
            {
                if (typeof(DataModelBase).IsAssignableFrom(prop.PropertyType))
                {
                    continue;
                }
                
                var obj = prop.GetValue(component);
                
                var token = JToken.FromObject(obj);
                
                saveObject.Add(prop.Name, token);
            }
            
            _playerPrefs.SetString(key, saveObject.ToString());
            _playerPrefs.Save();
            
            if (DebugMessages) Debug.Log($"[Save Game Manager] Save key {key} with {saveObject}");
        }

        public void SaveAllData()
        {
            foreach (var persistentObj in _persistentObjects.Values)
            {
                persistentObj.SaveData();
            }
        }

        public virtual void DeleteData<T>(T component) where T : MonoBehaviour, IGameSaveData
        {
            if (!_persistentObjects.TryGetValue(component, out var persistentObj))
            {
                Debug.LogError(
                    $"[Save Game Manager] Tried to load data for component {component}, but it is not registered!");
                return;
            }

            var id = persistentObj.GetObjectId();
            if (id == null) return;

            var key = DataUtils.CreateKey(id, SlotId, component.GetType().ToString());
            
            if (!_playerPrefs.HasKey(key))
            {
                if (DebugMessages) Debug.Log($"[Save Game Manager] Not found data to delete for component {component.GetType()} with key {key}");
                return;
            }
            
            _playerPrefs.DeleteKey(key);
            if (DebugMessages) Debug.Log($"[Save Game Manager] Deleted data for component {component.GetType()} with key {key}");
        }

        public void SetCurrentSlot(string slotId)
        {
            SlotId = slotId;
        }
    }
}