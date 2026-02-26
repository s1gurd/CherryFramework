using System;
using System.Collections.Generic;
using CherryFramework.BaseClasses;
using CherryFramework.TickDispatcher;
using UnityEngine;

namespace CherryFramework.StateService
{
    public class StateService : GeneralClassBase, ILateTickable
    {
        protected readonly Ticker Ticker =  new Ticker();
        
        private StateAccessor _stateAccessor;

        private Dictionary<string, EventBase> _currentEvents = new();
        private readonly Dictionary<string, EventBase> _pastEvents = new();

        private readonly Dictionary<string, StateStatus> _activeStatuses = new();
        private readonly Dictionary<string, StateStatus> _inactiveStatuses = new();
        private readonly Dictionary<string, StateStatus> _becameActiveStatuses = new();
        private readonly Dictionary<string, StateStatus> _becameInactiveStatuses = new();

        private readonly Dictionary<object, List<StateSubscription>> _subscriptions = new();
        private readonly Dictionary<object, List<StateSubscription>> _newSubscriptions = new();
        private readonly List<object> _subscribersToRemove = new();

        private bool _updateNeeded;
        private bool _debugMessages;
        
        public StateService(bool debugMessages)
        {
            Ticker.Register(this);
            _stateAccessor = new StateAccessor(this);
            _debugMessages = debugMessages;
        }

        public void LateTick(float deltaTime)
        {
            if (_subscribersToRemove.Count > 0)
            {
                foreach (var subscriber in _subscribersToRemove)
                {
                    _subscriptions.Remove(subscriber);
                }
                _subscribersToRemove.Clear();
            }

            if (_newSubscriptions.Count > 0)
            {
                foreach (var newSubscription in _newSubscriptions)
                {
                    _subscriptions.Add(newSubscription.Key, newSubscription.Value);
                }
                _newSubscriptions.Clear();
            }

            if (!_updateNeeded) return;
            _updateNeeded = false;
                
            var volatileEvents = new Dictionary<string, EventBase>(_currentEvents);
            var volatileActiveStatuses = new Dictionary<string, StateStatus>(_becameActiveStatuses);
            var volatileInactiveStatuses = new Dictionary<string, StateStatus>(_becameInactiveStatuses);

            var counter = 0;
                
            foreach (var kvp in _subscriptions)
            {
                for (var index = kvp.Value.Count - 1; index >= 0; index--)
                {
                    var subscription = kvp.Value[index];
                    if (!subscription.Condition.Invoke(_stateAccessor)) continue;
                    subscription.Callback.Invoke();
                    counter++;
                    if (subscription.DestroyAfterInvoke) kvp.Value.RemoveAt(index);
                }
            }

            if (_debugMessages)
            {
                Debug.Log(
                    $"[State Service] Invoked {counter} events at frame {Time.frameCount}");
            }

            foreach (var kvp in volatileEvents)
            {
                _pastEvents[kvp.Key] = kvp.Value;
                _currentEvents.Remove(kvp.Key);
            }

            foreach (var kvp in volatileActiveStatuses)
            {
                _activeStatuses[kvp.Key] = kvp.Value;
                _becameActiveStatuses.Remove(kvp.Key);
            }

            foreach (var kvp in volatileInactiveStatuses)
            {
                _inactiveStatuses[kvp.Key] = kvp.Value;
                _becameInactiveStatuses.Remove(kvp.Key);
            }
        }

        public void EmitEvent<T>(string key, T payload)
        {
            _updateNeeded = true;
            _currentEvents[key] = new PayloadEvent<T>(payload, Time.frameCount);
            if (_debugMessages)
            {
                Debug.Log(
                    $"[State Service] Emit event \"{key}\" with {typeof(T)} payload at frame {Time.frameCount}");
            }
        }

        public void EmitEvent(string key)
        {
            _updateNeeded = true;
            _currentEvents[key] = new BasicEvent(Time.frameCount);
            if (_debugMessages)
            {
                Debug.Log(
                    $"[State Service] Emit event \"{key}\" at frame {Time.frameCount}");
            }
        }

        public bool IsEventActive(string key)
        {
            return _currentEvents.ContainsKey(key);
        }

        public bool EventPassed(string key)
        {
            return _pastEvents.ContainsKey(key);
        }

        public EventBase GetEvent(string key)
        {
            return _currentEvents.TryGetValue(key, out var evtCurrent)
                ? evtCurrent
                : _pastEvents.GetValueOrDefault(key);
        }

