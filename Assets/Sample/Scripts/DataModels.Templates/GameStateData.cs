using System;

// Source classes for Data Models are called Templates
// They must be in a namespace ending with DataModels.Templates
// When you call Menu - Tools - UnityGodeGen - Generate all the templates are processed
// and resulting Data Models are placed in Assets/GeneratedDataModels folder
namespace Sample.DataModels.Templates
{
    [Serializable]
    public class GameStateData
    {
        public float GameSpeed;
        public float JumpForce;
        public bool PlayerDead;

        public int DistanceTraveled;
        public int RunTime;
    }
}