using System;

namespace CherryFramework.UI.UiAnimation.Enums
{
    [Serializable]
    public enum LaunchMode
    {
        AtGlobalAnimationStart = 0,
        AtPreviousAnimatorStart = 1,
        AfterPreviousAnimatorFinished = 2
    }
}