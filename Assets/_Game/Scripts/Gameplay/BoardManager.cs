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
        GameObject linePrefab  = Resources.Load<GameObject>("Prefabs/ConnectionLine");
        connectionLinePrefab = linePrefab.GetComponent<LineRenderer>();
    }
    public List<Node> Nodes => nodes;
    public List<BasePiece> Pieces => pieces;

    public void Initialize()
    {
        if (isInitialized) return;
        Debug.Log("BoardManager initialized");
        isInitialized = true;
    }

    public void Cleanup()
    {
        ClearBoard();
        Debug.Log("BoardManager cleaned up");
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

        Debug.Log($"Creating board with {levelData.nodeCount} nodes and {levelData.connections.Count} connections");

        for (int i = 0; i < levelData.nodeCount; i++)
        {
            if (i >= levelData.nodePositions.Count)
            {
                Debug.LogError($"Not enough node positions defined! Expected {levelData.nodeCount}, got {levelData.nodePositions.Count}");
                break;
            }

            GameObject nodeObj = Instantiate(nodePrefab, transform);
            Node node = nodeObj.GetComponent<Node>();
            node.Initialize(i, levelData.nodePositions[i]);
            nodes.Add(node);
        }

        Debug.Log($"Created {nodes.Count} nodes");

        foreach (Connection connection in levelData.connections)
        {
            if (connection.from >= nodes.Count || connection.to >= nodes.Count)
            {
                Debug.LogWarning($"Invalid connection indices: {connection.from} -> {connection.to}. Nodes count: {nodes.Count}");
                continue;
            }

            Node fromNode = nodes[connection.from];
            Node toNode = nodes[connection.to];

            fromNode.AddConnection(toNode);
            toNode.AddConnection(fromNode);

            CreateConnectionVisual(fromNode, toNode);
        }

        Debug.Log($"Created {connectionLines.Count} connection visuals");

        IPieceFactory pieceFactory = ServiceLocator.Instance.Get<IPieceFactory>();
        for (int i = 0; i < levelData.initialPositions.Count; i++)
        {
            if (i >= levelData.initialPositions.Count)
            {
                Debug.LogError($"Not enough initial positions defined! Expected {levelData.pieceCount}, got {levelData.initialPositions.Count}");
                break;
            }

            int nodeIndex = levelData.initialPositions[i];
            if (nodeIndex >= nodes.Count)
            {
                Debug.LogError($"Invalid node index {nodeIndex} for piece {i}. Nodes count: {nodes.Count}");
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

        Debug.Log($"Created {pieces.Count} pieces");

        boardState = new BoardState(pieces, nodes);
        Debug.Log("Board creation completed successfully");
    }

    private void CreateConnectionVisual(Node fromNode, Node toNode)
    {
        if (fromNode == null || toNode == null)
        {
            Debug.LogError("Cannot create connection visual: one of the nodes is null");
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

    public void MovePiece(BasePiece piece, Node targetNode)
    {
        if (piece == null)
        {
            Debug.LogError("Cannot move null piece");
            return;
        }

        if (targetNode == null)
        {
            Debug.LogError("Cannot move to null node");
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

        Debug.Log("Board cleared");
    }

    public Node GetNodeAtPosition(Vector2 worldPosition, float radius = 0.5f)
    {
        foreach (var node in nodes)
        {
            if (node != null && Vector2.Distance(worldPosition, node.transform.position) <= radius)
                return node;
        }
        return null;
    }

    public BasePiece GetPieceAtPosition(Vector2 worldPosition, float radius = 0.5f)
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

    [ContextMenu("Debug Board Info")]
    public void DebugBoardInfo()
    {
        Debug.Log($"=== BOARD DEBUG INFO ===");
        Debug.Log($"Nodes: {nodes.Count}");
        foreach (var node in nodes)
        {
            if (node != null)
            {
                string pieceInfo = node.CurrentPiece != null ? $"Piece: {node.CurrentPiece.PieceId}" : "Empty";
                Debug.Log($"Node {node.Id} at {node.transform.position} - {pieceInfo}");
            }
        }

        Debug.Log($"Pieces: {pieces.Count}");
        foreach (var piece in pieces)
        {
            if (piece != null)
            {
                Debug.Log($"Piece {piece.PieceId} at Node {piece.CurrentNode?.Id}");
            }
        }

        Debug.Log($"Connection Lines: {connectionLines.Count}");
        Debug.Log($"=== END DEBUG INFO ===");
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.blue;
        foreach (var node in nodes)
        {
            if (node != null)
            {
                Gizmos.DrawWireSphere(node.transform.position, 0.2f);
            }
        }

        Gizmos.color = Color.green;
        foreach (var node in nodes)
        {
            if (node != null)
            {
                foreach (var connectedNode in node.ConnectedNodes)
                {
                    if (connectedNode != null)
                    {
                        Gizmos.DrawLine(node.transform.position, connectedNode.transform.position);
                    }
                }
            }
        }
    }
}