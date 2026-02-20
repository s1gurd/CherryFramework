using System;
using System.Collections.Generic;

namespace CherryFramework.DataModels.DataProviders
{
    public interface IDataStorageBridge
    {
        void Setup(Dictionary<Type, DataModelBase> singletonModels, HashSet<(DataModelBase model, string id)> playerPrefsModels, bool debugMode);
        void DeleteModelFromStorage(DataModelBase model, string id);
        bool ModelExistsInStorage(DataModelBase model, string id);
        bool ModelExistsInStorage<T>(string id);
        void LinkModelToStorage(DataModelBase model, string id, bool ready = true);
        void SaveModelToStorage(DataModelBase model);
        void SaveAllModelsById(string id);
        void SaveAllModels();
    }
}