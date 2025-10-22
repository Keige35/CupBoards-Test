using UnityEngine;

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

        GameEvents.OnPieceMoved += OnPieceMoved;
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
    }
}