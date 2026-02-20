using CherryFramework.UI.InteractiveElements.Widgets;
using CherryFramework.UI.UiAnimation.Enums;
using DG.Tweening;

namespace CherryFramework.UI.InteractiveElements.Populators
{
    public abstract class PopulatorElementBase<T> : WidgetElement where T : class
    {
        public T data;

        public virtual void SetData(T data)
        {
            Inject(); // TODO Sometimes SetData is called before injection.
            this.data = data;
        } 

        public virtual Sequence Refresh()
        {
            var seq = CreateSequence(animators, Purpose.Hide);
            seq.Append(CreateSequence(animators, Purpose.Show));
            seq.AppendCallback(OnRefreshComplete);
            return seq;
        }
        
        protected virtual void OnRefreshComplete()
        {
        }
    }
}