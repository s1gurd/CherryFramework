namespace CherryFramework.UI.InteractiveElements.Widgets
{
    public enum WidgetStartupBehaviour
    {
        ExecuteShowOnCurrentState = 0,
        SimultaneouslyExecuteShowOnSelfAndCurrentState = 1,
        SequentiallyExecuteShowOnSelfAndCurrentState = 2,
        JustSetCurrentState = 3
    }
}