using System;

// Source classes for Data Models are called Templates
// They must be in a namespace ending with DataModels.Templates
// When you call Menu - Tools - UnityGodeGen - Generate all the templates are processed
// and resulting Data Models are placed in Assets/GeneratedDataModels folder
namespace Sample.DataModels.Templates
{
    [Serializable]
    public class GameStatistics
    {
        public bool GameRunning;
        public int MaxDistance;
        public int TotalRunTime;
        public int TotalDistance;
        public int TriesNum ;
    }
}