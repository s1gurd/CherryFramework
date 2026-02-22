using System;
using System.Collections.Generic;
using System.Linq;
using CherryFramework.Utils;
using CherryFramework.Utils.PlayerPrefsWrapper;
using Newtonsoft.Json;
using UnityEngine;

namespace CherryFramework.DataModels.ModelDataStorageBridges
{
    public class PlayerPrefsBridge : IModelDataStorageBridge
    {
	    private const string SingletonPrefix = "SINGLETON";
	    
	    private Dictionary<Type, DataModelBase> _singletonModels;
	    private HashSet<(DataModelBase model, string id)> _playerPrefsModels;
	    
	    private IPlayerPrefs _playerPrefs;
	    private DateTime _lastSaveTime;
	    private bool _debugMode;

	    public PlayerPrefsBridge(IPlayerPrefs playerPrefs)
	    {
		    _playerPrefs = playerPrefs;
	    }

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
	        var key = DataUtils.CreateKey(id, model.GetType().ToString());
	        if (_playerPrefs.HasKey(key))
	        {
		        _playerPrefs.DeleteKey(key);
	        }
        }
        
        public bool ModelExistsInStorage(DataModelBase model, string id)
        {
	        if (string.IsNullOrEmpty(id))
		        id = SingletonPrefix;
	        
	        var key = DataUtils.CreateKey(id, model.GetType().ToString());
	        return _playerPrefs.HasKey(key);
        }

        public bool ModelExistsInStorage<T>(string id)
        {
	        if (string.IsNullOrEmpty(id))
		        id = SingletonPrefix;
	        
	        var key = DataUtils.CreateKey(id, typeof(T).ToString());
	        return _playerPrefs.HasKey(key);
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
			
			var key = DataUtils.CreateKey(id, model.GetType().ToString());
			
			if (_debugMode)
				Debug.Log($"[Model Service - PlayerPrefs] Linking model {model.GetType()} to Player Prefs with key {key}...");
			
			if (_playerPrefs.HasKey(key))
			{
				var json = _playerPrefs.GetString(key);
				if (_debugMode)
					Debug.Log($"[Model Service - PlayerPrefs] Got model by key: {key} from PlayerPrefs: {json}");
				JsonConvert.PopulateObject(json, model);
			}
			else
			{
				if (_debugMode) 
					Debug.Log($"[Model Service - PlayerPrefs] NOT FOUND model by key: {key} in PlayerPrefs");
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
			var key = DataUtils.CreateKey(id, model.GetType().ToString());
			var json = JsonConvert.SerializeObject(model);
			_playerPrefs.SetString(key, json);
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