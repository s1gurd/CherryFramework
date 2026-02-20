using System;
using System.Linq;

namespace CherryFramework.DataModels.Editor
{
    public static class TypeUtils
    {
        public static string GetFormattedName(Type t)
        {
            if (t?.FullName == null)
            {
                throw new ArgumentException("[Models Generator] Tried to get formatted name of NULL!");
            }
            
            if (t.IsArray)
                return $"{GetFormattedName(t.GetElementType())}{t.FullName.Substring(t.FullName.LastIndexOf('['))}";
            if (t.IsGenericType && !t.IsGenericTypeDefinition)
                return $"{GetFormattedName(t.GetGenericTypeDefinition())}<{string.Join(',', t.GetGenericArguments().Select(x => GetFormattedName(x)))}>";
            if (t.IsGenericTypeDefinition)
                return t.FullName.Remove(t.FullName.IndexOf('`'));

            return t.FullName;
        } 
    }
}