        public T GetPayload<T>(string key) => TryGetPayload(key, out T result) ? result : default;

        public bool TryGetEvent<T>(string key, out PayloadEvent<T> result)
        {
            if (!_currentEvents.TryGetValue(key, out var eventBase) || eventBase is not PayloadEvent<T> payloadEvent)
            {
                result = null;
                return false;
            }

            result = payloadEvent;
            return true;
        }

        public bool TryGetPayload<T>(string key, out T result)
        {
            if (!TryGetEvent<T>(key, out var payloadEvent))
            {
                result = default;
                return false;
            }

            result = payloadEvent.Payload;
            return true;
        }

        public void SetStatus(string key)
        {
            if (_activeStatuses.ContainsKey(key) || _becameActiveStatuses.ContainsKey(key))
                return;
            
            _updateNeeded = true;
            _becameActiveStatuses[key] = new StateStatus(Time.frameCount);
            _inactiveStatuses.Remove(key);
            _becameInactiveStatuses.Remove(key);
            if (_debugMessages)
            {
                Debug.Log(
                    $"[State Service] Set status \"{key}\" at frame {Time.frameCount}");
            }
        }
        
        public void UnsetStatus(string key)
        {
            if (!_activeStatuses.ContainsKey(key) && !_becameActiveStatuses.ContainsKey(key))
                return;
            
            _updateNeeded = true;
            _becameInactiveStatuses[key] = new StateStatus(Time.frameCount);
            _activeStatuses.Remove(key);
            _becameActiveStatuses.Remove(key);
            if (_debugMessages)
            {
                Debug.Log(
                    $"[State Service] Unset status \"{key}\" at frame {Time.frameCount}");
            }
        }

        public bool IsStatusActive(string key)
        {
            return _activeStatuses.ContainsKey(key) || _becameActiveStatuses.ContainsKey(key);
        }

        public bool IsStatusJustBecameActive(string key)
        {
            return _becameActiveStatuses.ContainsKey(key);
        }
        
        public bool IsStatusInactive(string key)
        {
            return _inactiveStatuses.ContainsKey(key) || _becameInactiveStatuses.ContainsKey(key);
        }

        public bool IsStatusJustBecameInactive(string key)
        {
            return _becameInactiveStatuses.ContainsKey(key);
        }

        public StateStatus GetStatus(string key)
        {
            return _activeStatuses.TryGetValue(key, out var statusCurrent)
                ? statusCurrent
                : _inactiveStatuses.TryGetValue(key, out var statusPast)
                    ? statusPast
                    : _becameActiveStatuses.TryGetValue(key, out var statusNew)
                        ? statusNew
                        : _becameInactiveStatuses.GetValueOrDefault(key, null);
        }


        public StateSubscription AddStateSubscription(Predicate<StateAccessor> condition, Action callback,
            object obj = null, bool destroyAfterInvoke = false)
        {
            obj ??= callback.Target;

            if (obj == null)
            {
                Debug.LogError(
                    "[State Service] Trying to add state subscription with callback target == NULL, which is not allowed!!!");
                return null;
            }

            var subscription = new StateSubscription(condition, callback, destroyAfterInvoke);

            if (_subscriptions.TryGetValue(obj, out var subscriptionList))
                subscriptionList.Add(subscription);
            else
            {
                if (_newSubscriptions.TryGetValue(obj, out var newSubsList))
                {
                    newSubsList.Add(subscription);
                }
                else
                {
                    _newSubscriptions.Add(obj, new List<StateSubscription> { subscription });
                }
            }

            if (obj is IUnsubscriber unsubscriber)
            {
                unsubscriber.AddUnsubscription(() => this.RemoveAllSubscriptions(obj));
            }
            
            return subscription;
        }

        public void RemoveSubscription(StateSubscription subscription, object subscriber = null)
        {
            subscriber ??= subscription.Callback.Target;

            if (!_subscriptions.TryGetValue(subscriber, out var subscriptionList)) return;
            subscriptionList.Remove(subscription);
        }

        public void RemoveAllSubscriptions(object subscriber)
        {
            _subscribersToRemove.Add(subscriber);
        }

        private class BasicEvent : EventBase
        {
            public BasicEvent(int emitFrame) : base(emitFrame)
            {
            }
        }
    }
}