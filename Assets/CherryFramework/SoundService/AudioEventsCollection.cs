using System.Collections.Generic;
using TriInspector;
using UnityEngine;

namespace CherryFramework.SoundService
{
    [CreateAssetMenu(menuName = "Space/Audio Events Collection", fileName = "AudioEventsCollection")]
    public class AudioEventsCollection : ScriptableObject
    {
        [ListDrawerSettings(
                Draggable = true,
                HideAddButton = false,
                HideRemoveButton = false,
                AlwaysExpanded = true
        )]
        [HideLabel, LabelText("Audio Settings")]
        public List<AudioEvent> audioEvents = new();
    }
}