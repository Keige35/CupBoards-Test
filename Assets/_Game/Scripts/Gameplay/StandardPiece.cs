using System.Collections;
using UnityEngine;

public class StandardPiece : BasePiece
{
    [SerializeField] private int pieceId;
    [SerializeField] private Node currentNode;
    [SerializeField] private Color pieceColor;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    public override int PieceId => pieceId;
    public override Node CurrentNode { get => currentNode; protected set => currentNode = value; }
    public override Color PieceColor => pieceColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void Initialize(int id, Color color, Node startNode)
    {
        pieceId = id;
        pieceColor = color;
        currentNode = startNode;

        transform.position = startNode.transform.position;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            originalColor = color;
        }

        startNode.CurrentPiece = this;
    }

    public override bool CanMoveTo(Node target)
    {
        if (target.CurrentPiece != null) return false;

        var pathfinder = ServiceLocator.Instance.Get<IPathfinder>();
        var boardManager = ServiceLocator.Instance.Get<BoardManager>();
        return pathfinder.IsPathClear(currentNode, target, boardManager.GetBoardState());
    }

    public override IEnumerator MoveTo(Node target, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = target.transform.position;
        float elapsed = 0;

        currentNode.CurrentPiece = null;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;

        currentNode = target;
        currentNode.CurrentPiece = this;

        GameEvents.PieceMoved(this, target);
    }

    public override void SetHighlight(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    public override void ResetHighlight()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}