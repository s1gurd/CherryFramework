using UnityEngine;

namespace CherryFramework.SaveGameManager
{
    public interface IGameSaveData
    {
        void OnBeforeLoad() { }
        void OnAfterLoad() { }
        void OnBeforeSave() { }
        void OnAfterDataSave() { }
    }
}