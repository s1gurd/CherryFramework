using System.Collections.Generic;
using DG.Tweening;
using TriInspector;
using UnityEngine;

namespace CherryFramework.UI.UiAnimation.Animators
{
    public class UiSlide : UiAnimationBase
    {
        [InfoBox("Delta is counted as a ratio to target transform dimensions")]
        [SerializeField] private Vector2 positionDelta;
        [SerializeField] private bool reverseDirectionOnHide = true;
        
        private readonly List<(RectTransform rectTransform, Vector3 startValue, Vector3 basevalue, Vector3 endValue)> _targetGroups = new();

        protected override void OnInitialize()
        {
            foreach (var target in Targets)
            {
                var basePosition = target.localPosition;
                var delta = new Vector3(target.rect.width * positionDelta.x, target.rect.height * positionDelta.y, 0f);
                var startPosition = basePosition + delta;
                var endPosition = reverseDirectionOnHide ? startPosition : basePosition - delta;
                var group = (target, startPosition, basePosition, endPosition);
                _targetGroups.Add(group);
            }

            MainSequence = DOTween.Sequence();
            base.OnInitialize();
            ResetTargetGroups();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetTargetGroups();
        }
        
        protected void ResetTargetGroups()
        {
            foreach (var group in _targetGroups)
            {
                group.rectTransform.localPosition = group.startValue;
            }
        }
        
        public override Sequence Show(float delay = 0f)
        {
            Slide(delay, true);
            return MainSequence;
        }

        public override Sequence Hide(float delay = 0f)
        {
            Slide(delay, false);
            return MainSequence;
        }

        private void Slide(float delay, bool slideIn)
        {
            MainSequence = MainSequence.ReCreate();
            
            foreach (var tuple in _targetGroups)
            {
                if (slideIn)
                {
                    MainSequence.Insert(0, tuple.rectTransform.DOLocalMove(tuple.basevalue, duration).SetEase(showEasing).From(tuple.startValue));
                }
                else
                {
                    MainSequence.Insert(0, tuple.rectTransform.DOLocalMove(tuple.endValue, duration).SetEase(showEasing).From(tuple.basevalue));
                }
            }

            if (delay > 0f)
            {
                MainSequence.PrependInterval(delay);
            }
        }
    }
}