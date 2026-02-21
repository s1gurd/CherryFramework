namespace CherryFramework.TickDispatcher
{
    public interface IFixedTickable : ITickableBase
    {
        void FixedTick(float deltaTime);
    }
}