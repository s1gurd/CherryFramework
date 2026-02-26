using System.Collections.Generic;
using UnityEngine;

namespace CherryFramework.SoundService
{
    [CreateAssetMenu(menuName = "Audio/Sound Service/Audio Events Collection", fileName = "AudioEventsCollection")]
    public class AudioEventsCollection : ScriptableObject
    {
        public List<AudioEvent> audioEvents;
    }
}