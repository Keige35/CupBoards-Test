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
        var currentPositions = current.GetCurrentPositions();
        return target.targetPositions.SequenceEqual(currentPositions);
    }
}