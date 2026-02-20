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
        public static Type GetPresenterType(string typeString) => Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.Name.Equals(typeString) &&t.BaseType == typeof(PresenterBase));

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