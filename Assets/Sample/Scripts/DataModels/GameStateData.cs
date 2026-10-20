using System;

// Source classes for Data Models are called Templates
// They must be in a namespace ending with DataModels
// When you call Menu - Tools - UnityCodeGen - Generate all the templates are processed
// and resulting Data Models are placed in the generated folder next to the template
namespace Sample.DataModels
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