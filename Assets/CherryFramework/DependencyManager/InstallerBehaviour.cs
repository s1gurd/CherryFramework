using TriInspector;
using UnityEngine;

namespace CherryFramework.DependencyManager
{
    [DefaultExecutionOrder(-1000)]
    public abstract class InstallerMonoBehaviour : InstallerBehaviourBase
    {
        [Title("Secondary installers handling")]
        [SerializeField] private bool installSecondaryInstallers;
        [ShowIf(nameof(installSecondaryInstallers))] [SerializeField] private bool searchInstallersAutomatically = true;
        [ShowIf(nameof(installSecondaryInstallers))] [HideIf(nameof(searchInstallersAutomatically))] [SerializeField]
        private SecondaryInstallerMonoBehaviour[] secondaryInstallers;

        protected abstract void Install();

        private void Awake()
        {
            Install();

            if (installSecondaryInstallers)
            {
                if (searchInstallersAutomatically)
                {
                    secondaryInstallers = FindObjectsByType<SecondaryInstallerMonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                }

                if (secondaryInstallers == null || secondaryInstallers.Length == 0)
                {
                    Debug.LogError($"[{this.GetType().Name}] No secondary installers found!");
                    return;
                }
                
                foreach (var secondaryInstaller in secondaryInstallers)
                {
                    secondaryInstaller.Install();
                }
            }
        }
    }
}