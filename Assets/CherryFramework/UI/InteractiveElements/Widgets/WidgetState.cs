using System;
using System.Collections.Generic;

namespace CherryFramework.UI.InteractiveElements.Widgets
{
    [Serializable]
    public class WidgetState
    {
        public string stateName = "";
        public List<WidgetElement> stateElements = new();
    }
}