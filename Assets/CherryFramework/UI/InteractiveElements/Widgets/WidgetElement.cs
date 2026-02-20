using CherryFramework.UI.UiAnimation.Enums;
using DG.Tweening;
using TriInspector;

namespace CherryFramework.UI.InteractiveElements.Widgets
{
    public class WidgetElement : InteractiveElementBase
    {
#if UNITY_EDITOR
        [Button]
        private void TryShow() => Show();
        
        [Button]
        private void TryHide() => Hide();
#endif
        
        public virtual Sequence Show()
        {
            // TODO The use of an intermediate variable is mandatory. ps: DoTween bug
            var seq = CreateSequence(animators, Purpose.Show);
            return seq;
        }

        public virtual Sequence Hide()
        {
            // TODO The use of an intermediate variable is mandatory. ps: DoTween bug
            var seq = CreateSequence(animators, Purpose.Hide);
            return seq;
        }
    }
}