using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour, IGameService
{
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private LineRenderer connectionLinePrefab;
    [SerializeField] private Color connectionColor = Color.gray;
    [SerializeField] private float connectionWidth = 0.1f;

    private List<Node> nodes = new List<Node>();
    private List<BasePiece> pieces = new List<BasePiece>();
    private List<LineRenderer> connectionLines = new List<LineRenderer>();
    private BoardState boardState;
    private bool isInitialized = false;

    private void Awake()
    {
        nodePrefab = Resources.Load<GameObject>("Prefabs/Node");
        GameObject linePrefab = Resources.Load<GameObject>("Prefabs/ConnectionLine");
        connectionLinePrefab = linePrefab.GetComponent<LineRenderer>();
    }

    public List<Node> Nodes => nodes;
    public List<BasePiece> Pieces => pieces;

    public void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;
    }

    public void Cleanup()
    {
        ClearBoard();
        isInitialized = false;
    }

    public void CreateBoard(LevelData levelData)
    {
        ClearBoard();

        if (levelData == null)
        {
            Debug.LogError("LevelData is null!");
            return;
        }


        for (int i = 0; i < levelData.nodeCount; i++)
        {
            if (i >= levelData.nodePositions.Count)
            {
                break;
            }

            GameObject nodeObj = Instantiate(nodePrefab, transform);
            Node node = nodeObj.GetComponent<Node>();
            node.Initialize(i, levelData.nodePositions[i]);
            nodes.Add(node);
        }


        foreach (Connection connection in levelData.connections)
        {
            if (connection.from >= nodes.Count || connection.to >= nodes.Count)
            {
                continue;
            }

            Node fromNode = nodes[connection.from];
            Node toNode = nodes[connection.to];

            fromNode.AddConnection(toNode);
            toNode.AddConnection(fromNode);

            CreateConnectionVisual(fromNode, toNode);
        }

        IPieceFactory pieceFactory = ServiceLocator.Instance.Get<IPieceFactory>();
        for (int i = 0; i < levelData.initialPositions.Count; i++)
        {
            if (i >= levelData.initialPositions.Count)
            {
                break;
            }

            int nodeIndex = levelData.initialPositions[i];
            if (nodeIndex >= nodes.Count)
            {
                continue;
            }

            Node node = nodes[nodeIndex];
            Color color = Color.HSVToRGB(i / (float)levelData.initialPositions.Count, 0.8f, 0.9f);
            BasePiece piece = pieceFactory.CreatePiece(i, color, node);

            if (piece != null)
            {
                pieces.Add(piece);
            }
            else
            {
                Debug.LogError($"Failed to create piece {i}");
            }
        }

        boardState = new BoardState(pieces, nodes);
    }

    private void CreateConnectionVisual(Node fromNode, Node toNode)
    {
        if (fromNode == null || toNode == null)
        {
            return;
        }

        LineRenderer lineRenderer;

        if (connectionLinePrefab != null)
        {
            lineRenderer = Instantiate(connectionLinePrefab, transform);
        }
        else
        {
            GameObject lineObject = new GameObject($"Connection_{fromNode.Id}_{toNode.Id}");
            lineObject.transform.SetParent(transform);

            lineRenderer = lineObject.AddComponent<LineRenderer>();
            SetupDefaultLineRenderer(lineRenderer);
        }

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, fromNode.transform.position);
        lineRenderer.SetPosition(1, toNode.transform.position);

        connectionLines.Add(lineRenderer);
    }

    private void SetupDefaultLineRenderer(LineRenderer lineRenderer)
    {
        lineRenderer.startWidth = connectionWidth;
        lineRenderer.endWidth = connectionWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = connectionColor;
        lineRenderer.endColor = connectionColor;

        lineRenderer.alignment = LineAlignment.TransformZ;
        lineRenderer.textureMode = LineTextureMode.Tile;

        if (lineRenderer.material == null)
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.material.color = connectionColor;
        }
    }

    public BoardState GetBoardState()
    {
        return boardState;
    }

    // ДОБАВЛЕНО: Метод для обновления состояния доски
    public void UpdateBoardState()
    {
        boardState = new BoardState(pieces, nodes);
    }

    public void MovePiece(BasePiece piece, Node targetNode)
    {
        if (piece == null)
        {
            return;
        }

        if (targetNode == null)
        {
            return;
        }

        if (piece.CanMoveTo(targetNode))
        {
            StartCoroutine(piece.MoveTo(targetNode, 0.5f));
        }
        else
        {
            Debug.LogWarning($"Cannot move piece {piece.PieceId} to node {targetNode.Id}");
        }
    }

    private void ClearBoard()
    {
        foreach (var piece in pieces)
        {
            if (piece != null && piece.gameObject != null)
                Destroy(piece.gameObject);
        }
        pieces.Clear();

        foreach (var node in nodes)
        {
            if (node != null && node.gameObject != null)
                Destroy(node.gameObject);
        }
        nodes.Clear();

        foreach (var line in connectionLines)
        {
            if (line != null && line.gameObject != null)
                Destroy(line.gameObject);
        }
        connectionLines.Clear();
    }

    public Node GetNodeAtPosition(Vector2 worldPosition, float radius = 1f)
    {
        foreach (var node in nodes)
        {
            if (node != null && Vector2.Distance(worldPosition, node.transform.position) <= radius)
                return node;
        }
        return null;
    }

    public BasePiece GetPieceAtPosition(Vector2 worldPosition, float radius = 1.2f)
    {
        foreach (var piece in pieces)
        {
            if (piece != null && Vector2.Distance(worldPosition, piece.transform.position) <= radius)
                return piece;
        }
        return null;
    }

    public Node GetNodeById(int id)
    {
        foreach (var node in nodes)
        {
            if (node != null && node.Id == id)
                return node;
        }
        return null;
    }
}