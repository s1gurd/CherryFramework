using System;
using System.Collections.Generic;
using CherryFramework.UI.UiAnimation.Enums;
using DG.Tweening;
using TriInspector;
using UnityEngine;

namespace CherryFramework.UI.InteractiveElements.Widgets
{
    [DisallowMultipleComponent]
    public class WidgetBase : InteractiveElementBase
    {
        [Title("Widget states and settings")]
        [SerializeField] private WidgetStartupBehaviour startupBehaviour;
        
        [InfoBox("First element will become current state")]
        [SerializeField]
        protected List<WidgetState> widgetStates = new();

        [SerializeField] private bool forceDisableElementAfterHide;

        public int CurrentState { get; private set; }
        public int StatesCount => widgetStates.Count;
        public bool Inited { get; private set; }
        public bool Playing { get; private set; }

        private Sequence _currentSequence;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!Inited)
                Init();
        }

        public delegate void OnStateChangedDelegate(string newStateName);
        public event OnStateChangedDelegate OnStartStateChange = delegate { };
        public event OnStateChangedDelegate OnFinishStateChange = delegate { };

        public void SetState(int state)
        {
            if (!Inited)
                Init();

            if (state == CurrentState) 
                return;

            if (state >= widgetStates.Count || state < 0)
            {
                Debug.LogError($"[Widget] Not found requested state: {state}, aborting!");
                return;
            }

            CurrentState = state;
            if (Playing)
                _currentSequence?.Kill(true);

            _currentSequence = DOTween.Sequence();
            Playing = true;

            for (var i = 0; i < widgetStates.Count; i++)
            {
                if (i == CurrentState)
                    continue;
                
                _currentSequence.Append(SetElementsInState(widgetStates[i], false));
            }

            _currentSequence.Append(SetElementsInState(widgetStates[CurrentState], true));

            OnStartStateChange.Invoke(widgetStates[state].stateName);
            _currentSequence.OnComplete(() =>
            {
                Playing = false;
                OnFinishStateChange.Invoke(widgetStates[state].stateName);
            });
        }

        public void SetState(string stateName)
        {
            if (!Inited)
                Init();

            var entries = widgetStates.FindAll(s => s.stateName.Equals(stateName, StringComparison.Ordinal));
            switch (entries.Count)
            {
                case 0:
                    Debug.LogError($"[Widget] Not found requested state: \'{stateName}\', aborting!");
                    return;
                case > 1:
                    Debug.LogError($"[Widget] Found {entries.Count} states with identical name: \'{stateName}\', aborting!");
                    return;
                default:
                    var index = widgetStates.IndexOf(entries[0]);
                    SetState(index);
                    break;
            }
        }

        public string GetStateName(int state)
        {
            if (state >= widgetStates.Count || state < 0)
            {
                Debug.LogError($"[Widget] Not found requested state: {state}, aborting!");
                return null;
            }

            return widgetStates[state].stateName;
        }
        
        private Sequence SetElementsInState(WidgetState state, bool show)
        {
            var seq = DOTween.Sequence();

            foreach (var element in state.stateElements)
            {
                seq.AppendCallback(() => element.gameObject.SetActive(true));
                if (show)
                {
                    seq.Append(element.Show());
                }
                else
                {
                    seq.Append(element.Hide());
                    if (forceDisableElementAfterHide)
                        seq.AppendCallback(() => element.gameObject.SetActive(false));
                }
            }

            return seq;
        }

        protected virtual void Init()
        {
            CurrentState = 0;
            Playing = true;
            _currentSequence?.Kill(true);
            _currentSequence = DOTween.Sequence();

            if (startupBehaviour is WidgetStartupBehaviour.SequentiallyExecuteShowOnSelfAndCurrentState 
                or WidgetStartupBehaviour.SimultaneouslyExecuteShowOnSelfAndCurrentState)
                _currentSequence.Append(CreateSequence(animators, Purpose.Show));
            
            for (var i = 0; i < widgetStates.Count; i++)
            {
                var innerSeq = SetElementsInState(widgetStates[i], i == CurrentState);
                
                switch (startupBehaviour)
                {
                    case WidgetStartupBehaviour.ExecuteShowOnCurrentState:
                    case WidgetStartupBehaviour.SimultaneouslyExecuteShowOnSelfAndCurrentState:
                        _currentSequence.Insert(0, innerSeq);
                        break;
                    
                    case WidgetStartupBehaviour.SequentiallyExecuteShowOnSelfAndCurrentState:
                        _currentSequence.Append(innerSeq);
                        break;
                    
                    case WidgetStartupBehaviour.JustSetCurrentState:
                        innerSeq.Complete(true);
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            _currentSequence.OnComplete(() => Playing = false);
            Inited = true;
        }
    }
}