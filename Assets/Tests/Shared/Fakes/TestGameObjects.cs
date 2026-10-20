using CherryFramework.SaveGameManager;
using UnityEngine;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// Creates the GameObjects the tests need and takes them down again.
    /// </summary>
    public static class TestGameObjects
    {
        public static PersistentObject CreatePersistentObject(string guid)
        {
            var gameObject = new GameObject("TestPersistentObject");
            var persistentObject = gameObject.AddComponent<PersistentObject>();
            persistentObject.guid = guid;

            return persistentObject;
        }

        public static void Destroy(GameObject gameObject)
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
