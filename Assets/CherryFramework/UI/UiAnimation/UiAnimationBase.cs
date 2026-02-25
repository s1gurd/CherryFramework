using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CherryFramework.UI.UiAnimation
{
    public abstract class UiAnimationBase : MonoBehaviour
    {
        [SerializeField] protected float duration = 0.3f;
        [SerializeField] protected Ease showEasing = Ease.OutQuad;
        [SerializeField] protected Ease hideEasing = Ease.OutQuad;

        protected RectTransform Target
        {
            get
            {
                _target ??= GetComponent<RectTransform>();
                return _target;
            }
        }

        protected Sequence MainSequence;
        
        protected bool Inited;

        private RectTransform _target;
        
        public void Initialize()
        {
            if (Inited)
                return;
            
            Inited = true;
            OnInitialize();
        }

        protected abstract void OnInitialize();

        protected virtual void OnEnable() => Initialize();

        public abstract Sequence Show(float delay = 0f);
        public abstract Sequence Hide(float delay = 0f);
    }
}