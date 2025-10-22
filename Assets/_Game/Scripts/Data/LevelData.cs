using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelData
{
    public int pieceCount;
    public int nodeCount;
    public List<Vector2> nodePositions;
    public List<int> initialPositions;
    public List<int> targetPositions;
    public int connectionCount;
    public List<Connection> connections;
}

[Serializable]
public class Connection
{
    public int from;
    public int to;

    public Connection(int from, int to)
    {
        this.from = from;
        this.to = to;
    }
}
