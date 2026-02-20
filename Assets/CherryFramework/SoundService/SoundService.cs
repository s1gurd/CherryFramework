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
        [Inject] private readonly Camera _camera;
        [Inject] private readonly GlobalAudioSettings _audioSettings;
        [Inject] private readonly StateService.StateService _stateService;
        
        private readonly Dictionary<string, AudioEvent> _events = new ();
        private SimplePool<AudioEmitter> _emitters = new ();

        private uint _currentHandler = 0;
        
        public SoundService(List<AudioEventsCollection> settingsCollection)
        {
            foreach (var settings in settingsCollection)
            {
                foreach (var evt in settings.audioEvents)
                {
                    _events.Add(evt.eventKey, evt);
                }
            }

            var listener = _camera.gameObject.GetComponent<AudioListener>();

            if (!listener)
            {
                throw new Exception("[Sound System] No audio listener is attached to camera!!! Aborting...");
            }
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
    }
}