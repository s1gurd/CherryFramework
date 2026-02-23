using System;
using System.Collections.Generic;
using CherryFramework.Utils.PlayerPrefsWrapper;

namespace CherryFramework.DataModels.ModelDataStorageBridges
{
    public interface IModelDataStorageBridge
    {
        void Setup(Dictionary<Type, DataModelBase> singletonModels, bool debugMode);
        bool ModelExistsInStorage(DataModelBase model);
        bool SingletonModelExistsInStorage<T>(string slotId = "", string id = "");
        void LinkModelToStorage(DataModelBase model, bool ready = true);
        void SaveModelToStorage(DataModelBase model);
        DataModelBase[] GetAllLinkedModels(); 
        void SaveAllModelsById(string id);
        void SaveAllModelsBySlot(string slotId);
        void SaveAllModels();
        void DeleteModelFromStorage(DataModelBase model);
        void UnlinkModelFromStorage(DataModelBase model);
    }
}