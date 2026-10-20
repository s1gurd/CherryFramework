using System;
using System.Reflection;

namespace CherryFramework.Tests.Fakes
{
    /// <summary>
    /// Writes the private serialized fields of a component. The framework
    /// keeps its wiring in private fields, and a test has to fill them the
    /// way the inspector would.
    /// </summary>
    public static class SerializedFields
    {
        public static void SetField(object target, string fieldName, object value)
        {
            var field = FindField(target.GetType(), fieldName);

            if (field == null)
            {
                throw new ArgumentException($"Field {fieldName} not found on {target.GetType()}");
            }

            field.SetValue(target, value);
        }

        public static T GetField<T>(object target, string fieldName)
        {
            var field = FindField(target.GetType(), fieldName);

            if (field == null)
            {
                throw new ArgumentException($"Field {fieldName} not found on {target.GetType()}");
            }

            return (T)field.GetValue(target);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

            while (type != null)
            {
                var field = type.GetField(fieldName, flags);

                if (field != null)
                {
                    return field;
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
