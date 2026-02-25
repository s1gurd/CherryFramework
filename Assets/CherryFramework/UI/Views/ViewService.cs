using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using CherryFramework.UI.InteractiveElements.Presenters;
using CherryFramework.Utils;
using DG.Tweening;
using UnityEngine;

namespace CherryFramework.UI.Views
{
    public class ViewService : GeneralClassBase
    {
        public delegate void OnViewChangedDelegate();
        public event OnViewChangedDelegate OnAnyViewBecameActive = delegate { };
        public event OnViewChangedDelegate OnAllViewsBecameInactive = delegate { };
        
        
        [Inject] private readonly StateService.StateService _stateService;
        
        private readonly PresenterBase _root;
        private PresenterLoadingBase _loadingScreen;
        private PresenterErrorBase _errorScreen;

        private readonly Stack<List<PresenterBase>> _history = new();
        private readonly bool _debugMessages;
        
        public bool IsViewActive => _history.Count > 0;
        public bool IsLastView => _history.Count == 1;
        public PresenterBase ActiveView { get; private set; }

        public ViewService(RootPresenterBase root, bool debugMessages)
        {
            _loadingScreen = root.LoadingScreen;
            _errorScreen = root.ErrorScreen;
            _root = root;
            _debugMessages = debugMessages;
        }

        public Sequence PopLoadingView()
        {
            var seq = PopView(_loadingScreen, out var newLoading);
            _loadingScreen = newLoading as PresenterLoadingBase;
            return seq;
        }

        public Sequence PopErrorView(string title, string message)
        {
            var seq = PopView(_errorScreen, out var newError);
            _errorScreen = newError as PresenterErrorBase;
            _errorScreen?.SetError(title, message);
            return seq;
        }

        public Sequence PopView<T>(PresenterBase mountingPoint = null,
            bool skipAnimation = false) where T : PresenterBase
        {
            return PopView<T>(out _, mountingPoint, skipAnimation);
        }

        public Sequence PopView<T>(out T newView, PresenterBase mountingPoint = null, bool skipAnimation = false) where T : PresenterBase
        {
            var seq = PopView(typeof(T), out var result, mountingPoint, skipAnimation);
            newView = result as T;
            return seq;
        }

        public Sequence PopView(string typeString, PresenterBase mountingPoint = null, bool skipAnimation = false)
        {
            return PopView(typeString, out _, mountingPoint, skipAnimation);
        }
        
        public Sequence PopView(string typeString, out PresenterBase newView, PresenterBase mountingPoint = null, bool skipAnimation = false)
        {
            newView = null;
            var type = ViewUtils.GetPresenterType(typeString);
            if (type == null)
            {
                Debug.LogError($"[View Service] View of type: {typeString} not found! Aborting...");
                return DOTween.Sequence();;
            }

            return PopView(type, out newView, mountingPoint, skipAnimation);
        }

        public Sequence PopView(Type type, PresenterBase mountingPoint = null, bool skipAnimation = false)
        {
            return PopView(type, out _, mountingPoint, skipAnimation);
        }
        
        public Sequence PopView(Type type, out PresenterBase newView, PresenterBase mountingPoint = null, bool skipAnimation = false)
        {
            newView = null;
            
            var parentPresenter = mountingPoint ? mountingPoint : _root; 

            var newViewSource = parentPresenter.ChildPresenters.FirstOrDefault(p => p && p.GetType() == type);

            if (newViewSource is null)
            {
                Debug.LogError(
                    $"[View Service] View of type: {type.Name} not registered in View Container: {parentPresenter.gameObject.name} of {mountingPoint?.gameObject.name}! Aborting...",
                    mountingPoint ? mountingPoint.gameObject : null);
                
                return DOTween.Sequence();;
            }

            return PopView(newViewSource, out newView, mountingPoint, skipAnimation);
        }

        public virtual Sequence PopView(PresenterBase view, PresenterBase mountingPoint = null, bool skipAnimation = false)
        {
            return PopView(view, out _, mountingPoint, skipAnimation);
        }
        
