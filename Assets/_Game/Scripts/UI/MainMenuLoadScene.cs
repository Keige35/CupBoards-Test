using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuLoadScene : MonoBehaviour
{
    public void OnSceneSelect(int sceneNumber)
    {
        SceneManager.LoadScene(sceneNumber);
    }

    public void OnApplicationQuit()
    {
        Application.Quit();
    }
}
