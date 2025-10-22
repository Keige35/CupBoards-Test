using System;

public static class GameEvents
{
    public static event Action<BasePiece> OnPieceSelected;
    public static event Action<BasePiece, Node> OnPieceMoved;
    public static event Action OnLevelCompleted;
    public static event Action OnLevelLoaded;
    public static event Action<BasePiece, Node> OnMoveRequested;
    public static void PieceSelected(BasePiece piece) => OnPieceSelected?.Invoke(piece);
    public static void PieceMoved(BasePiece piece, Node node) => OnPieceMoved?.Invoke(piece, node);
    public static void LevelCompleted() => OnLevelCompleted?.Invoke();
    public static void LevelLoaded() => OnLevelLoaded?.Invoke();
    public static void MoveRequested(BasePiece piece, Node node) => OnMoveRequested?.Invoke(piece, node);
}
