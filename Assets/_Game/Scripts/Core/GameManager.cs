using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string initialLevelPath = "Levels/level1";

    private void Awake()
    {
        InitializeServices();
        LoadLevel(initialLevelPath);
    }

    private void InitializeServices()
    {
        ServiceLocator.Instance.Register<ILevelLoader>(gameObject.AddComponent<LevelLoader>());
        ServiceLocator.Instance.Register<IPathfinder>(new PathfinderService());
        ServiceLocator.Instance.Register<IVictoryChecker>(new VictoryChecker());
        ServiceLocator.Instance.Register<IHighlightManager>(gameObject.AddComponent<HighlightManager>());
        ServiceLocator.Instance.Register<BoardManager>(gameObject.AddComponent<BoardManager>());
        ServiceLocator.Instance.Register<InputHandler>(gameObject.AddComponent<InputHandler>());
        ServiceLocator.Instance.Register<IPieceFactory>(new PieceFactory());

        ServiceLocator.Instance.Register<AnimationMovementService>(new AnimationMovementService());

        GameEvents.OnPieceMoved += OnPieceMoved;
        GameEvents.OnMoveRequested += OnMoveRequested;
    }

    private void LoadLevel(string levelPath)
    {
        var levelLoader = ServiceLocator.Instance.Get<ILevelLoader>();
        var boardManager = ServiceLocator.Instance.Get<BoardManager>();

        LevelData levelData = levelLoader.LoadLevel(levelPath);
        if (levelData != null)
        {
            boardManager.CreateBoard(levelData);
            GameEvents.LevelLoaded();
        }
        else
        {
            Debug.LogError("Failed to load level: " + levelPath);
        }
    }

    private void OnMoveRequested(BasePiece piece, Node targetNode)
    {
        var animationService = ServiceLocator.Instance.Get<AnimationMovementService>();
        var pathfinder = ServiceLocator.Instance.Get<IPathfinder>();
        var boardManager = ServiceLocator.Instance.Get<BoardManager>();

        if (animationService.IsAnimating) return;

        if (pathfinder.CanMoveTo(piece, targetNode, boardManager.GetBoardState()))
        {
            StartCoroutine(PerformAnimatedMove(piece, targetNode, animationService, pathfinder));
        }
    }

    private IEnumerator PerformAnimatedMove(BasePiece piece, Node targetNode,
                                         AnimationMovementService animationService,
                                         IPathfinder pathfinder)
    {
        yield return StartCoroutine(animationService.AnimateMovement(piece, targetNode, pathfinder));

        UpdatePiecePosition(piece, targetNode);

        GameEvents.PieceMoved(piece, targetNode);
    }

    private void UpdatePiecePosition(BasePiece piece, Node targetNode)
    {
        if (piece.CurrentNode != null)
        {
            piece.CurrentNode.CurrentPiece = null;
        }

        piece.CurrentNode = targetNode;
        targetNode.CurrentPiece = piece;

        piece.transform.position = targetNode.transform.position;
    }

    private void OnPieceMoved(BasePiece piece, Node node)
    {
        CheckVictoryCondition();
    }

    private void CheckVictoryCondition()
    {
        var levelLoader = ServiceLocator.Instance.Get<ILevelLoader>();
        var victoryChecker = ServiceLocator.Instance.Get<IVictoryChecker>();
        var boardManager = ServiceLocator.Instance.Get<BoardManager>();

        LevelData currentLevelData = levelLoader.LoadLevel(initialLevelPath);
        if (victoryChecker.CheckVictory(currentLevelData, boardManager.GetBoardState()))
        {
            GameEvents.LevelCompleted();
            Debug.Log("Level Completed! Congratulations!");
        }
    }

    private void OnDestroy()
    {
        GameEvents.OnPieceMoved -= OnPieceMoved;
        GameEvents.OnMoveRequested -= OnMoveRequested;
    }
}