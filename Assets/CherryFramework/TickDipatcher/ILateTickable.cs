namespace CherryFramework.TickDipatcher
{
    public interface ILateTickable : ITickableBase
    {
        void LateTick(float deltaTime);
    }
}