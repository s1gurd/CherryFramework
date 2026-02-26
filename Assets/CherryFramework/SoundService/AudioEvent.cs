using System;
using CherryFramework.Utils;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Audio;
using Void = EditorAttributes.Void;

namespace CherryFramework.SoundService
{
    [Serializable]
    public class AudioEvent
    {
        [Validate("Event key cannot be null or empty!",  nameof(EventNameCorrect), MessageMode.Error)]
        public string eventKey;
        public AudioResource audioResource;
        
        [FoldoutGroup("Emitter positioning", 
            nameof(positionToListener), 
            nameof(orientToListener), 
            nameof(freezeTransform))]
        [SerializeField] private Void groupHolder1;
        
        [HideProperty][Clamp(0f,1f)] [HelpBox("At value 0 sound source is positioned at emitter object, at value 1 it is positioned at camera", drawAbove:true)]
        public float positionToListener;
        [HideProperty][Clamp(0f,1f)] [HelpBox("At value 0 sound source is oriented as emitter object, at value 1 it is oriented to camera", drawAbove:true)] 
        public float orientToListener;
        [HideProperty][HelpBox("Controls whether emitter should follow changing transforms of emitter and camera objects or remain static", drawAbove:true)]
        public bool freezeTransform;
        [HideProperty] public bool doNotDeactivateOnStop = false;
        
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
        [SerializeField] private Void groupHolder2;
        
        [HideProperty]public AudioMixerGroup output;
        [HideProperty]public bool mute;
        [HideProperty]public bool bypassEffects;
        [HideProperty]public bool bypassListenerEffects;
        [HideProperty]public bool bypassReverbZones;
        [HideProperty]public bool loop;
        [HideProperty][Range(0f,1f)] public float volume = 1f;
        [HideProperty][Range(0f,3f)] public float pitch = 1f;
        [HideProperty][Range(-1f,1f)] public float panStereo;
        [HideProperty][Range(0f,1f)] public float spatialBlend;
        [HideProperty][Range(0f,1.1f)] public float reverbZoneMix = 1f;
        [HideProperty][Range(0f,5f)] public float dopplerLevel = 1f;
        [HideProperty][Range(0f,360f)] public float spread;
        [HideProperty]public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        [HideProperty][Range(0, 500f)]public float minDistance = 1f;
        [HideProperty][Range(0, 500f)]public float maxDistance = 100f;
        [HideProperty][ShowField(nameof(CurveShow))]public AnimationCurve volumeCurve = new AnimationCurve(new Keyframe(1, 0), new Keyframe(0, 1));

        private bool CurveShow => rolloffMode == AudioRolloffMode.Custom;
        private bool EventNameCorrect => eventKey.IsNullOrWhiteSpace();
    }
}