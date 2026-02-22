namespace CherryFramework.Utils.PlayerPrefsWrapper
{
    public interface IPlayerPrefs
    {
        void SetString(string key, string value);

        string GetString(string key);
    
        bool HasKey(string key);
        
        void DeleteKey(string key);

        void DeleteAll();

        void Save();
    }
}