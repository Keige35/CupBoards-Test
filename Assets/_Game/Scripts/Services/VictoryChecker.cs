using System.Linq;
using UnityEngine;

public class VictoryChecker : IVictoryChecker
{
    private bool _isInitialized = false;

    public void Initialize()
    {
        if (_isInitialized) return;
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
            return false;
        }

        var currentPositions = current.GetCurrentPositions();

        if (target.targetPositions.Count != currentPositions.Count)
        {
            return false;
        }

        bool victory = target.targetPositions.SequenceEqual(currentPositions);

        return victory;
    }
}