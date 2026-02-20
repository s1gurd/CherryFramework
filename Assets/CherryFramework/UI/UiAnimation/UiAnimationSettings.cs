using System;
using CherryFramework.UI.UiAnimation.Enums;

namespace CherryFramework.UI.UiAnimation
{
    [Serializable]
    public class UiAnimationSettings
    {
        public UiAnimationBase animator;
        public float delay = 0f;
        public LaunchMode launchMode;
    }
}