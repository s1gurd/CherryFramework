using DG.Tweening;
using TriInspector;
using UnityEngine;

namespace CherryFramework.UI.UiAnimation.Animators
{
    [RequireComponent(typeof(RectTransform))]
    public class UiSlide : UiAnimationBase
    {
        [InfoBox("Delta is counted as a ratio to target transform dimensions")]
        [SerializeField] private Vector2 positionDelta;
        [SerializeField] private bool reverseDirectionOnHide = true;
        
        private (Vector2 startValue, Vector2 basevalue, Vector2 endValue) _targetGroup;

        protected override void OnInitialize()
        {
            var basePosition = Target.anchoredPosition;
            var delta = new Vector2(Target.rect.width * positionDelta.x, Target.rect.height * positionDelta.y);
            var startPosition = basePosition + delta;
            var endPosition = reverseDirectionOnHide ? startPosition : basePosition + delta;
            var group = (startPosition, basePosition, endPosition);
            _targetGroup = group;
                
            MainSequence = DOTween.Sequence();
            ResetTarget();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetTarget();
        }
        
        protected void ResetTarget()
        {
            Target.anchoredPosition = _targetGroup.startValue;
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
            if (!Inited) Initialize();
            
            MainSequence = MainSequence.ReCreate();
            
            if (slideIn)
                MainSequence.Append(
                    Target.DOAnchorPos(_targetGroup.basevalue, duration).SetEase(showEasing)
                        .From(_targetGroup.startValue));
            else
                MainSequence.Append(
                    Target.DOAnchorPos(_targetGroup.endValue, duration).SetEase(showEasing)
                        .From(_targetGroup.basevalue));

            if (delay > 0f)
            {
                MainSequence.PrependInterval(delay);
            }
        }
    }
}