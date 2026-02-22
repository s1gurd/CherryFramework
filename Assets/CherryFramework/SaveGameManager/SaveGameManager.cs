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
        
        private IPlayerPrefs _playerPrefs;
        public HashSet<PersistentObject> PersistentObjects { get; } =  new ();

        protected SaveGameManager(IPlayerPrefs playerPrefs, bool debugMessages)
        {
            _playerPrefs = playerPrefs;
            DebugMessages = debugMessages;
        }

        public virtual void LoadData<T>(T component, bool reset = false) where T : MonoBehaviour, IGameSaveData
        {
            var persistentObj = component.gameObject.GetComponent<PersistentObject>();
            if (!persistentObj)
            {
                if (DebugMessages) Debug.Log($"[Save Game Manager] Tried to get save game data for component {component}, whose gameobject does not have PersistentObject component!", component);
                return;
            }

            var id = persistentObj.GetObjectId();
            if (id == null) return;
            
            var key = DataUtils.CreateKey(id, component.GetType().ToString());
            
            if (DebugMessages) Debug.Log($"[Save Game Manager] Linking component {component.GetType()} with key  {key}");
            
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
            
            PersistentObjects.Add(persistentObj);
            persistentObj.RegisterComponent(component);
            
            reset &=  component is not IIgnoreReset;
            reset |= persistentObj.ForceReset;

            foreach (var field in fields)
            {
                if (typeof(DataModelBase).IsAssignableFrom(field.FieldType))
                {
                    Debug.LogError($"[Save Game Manager] \"{field.FieldType}\" Data models are not supported by Save Game Manager! Use {nameof(ModelService.LinkModelToStorage)} instead.");
                }
            }

            foreach (var prop in props)
            {
                if (typeof(DataModelBase).IsAssignableFrom(prop.PropertyType))
                {
                    Debug.LogError($"[Save Game Manager] \"{prop.PropertyType}\" Data models are not supported by Save Game Manager! Use {nameof(ModelService.LinkModelToStorage)} instead.");
                }
            }
            
            if (!_playerPrefs.HasKey(key)) return;
            
            var str = _playerPrefs.GetString(key);
            if (DebugMessages) Debug.Log($"[Save Game Manager] Component {component.GetType()} with key {key} found data: {str}");
            var json = (JObject)JsonConvert.DeserializeObject(str);

            if (json != null)
            {
                foreach (var data in json)
                {
                    if (data.Value == null) continue;
                    
                    var field = fields.FirstOrDefault(f => f.Name == data.Key);
                    if (field != null)
                    {
                        if (!reset)
                        {
                            field.SetValue(component, data.Value.ToObject(field.FieldType));
                        }
                        continue;
                    }
                    
                    var prop = props.FirstOrDefault(p => p.Name == data.Key);
                    if (prop != null)
                    {
                        if (!reset)
                        {
                            prop.SetValue(component, data.Value.ToObject(prop.PropertyType));
                        }
                        continue;
                    }
                }
            }
        }

        public virtual void SaveData(KeyValuePair<IGameSaveData, string> source)
        {
            var component = source.Key;
            
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

            var key = DataUtils.CreateKey(source.Value, component.GetType().ToString());
            
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
            foreach (var persistentObj in PersistentObjects)
            {
                persistentObj.SaveData();
            }
        }
    }
}