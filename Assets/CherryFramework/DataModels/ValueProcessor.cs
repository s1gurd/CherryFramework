using System;

namespace CherryFramework.DataModels
{
    public class ValueProcessor
    {
        public readonly DataModelBase Model;
        public readonly string MemberName;
        public readonly int Priority;
        public readonly Delegate Action;

        public ValueProcessor(DataModelBase model, string memberName, int priority, Delegate action)
        {
            Model = model;
            MemberName = memberName;
            Priority = priority;
            Action = action;
        }
    }
}