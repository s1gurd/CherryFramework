using UnityEngine;

namespace CherryFramework.Utils
{
    public static class ComponentUtils
    {
        public static bool SafeIsUnityNull(this Component obj) => !obj || !obj.gameObject;
    }
}