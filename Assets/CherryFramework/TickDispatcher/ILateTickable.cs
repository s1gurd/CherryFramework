namespace CherryFramework.TickDispatcher
{
    public interface ILateTickable : ITickableBase
    {
        void LateTick(float deltaTime);
    }
}