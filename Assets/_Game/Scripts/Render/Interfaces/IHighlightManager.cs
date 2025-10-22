using System.Collections.Generic;

public interface IHighlightManager : IGameService
{
    void HighlightPiece(BasePiece piece);
    void HighlightNodes(List<Node> nodes);
    void ClearHighlights();
}