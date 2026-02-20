using System.Collections.Generic;
using DG.Tweening;
using TMPro;

namespace CherryFramework.UI.UiAnimation.Animators
{
    public class UiTextFade : UiAnimationBase
    {
        private List<(TMP_Text tmpText, float baseAlpha)> _targetGroups = new();

        protected override void OnInitialize()
        {
            foreach (var target in Targets)
            {
                var txt = target.GetComponent<TMP_Text>();
                if (txt)
                {
                    _targetGroups.Add((txt, txt.alpha));
                    txt.alpha = 0f;
                }
            }

            MainSequence = DOTween.Sequence();
            ResetTargetGroups();
            base.OnInitialize();
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
                group.tmpText.alpha = 0f;
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
                        ? tuple.tmpText.DOFade(tuple.baseAlpha, duration).SetEase(showEasing)
                        : tuple.tmpText.DOFade(0f, duration).SetEase(hideEasing));
            }

            if (delay > 0f)
            {
                MainSequence.PrependInterval(delay);
            }
        }
    }
}