using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LevelLoader : MonoBehaviour, ILevelLoader
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

    public LevelData LoadLevel(string path)
    {
        try
        {
            TextAsset textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
            {
                throw new FileNotFoundException($"Level file not found: {path}");
            }

            string[] lines = textAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int lineIndex = 0;

            LevelData levelData = new LevelData();

            levelData.pieceCount = int.Parse(lines[lineIndex++]);

            levelData.nodeCount = int.Parse(lines[lineIndex++]);

            levelData.nodePositions = new List<Vector2>();
            for (int i = 0; i < levelData.nodeCount; i++)
            {
                string[] coords = lines[lineIndex++].Split(',');
                float x = float.Parse(coords[0]);
                float y = float.Parse(coords[1]);
                levelData.nodePositions.Add(new Vector2(x, y));
            }

            string[] initialIndices = lines[lineIndex++].Split(',');
            levelData.initialPositions = new List<int>();
            foreach (string indexStr in initialIndices)
            {
                levelData.initialPositions.Add(int.Parse(indexStr));
            }

            string[] targetIndices = lines[lineIndex++].Split(',');
            levelData.targetPositions = new List<int>();
            foreach (string indexStr in targetIndices)
            {
                levelData.targetPositions.Add(int.Parse(indexStr));
            }

            levelData.connectionCount = int.Parse(lines[lineIndex++]);

            levelData.connections = new List<Connection>();
            for (int i = 0; i < levelData.connectionCount; i++)
            {
                string[] connection = lines[lineIndex++].Split(',');
                int from = int.Parse(connection[0]);
                int to = int.Parse(connection[1]);
                levelData.connections.Add(new Connection(from, to));
            }

            return levelData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading level: {ex.Message}");
            return null;
        }
    }

    public LevelData[] GetAvailableLevels()
    {
        return new LevelData[]
        {
            LoadLevel("Levels/level1"),
            LoadLevel("Levels/level2"),
            LoadLevel("Levels/level3")
        };
    }
}