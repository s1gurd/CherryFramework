using DG.Tweening;

namespace CherryFramework.UI.UiAnimation.Animators
{
    public class UiActive : UiAnimationBase
    {
        protected override void OnInitialize()
        {
            base.OnInitialize();
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
            foreach (var target in Targets)
            {
                target.gameObject.SetActive(active);
            }
        }
    }
}