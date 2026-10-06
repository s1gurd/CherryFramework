using System;
using System.Collections.Generic;
using CherryFramework.DataModels.ModelDataStorageBridges;
using CherryFramework.Utils;
using UnityEngine;

namespace CherryFramework.DataModels
{
	public class ModelService
	{
		public readonly ModelDataStorageBridgeBase DataStorage;
		
		private readonly Dictionary<Type, DataModelBase> _singletonModels = new();
		private readonly Dictionary<string, DataModelBase> _transientModels = new();
		
		public ModelService(ModelDataStorageBridgeBase bridge, bool debugMessages)
		{
			DataStorage = bridge;
			DataStorage.Setup(_singletonModels, debugMessages);
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

        public T GetOrCreateTransientModel<T>(string id) where T : DataModelBase, new()
        {
	        if (id.IsNullOrWhiteSpace())
	        {
		        Debug.LogError($"[Model Service] Tried to get or create model {typeof(T).Name} without ID!!!");
	        }
	        if (_transientModels.TryGetValue(id , out var model))
	        {
		        return model as T;
	        }
	        
	        var newModel = new T
	        {
		        Id = id
	        };
	        _transientModels.Add(id, newModel);
	        return newModel;
        }

        public bool ReleaseTransientModel(string id)
        {
	        if (_transientModels.Remove(id))
	        {
		        return true;
	        }
	        return false;
        }

        public bool ReleaseTransientModel(DataModelBase model)
        {
	        if (_transientModels.Remove(model.Id))
	        {
		        return true;
	        }
	        return false;
        }

        public bool MakeModelSingleton<T>(T source) where T : DataModelBase
        {
	        if (_singletonModels.ContainsKey(typeof(T)))
	        {
		        Debug.LogError($"[Model Service] Tried to set model {typeof(T).Name} as singleton, but it is already present!");
		        return false;
	        }
	        
	        _singletonModels.Add(typeof(T), source);
	        return true;
        }
	}
}