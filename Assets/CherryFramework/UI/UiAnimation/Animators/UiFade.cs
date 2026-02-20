using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CherryFramework.UI.UiAnimation.Animators
{
    public class UiFade : UiAnimationBase
    {
        private readonly List<(CanvasGroup canvasGroup, float baseAlpha)> _targetGroups = new();

        protected override void OnInitialize()
        {
            foreach (var target in Targets)
            {
                var canvasGroup = target.GetComponent<CanvasGroup>();
                if (canvasGroup)
                {
                    _targetGroups.Add((canvasGroup, canvasGroup.alpha));
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                }
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
                group.canvasGroup.alpha = 0f;
                group.canvasGroup.blocksRaycasts = false;
            }
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
            foreach (var tuple in _targetGroups)
            {
                MainSequence.Insert(0,
                    fadeIn
                        ? tuple.canvasGroup.DOFade(tuple.baseAlpha, duration).SetEase(showEasing)
                        : tuple.canvasGroup.DOFade(0f, duration).SetEase(hideEasing));

                MainSequence.PrependCallback(() => tuple.canvasGroup.blocksRaycasts = fadeIn);
            }

            if (delay > 0f)
            {
                MainSequence.PrependInterval(delay);
            }
        }
    }
}