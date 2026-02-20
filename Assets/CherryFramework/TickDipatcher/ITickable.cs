namespace CherryFramework.TickDipatcher
{
    public interface ITickable : ITickableBase
    {
        void Tick(float deltaTime);
    }
}