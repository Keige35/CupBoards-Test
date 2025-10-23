using System.Collections.Generic;
using UnityEngine;

public class PathfinderService : IPathfinder
{
    private bool _isInitialized = false;

    public void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;
    }

    public void Cleanup()
    {
        _isInitialized = false;
    }

    public List<Node> FindAvailableMoves(Node startNode, BoardState state)
    {
        var available = new List<Node>();
        var visited = new HashSet<Node>();
        var queue = new Queue<Node>();

        queue.Enqueue(startNode);
        visited.Add(startNode);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in current.ConnectedNodes)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);

                    if (neighbor.CurrentPiece == null)
                    {
                        available.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return available;
    }

    public bool IsPathClear(Node start, Node end, BoardState state)
    {
        if (start == end) return true;

        var visited = new HashSet<Node>();
        var queue = new Queue<Node>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == end)
                return true;

            foreach (var neighbor in current.ConnectedNodes)
            {
                if (!visited.Contains(neighbor))
                {
                    if (neighbor.CurrentPiece == null || neighbor == end)
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return false;
    }


    public List<Node> GetAvailableMoves(BasePiece piece, BoardState boardState)
    {
        return FindAvailableMoves(piece.CurrentNode, boardState);
    }

    public List<Node> FindPath(Node startNode, Node targetNode, BasePiece piece)
    {
        var queue = new Queue<Node>();
        var visited = new HashSet<Node>();
        var cameFrom = new Dictionary<Node, Node>();

        queue.Enqueue(startNode);
        visited.Add(startNode);
        cameFrom[startNode] = null;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == targetNode)
            {
                return ReconstructPath(cameFrom, targetNode);
            }

            foreach (var neighbor in current.ConnectedNodes)
            {
                if (!visited.Contains(neighbor))
                {
                    if (neighbor.CurrentPiece == null || neighbor == targetNode || neighbor == startNode)
                    {
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return null;
    }

    public bool CanMoveTo(BasePiece piece, Node targetNode, BoardState boardState)
    {
        if (targetNode == null || piece == null) return false;
        if (targetNode.CurrentPiece != null) return false;
        if (piece.CurrentNode == targetNode) return false;

        return IsPathClear(piece.CurrentNode, targetNode, boardState);
    }

    private List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node endNode)
    {
        var path = new List<Node>();
        var current = endNode;

        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path;
    }
}