using UnityEngine;

public class InputHandler : MonoBehaviour, IGameService
{
    private BasePiece selectedPiece;
    private bool isInitialized = false;

    public void Initialize()
    {
        if (isInitialized) return;

        GameEvents.OnPieceSelected += OnPieceSelected;

        Debug.Log("InputHandler initialized");
        isInitialized = true;
    }

    public void Cleanup()
    {
        GameEvents.OnPieceSelected -= OnPieceSelected;
        Debug.Log("InputHandler cleaned up");
        isInitialized = false;
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        var boardManager = ServiceLocator.Instance.Get<BoardManager>();
        var highlightManager = ServiceLocator.Instance.Get<IHighlightManager>();

        BasePiece clickedPiece = boardManager.GetPieceAtPosition(worldPos);
        if (clickedPiece != null)
        {
            GameEvents.PieceSelected(clickedPiece);
            return;
        }

        if (selectedPiece != null)
        {
            Node clickedNode = boardManager.GetNodeAtPosition(worldPos);
            if (clickedNode != null)
            {
                boardManager.MovePiece(selectedPiece, clickedNode);
                highlightManager.ClearHighlights();
                selectedPiece = null;
            }
        }
    }

    private void OnPieceSelected(BasePiece piece)
    {
        selectedPiece = piece;

        var highlightManager = ServiceLocator.Instance.Get<IHighlightManager>();
        var boardManager = ServiceLocator.Instance.Get<BoardManager>();
        var pathfinder = ServiceLocator.Instance.Get<IPathfinder>();

        highlightManager.HighlightPiece(piece);

        var availableMoves = pathfinder.FindAvailableMoves(piece.CurrentNode, boardManager.GetBoardState());
        highlightManager.HighlightNodes(availableMoves);
    }
}