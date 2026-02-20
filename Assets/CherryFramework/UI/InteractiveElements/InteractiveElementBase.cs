using System;
using System.Collections.Generic;
using CherryFramework.BaseClasses;
using CherryFramework.UI.UiAnimation;
using CherryFramework.UI.UiAnimation.Enums;
using DG.Tweening;
using TriInspector;
using UnityEngine;

namespace CherryFramework.UI.InteractiveElements
{
    public abstract class InteractiveElementBase : BehaviourBase
    {
        [Title("Animation Settings")] [SerializeField]
        protected List<UiAnimationSettings> animators;

        protected Sequence CreateSequence(List<UiAnimationSettings> anims, Purpose purpose)
        {
            var result = DOTween.Sequence();
            
            switch (purpose)
            {
                case Purpose.Show:
                    result.AppendCallback(OnShowStart);
                    break;
                case Purpose.Hide:
                    result.AppendCallback(OnHideStart);
                    break;
            }

            foreach (var anim in anims)
            {
                Func<float, Tween> action = purpose switch
                {
                    Purpose.Show => delay => anim.animator.Show(delay),
                    Purpose.Hide => delay => anim.animator.Hide(delay),
                    _ => throw new ArgumentOutOfRangeException()
                };

                switch (anim.launchMode)
                {
                    case LaunchMode.AtGlobalAnimationStart:
                        result.Insert(0, action.Invoke(anim.delay));
                        break;
                    case LaunchMode.AtPreviousAnimatorStart:
                        result.Join(action.Invoke(anim.delay));
                        break;
                    case LaunchMode.AfterPreviousAnimatorFinished:
                        result.Append(action.Invoke(anim.delay));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            switch (purpose)
            {
                case Purpose.Show:
                    result.AppendCallback(OnShowComplete);
                    break;
                case Purpose.Hide:
                    result.AppendCallback(OnHideComplete);
                    break;
            }

            return result;
        }

        protected virtual void OnShowStart()
        {
        }
        
        protected virtual void OnShowComplete()
        {
        }
        
        protected virtual void OnHideStart()
        {
        }

        protected virtual void OnHideComplete()
        {
        }
    }
}