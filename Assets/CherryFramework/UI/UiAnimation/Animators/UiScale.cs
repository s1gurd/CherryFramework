using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CherryFramework.UI.UiAnimation.Animators
{
    [RequireComponent(typeof(RectTransform))]
    public class UiScale : UiAnimationBase
    {
        [SerializeField] private UiAnimatorEndValueTypes type;
        [SerializeField] private Vector3 value;

        private (RectTransform rectTransform, Vector3 baseValue, Vector3 endValue) _targetGroup;

        protected override void OnInitialize()
        {
            Vector3 startValue;
            Vector3 endValue;

            if (type == UiAnimatorEndValueTypes.To)
            {
                startValue = Target.localScale;
                endValue = value;
            }
            else
            {
                startValue = value;
                endValue = Target.localScale;
            }

            _targetGroup = (Target, startValue, endValue);

            MainSequence = DOTween.Sequence();
            ResetTargetGroups();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResetTargetGroups();
        }

        protected void ResetTargetGroups()
        {
            _targetGroup.rectTransform.localScale = _targetGroup.baseValue;
        }
        
        public override Sequence Show(float delay = 0f)
        {
            MainSequence = MainSequence.ReCreate();

            Scale(delay, true);

            return MainSequence;
        }

        public override Sequence Hide(float delay = 0f)
        {
            MainSequence = MainSequence.ReCreate();

            Scale(delay, false);

            return MainSequence;
        }

        private void Scale(float delay, bool show)
        {
            MainSequence.Insert(
                0,
                show
                    ? _targetGroup.rectTransform.DOScale(_targetGroup.endValue, duration).SetEase(showEasing)
                    : _targetGroup.rectTransform.DOScale(_targetGroup.baseValue, duration).SetEase(hideEasing));

            if (delay > 0f) MainSequence.PrependInterval(delay);
        }
    }
}