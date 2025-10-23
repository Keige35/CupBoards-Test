using System.Collections.Generic;
using UnityEngine;

public class VictoryPreviewBoard : MonoBehaviour
{
    [Header("Preview Settings")]
    [SerializeField] private string levelPath = "Levels/level1"; // Исправлено: тот же уровень что и в GameManager
    [SerializeField] private float previewScale = 1f;
    [SerializeField] private Vector2 previewOffset = new Vector2(50f, 0);
    [SerializeField] private Color connectionColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
    [SerializeField] private float connectionWidth = 0.03f;

    [Header("Prefabs (Optional)")]
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private GameObject piecePrefab;

    private List<GameObject> previewNodes = new List<GameObject>();
    private List<GameObject> previewPieces = new List<GameObject>();
    private List<LineRenderer> connectionLines = new List<LineRenderer>();
    private LevelData levelData;

    private void Start()
    {
        InitializePreview();
    }

    private void InitializePreview()
    {
        LoadLevelData();
        CreatePreviewBoard();
        SetupDisplay();
    }

    private void LoadLevelData()
    {
        ILevelLoader levelLoader = CreateTemporaryLevelLoader();
        levelData = levelLoader.LoadLevel(levelPath);

        if (levelData == null)
        {
            Debug.LogError($"VictoryPreviewBoard: Failed to load level from {levelPath}");
            return;
        }

        Debug.Log($"VictoryPreviewBoard: Loaded level with {levelData.nodeCount} nodes and {levelData.targetPositions.Count} target positions");
    }

    private ILevelLoader CreateTemporaryLevelLoader()
    {
        GameObject tempLoaderObj = new GameObject("TempLevelLoader");
        LevelLoader levelLoader = tempLoaderObj.AddComponent<LevelLoader>();
        return levelLoader;
    }

    private void CreatePreviewBoard()
    {
        if (levelData == null) return;

        ClearPreview();
        CreatePreviewNodes();
        CreatePreviewConnections();
        CreatePreviewPieces();
    }

    private void CreatePreviewNodes()
    {
        for (int i = 0; i < levelData.nodeCount; i++)
        {
            if (i >= levelData.nodePositions.Count) break;

            Vector2 scaledPosition = levelData.nodePositions[i] * previewScale + previewOffset;
            GameObject nodeObject = CreateNodeObject(i, scaledPosition);
            previewNodes.Add(nodeObject);
        }
    }

    private GameObject CreateNodeObject(int nodeId, Vector2 position)
    {
        GameObject nodeObject;

        if (nodePrefab != null)
        {
            nodeObject = Instantiate(nodePrefab, transform);
            nodeObject.transform.position = position;
            nodeObject.transform.localScale = Vector3.one * previewScale;
        }
        else
        {
            nodeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nodeObject.name = $"PreviewNode_{nodeId}";
            nodeObject.transform.SetParent(transform);
            nodeObject.transform.position = position;

            Collider collider = nodeObject.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            Renderer renderer = nodeObject.GetComponent<Renderer>();
            renderer.material.color = Color.gray;
        }

        return nodeObject;
    }

    private void CreatePreviewConnections()
    {
        foreach (Connection connection in levelData.connections)
        {
            if (connection.from >= previewNodes.Count || connection.to >= previewNodes.Count) continue;

            GameObject fromNode = previewNodes[connection.from];
            GameObject toNode = previewNodes[connection.to];

            CreateConnectionVisual(fromNode, toNode);
        }
    }

    private void CreateConnectionVisual(GameObject fromNode, GameObject toNode)
    {
        GameObject lineObject = new GameObject($"PreviewConnection_{fromNode.name}_{toNode.name}");
        lineObject.transform.SetParent(transform);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, fromNode.transform.position);
        lineRenderer.SetPosition(1, toNode.transform.position);

        lineRenderer.startWidth = connectionWidth;
        lineRenderer.endWidth = connectionWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = connectionColor;
        lineRenderer.endColor = connectionColor;
        lineRenderer.sortingOrder = -1;

        connectionLines.Add(lineRenderer);
    }

    private void CreatePreviewPieces()
    {
        for (int i = 0; i < levelData.targetPositions.Count; i++)
        {
            int targetNodeIndex = levelData.targetPositions[i];
            if (targetNodeIndex >= previewNodes.Count) continue;

            GameObject targetNode = previewNodes[targetNodeIndex];
            Color pieceColor = GetPieceColor(i, levelData.targetPositions.Count);

            CreatePieceObject(i, pieceColor, targetNode.transform.position);
        }
    }

    private void CreatePieceObject(int pieceId, Color color, Vector3 position)
    {
        GameObject pieceObject;

        if (piecePrefab != null)
        {
            pieceObject = Instantiate(piecePrefab, transform);
            pieceObject.transform.position = position;
            pieceObject.transform.localScale = Vector3.one * previewScale * 2;
        }
        else
        {
            pieceObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pieceObject.name = $"PreviewPiece_{pieceId}";
            pieceObject.transform.SetParent(transform);
            pieceObject.transform.position = position + Vector3.forward * 0.1f;
            pieceObject.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f) * previewScale;

            Collider collider = pieceObject.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        Renderer renderer = pieceObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        previewPieces.Add(pieceObject);
    }

    private Color GetPieceColor(int pieceIndex, int totalPieces)
    {
        return Color.HSVToRGB(pieceIndex / (float)totalPieces, 0.8f, 0.9f);
    }

    private void SetupDisplay()
    {
        CreateTitleText();

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            transform.SetParent(mainCamera.transform);
            transform.localRotation = Quaternion.identity;
        }
    }

    private void CreateTitleText()
    {
        GameObject titleObject = new GameObject("PreviewTitle");
        titleObject.transform.SetParent(transform);
        titleObject.transform.localPosition = Vector3.zero;

        TextMesh textMesh = titleObject.AddComponent<TextMesh>();
        textMesh.text = "Победная Комбинация";
        textMesh.fontSize = 45;
        textMesh.anchor = TextAnchor.UpperCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        titleObject.transform.localPosition = new Vector3(25f, 30f, 0f);
    }

    private void ClearPreview()
    {
        foreach (var node in previewNodes)
            if (node != null) Destroy(node);
        previewNodes.Clear();

        foreach (var piece in previewPieces)
            if (piece != null) Destroy(piece);
        previewPieces.Clear();

        foreach (var line in connectionLines)
            if (line != null) Destroy(line.gameObject);
        connectionLines.Clear();
    }

    public void UpdatePreview(string newLevelPath = null)
    {
        if (newLevelPath != null)
        {
            levelPath = newLevelPath;
        }

        InitializePreview();
    }

    private void OnDestroy()
    {
        ClearPreview();
    }

    [ContextMenu("Update Preview")]
    private void EditorUpdatePreview()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            UpdatePreview();
        }
#endif
    }
}