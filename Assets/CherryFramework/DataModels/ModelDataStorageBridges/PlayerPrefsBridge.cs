using CherryFramework.Utils;
using CherryFramework.Utils.PlayerPrefsWrapper;
using Newtonsoft.Json;
using UnityEngine;

namespace CherryFramework.DataModels.ModelDataStorageBridges
{
	public class PlayerPrefsBridge<T> : ModelDataStorageBridgeBase where T : IPlayerPrefs, new()
    {
	    private readonly IPlayerPrefs _playerPrefs = new T();
	    
        public override bool ModelExistsInStorage(DataModelBase model)
        {
	        var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
	        var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
	        return _playerPrefs.HasKey(key);
        }

        public override bool SingletonModelExistsInStorage<T1>(string slotId ="", string id = "")
        {
	        if (string.IsNullOrEmpty(id))
		        id = SingletonPrefix;
	        
	        var key = DataUtils.CreateKey(id, slotId, typeof(T1).ToString());
	        return _playerPrefs.HasKey(key);
        }

		public override bool LinkModelToStorage(DataModelBase model, bool makeReady = true)
		{
			if (!base.LinkModelToStorage(model, makeReady))
				return false;
			
			var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
			var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
			
			if (DebugMode)
				Debug.Log($"[Model Service - PlayerPrefs] Linking model {model.GetType()} to Player Prefs with key {key}...");

			var result = false;
			if (_playerPrefs.HasKey(key))
			{
				var json = _playerPrefs.GetString(key);
				if (DebugMode)
					Debug.Log($"[Model Service - PlayerPrefs] Got model by key: {key} from PlayerPrefs: {json}");
				JsonConvert.PopulateObject(json, model);
				result = true;
			}
			else
			{
				if (DebugMode) 
					Debug.Log($"[Model Service - PlayerPrefs] NOT FOUND model by key: {key} in PlayerPrefs");
			}
			
			if (makeReady)
				model.Ready = true;
			
			return result;
		}

		public override bool SaveModelToStorage(DataModelBase model)
		{
			if (!base.SaveModelToStorage(model))
				return false;
			
			var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
			var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
			var json = JsonConvert.SerializeObject(model);
			_playerPrefs.SetString(key, json);
			if (DebugMode) 
				Debug.Log($"[Model Service - PlayerPrefs] Saved model {key} with content: {json}");
			return true;
		}
		
		public override bool DeleteModelFromStorage(DataModelBase model)
		{
			var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
			var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
			if (_playerPrefs.HasKey(key))
			{
				if (DebugMode)
					Debug.Log($"[Model Service - PlayerPrefs] Removed model {model.GetType()} from Player Prefs...");
				_playerPrefs.DeleteKey(key);
			}
			else
			{
				if (DebugMode)
					Debug.Log($"[Model Service - PlayerPrefs] Not found model {model.GetType()} in Player Prefs...");
				return false;
			}
			return true;
		}

    }
}