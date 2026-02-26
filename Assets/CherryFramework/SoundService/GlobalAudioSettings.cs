using UnityEngine;

namespace CherryFramework.SoundService
{
    [CreateAssetMenu(menuName = "Audio/Sound Service/Settings", fileName = "AudioSettings")]
    public class GlobalAudioSettings : ScriptableObject
    {
        [Header("Sound Emitting settings")]
        public AudioEmitter emitterSample;
        public float defaultFadeDuration = 0.5f;
    }
}