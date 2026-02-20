namespace CherryFramework.Utils
{
    public static class PlayerPrefsUtils
    {
        public static string CreateKey(params string[] value) =>  string.Join("-", value);
    }
}