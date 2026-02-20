using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace CherryFramework.UI.UiAnimation
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class UiAnimationBase : MonoBehaviour
    {
        [SerializeField] private List<RectTransform> targets = new();
        [SerializeField] protected float duration = 0.3f;
        [SerializeField] protected Ease showEasing = Ease.OutQuad;
        [SerializeField] protected Ease hideEasing = Ease.OutQuad;

        protected List<RectTransform> Targets => targets.Count > 0 ? targets : new List<RectTransform> { (RectTransform)transform };

        protected Sequence MainSequence;
        
        protected bool Inited;
        
        public void Initialize()
        {
            if (Inited)
                return;
            
            Inited = true;
            OnInitialize();
        }
        
        protected virtual void OnInitialize(){}

        protected virtual void OnEnable() => Initialize();

        public abstract Sequence Show(float delay = 0f);
        public abstract Sequence Hide(float delay = 0f);
    }
}