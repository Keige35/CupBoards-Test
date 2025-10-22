using System.Collections.Generic;
using UnityEngine;

public class HighlightManager : MonoBehaviour, IHighlightManager
{
    [SerializeField] private Color selectedPieceColor = Color.yellow;
    [SerializeField] private Color availableNodeColor = Color.green;

    private List<Node> highlightedNodes = new List<Node>();
    private BasePiece highlightedPiece;
    private bool isInitialized = false;

    public void Initialize()
    {
        if (isInitialized) return;
        Debug.Log("HighlightManager initialized");
        isInitialized = true;
    }

    public void Cleanup()
    {
        ClearHighlights();
        Debug.Log("HighlightManager cleaned up");
        isInitialized = false;
    }

    public void HighlightPiece(BasePiece piece)
    {
        if (highlightedPiece != null && highlightedPiece != piece)
        {
            highlightedPiece.ResetHighlight();
        }

        highlightedPiece = piece;
        if (highlightedPiece != null)
        {
            highlightedPiece.SetHighlight(selectedPieceColor);
        }
    }

    public void HighlightNodes(List<Node> nodes)
    {
        ClearNodeHighlights();

        foreach (var node in nodes)
        {
            node.SetHighlight(availableNodeColor);
            highlightedNodes.Add(node);
        }
    }

    public void ClearHighlights()
    {
        ClearPieceHighlight();
        ClearNodeHighlights();
    }

    private void ClearPieceHighlight()
    {
        if (highlightedPiece != null)
        {
            highlightedPiece.ResetHighlight();
            highlightedPiece = null;
        }
    }

    private void ClearNodeHighlights()
    {
        foreach (var node in highlightedNodes)
        {
            node.ResetHighlight();
        }
        highlightedNodes.Clear();
    }
}