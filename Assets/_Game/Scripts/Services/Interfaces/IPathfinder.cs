using System.Collections.Generic;

public interface IPathfinder: IGameService
{
    void Initialize();
    void Cleanup();
    List<Node> FindAvailableMoves(Node startNode, BoardState state);
    bool IsPathClear(Node start, Node end, BoardState state);
    List<Node> GetAvailableMoves(BasePiece piece, BoardState boardState);
    List<Node> FindPath(Node startNode, Node targetNode, BasePiece piece);
    bool CanMoveTo(BasePiece piece, Node targetNode, BoardState boardState);
}