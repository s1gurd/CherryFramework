using System;
using System.Collections.Generic;

namespace CherryFramework.DataModels
{
    public class Bindings
    {
        private readonly List<DownwardBindingHandler> _downwardHandlers = new();
        
        public DownwardBindingHandler CreateBinding<T>(Accessor<T> accessor, Action<T> callback, BindingActivation activationMode = BindingActivation.InvokeImmediate)
        {
            var handler = accessor.BindDownwards(callback, activationMode);
            _downwardHandlers.Add(handler);
            return handler;
        }
        
        public void ReleaseAllBindings()
        {
            for (var i = _downwardHandlers.Count - 1; i >= 0; i--)
            {
                ReleaseBinding(_downwardHandlers[i]);
            }
        }

        public void ReleaseBinding(DownwardBindingHandler handler)
        {
            _downwardHandlers.Remove(handler);
            handler?.Model?.RemoveBinding(handler);
        }
    }
}