using TriInspector;
using UnityEngine;

namespace CherryFramework.SoundService
{
    [CreateAssetMenu(menuName = "Space/Audio Settings", fileName = "AudioSettings")]
    public class GlobalAudioSettings : ScriptableObject
    {
        [Title("Sound Emitting settings")]
        public AudioEmitter emitterSample;
        public float defaultFadeDuration = 0.5f;
    }
}