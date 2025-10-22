using UnityEngine;

public interface IPieceFactory : IGameService
{
    BasePiece CreatePiece(int id, Color color, Node startNode);
}