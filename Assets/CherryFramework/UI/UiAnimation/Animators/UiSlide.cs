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
        
        private (RectTransform rectTransform, Vector3 startValue, Vector3 basevalue, Vector3 endValue) _targetGroup;

        protected override void OnInitialize()
        {
            var basePosition = Target.anchoredPosition3D;
            var delta = new Vector3(Target.rect.width * positionDelta.x, Target.rect.height * positionDelta.y, 0f);
            var startPosition = basePosition + delta;
            var endPosition = reverseDirectionOnHide ? startPosition : basePosition + delta;
            var group = (Target, startPosition, basePosition, endPosition);
            _targetGroup = group;
                
            MainSequence = DOTween.Sequence();
            ResetTargetGroups();
        }

        // protected override void OnEnable()
        // {
        //     base.OnEnable();
        //     ResetTargetGroups();
        // }
        
        protected void ResetTargetGroups()
        {
            _targetGroup.rectTransform.anchoredPosition3D = _targetGroup.startValue;
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
            
            if (slideIn)
                MainSequence.Append(
                    _targetGroup.rectTransform.DOAnchorPos3D(_targetGroup.basevalue, duration).SetEase(showEasing)
                        .From(_targetGroup.startValue));
            else
                MainSequence.Append(
                    _targetGroup.rectTransform.DOAnchorPos3D(_targetGroup.endValue, duration).SetEase(showEasing)
                        .From(_targetGroup.basevalue));

            if (delay > 0f)
            {
                MainSequence.PrependInterval(delay);
            }
        }
    }
}