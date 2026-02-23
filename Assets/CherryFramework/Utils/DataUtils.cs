using System.Linq;

namespace CherryFramework.Utils
{
    public static class DataUtils
    {
        public static string CreateKey(params string[] value) =>  string.Join("-", value.Where(s => !string.IsNullOrEmpty(s)));
    }
}