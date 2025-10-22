public interface ILevelLoader : IGameService
{
    LevelData LoadLevel(string path);
    LevelData[] GetAvailableLevels();
}