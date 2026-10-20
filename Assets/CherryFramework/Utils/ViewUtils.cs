using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CherryFramework.UI.InteractiveElements.Presenters;
using UnityEngine;

namespace CherryFramework.Utils
{
    public static class ViewUtils
    {
        /// <summary>
        /// Finds a presenter by its type name. The search runs over every loaded
        /// assembly, so a presenter that lives in the game code is found as
        /// well, and a presenter that derives from an intermediate base class
        /// counts too.
        /// </summary>
        public static Type GetPresenterType(string typeString) => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetTypesSafe)
            .FirstOrDefault(type => type.Name.Equals(typeString, StringComparison.Ordinal)
                                     && typeof(PresenterBase).IsAssignableFrom(type)
                                     && !type.IsAbstract);

        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        public static void DebugLogPath(this List<PresenterBase> list)
        {
            var sb = new StringBuilder();
            sb.Append("Path: ");
            foreach (var item in list)
            {
                sb.Append("/");
                sb.Append(item.gameObject.name);
            }
            Debug.Log(sb.ToString());
        }
        
    }
}