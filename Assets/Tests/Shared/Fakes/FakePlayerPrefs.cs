using System.Collections.Generic;
using CherryFramework.Utils.PlayerPrefsWrapper;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// Keeps the saved values in memory. The tests never touch the real
    /// PlayerPrefs, so they cannot leak into a real save file.
    /// </summary>
    public class FakePlayerPrefs : IPlayerPrefs
    {
        private readonly Dictionary<string, string> _values = new();

        public IReadOnlyDictionary<string, string> Values => _values;

        public void SetString(string key, string value) => _values[key] = value;
        public string GetString(string key) => _values.TryGetValue(key, out var value) ? value : null;
        public bool HasKey(string key) => _values.ContainsKey(key);
        public void DeleteKey(string key) => _values.Remove(key);
        public void DeleteAll() => _values.Clear();
        public void Save()
        {
        }
    }
}
