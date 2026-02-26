using System;
using CherryFramework.BaseClasses;
using CherryFramework.DependencyManager;
using CherryFramework.TickDispatcher;
using CherryFramework.Utils;
using DG.Tweening;
using EditorAttributes;
using UnityEngine;

namespace CherryFramework.SoundService
{
    public class AudioEmitter : BehaviourBase, ITickable
    {
        [SerializeField] private AudioSource source;

        [Inject] private readonly GlobalAudioSettings _globalSettings;
        [Inject] private readonly ListenerCamera _camera;

        [ShowInInspector] [ReadOnly] public uint CurrentHandler { get; private set; }
        [ShowInInspector] [ReadOnly] public string EventKey { get; private set; }
        [HideInInspector] public AudioSource Source => source;
        
        private Ticker _ticker;
        
        private Transform _emitterTransform;
        private float _moveRatio;
        private float _orientRatio;
        private bool _follow;
        private bool _persist;
        private bool _isPlaying;
        private bool _isWaiting;
        private float _volume;
        private Action _onStop;

        private void Start()
        {
            _ticker = new Ticker();
            _ticker.Register(this);
        }

        public void Tick(float deltaTime)
        {
            if (!_isPlaying || _isWaiting)
                return;
            
            if (!source.isPlaying && !_isWaiting && !_persist)
            {
                _isPlaying = false;
                _onStop?.Invoke();
                gameObject.SetActive(false);
                return;
            }

            if (_follow)
            {
                SetTransformation();
            }
        }

        public void PlayEvent(AudioEvent evt, Transform emitter, float delay, uint handler,
            Action onPlayEnd = null)
        {
            PlayEventImpl(evt, emitter, delay, handler, null, onPlayEnd);
        }
        
        private void PlayEventImpl(AudioEvent evt, Transform emitter, float delay, uint handler, Action onPlayStart = null,
            Action onPlayEnd = null)
        {
            gameObject.SetActive(true);
            CurrentHandler = handler;
            EventKey = evt.eventKey;

            _volume = evt.volume;

            _onStop = onPlayEnd;

            _emitterTransform = emitter;
            _moveRatio = evt.positionToListener;
            _orientRatio = evt.orientToListener;
            _follow = !evt.freezeTransform;
            _persist = evt.doNotDeactivateOnStop;
            
            SetTransformation();

            if (_follow)
            {
                if (_moveRatio == 0f && _orientRatio == 0f)
                {
                    transform.parent = _emitterTransform;
                    _follow = false;
                }

                if (_moveRatio >= 1f && _orientRatio >= 1f)
                {
                    transform.parent = _camera.Camera.transform;
                    _follow = false;
                }
            }
            
            source.volume = _volume;

            source.resource = evt.audioResource;
            source.outputAudioMixerGroup = evt.output;
            source.mute = evt.mute;
            source.bypassEffects = evt.bypassEffects;
            source.bypassListenerEffects = evt.bypassListenerEffects;
            source.bypassReverbZones = evt.bypassReverbZones;
            source.loop = evt.loop;
            source.pitch = evt.pitch;
            source.panStereo = evt.panStereo;
            source.spatialBlend = evt.spatialBlend;
            source.reverbZoneMix = evt.reverbZoneMix;
            source.dopplerLevel = evt.dopplerLevel;
            source.spread = evt.spread;
            source.rolloffMode = evt.rolloffMode;
            source.minDistance = evt.minDistance;
            source.maxDistance = evt.maxDistance;

            if (evt.rolloffMode == AudioRolloffMode.Custom)
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, evt.volumeCurve);

            
            var seq = DOTween.Sequence();
            seq.PrependInterval(delay);
            seq.AppendCallback(() =>
            {
                if (!emitter.SafeIsUnityNull() && emitter.gameObject.activeInHierarchy)
                {
                    PlayStart(evt, emitter, delay, handler, onPlayStart);
                }
            });
            _isWaiting = delay > 0f;
            gameObject.name = $"Sound - {evt.eventKey}";
        }

        public void Stop(float delay = 0f)
        {
            var sequence = DOTween.Sequence();
            sequence.PrependInterval(delay);
            sequence.AppendCallback(() =>
            {
                if (!source.SafeIsUnityNull() && !this.SafeIsUnityNull()) source.Stop();
            });
        }

        public void Pause(float delay = 0f)
        {
            var sequence = DOTween.Sequence();
            sequence.PrependInterval(delay);
            sequence.AppendCallback(() =>
            {
                if (!source.SafeIsUnityNull())
                {
                    _isWaiting = true;
                    source?.Pause();
                }
            });
        }
        
        public void Resume(float delay = 0f)
        {
            var sequence = DOTween.Sequence();
            sequence.PrependInterval(delay);
            sequence.AppendCallback(() =>
            {
                _isWaiting = false;
                source.UnPause();
            });
        }

        public void FadeIn(AudioEvent evt, Transform emitter, float delay, uint handler, float fadeInDuration,
            Action onPlayEnd = null)
        {
            if (fadeInDuration == 0f)
            {
                fadeInDuration = _globalSettings.defaultFadeDuration;
            }
            PlayEventImpl(evt, emitter, delay, handler, OnStart, onPlayEnd);

            return;
            
            void OnStart()
            {
                source.volume = 0f;
                source.DOFade(_volume, fadeInDuration);
            }
        }

        public void FadeIn(AudioEvent evt, Transform emitter, float delay, uint handler, Action onPlayEnd = null)
        {
            FadeIn(evt, emitter, delay, handler, _globalSettings.defaultFadeDuration, onPlayEnd);
        }

        public void FadeIn(AudioEvent evt, Transform emitter, uint handler, Action onPlayEnd = null)
        {
            FadeIn(evt, emitter, 0f, handler, _globalSettings.defaultFadeDuration, onPlayEnd);
        }

        public void FadeOut(float fadeOutDuration, float delay)
        {
            if (!_isPlaying)
            {
                Debug.LogWarning("[AudioEmitter] Can't fade out while not playing.");
                return;
            }

            if (fadeOutDuration == 0f)
            {
                fadeOutDuration = _globalSettings.defaultFadeDuration;
            }
            
            var seq = DOTween.Sequence();
            seq.PrependInterval(delay);
            seq.Append(source.DOFade(0f, fadeOutDuration));
            seq.AppendCallback(() => source.Stop());
        }

        public void FadeOut(float delay = 0f)
        {
            FadeOut(_globalSettings.defaultFadeDuration, delay);
        }

        public void AppendOnStopCallback(Action callback)
        {
            _onStop += callback;
        }

        public void RemoveOnStopCallback(Action callback)
        {
            _onStop -= callback;
        }
        
        public void ClearOnStopCallback()
        {
            _onStop = null;
        }

        private void PlayStart(AudioEvent evt, Transform emitter, float delay, uint handler, Action onPlayStart)
        {
            _isPlaying = true;
            _isWaiting = false;

            source.Play();
            onPlayStart?.Invoke();
        }

        private void SetTransformation()
        {
            transform.position = Vector3.Lerp(_emitterTransform.position, _camera.Camera.transform.position, _moveRatio);
            var camDir = Quaternion.LookRotation(_camera.Camera.transform.position - _emitterTransform.position, Vector3.up);
            transform.rotation =
                Quaternion.Lerp(_emitterTransform.rotation, _camera.Camera.transform.rotation, _orientRatio);
        }
    }
}