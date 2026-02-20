using UnityEngine;

namespace CherryFramework.DependencyManager
{
    public abstract class SecondaryInstallerMonoBehaviour : InstallerBehaviourBase
    {
        private bool _installed;

        public void Install()
        {
            if (_installed)
            {
                Debug.LogError($"[{this.GetType().Name}] Already installed, aborting!");
                return;
            }
            
            InstallImpl();
            _installed = true;
        }

        protected abstract void InstallImpl();
    }
}