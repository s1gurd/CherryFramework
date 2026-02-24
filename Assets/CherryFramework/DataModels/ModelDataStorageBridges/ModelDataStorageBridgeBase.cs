using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CherryFramework.DataModels.ModelDataStorageBridges
{
    public abstract class ModelDataStorageBridgeBase
    {
        protected const string SingletonPrefix = "SINGLETON";
		
        protected readonly HashSet<DataModelBase> DataLinkedModels = new ();
        protected Dictionary<Type, DataModelBase> SingletonModels;
        protected bool DebugMode;

        public virtual void Setup(Dictionary<Type, DataModelBase> singletonModels, bool debugMode)
        {
            SingletonModels = singletonModels;
            DebugMode = debugMode;
        }

        public virtual bool ModelExistsInStorage(DataModelBase model) => true;

        public virtual bool SingletonModelExistsInStorage<T1>(string slotId = "", string id = "") => true;

        public virtual bool LinkModelToStorage(DataModelBase model, bool makeReady = true)
        {
            if (DataLinkedModels.Contains(model))
            {
                if (DebugMode)
                    Debug.Log($"[Model Service - PlayerPrefs] Tried to link model {model.GetType()} to Player Prefs but it is already linked...");
                return false;
            }
			
            if (string.IsNullOrEmpty(model.Id) && !SingletonModels.ContainsKey(model.GetType()))
            {
                Debug.LogError($"[Model Service - PlayerPrefs] Got request to link model of type {model.GetType()} without ID, while model of this type is not registered as singleton!");
                return false;
            }
			
            DataLinkedModels.Add(model);

            return true;
        }

        public virtual bool SaveModelToStorage(DataModelBase model)
        {
            if (!DataLinkedModels.Contains(model))
            {
                Debug.LogError($"[Model Service - PlayerPrefs] Got request to save model of type {model.GetType()} which is not linked to Player Prefs!!");
                return false;
            }
            return true;
        }

        public virtual bool DeleteModelFromStorage(DataModelBase model) => true;
		
        public virtual void SaveAllModelsById(string id)
        {
            if (DataLinkedModels.Any(m => m.Id == id))
            {
                Debug.LogError($"[Model Service - PlayerPrefs] Got request to save models with ID: \"{id}\" which are not found!!!");
                return;
            }

            foreach (var m in DataLinkedModels.Where(m => m.Id.Equals(id)))
            {
                SaveModelToStorage(m);
            }
        }

        public virtual void SaveAllModelsBySlot(string slotId)
        {
            if (DataLinkedModels.All(m => m.SlotId != slotId))
            {
                Debug.LogError($"[Model Service - PlayerPrefs] Got request to save models for slot: \"{slotId}\" which are not found!!!");
                return;
            }

            foreach (var m in DataLinkedModels.Where(m => m.SlotId.Equals(slotId)))
            {
                SaveModelToStorage(m);
            }
        }

        public virtual void SaveAllModels()
        {
            foreach (var m in DataLinkedModels)
            {
                SaveModelToStorage(m);
            }
            PlayerPrefs.Save();
        }
		
        public virtual DataModelBase[] GetAllLinkedModels()
        {
            return DataLinkedModels.ToArray();
        }

        public virtual void UnlinkModelFromStorage(DataModelBase model)
        {
            DataLinkedModels.Remove(model);
        }
    }
}