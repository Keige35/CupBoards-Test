using UnityEngine;

public class PieceFactory : IPieceFactory
{
    private GameObject piecePrefab;
    private bool isInitialized = false;

    public void Initialize()
    {
        if (isInitialized) return;

        piecePrefab = Resources.Load<GameObject>("Prefabs/Piece");
        if (piecePrefab == null)
        {
            Debug.LogError("Not found Piece prefab");
        }

        isInitialized = true;
    }

    public void Cleanup()
    {
        isInitialized = false;
    }

    public BasePiece CreatePiece(int id, Color color, Node startNode)
    {
        if (piecePrefab == null)
        {
            Debug.LogError("Piece prefab is not loaded");
            return null;
        }

        GameObject pieceObj = Object.Instantiate(piecePrefab);
        BasePiece piece = pieceObj.GetComponent<BasePiece>();
        if (piece == null)
        {
            Debug.LogError("Piece prefab doesn't have BasePiece component");
            return null;
        }

        piece.Initialize(id, color, startNode);
        return piece;
    }
}