using CherryFramework.SaveGameManager;
using UnityEngine;

namespace Sample.Settings
{
    [CreateAssetMenu(fileName = "DinoGameSettings", menuName = "Dino Game Sample/Settings file")]
    public class GameSettings : ScriptableObject
    {
        [Header("Environment settings")]
        public float initialGameSpeed = 5f;
        public float speedIncreasePeriod = 5f;
        public float gameSpeedIncrease = 0.5f;
        
        [Header("Player settings")]
        public float gravity=9.8f*2f;
        public float jumpForce=8f;
        
        [Header("Obstacle spawning")]
        public SpawnableObject[] spawnObjects;
        public float minSpawnRate = 1f;
        public float maxSpawnRate = 2f;

        [Header("Notification settings")] 
        public float notificationShowTime = 1.5f;
        
        [Header("Rocket power up settings")]
        public float powerUpLifetime = 8f;
        public float jumpForceMultiplier = 1.6f;
    }
    
    [System.Serializable]
    public struct SpawnableObject{
        public PersistentObject source;
        [Range(0,100)]
        public int spawnChance;
    }
}