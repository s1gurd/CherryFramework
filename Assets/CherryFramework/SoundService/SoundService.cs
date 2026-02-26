using System;
using System.Collections.Generic;
using System.Linq;
using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using CherryFramework.SimplePool;
using UnityEngine;

namespace CherryFramework.SoundService
{
    public class SoundService : GeneralClassBase
    {
        private GlobalAudioSettings _audioSettings;
        private readonly Dictionary<string, AudioEvent> _events = new ();
        private SimplePool<AudioEmitter> _emitters = new ();
        private ListenerCamera _camera;

        private uint _currentHandler = 0;

        public SoundService(GlobalAudioSettings globalAudioSettings, AudioEventsCollection audioEvents, Camera camera = null) : this(globalAudioSettings, new List<AudioEventsCollection> { audioEvents }, camera)
        {
        }

        public SoundService(GlobalAudioSettings globalAudioSettings, List<AudioEventsCollection> settingsCollection, Camera camera = null)
        {
            foreach (var settings in settingsCollection)
            {
                foreach (var evt in settings.audioEvents)
                {
                    _events.Add(evt.eventKey, evt);
                }
            }
            
            _audioSettings = globalAudioSettings;
            DependencyContainer.Instance.BindAsSingleton(_audioSettings);

            if (camera)
            {
                _camera = new ListenerCamera(camera);
            }
            else if (DependencyContainer.Instance.HasDependency<Camera>())
            {
                _camera = new ListenerCamera(DependencyContainer.Instance.GetInstance<Camera>());
            }
            else
            {
                _camera = new ListenerCamera(Camera.main);
            }
            DependencyContainer.Instance.BindAsSingleton(_camera);
        }

        public uint Play(string eventName, Transform emitter, float delay = 0f, Action onPlayEnd = null)
        {
            if (!_events.TryGetValue(eventName, out var evt))
            {
                Debug.LogError($"[Sound System] No sound event with the name {eventName} found!");
                return 0;
            }
            
            var sound = _emitters.Get(_audioSettings.emitterSample);
            _currentHandler++;
            sound.PlayEvent(evt, emitter, delay, _currentHandler, onPlayEnd);
            return _currentHandler;
        }

        public uint FadeIn(string eventName, Transform emitter, float fadeDuration = 0f, float delay = 0f,
            Action onPlayEnd = null)
        {
            if (!_events.TryGetValue(eventName, out var evt))
            {
                Debug.LogError($"[Sound System] No sound event with the name {eventName} found!");
                return 0;
            }
            
            var sound = _emitters.Get(_audioSettings.emitterSample);
            _currentHandler++;
            sound.FadeIn(evt, emitter, delay, _currentHandler, fadeDuration, onPlayEnd);
            return _currentHandler;
        }

        public void Stop(uint handler)
        {
            var emitter = GetEmitter(handler);

            if (!emitter)
            {
                Debug.LogError($"[Sound System] No emitter with handler {handler} found while trying to Stop!");
                return;
            }
            
            emitter.Stop();
        }

        public void FadeOut(uint handler, float duration = 0f)
        {
            var emitter = GetEmitter(handler);

            if (emitter == null)
            {
                Debug.LogError($"[Sound System] No emitter with handler {handler} found while trying to FadeOut!");
                return;
            }
            
            emitter.FadeOut(duration);
        }

        public void StopAll()
        {
            var sounds = _emitters.ActiveObjects(_audioSettings.emitterSample);
            foreach (var sound in sounds)
            {
                sound.Stop();
            }
            _emitters.Clear();
        }
        
        public AudioEmitter GetEmitter(uint handler)
        {
            return _emitters.ActiveObjects(_audioSettings.emitterSample).FirstOrDefault(e => e.CurrentHandler == handler);
        }

        public IEnumerable<AudioEmitter> GetEmitters(string eventKey)
        {
            return _emitters.ActiveObjects(_audioSettings.emitterSample).Where(e => e.EventKey == eventKey);
        }

        public bool IsPlaying(uint handler)
        {
            var e = GetEmitter(handler);
            return e && e.Source.isPlaying;
        }

        public override void Dispose()
        {
            DependencyContainer.Instance.RemoveDependency(typeof(GlobalAudioSettings));
            DependencyContainer.Instance.RemoveDependency(typeof(ListenerCamera));
            base.Dispose();
        }
    }

    internal class ListenerCamera
    {
        public ListenerCamera(Camera cam)
        {
            Camera = cam;
        }
        public Camera Camera { get; }
    }
}