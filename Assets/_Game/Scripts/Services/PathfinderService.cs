using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PathfinderService : IPathfinder
{
    private bool _isInitialized = false;

    public void Initialize()
    {
        if (_isInitialized) return;
        Debug.Log("PathfinderService initialized");
        _isInitialized = true;
    }

    public void Cleanup()
    {
        Debug.Log("PathfinderService cleaned up");
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
}