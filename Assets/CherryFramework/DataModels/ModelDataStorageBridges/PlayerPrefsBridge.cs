using System;
using CherryFramework.Utils;
using CherryFramework.Utils.PlayerPrefsWrapper;
using Newtonsoft.Json;
using UnityEngine;

namespace CherryFramework.DataModels.ModelDataStorageBridges
{
	public class PlayerPrefsBridge : ModelDataStorageBridgeBase
    {
	    private readonly IPlayerPrefs _playerPrefs;
	    
	    public PlayerPrefsBridge(IPlayerPrefs playerPrefs)
	    {
		    _playerPrefs = playerPrefs;
	    }
	    
        public override bool ModelExistsInStorage(DataModelBase model)
        {
	        var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
	        var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
	        return _playerPrefs.HasKey(key);
        }

        public override bool ModelExistsInStorage<T1>(string slotId ="", string id = "")
        {
	        if (string.IsNullOrEmpty(id))
		        id = SingletonPrefix;
	        
	        var key = DataUtils.CreateKey(id, slotId, typeof(T1).ToString());
	        return _playerPrefs.HasKey(key);
        }

        public override bool LoadModelData(DataModelBase model, ReadyMode makeReady = ReadyMode.MakeReadyAnyway)
		{
			if (!base.LoadModelData(model, makeReady))
				return false;
			
			var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
			var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());

			var result = false;
			
			if (_playerPrefs.HasKey(key))
			{
				var json = _playerPrefs.GetString(key);
				if (DebugMessages)
					Debug.Log($"[Model Service - PlayerPrefs] Loaded model by key: {key} from PlayerPrefs: {json}");
				JsonConvert.PopulateObject(json, model);
				result = true;
			}
			else
			{
				if (DebugMessages) 
					Debug.Log($"[Model Service - PlayerPrefs] NOT FOUND model by key: {key} in PlayerPrefs");
			}

			switch (makeReady)
			{
				case ReadyMode.MakeReadyAnyway:
					model.Ready = true;
					break;
				case ReadyMode.MakeReadyWhenDataFound:
					if (result)
						model.Ready = true;
					break;
				case ReadyMode.DoNotMakeReady:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(makeReady), makeReady, null);
			}
			
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
			if (DebugMessages) 
				Debug.Log($"[Model Service - PlayerPrefs] Saved model {key} with content: {json}");
			return true;
		}
		
		public override bool DeleteModelFromStorage(DataModelBase model)
		{
			var id = string.IsNullOrEmpty(model.Id) ? SingletonPrefix : model.Id;
			var key = DataUtils.CreateKey(id, model.SlotId, model.GetType().ToString());
			if (_playerPrefs.HasKey(key))
			{
				if (DebugMessages)
					Debug.Log($"[Model Service - PlayerPrefs] Removed model {model.GetType()} from Player Prefs...");
				_playerPrefs.DeleteKey(key);
			}
			else
			{
				if (DebugMessages)
					Debug.Log($"[Model Service - PlayerPrefs] Not found model {model.GetType()} in Player Prefs...");
				return false;
			}
			return true;
		}

    }
}