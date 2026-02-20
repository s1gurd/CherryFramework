namespace CherryFramework.TickDipatcher
{
    public interface IFixedTickable : ITickableBase
    {
        void FixedTick(float deltaTime);
    }
}