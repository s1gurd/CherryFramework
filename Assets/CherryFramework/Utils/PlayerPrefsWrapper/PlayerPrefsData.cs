using UnityEngine;

namespace CherryFramework.Utils.PlayerPrefsWrapper
{
    public class PlayerPrefsData : IPlayerPrefs
    {
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);

        public string GetString(string key) => PlayerPrefs.GetString(key);

        public bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

        public void DeleteAll() => PlayerPrefs.DeleteAll();

        public void Save() => PlayerPrefs.Save();
    }
}