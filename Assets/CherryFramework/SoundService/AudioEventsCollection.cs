using System.Collections.Generic;
using TriInspector;
using UnityEngine;

namespace CherryFramework.SoundService
{
    [CreateAssetMenu(menuName = "Audio/Sound Service/Audio Events Collection", fileName = "AudioEventsCollection")]
    public class AudioEventsCollection : ScriptableObject
    {
        [ListDrawerSettings(
                Draggable = true,
                HideAddButton = false,
                HideRemoveButton = false,
                AlwaysExpanded = true,
                FixDefaultValue = true
        )]
        [HideLabel, LabelText("Audio Settings")]
        public List<AudioEvent> audioEvents;
        
        
    }
}