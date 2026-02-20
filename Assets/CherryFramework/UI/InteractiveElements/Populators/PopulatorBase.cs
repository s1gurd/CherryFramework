using System;
using System.Collections.Generic;
using System.Linq;
using CherryFramework.Utils;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace CherryFramework.UI.InteractiveElements.Populators
{
    public abstract class PopulatorBase<T> where T : class
    {
        protected T[] Data;
        protected PopulatorElementBase<T> ElementSample;
        protected Transform ElementsRoot;
        protected virtual bool SiblingsByElementIndex => true;
        public IReadOnlyCollection<PopulatorElementBase<T>> Active => active;
        protected readonly List<PopulatorElementBase<T>> active = new();

        
        private readonly ObjectPool<PopulatorElementBase<T>> _populatorPool;
        
        protected PopulatorBase(PopulatorElementBase<T> elementSample, Transform root)
        {
            ElementSample = elementSample;
            ElementsRoot = root;
            
            _populatorPool = new ObjectPool<PopulatorElementBase<T>>(
                createFunc: () =>
                {
                    var cell = Object.Instantiate(ElementSample, ElementsRoot);
                    cell.gameObject.SetActive(false);
                    return cell;
                },
                actionOnGet: cell =>
                {
                    cell.gameObject.SetActive(true);
                    active.Add(cell);
                },
                actionOnRelease: cell =>
                {
                    cell.SetData(null);
                    cell.gameObject.SetActive(false);
                    active.Remove(cell);
                },
                actionOnDestroy: cell =>
                {
                    active.Remove(cell);
                    Object.Destroy(cell.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 100
            );
        }
        
        public virtual void UpdateElements(IEnumerable<T> data, float delayEveryElement = 0f)
        {
            var seq = DOTween.Sequence();
            Data = data as T[] ?? data?.ToArray() ?? Array.Empty<T>();
            
            foreach (var element in active.Skip(Data.Length).ToArray())
                _populatorPool.Release(element);
            
            for (var i = 0; i < Data.Length; i++)
            {
                var itemData = Data[i];
                var currentDelay = i * delayEveryElement;

                var reuse = active.InRange(i);
                var cell = reuse ? active[i] : _populatorPool.Get();
                cell.SetData(itemData);

                if (SiblingsByElementIndex)
                    cell.transform.SetSiblingIndex(i);

                seq.Insert(currentDelay, reuse ? cell.Refresh() : cell.Show());
            }
        }

        public void Clear()
        {
            _populatorPool.Clear();
        }
    }
}