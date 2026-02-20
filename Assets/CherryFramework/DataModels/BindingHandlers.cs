using System;

namespace CherryFramework.DataModels
{
    public class DownwardBindingHandler
    {
        public readonly DataModelBase Model;

        protected DownwardBindingHandler(DataModelBase model)
        {
            Model = model;
        }
    }

    public class DownwardBindingHandler<T> : DownwardBindingHandler
    {
        public Action<T> DownwardCallback { get; }
        
        public DownwardBindingHandler(DataModelBase model, Action<T> callback) : base(model)
        {
            DownwardCallback = callback;
        }
    }
}