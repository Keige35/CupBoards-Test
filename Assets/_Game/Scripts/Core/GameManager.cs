using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string initialLevelPath = "Levels/level1";
    [SerializeField] private Canvas canvas;
    private LevelData currentLevelData;

    private void Awake()
    {
        canvas = FindAnyObjectByType<Canvas>();
        canvas.gameObject.SetActive(false);
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

        currentLevelData = levelLoader.LoadLevel(levelPath);
        if (currentLevelData != null)
        {
            boardManager.CreateBoard(currentLevelData);
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

        var boardManager = ServiceLocator.Instance.Get<BoardManager>();
        boardManager.UpdateBoardState();
    }

    private void OnPieceMoved(BasePiece piece, Node node)
    {
        CheckVictoryCondition();
    }

    private void CheckVictoryCondition()
    {
        var victoryChecker = ServiceLocator.Instance.Get<IVictoryChecker>();
        var boardManager = ServiceLocator.Instance.Get<BoardManager>();

        if (victoryChecker.CheckVictory(currentLevelData, boardManager.GetBoardState()))
        {
            GameEvents.LevelCompleted();
            StartCoroutine(VictoryAnimation());
            canvas.gameObject.SetActive(true);
        }
    }

    private IEnumerator VictoryAnimation()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Color originalColor = mainCamera.backgroundColor;

            for (int i = 0; i < 3; i++)
            {
                mainCamera.backgroundColor = Color.green;
                yield return new WaitForSeconds(0.3f);
                mainCamera.backgroundColor = originalColor;
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    private void OnDestroy()
    {
        GameEvents.OnPieceMoved -= OnPieceMoved;
        GameEvents.OnMoveRequested -= OnMoveRequested;
    }
}