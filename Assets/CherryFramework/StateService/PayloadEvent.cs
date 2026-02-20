namespace CherryFramework.StateService
{
    public class PayloadEvent<T> : EventBase
    {
        public T Payload;

        public PayloadEvent(T payload, int emitFrame) : base(emitFrame)
        {
            Payload = payload;
        }
    }
}