using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace CherryFramework.DataModels
{
    public class Accessor<T>
    {
        private readonly DataModelBase _model;
        private readonly string _memberName;
        
        private readonly List<ValueProcessor> _processors = new ();

        public Accessor(DataModelBase model, string memberName)
        {
            _memberName = memberName;
            _model = model;
        }

        [Pure]
        public DownwardBindingHandler BindDownwards(Action<T> callback, bool invokeImmediate = true)
        {
           var handler = new DownwardBindingHandler<T>(_model, callback);
           
            _model.AddBinding<T>(_memberName, handler, invokeImmediate);
            return handler;
        }

        public void InvokeDownwardBindings()
        {
            _model.InvokeBinding<T>(_memberName);
        }

        public ValueProcessor AddProcessor(Func<T, T> processor, int priority = 0)
        {
            var proc = new ValueProcessor(_model, _memberName, priority, processor);
            var index = _processors.FindLastIndex(p => p.Priority <= priority);
            _processors.Insert(index + 1, proc);
            return proc;
        }

        public void RemoveProcessor(ValueProcessor processor)
        {
            _processors.Remove(processor);
        }

        public void RemoveAllProcessors()
        {
            _processors.Clear();
        }

        public T Value => _model.GetValue<T>(_memberName);

        public T ProcessedValue
        {
            get
            {
                var result = _model.GetValue<T>(_memberName);

                foreach (var processor in _processors)
                {
                    result = (processor.Action as Func<T, T>)!.Invoke(result);
                }

                return result;
            }
        }
    }
}