using System;

namespace CherryFramework.DataModels
{
    public class DownwardBindingHandler
    {
        public readonly DataModelBase Model;
        public readonly BindingActivation ActivationMode;

        protected DownwardBindingHandler(DataModelBase model, BindingActivation activationMode)
        {
            Model = model;
            ActivationMode = activationMode;
        }
    }

    public class DownwardBindingHandler<T> : DownwardBindingHandler
    {
        public Action<T> DownwardCallback { get; }
        
        public DownwardBindingHandler(DataModelBase model, Action<T> callback, BindingActivation activationMode) : base(model, activationMode)
        {
            DownwardCallback = callback;
        }
    }
}