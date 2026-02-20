using System;
using CherryFramework.Utils;
using TriInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace CherryFramework.SoundService
{
    [Serializable]
    [DeclareFoldoutGroup("emitter", Title = "Emitter Settings")]
    [DeclareFoldoutGroup("clip", Title = "Clip Settings")]
    public class AudioEvent
    { 
        [Title("Event description")][ValidateInput(nameof(EventNameCorrect))]
        public string eventKey;
        
        [Title("Emitter positioning")] 
        [Group("emitter")][Range(0f,1f)] [InfoBox("At value 0 sound source is oriented as emitter object, at value 1 it is oriented to camera")] 
        public float orientToListener = 0f;
        [Group("emitter")][Range(0f,1f)] [InfoBox("At value 0 sound source is positioned at emitter object, at value 1 it is positioned at camera")]
        public float positionToListener = 0f;
        [Group("emitter")][InfoBox("Controls whether emitter should follow changing transforms of emitter and camera objects or remain static")]
        public bool freezeTransform = false;
        [Group("emitter")] public bool doNotDeactivateOnStop = false;
        
        [Title("Audio clip component settings")]
        [Group("clip")]public AudioResource audioResource;
        [Group("clip")]public AudioMixerGroup output;
        [Group("clip")]public bool mute;
        [Group("clip")]public bool bypassEffects;
        [Group("clip")]public bool bypassListenerEffects;
        [Group("clip")]public bool bypassReverbZones;
        [Group("clip")]public bool loop;
        [Group("clip")][Range(0f,1f)] public float volume = 1f;
        [Group("clip")][Range(0f,3f)] public float pitch = 1f;
        [Group("clip")][Range(-1f,1f)] public float panStereo = 0f;
        [Group("clip")][Range(0f,1f)] public float spatialBlend = 0f;
        [Group("clip")][Range(0f,1.1f)] public float reverbZoneMix = 1f;
        [Group("clip")][Range(0f,5f)] public float dopplerLevel = 1f;
        [Group("clip")][Range(0f,360f)] public float spread = 0f;
        [Group("clip")]public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        [Group("clip")][Range(0, 500f)]public float minDistance = 1f;
        [Group("clip")][Range(0, 500f)]public float maxDistance = 100f;
        [Group("clip")][ShowIf(nameof(CurveShow))]public AnimationCurve volumeCurve = new AnimationCurve(new Keyframe(1, 0), new Keyframe(0, 1));

        private bool CurveShow => rolloffMode == AudioRolloffMode.Custom;
        private bool EventNameCorrect => !eventKey.IsNullOrWhiteSpace();
    }
}