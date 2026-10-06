using System;
using System.Linq;

namespace CherryFramework.DataModels.Editor
{
    public static class TypeUtils
    {
        public static string GetFormattedName(Type t)
        {
            if (t == null)
                throw new ArgumentException("[Models Generator] Tried to get formatted name of NULL!");

            if (t.IsGenericParameter)
                return t.Name;

            if (t.IsArray)
            {
                var suffix = t.FullName != null ? t.FullName.Substring(t.FullName.LastIndexOf('[')) : "[]";
                return $"{GetFormattedName(t.GetElementType())}{suffix}";
            }

            if (t.IsGenericType && !t.IsGenericTypeDefinition)
                return $"{GetFormattedName(t.GetGenericTypeDefinition())}<{string.Join(',', t.GetGenericArguments().Select(x => GetFormattedName(x)))}>";
            if (t.IsGenericTypeDefinition)
            {
                var definitionName = t.FullName ?? t.Name;
                return definitionName.Remove(definitionName.IndexOf('`'));
            }

            if (t.FullName == null)
                return t.Name;

            return t.FullName;
        }
        
        public static string FieldToPropertyNaming(string fieldName) => fieldName[..1].ToUpper() + fieldName[1..];
    }
}