using System;

namespace CherryFramework.BaseClasses
{
    public interface IUnsubscriber
    {
        void AddUnsubscription(Action action);
    }
}