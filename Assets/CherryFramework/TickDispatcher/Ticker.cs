using System.Collections.Generic;
using System.Linq;
using CherryFramework.BaseClasses;
using UnityEngine;

// ReSharper disable ForCanBeConvertedToForeach
namespace CherryFramework.TickDispatcher
{
    public partial class Ticker : GeneralClassBase
    {
        private readonly List<Tickable> _tickables = new();
        private readonly List<LateTickable> _lateTickables = new();
        private readonly List<FixedTickable> _fixedTickables = new();
        
        private readonly List<Tickable> _removeTickDelayed = new();
        private readonly List<LateTickable> _removeLateTickDelayed = new();
        private readonly List<FixedTickable> _removeFixTickDelayed = new();
        
        private readonly Dictionary<object, MonoBehaviour> _checkActivity = new();

        public Ticker() : base()
        {
            var go = new GameObject("Ticker");
            var ticker = go.AddComponent<TickerBehaviour>();
            ticker.Setup(this);
        }
        
        internal void Update()
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

        internal void LateUpdate()
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

        internal void FixedUpdate()
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

        private void AddTick(ITickable obj, float tickPeriod = 0f)
        {
            if (_tickables.Exists(t => ReferenceEquals(t.Obj, obj)) && !_removeTickDelayed.Exists(t => ReferenceEquals(t.Obj, obj)))
            {
                Debug.LogError("[Ticker] Tried to register tickable which is already registered!");
                return;
            }
            _tickables.Add(new Tickable(obj, tickPeriod));
            AddUnsubscription(obj);
        }

        private void AddLateTick(ILateTickable obj, float tickPeriod = 0f)
        {
            if (_lateTickables.Exists(t => ReferenceEquals(t.Obj, obj)) && !_removeLateTickDelayed.Exists(t => ReferenceEquals(t.Obj, obj)))
            {
                Debug.LogError("[Ticker] Tried to register late tickable which is already registered!");
                return;
            }
            _lateTickables.Add(new LateTickable(obj, tickPeriod));
            AddUnsubscription(obj);
        }

        private void AddFixedTick(IFixedTickable obj, float tickPeriod = 0f)
        {
            if (_fixedTickables.Exists(t => ReferenceEquals(t.Obj, obj)) && !_removeFixTickDelayed.Exists(t => ReferenceEquals(t.Obj, obj)))
            {
                Debug.LogError("[Ticker] Tried to register fixed tickable which is already registered!");
                return;
            }
            _fixedTickables.Add(new FixedTickable(obj, tickPeriod));
            AddUnsubscription(obj);
        }

        private void RemoveTick(ITickable obj) 
            => _removeTickDelayed.AddRange(_tickables.Where(x => ReferenceEquals(x.Obj, obj)));

        private void RemoveLateTick(ILateTickable obj)
            => _removeLateTickDelayed.AddRange(_lateTickables.Where(x=> ReferenceEquals(x.Obj, obj)));

        private void RemoveFixedTick(IFixedTickable obj)
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