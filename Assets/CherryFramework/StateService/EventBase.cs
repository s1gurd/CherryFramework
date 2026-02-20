namespace CherryFramework.StateService
{
    public abstract class EventBase
    {
        public int EmitFrame;

        protected EventBase(int emitFrame)
        {
            EmitFrame = emitFrame;
        }
    }
}