        public virtual Sequence PopView(PresenterBase view, out PresenterBase newView, PresenterBase mountingPoint = null, bool skipAnimation = false)
        {
            if (_history.TryPeek(out var current))
            {
                if (current.Last() is IModal || current.Last().Modal)
                {
                    newView = null;
                    DebugHistory("Blocked by modal view");
                    return DOTween.Sequence();
                }

                if (current.Last() is IPopUp)
                {
                    _history.Pop();
                }
            }

            var parentPresenter = mountingPoint ? mountingPoint : _root;

            if (!parentPresenter.ChildPresenters.Any(p => p.Equals(view)))
            {
                Debug.LogError(
                    $"[View Service] View : {view.gameObject.name} is not registered in View Container: {parentPresenter.gameObject.name} of {mountingPoint?.gameObject.name}! Aborting...",
                    mountingPoint ? mountingPoint.gameObject : null);
            }

            if (view.gameObject.scene.IsValid())
            {
                newView = view;
                newView.gameObject.SetActive(true);
            }
            else
            {
                var index = parentPresenter.ChildPresenters.IndexOf(view);
                newView = UnityEngine.Object.Instantiate(view, parentPresenter.ChildrenContainer.transform);
                parentPresenter.ChildPresenters[index] = newView;
                
                newView.InitializePresenter();
            }

            var newPath = new List<PresenterBase>();

            if (mountingPoint)
            {
                mountingPoint.currentChild = newView;
                newPath.AddRange(mountingPoint.uiPath);
                newPath.Add(mountingPoint);
            }

            newView.uiPath = newPath;

            var historyItem = new List<PresenterBase>(newPath) { newView };
            
            var seq = DOTween.Sequence();
            if (!IsViewActive)
            {
                seq.AppendCallback(() => OnAnyViewBecameActive.Invoke());
            }

            if (newView.ChildrenContainer != null && newView.ChildPresenters.Count > 0)
            {
                var viewToPop = newView.currentChild != null ? newView.currentChild : newView.ChildPresenters.First();
                seq.Insert(0,PopView(viewToPop, out var newChild, newView, skipAnimation));
                historyItem.Add(newChild);
            }
            else
            {
                if (current != null
                    && current.Last() is not IPopUp
                    && !current.Except(historyItem).Any()
                    && !historyItem.Except(current).Any())
                {
                    DebugHistory("History duplicate");
                }
                else
                {
                    _history.Push(historyItem);
                    DebugHistory("History push");
                }
            }
            
            ActiveView = newView;
            seq.Insert(0, newView.ShowFrom(current?.Last(), skipAnimation));
            return seq;
        }

        public void ClearHistory()
        {
            var current = _history.Pop();
            //foreach (var item in _history)
            //{
            //    item.First().gameObject.SetActive(false);
            //}
            _history.Clear();
            _history.Push(current);
            DebugHistory("History clear");
        }

        public virtual Sequence Back(bool skipAnimation = false)
        {
            if (_history.Count < 1)
            {
                DebugHistory("History is empty");
                return DOTween.Sequence();
            }
                
            if (_history.TryPeek(out var c) && c.Last() is IModal)
            {
                DebugHistory("Blocked by modal view");
                return DOTween.Sequence();
            }

            var current = _history.Pop();
            
            if (_history.TryPeek(out var path))
            {
                DebugHistory("History back");
                
                var lastItem = path.Last();

                for (var i = 0; i < path.Count; i++)
                {
                    path[i].transform.SetAsLastSibling();
                    path[i].gameObject.SetActive(true);
                    if (current.Count == i + 1)
                    {
                        current[i].transform.SetAsLastSibling();
                        lastItem = path[i];
                    }
                }
                
                return current.Last().HideTo(lastItem, skipAnimation);
            }
            else
            {
                DebugHistory("History back to clear screen");
                ActiveView = null;
                _history.Clear();
                return current.Last().HideTo(null, skipAnimation).AppendCallback(()  => OnAllViewsBecameInactive.Invoke());
            }
        }

        public Sequence HideAndReset(bool skipAnimation = false)
        {
            if (_history.Count < 1) return DOTween.Sequence();
            
            var current = _history.Pop();
            ActiveView = null;
            _history.Clear();
            return current.Last().HideTo(null, skipAnimation).AppendCallback(()  => OnAllViewsBecameInactive.Invoke());
        }

        private void DebugHistory(string msg)
        {
            if (!_debugMessages)
                return;
            
            var sb = new StringBuilder();
            sb.Append($"[View Service] {msg}:\n");
            foreach (var item in _history)
            {
                sb.Append("#");
                foreach (var element in item)
                {
                    sb.Append(element.name);
                    sb.Append("/");
                }

                sb.Append("\n");
            }

            Debug.Log(sb.ToString());
        }
    }
}