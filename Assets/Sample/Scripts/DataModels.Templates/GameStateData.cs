using Newtonsoft.Json;

namespace Sample.Scripts.DataModels.Templates
{
    public class GameStateData
    {
        public float GameSpeed;
        [JsonIgnore] public bool PlayerDead;
    }
}