using System;
using System.Collections.Generic;
using System.Linq;
using CherryFramework.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace CherryFramework.DataModels.DataProviders
{
    public class PlayerPrefsBridge : IDataStorageBridge
    {
	    private const string SingletonPrefix = "SINGLETON";
	    
	    private Dictionary<Type, DataModelBase> _singletonModels;
	    private HashSet<(DataModelBase model, string id)> _playerPrefsModels;
	    
	    private DateTime _lastSaveTime;
	    private bool _debugMode;

	    public void Setup(Dictionary<Type, DataModelBase> singletonModels, HashSet<(DataModelBase model, string id)> playerPrefsModels, bool debugMode = false)
	    {
		    _singletonModels = singletonModels;
		    _playerPrefsModels = playerPrefsModels;
		    _debugMode = debugMode;
		    _lastSaveTime = DateTime.Now;
	    }

	    public void DeleteModelFromStorage(DataModelBase model, string id)
	    {
		    if (string.IsNullOrEmpty(id))
			    id = SingletonPrefix;
		    
	        if (_debugMode)
		        Debug.Log($"[Model Service - PlayerPrefs] Removing model {model.GetType()} from Player Prefs...");
	        var key = PlayerPrefsUtils.CreateKey(id, model.GetType().ToString());
	        if (PlayerPrefs.HasKey(key))
	        {
		        PlayerPrefs.DeleteKey(key);
	        }
        }
        
        public bool ModelExistsInStorage(DataModelBase model, string id)
        {
	        if (string.IsNullOrEmpty(id))
		        id = SingletonPrefix;
	        
	        var key = PlayerPrefsUtils.CreateKey(id, model.GetType().ToString());
	        return PlayerPrefs.HasKey(key);
        }

        public bool ModelExistsInStorage<T>(string id)
        {
	        if (string.IsNullOrEmpty(id))
		        id = SingletonPrefix;
	        
	        var key = PlayerPrefsUtils.CreateKey(id, typeof(T).ToString());
	        return PlayerPrefs.HasKey(key);
        }

		public void LinkModelToStorage(DataModelBase model, string id, bool makeReady = true)
		{
			if (string.IsNullOrEmpty(id))
				id = SingletonPrefix;
			
			if (_playerPrefsModels.Contains((model, id)))
			{
				if (_debugMode)
					Debug.Log($"[Model Service - PlayerPrefs] Tried to link model {model.GetType()} to Player Prefs but it is already linked...");
				return;
			}
			
			if (id.Equals(SingletonPrefix) && !_singletonModels.ContainsKey(model.GetType()))
			{
				Debug.LogError($"[Model Service - PlayerPrefs] Got request to link model of type {model.GetType()} without ID, while model of this type is not registered as singleton!");
				return;
			}
			
			var key = PlayerPrefsUtils.CreateKey(id, model.GetType().ToString());
			
			if (_debugMode)
				Debug.Log($"[Model Service - PlayerPrefs] Linking model {model.GetType()} to Player Prefs with key {key}...");
			
			if (PlayerPrefs.HasKey(key) && !PlayerPrefs.HasKey("FullDataReset"))
			{
				var json = PlayerPrefs.GetString(key);
				if (_debugMode)
					Debug.Log($"[Model Service - PlayerPrefs] Got model by key: {key} from PlayerPrefs: {json}");
				JsonConvert.PopulateObject(json, model);
			}
			else
			{
				if (_debugMode) 
					Debug.Log($"[Model Service - PlayerPrefs] {(PlayerPrefs.HasKey("FullDataReset") ? "RESET" : "NOT FOUND")} model by key: {key} in PlayerPrefs");
			}
			
			_playerPrefsModels.Add((model, id));
			if (makeReady)
				model.Ready = true;
		}

		public void SaveModelToStorage(DataModelBase model)
		{
			if (_playerPrefsModels.All(t => t.model != model))
			{
				Debug.LogError($"[Model Service - PlayerPrefs] Got request to save model of type {model.GetType()} which is not linked to Player Prefs!!");
				return;
			}
			
			var id = _playerPrefsModels.FirstOrDefault(t => t.model == model).id;
			var key = PlayerPrefsUtils.CreateKey(id, model.GetType().ToString());
			var json = JsonConvert.SerializeObject(model);
			PlayerPrefs.SetString(key, json);
			if (_debugMode) 
				Debug.Log($"[Model Service - PlayerPrefs] Saved model {key} with content: {json}");
		}

		public void SaveAllModelsById(string id)
		{
			if (_playerPrefsModels.All(t => t.id != id))
			{
				Debug.LogError($"[Model Service - PlayerPrefs] Got request to save models with ID: \"{id}\" which are not found!!!");
				return;
			}
			
			foreach (var t in _playerPrefsModels)
			{
				if (t.id.Equals(id))
				{
					SaveModelToStorage(t.model);
				}
			}
		}

		public void SaveAllModels()
		{
			if (DateTime.Now.Subtract(_lastSaveTime).TotalSeconds < 5)
				return;
			
			_lastSaveTime = DateTime.Now;
			foreach (var t in _playerPrefsModels)
			{
				SaveModelToStorage(t.model);
			}
			PlayerPrefs.Save();
		}
    }
}