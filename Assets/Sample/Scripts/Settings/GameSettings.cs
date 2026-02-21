using TriInspector;
using UnityEngine;

namespace Sample.Scripts.Settings
{
    [CreateAssetMenu(fileName = "DinoGameSettings", menuName = "Dino Game Sample/Settings file")]
    public class GameSettings : ScriptableObject
    {
        [Title("Environment settings")]
        public float initialGameSpeed = 5f;
        public float speedIncreasePeriod = 5f;
        public float gameSpeedIncrease = 0.5f;
        
        [Title("Player settings")]
        public float gravity=9.8f*2f;
        public float jumpForce=8f;
    }
}