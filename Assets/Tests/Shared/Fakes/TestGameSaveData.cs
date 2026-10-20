using CherryFramework.SaveGameManager;

namespace CherryFramework.Tests.Fakes
{
    public class TestGameSaveData : IGameSaveData
    {
        [SaveGameData] public int Health;
        [SaveGameData] public string Title = string.Empty;

        public int LoadCount { get; private set; }
        public int SaveCount { get; private set; }

        public void OnAfterLoad() => LoadCount++;
        public void OnAfterSave() => SaveCount++;
    }
}
