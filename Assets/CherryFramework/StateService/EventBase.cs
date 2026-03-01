namespace CherryFramework.StateService
{
    public abstract class EventBase
    {
        public float EmitTime;

        protected EventBase(float emitTime)
        {
            EmitTime = emitTime;
        }
    }
}