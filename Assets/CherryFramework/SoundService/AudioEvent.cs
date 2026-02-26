using System;
using CherryFramework.Utils;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Audio;
using Void = System.Void;

namespace CherryFramework.SoundService
{
    [Serializable]
    public class AudioEvent
    {
        [SerializeField, MessageBox("Event key cannot be null or empty!", nameof(EventNameCorrect), MessageMode.Error)]
        private EditorAttributes.Void messageBoxError;
       
        public string eventKey;
        public AudioResource audioResource;
        
        [FoldoutGroup("Emitter positioning", 
            nameof(positionToListener), 
            nameof(orientToListener), 
            nameof(freezeTransform))]
        [SerializeField] private EditorAttributes.Void groupHolder1;
        
        [Range(0f,1f)] [HelpBox("At value 0 sound source is positioned at emitter object, at value 1 it is positioned at camera")]
        public float positionToListener;
        [Range(0f,1f)] [HelpBox("At value 0 sound source is oriented as emitter object, at value 1 it is oriented to camera")] 
        public float orientToListener;
        [HelpBox("Controls whether emitter should follow changing transforms of emitter and camera objects or remain static")]
        public bool freezeTransform;
        public bool doNotDeactivateOnStop = false;
        
        [FoldoutGroup("Audio clip component settings", 
            nameof(output), 
            nameof(mute), 
            nameof(bypassEffects),
            nameof(bypassListenerEffects),
            nameof(bypassReverbZones),
            nameof(loop),
            nameof(volume),
            nameof(pitch),
            nameof(panStereo),
            nameof(spatialBlend),
            nameof(reverbZoneMix),
            nameof(dopplerLevel),
            nameof(spread),
            nameof(rolloffMode),
            nameof(minDistance),
            nameof(maxDistance),
            nameof(volumeCurve))]
        [SerializeField] private EditorAttributes.Void groupHolder2;

        
        [Title("Audio clip component settings")]
        public AudioMixerGroup output;
        public bool mute;
        public bool bypassEffects;
        public bool bypassListenerEffects;
        public bool bypassReverbZones;
        public bool loop;
        [Range(0f,1f)] public float volume = 1f;
        [Range(0f,3f)] public float pitch = 1f;
        [Range(-1f,1f)] public float panStereo;
        [Range(0f,1f)] public float spatialBlend;
        [Range(0f,1.1f)] public float reverbZoneMix = 1f;
        [Range(0f,5f)] public float dopplerLevel = 1f;
        [Range(0f,360f)] public float spread;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        [Range(0, 500f)]public float minDistance = 1f;
        [Range(0, 500f)]public float maxDistance = 100f;
        [ShowField(nameof(CurveShow))]public AnimationCurve volumeCurve = new AnimationCurve(new Keyframe(1, 0), new Keyframe(0, 1));

        private bool CurveShow => rolloffMode == AudioRolloffMode.Custom;
        private bool EventNameCorrect => !eventKey.IsNullOrWhiteSpace();
    }
}