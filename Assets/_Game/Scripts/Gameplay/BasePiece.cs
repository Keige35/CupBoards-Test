using System.Collections;
using UnityEngine;

public abstract class BasePiece : MonoBehaviour
{
    public abstract int PieceId { get; }
    public abstract Node CurrentNode { get; protected set; }
    public abstract Color PieceColor { get; }

    public abstract void Initialize(int id, Color color, Node startNode);
    public abstract bool CanMoveTo(Node target);
    public abstract IEnumerator MoveTo(Node target, float duration);
    public abstract void SetHighlight(Color color);
    public abstract void ResetHighlight();
}
