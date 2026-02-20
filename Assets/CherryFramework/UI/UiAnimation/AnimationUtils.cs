using DG.Tweening;

namespace CherryFramework.UI.UiAnimation
{
    public static class AnimationUtils
    {
        public static Sequence ReCreate(this Sequence seq, bool complete = false)
        {
            seq.Kill(complete);
            seq = DOTween.Sequence();
            return seq;
        }
    }
}