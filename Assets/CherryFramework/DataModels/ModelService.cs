using System;
using System.Collections.Generic;
using CherryFramework.DataModels.ModelDataStorageBridges;
using UnityEngine;

namespace CherryFramework.DataModels
{
	public class ModelService
	{
		public readonly ModelDataStorageBridgeBase DataStorage;
		
		private readonly Dictionary<Type, DataModelBase> _singletonModels = new();
		
		public ModelService(ModelDataStorageBridgeBase bridge, bool debug)
		{
			DataStorage = bridge;
			DataStorage.Setup(_singletonModels, debug);
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