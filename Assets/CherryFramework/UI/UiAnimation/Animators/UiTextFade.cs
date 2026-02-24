using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace CherryFramework.UI.UiAnimation.Animators
{
    [RequireComponent(typeof(RectTransform), typeof(TMP_Text))]
    public class UiTextFade : UiAnimationBase
    {
        private (TMP_Text tmpText, float baseAlpha) _targetGroup;

        protected override void OnInitialize()
        {
            var txt = Target.GetComponent<TMP_Text>();
            if (txt)
            {
                _targetGroup = (txt, txt.alpha);
                txt.alpha = 0f;
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
            _targetGroup.tmpText.alpha = 0f;
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
                    ? _targetGroup.tmpText.DOFade(_targetGroup.baseAlpha, duration).SetEase(showEasing)
                    : _targetGroup.tmpText.DOFade(0f, duration).SetEase(hideEasing));
            
            if (delay > 0f)
            {
                MainSequence.PrependInterval(delay);
            }
        }
    }
}