using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    [SerializeField] private int id;
    [SerializeField] private List<Node> connectedNodes = new List<Node>();
    private BasePiece currentPiece;
    private SpriteRenderer spriteRenderer;

    public int Id => id;
    public List<Node> ConnectedNodes => connectedNodes;
    /*public BasePiece CurrentPiece
    {
        get => currentPiece;
        set => currentPiece = value;
    }*/
    public BasePiece CurrentPiece { get; set; }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(int id, Vector2 position)
    {
        this.id = id;
        transform.position = position;
    }

    public void AddConnection(Node node)
    {
        if (!connectedNodes.Contains(node))
            connectedNodes.Add(node);
    }

    public void SetHighlight(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }

    public void ResetHighlight()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        foreach (var node in connectedNodes)
        {
            if (node != null)
                Gizmos.DrawLine(transform.position, node.transform.position);
        }
    }
}