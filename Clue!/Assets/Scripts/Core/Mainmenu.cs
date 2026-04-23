using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    [Header("Scene Names")]
    public string gameSceneName = "GameScene";
    public string loadSceneName = "LoadScene";

    void Start()
    {
        ShowMainMenu();
    }

 
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void LoadGame()
    {
        SceneManager.LoadScene(loadSceneName);
    }

    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        ShowMainMenu();
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }
}