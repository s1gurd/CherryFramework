using System;

namespace CherryFramework.SaveGameManager
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class SaveGameDataAttribute : Attribute
    {
        
    }
}