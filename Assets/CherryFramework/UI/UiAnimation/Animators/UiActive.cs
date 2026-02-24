using DG.Tweening;
using UnityEngine;

namespace CherryFramework.UI.UiAnimation.Animators
{
    [RequireComponent(typeof(RectTransform))]
    public class UiActive : UiAnimationBase
    {
        protected override void OnInitialize()
        {
            MainSequence = DOTween.Sequence();
        }

        public override Sequence Show(float delay = 0)
        {
            MainSequence = MainSequence.ReCreate();
            return MainSequence.AppendInterval(duration + delay).AppendCallback(() => SetActive(true));
        }

        public override Sequence Hide(float delay = 0)
        {
            MainSequence = MainSequence.ReCreate();
            return MainSequence.AppendInterval(duration + delay).AppendCallback(() => SetActive(false));
        }

        private void SetActive(bool active)
        {
            Target.gameObject.SetActive(active);
        }
    }
}