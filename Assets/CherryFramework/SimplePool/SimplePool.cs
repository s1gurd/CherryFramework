using System.Collections.Generic;
using System.Linq;
using CherryFramework.Utils;
using UnityEngine;

namespace CherryFramework.SimplePool
{
    public class SimplePool<T> where T : Component
    {
        private readonly Dictionary<T, List<T>> _pool = new ();
        
        public T Get(T sample, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (_pool.TryGetValue(sample, out var objList))
            {
                var cleanNullRefs = false;
                foreach (var o in objList)
                {
                    if (o.SafeIsUnityNull())
                    {
                        cleanNullRefs = true;
                        continue;
                    }
                    
                    if (o.gameObject.activeSelf) continue;
                    o.transform.position = position;
                    o.transform.rotation = rotation;
                    o.transform.SetParent(parent);
                    return o;
                }

                if (cleanNullRefs)
                    objList.RemoveAll(x => x.SafeIsUnityNull());
            }
            else
            {
                _pool.Add(sample, new List<T>());
            }

            var newObj = Object.Instantiate(sample, position, rotation, parent);
            _pool[sample].Add(newObj);
            return newObj;
        }
        
        public T Get(T sample) => Get(sample, Vector3.zero, Quaternion.identity);
        
        public List<T> ActiveObjects(T sample) => _pool.ContainsKey(sample) ? _pool[sample].Where(o => !o.SafeIsUnityNull() && o.gameObject.activeSelf).ToList() : new List<T>();

        public void Clear()
        {
            _pool.Values.SelectMany(x => x).Each(x =>
            {
                if (!x.SafeIsUnityNull())
                    Object.Destroy(x.gameObject);
            });
            _pool.Clear();
        }
    }
}