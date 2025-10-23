using System.Collections.Generic;
using System.Linq;

public class BoardState
{
    private List<BasePiece> _pieces;
    private List<Node> _nodes;

    public BoardState(List<BasePiece> pieces, List<Node> nodes)
    {
        _pieces = pieces;
        _nodes = nodes;
    }

    public List<int> GetCurrentPositions()
    {
        return _pieces.OrderBy(p => p.PieceId).Select(p => p.CurrentNode.Id).ToList();
    }

    public Node GetNodeById(int id)
    {
        return _nodes.FirstOrDefault(n => n.Id == id);
    }

    public BasePiece GetPieceAtNode(int nodeId)
    {
        return _pieces.FirstOrDefault(p => p.CurrentNode.Id == nodeId);
    }
}