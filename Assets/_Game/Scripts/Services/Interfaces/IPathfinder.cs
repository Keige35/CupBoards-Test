using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public interface IPathfinder : IGameService
{
    List<Node> FindAvailableMoves(Node startNode, BoardState state);
    bool IsPathClear(Node start, Node end, BoardState state);
}