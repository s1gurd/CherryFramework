using System.Collections.Generic;
using System.Linq;
using CherryFramework.BaseClasses;
using UnityEngine;

// ReSharper disable ForCanBeConvertedToForeach
namespace CherryFramework.TickDipatcher
{
    public partial class Ticker : BehaviourBase
    {
        private readonly List<Tickable> _tickables = new();
        private readonly List<LateTickable> _lateTickables = new();
        private readonly List<FixedTickable> _fixedTickables = new();
        
        private readonly List<Tickable> _removeTickDelayed = new();
        private readonly List<LateTickable> _removeLateTickDelayed = new();
        private readonly List<FixedTickable> _removeFixTickDelayed = new();
        
        private readonly Dictionary<object, MonoBehaviour> _checkActivity = new();
        
        private void Update()
        {
            if (_removeTickDelayed.Count > 0)
            {
                while (_removeTickDelayed.Count > 0)
                {
                    _tickables.Remove(_removeTickDelayed[0]);
                    _removeTickDelayed.RemoveAt(0);
                }
            }
            
            var emitTime = Time.time;
            for (var i = 0; i < _tickables.Count; i++)
            {
                var obj = _tickables[i];
                
                if (_checkActivity.ContainsKey(obj) && !_checkActivity[obj].isActiveAndEnabled)
                    return;
                
                if (emitTime < obj.LastTick + obj.TickPeriod) 
                    continue;

                obj.Obj.Tick(emitTime - obj.LastTick);
                obj.LastTick = emitTime;
            }
        }

        private void LateUpdate()
        {
            if (_removeLateTickDelayed.Count > 0)
            {
                while (_removeLateTickDelayed.Count > 0)
                {
                    _lateTickables.Remove(_removeLateTickDelayed[0]);
                    _removeLateTickDelayed.RemoveAt(0);
                }
            }
            
            var emitTime = Time.time;
            for (var i = 0; i < _lateTickables.Count; i++)
            {
                var obj = _lateTickables[i];
                
                if (_checkActivity.ContainsKey(obj) && !_checkActivity[obj].isActiveAndEnabled)
                    return;
                
                if (emitTime < obj.LastTick + obj.TickPeriod) 
                    continue;

                obj.Obj.LateTick(emitTime - obj.LastTick);
                obj.LastTick = emitTime;
            }
        }

        private void FixedUpdate()
        {
            if (_removeFixTickDelayed.Count > 0)
            {
                while (_removeFixTickDelayed.Count > 0)
                {
                    _fixedTickables.Remove(_removeFixTickDelayed[0]);
                    _removeFixTickDelayed.RemoveAt(0);
                }
            }
            
            var emitTime = Time.time;
            for (var i = 0; i < _fixedTickables.Count; i++)
            {
                var obj = _fixedTickables[i];
                
                if (_checkActivity.ContainsKey(obj) && !_checkActivity[obj].isActiveAndEnabled)
                    return;
                
                if (emitTime < obj.LastTick + obj.TickPeriod) 
                    continue;

                obj.Obj.FixedTick(emitTime - obj.LastTick);
                obj.LastTick = emitTime;
            }
        }

        public void AddTick(ITickable obj, float tickPeriod = 0f)
        {
            _tickables.Add(new Tickable(obj, tickPeriod));
            AddUnsubscription(obj);
        }

        public void AddLateTick(ILateTickable obj, float tickPeriod = 0f)
        {
            _lateTickables.Add(new LateTickable(obj, tickPeriod));
            AddUnsubscription(obj);
        }

        public void AddFixedTick(IFixedTickable obj, float tickPeriod = 0f)
        {
            _fixedTickables.Add(new FixedTickable(obj, tickPeriod));
            AddUnsubscription(obj);
        }

        public void RemoveTick(ITickable obj) 
            => _removeTickDelayed.AddRange(_tickables.Where(x => ReferenceEquals(x.Obj, obj)));

        public void RemoveLateTick(ILateTickable obj)
            => _removeLateTickDelayed.AddRange(_lateTickables.Where(x=> ReferenceEquals(x.Obj, obj)));
        
        public void RemoveFixedTick(IFixedTickable obj)
            => _removeFixTickDelayed.AddRange(_fixedTickables.Where(x=> ReferenceEquals(x.Obj, obj)));

        public void Register(ITickableBase obj, float tickPeriod = 0f)
        {
            switch (obj)
            {
                case ITickable t:
                    AddTick(t, tickPeriod);
                    break;
                case ILateTickable l:
                    AddLateTick(l, tickPeriod);
                    break;
                case IFixedTickable f:
                    AddFixedTick(f, tickPeriod);
                    break;
            }
        }

        public void Register<T>(T obj, bool checkActivity, float tickPeriod = 0f) where T : MonoBehaviour, ITickableBase
        {
            if (checkActivity) 
                _checkActivity[obj] = obj;
            
            Register(obj, tickPeriod);
        }
        
        public void UnRegister(ITickableBase obj)
        {
            switch (obj)
            {
                case ITickable t:
                    RemoveTick(t);
                    break;
                case ILateTickable l:
                    RemoveLateTick(l);
                    break;
                case IFixedTickable f:
                    RemoveFixedTick(f);
                    break;
            }

            _checkActivity.Remove(obj);
        }
        
        private void AddUnsubscription(ITickableBase obj)
        {
            if (obj is IUnsubscriber unsubscriber)
            {
                unsubscriber.AddUnsubscription(() => this.UnRegister(obj));
            }
        }
    }
}