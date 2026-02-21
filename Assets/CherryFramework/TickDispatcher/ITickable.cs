namespace CherryFramework.TickDispatcher
{
    public interface ITickable : ITickableBase
    {
        void Tick(float deltaTime);
    }
}