public interface IVictoryChecker : IGameService
{
    bool CheckVictory(LevelData target, BoardState current);
}