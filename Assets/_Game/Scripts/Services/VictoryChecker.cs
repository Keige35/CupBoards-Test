using System.Linq;
using UnityEngine;

public class VictoryChecker : IVictoryChecker
{
    private bool _isInitialized = false;

    public void Initialize()
    {
        if (_isInitialized) return;
        Debug.Log("VictoryChecker initialized");
        _isInitialized = true;
    }

    public void Cleanup()
    {
        Debug.Log("VictoryChecker cleaned up");
        _isInitialized = false;
    }

    public bool CheckVictory(LevelData target, BoardState current)
    {
        if (target == null || current == null)
        {
            Debug.LogError("VictoryChecker: Null data provided");
            return false;
        }

        var currentPositions = current.GetCurrentPositions();

        // Детальная отладочная информация
        Debug.Log($"=== VICTORY CHECK ===");
        Debug.Log($"Target positions count: {target.targetPositions.Count}");
        Debug.Log($"Current positions count: {currentPositions.Count}");
        Debug.Log($"Target positions: [{string.Join(", ", target.targetPositions)}]");
        Debug.Log($"Current positions: [{string.Join(", ", currentPositions)}]");

        // Проверяем количество позиций
        if (target.targetPositions.Count != currentPositions.Count)
        {
            Debug.LogError($"VictoryChecker: Position count mismatch! Target: {target.targetPositions.Count}, Current: {currentPositions.Count}");
            return false;
        }

        bool victory = target.targetPositions.SequenceEqual(currentPositions);
        Debug.Log($"SequenceEqual result: {victory}");

        // Альтернативная проверка (если порядок не важен)
        bool sortedVictory = target.targetPositions.OrderBy(x => x).SequenceEqual(currentPositions.OrderBy(x => x));
        Debug.Log($"Sorted check result: {sortedVictory}");

        Debug.Log($"=== END CHECK ===");

        return victory;
    }
}