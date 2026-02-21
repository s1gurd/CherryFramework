using System;
using UnityEngine;

namespace CherryFramework.TickDispatcher
{
    public partial class Ticker
    {
        private class TickableBase<T>
        {
            public readonly T Obj;
            public readonly float TickPeriod;
            public float LastTick;

            protected TickableBase(T obj, float tickPeriod)
            {
                Obj = obj ?? throw new ArgumentNullException(nameof(obj));
                TickPeriod = tickPeriod;
            }
        }
        
        private class Tickable : TickableBase<ITickable>
        {
            public Tickable(ITickable obj, float tickPeriod) : base(obj, tickPeriod)
            {
                LastTick = Time.time;
            }
        }
        
        private class LateTickable : TickableBase<ILateTickable>
        {
            public LateTickable(ILateTickable obj, float tickPeriod) : base(obj, tickPeriod)
            {
            }
        }
        
        private class FixedTickable : TickableBase<IFixedTickable>
        {
            public FixedTickable(IFixedTickable obj, float tickPeriod) : base(obj, tickPeriod)
            {
            }
        }
    }
}