namespace CherryFramework.DependencyManager
{
    public static class DependencyUtils
    {
        public static void InjectDependencies(this IInjectTarget target)
        {
            DependencyContainer.Instance.InjectDependencies(target);
        }
    }
}