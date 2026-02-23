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
	    private HashSet<DataModelBase> _playerPrefsModels;
	    
	    private readonly IPlayerPrefs _playerPrefs;
	    private DateTime _lastSaveTime;
	    private bool _debugMode;

	    public PlayerPrefsBridge(IPlayerPrefs playerPrefs)
	    {
		    _playerPrefs = playerPrefs;
	    }

	    public void Setup(Dictionary<Type, DataModelBase> singletonModels,  bool debugMode = false)
	    {
		    _singletonModels = singletonModels;
		    _debugMode = debugMode;
		    _lastSaveTime = DateTime.Now;
	    }

	    public void DeleteModelFromStorage(DataModelBase model)
	    {
		    var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
		   
	        if (_debugMode)
		        Debug.Log($"[Model Service - PlayerPrefs] Removing model {model.GetType()} from Player Prefs...");
	        var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
	        if (_playerPrefs.HasKey(key))
	        {
		        _playerPrefs.DeleteKey(key);
	        }
        }
        
        public bool ModelExistsInStorage(DataModelBase model)
        {
	        var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
	        
	        var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
	        return _playerPrefs.HasKey(key);
        }

        public bool SingletonModelExistsInStorage<T>(string slotId ="", string id = "")
        {
	        if (string.IsNullOrEmpty(id))
		        id = SingletonPrefix;
	        
	        var key = DataUtils.CreateKey(id, slotId, typeof(T).ToString());
	        return _playerPrefs.HasKey(key);
        }

		public void LinkModelToStorage(DataModelBase model, bool makeReady = true)
		{
			var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
			
			if (_playerPrefsModels.Contains(model))
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
			
			var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
			
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
			
			_playerPrefsModels.Add(model);
			if (makeReady)
				model.Ready = true;
		}

		public void SaveModelToStorage(DataModelBase model)
		{
			if (!_playerPrefsModels.Contains(model))
			{
				Debug.LogError($"[Model Service - PlayerPrefs] Got request to save model of type {model.GetType()} which is not linked to Player Prefs!!");
				return;
			}
			
			var id = model.Id;
			var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
			var json = JsonConvert.SerializeObject(model);
			_playerPrefs.SetString(key, json);
			if (_debugMode) 
				Debug.Log($"[Model Service - PlayerPrefs] Saved model {key} with content: {json}");
		}

		public DataModelBase[] GetAllLinkedModels()
		{
			return _playerPrefsModels.ToArray();
		}

		public void SaveAllModelsById(string id)
		{
			if (_playerPrefsModels.Any(m => m.Id == id))
			{
				Debug.LogError($"[Model Service - PlayerPrefs] Got request to save models with ID: \"{id}\" which are not found!!!");
				return;
			}

			foreach (var m in _playerPrefsModels.Where(m => m.Id.Equals(id)))
			{
				SaveModelToStorage(m);
			}
		}

		public void SaveAllModelsBySlot(string slotId)
		{
			if (_playerPrefsModels.All(m => m.SlotId != slotId))
			{
				Debug.LogError($"[Model Service - PlayerPrefs] Got request to save models for slot: \"{slotId}\" which are not found!!!");
				return;
			}

			foreach (var m in _playerPrefsModels.Where(m => m.SlotId.Equals(slotId)))
			{
				SaveModelToStorage(m);
			}
		}

		public void SaveAllModels()
		{
			if (DateTime.Now.Subtract(_lastSaveTime).TotalSeconds < 5)
				return;
			
			_lastSaveTime = DateTime.Now;
			foreach (var m in _playerPrefsModels)
			{
				SaveModelToStorage(m);
			}
			PlayerPrefs.Save();
		}
    }
}