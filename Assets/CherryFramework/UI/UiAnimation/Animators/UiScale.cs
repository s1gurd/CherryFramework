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

        private (Vector3 baseValue, Vector3 endValue) _targetGroup;

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

            _targetGroup = (startValue, endValue);

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
            Target.localScale = _targetGroup.baseValue;
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
            if (!Inited) 
                Initialize();

            MainSequence.Insert(
                0,
                show
                    ? Target.DOScale(_targetGroup.endValue, duration).SetEase(showEasing)
                    : Target.DOScale(_targetGroup.baseValue, duration).SetEase(hideEasing));

            if (delay > 0f) MainSequence.PrependInterval(delay);
        }
    }
}