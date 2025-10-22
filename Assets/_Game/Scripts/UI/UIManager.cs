using UnityEngine;

public class UIManager : MonoBehaviour, IGameService
{
    [SerializeField] private GameObject victoryPanel;

    public void Initialize()
    {
        GameEvents.OnLevelCompleted += ShowVictoryScreen;
    }

    public void Cleanup()
    {
        GameEvents.OnLevelCompleted -= ShowVictoryScreen;
    }

    private void ShowVictoryScreen()
    {
        victoryPanel.SetActive(true);
    }

    public void OnNextLevelButton()
    {
        victoryPanel.SetActive(false);
    }
}