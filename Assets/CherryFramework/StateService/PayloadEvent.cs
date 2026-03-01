namespace CherryFramework.StateService
{
    public class PayloadEvent<T> : EventBase
    {
        public T Payload;

        public PayloadEvent(T payload, float emitTime) : base(emitTime)
        {
            Payload = payload;
        }
    }
}