using System;
using System.Collections.Generic;
using System.Linq;
using CherryFramework.DependencyManager;
using CherryFramework.UI.UiAnimation.Enums;
using CherryFramework.UI.ViewService;
using DG.Tweening;
using TriInspector;
using UnityEngine;

namespace CherryFramework.UI.InteractiveElements.Presenters
{
    [DisallowMultipleComponent]
    public abstract class PresenterBase : InteractiveElementBase
    {
        [Inject] protected readonly ViewService.ViewService ViewService;

        [Title("Hierarchy settings")]
        [SerializeField] private Canvas childrenContainer;

        [InfoBox("First presenter will be default")]
        [SerializeField, DrawWithUnity] protected List<PresenterBase> childPresenters = new();

        public Canvas ChildrenContainer => childrenContainer;
        public List<PresenterBase> ChildPresenters => childPresenters;

        [HideInInspector] public List<PresenterBase> uiPath = new();
        [HideInInspector] public PresenterBase currentChild;

        protected PresenterBase UiRoot => null;
        protected PresenterBase UiParent => uiPath.LastOrDefault();
        protected PresenterBase UiThis => this;
        protected bool Initialized { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializePresenter();
        }

        /// <summary>
        /// It is called once after Awake & Before 
        /// </summary>
        public void InitializePresenter()
        {
            if (Initialized)
                return;
            
            if (this is not RootPresenterBase && childrenContainer && childrenContainer.GetComponentInParent<PresenterBase>() != this)
                throw new Exception($"[Presenter - {this.gameObject.name}] Children container {childrenContainer.gameObject} must be a child of this Game Object!");
            
            Initialized = true;
            foreach (var animator in animators)
                animator.animator.Initialize();
            OnPresenterInitialized();
        }
        
        protected virtual void OnPresenterInitialized(){}

        public virtual void ShowFrom(PresenterBase previous, bool skipAnimation = false)
        {
            this.transform.SetAsLastSibling();
            var seq = CreateSequence(animators, Purpose.Show);
            
            if (skipAnimation) 
                seq.Complete(true);
        }

        public virtual void HideTo(PresenterBase next, bool skipAnimation = false)
        {
            var seq = CreateSequence(animators, Purpose.Hide);
            seq.AppendCallback(() => transform.SetAsFirstSibling());
            
            if (skipAnimation) 
                seq.Complete(true);
        }
    }
}
