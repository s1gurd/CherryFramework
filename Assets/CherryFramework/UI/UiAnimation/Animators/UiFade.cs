using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CherryFramework.UI.UiAnimation.Animators
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public class UiFade : UiAnimationBase
    {
        private (CanvasGroup canvasGroup, float baseAlpha) _targetGroup;

        protected override void OnInitialize()
        {
            var canvasGroup = Target.GetComponent<CanvasGroup>();
            if (canvasGroup)
            {
                _targetGroup = (canvasGroup, canvasGroup.alpha);
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
            
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
            _targetGroup.canvasGroup.alpha = 0f;
            _targetGroup.canvasGroup.blocksRaycasts = false;
        }

        public override Sequence Show(float delay = 0f)
        {
            MainSequence = MainSequence.ReCreate();

            Fade(delay, true);

            return MainSequence;
        }

        public override Sequence Hide(float delay = 0f)
        {
            MainSequence = MainSequence.ReCreate();

            Fade(delay, false);

            return MainSequence;
        }

        private void Fade(float delay, bool fadeIn)
        {
            MainSequence.Insert(0,
                fadeIn
                    ? _targetGroup.canvasGroup.DOFade(_targetGroup.baseAlpha, duration).SetEase(showEasing)
                    : _targetGroup.canvasGroup.DOFade(0f, duration).SetEase(hideEasing));

            MainSequence.PrependCallback(() => _targetGroup.canvasGroup.blocksRaycasts = fadeIn);
            
            if (delay > 0f)
            {
                MainSequence.PrependInterval(delay);
            }
        }
    }
}