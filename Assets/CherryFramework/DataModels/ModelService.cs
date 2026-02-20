using System;
using System.Collections.Generic;
using System.Linq;
using CherryFramework.DataModels.DataProviders;
using CherryFramework.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace CherryFramework.DataModels
{
	public class ModelService : IDisposable
	{
		private readonly Dictionary<Type, DataModelBase> _singletonModels = new();
		private readonly HashSet<(DataModelBase model, string id)> _playerPrefsModels = new();
		
		private IDataStorageBridge _dataStorageBridge;
		
		public ModelService(IDataStorageBridge bridge, bool debug)
		{
			_dataStorageBridge = bridge;
			_dataStorageBridge.Setup(_singletonModels, _playerPrefsModels, debug);
		}
		
        public T GetOrCreateSingletonModel<T>() where T : DataModelBase, new()
        {
            if (_singletonModels.TryGetValue(typeof(T) , out var model))
            {
                return model as T;
            }
            
            var newModel = new T();
            _singletonModels.Add(typeof(T), newModel);
            return newModel;
        }

        public bool AddSingletonModel<T>(T source) where T : DataModelBase
        {
	        if (_singletonModels.ContainsKey(typeof(T)))
	        {
		        Debug.LogError($"[Model Service] Tried to set model {typeof(T).Name} as singleton, but it is already present!");
		        return false;
	        }
	        
	        _singletonModels.Add(typeof(T), source);
	        return true;
        }

        public void DeleteModelFromStorage(DataModelBase model, string id = "")
        {
	        _dataStorageBridge.DeleteModelFromStorage(model, id);
        }
        
        public bool ModelExistsInStorage(DataModelBase model, string id = "")
        {
	        return _dataStorageBridge.ModelExistsInStorage(model, id);
        }

        public bool ModelExistsInStorage<T>(string id = "")
        {
	        return _dataStorageBridge.ModelExistsInStorage<T>(id);
        }

		public void LinkModelToStorage(DataModelBase model, string id = "", bool ready = true)
		{
			_dataStorageBridge.LinkModelToStorage(model, id, ready);
		}

		public void SaveModelToStorage(DataModelBase model)
		{
			_dataStorageBridge.SaveModelToStorage(model);
		}

		public void SaveAllModelsById(string id)
		{
			_dataStorageBridge.SaveAllModelsById(id);
		}

		public void SaveAllModels()
		{
			_dataStorageBridge.SaveAllModels();
		}

		public void Dispose()
		{
			SaveAllModels();
		}
	}
}