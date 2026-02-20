using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CherryFramework.BaseClasses;
using CherryFramework.DataModels;
using CherryFramework.DependencyManager;
using CherryFramework.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CherryFramework.SaveGameManager
{
    public class SaveGameManager : InjectClass
    {
        [Inject] protected readonly ModelService ModelService;
        
        protected bool DebugMessages = false;

        protected SaveGameManager(bool debugMessages)
        {
            DebugMessages = debugMessages;
        }

        public virtual void LinkData(BehaviourBase component, bool reset = false)
        {
            if (component is not IGameSaveData gameSave)
            {
                Debug.LogError($"[SaveGame Helper] Tried to get save game data for component {component}, which is not an IGameSaveData!", component);
                return;
            }
            
            var persistentObj = component.gameObject.GetComponent<PersistentObject>();
            if (!persistentObj)
            {
                if (DebugMessages) Debug.Log($"[SaveGame Helper] Tried to get save game data for component {component}, whose gameobject does not have PersistentObject component!", component);
                return;
            }

            var id = persistentObj.GetObjectId();
            if (id == null) return;
            
            var key = PlayerPrefsUtils.CreateKey(id, component.GetType().ToString());
            
            if (DebugMessages) Debug.Log($"[SaveGame Helper] Linking component {component.GetType()} with key  {key}");
            
            var props = component.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p =>
                    p.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any() && p.CanWrite).ToList();
            var fields = component.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(f =>
                    f.GetCustomAttributes(typeof(SaveGameDataAttribute), false).Any()).ToList();

            if (!props.Any() && !fields.Any())
            {
                Debug.LogError($"[SaveGame Helper] No data found to link in component {component}");
                return;
            }
            
            persistentObj.RegisterComponent(gameSave);
            
            reset &=  component is not IIgnoreReset;
            reset |= persistentObj.ForceReset;

            foreach (var field in fields)
            {
                if (typeof(DataModelBase).IsAssignableFrom(field.FieldType))
                {
                    var model = field.GetValue(component) as DataModelBase;
                    var modelKey = PlayerPrefsUtils.CreateKey(id, field.Name);
                    if (reset)
                    {
                        ModelService.DeleteModelFromStorage(model, modelKey);
                    }
                    ModelService.LinkModelToStorage(model, modelKey);
                }
            }

            foreach (var prop in props)
            {
                if (typeof(DataModelBase).IsAssignableFrom(prop.PropertyType))
                {
                    var model = prop.GetValue(component) as DataModelBase;
                    var modelKey = PlayerPrefsUtils.CreateKey(id, prop.Name);
                    if (reset)
                    {
                        ModelService.DeleteModelFromStorage(model, modelKey);
                    }
                    ModelService.LinkModelToStorage(model, modelKey);
                }
            }
            
            if (!PlayerPrefs.HasKey(key)) return;
            
            var str = PlayerPrefs.GetString(key);
            if (DebugMessages) Debug.Log($"[SaveGame Helper] Component {component.GetType()} with key {key} found data: {str}");
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
                Debug.LogError($"[SaveGame Helper] No data found to save in component {component}");
                return;
            }

            var key = PlayerPrefsUtils.CreateKey(source.Value, component.GetType().ToString());
            
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
            
            PlayerPrefs.SetString(key, saveObject.ToString());
            PlayerPrefs.Save();
            
            if (DebugMessages) Debug.Log($"[SaveGame Helper] Save key {key} with {saveObject}");
        }
    }
}