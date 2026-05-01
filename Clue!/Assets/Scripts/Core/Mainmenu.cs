using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    [Header("Scene Names")]
    public string gameSceneName = "gameBoard";   
    public string loadSceneName = "loadScene";   

    private void Start()
    {
        AudioManager.Instance?.PlayMenuMusic();
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.SetHumanCount(1);
            GameSettings.Instance.SetTotalPlayers(6);
        }
        ShowMainMenu();
    }

    public void StartGame() => SceneManager.LoadScene(gameSceneName);

    public void LoadGame() => SceneManager.LoadScene(loadSceneName);

    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void BackToMenu() => ShowMainMenu();

    public void QuitGame()
    {
        Debug.Log("rage quit");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }
}