namespace CherryFramework.DependencyManager
{
    public abstract class InjectClass : IInjectTarget
    {
        private bool _injected;

        protected InjectClass()
        {
            DependencyContainer.Instance.InjectDependencies(this);
        }

        protected void EnsureDependencies()
        {
            if (_injected)
                return;

            DependencyContainer.Instance.InjectDependencies(this);
            _injected = true;
        }
    }
}