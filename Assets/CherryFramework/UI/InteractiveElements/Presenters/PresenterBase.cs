using System;
using System.Collections.Generic;
using System.Linq;
using CherryFramework.DependencyManager;
using CherryFramework.UI.UiAnimation.Enums;
using CherryFramework.UI.Views;
using DG.Tweening;
using EditorAttributes;
using UnityEngine;

namespace CherryFramework.UI.InteractiveElements.Presenters
{
    [DisallowMultipleComponent]
    public abstract class PresenterBase : InteractiveElementBase
    {
        [Inject] protected readonly ViewService ViewService;

        [Header("Hierarchy settings")]
        [SerializeField] private Canvas childrenContainer;

        [MessageBox("First presenter will be shown by default", nameof(NotRootPresenter))]
        [SerializeField] protected List<PresenterBase> childPresenters = new();

        public Canvas ChildrenContainer => childrenContainer;
        public List<PresenterBase> ChildPresenters => childPresenters;
        
        [field: SerializeField] public virtual bool Modal { get; private set; }

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

        public virtual Sequence ShowFrom(PresenterBase previous, bool skipAnimation = false)
        {
            this.transform.SetAsLastSibling();
            var seq = CreateSequence(animators, Purpose.Show);
            
            if (skipAnimation) 
                seq.Complete(true);
            
            return seq;
        }

        public virtual Sequence HideTo(PresenterBase next, bool skipAnimation = false)
        {
            var seq = CreateSequence(animators, Purpose.Hide);
            seq.AppendCallback(() => transform.SetAsFirstSibling());
            
            if (skipAnimation) 
                seq.Complete(true);
            
            return seq;
        }
        
        private bool NotRootPresenter => this is not RootPresenterBase;
    }
}
