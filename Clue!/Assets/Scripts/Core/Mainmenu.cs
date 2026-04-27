using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    [Header("Scene Names")]
    public string gameSceneName = "GameScene";   // Scene loaded when starting a new game
    public string loadSceneName = "LoadScene";   // Scene loaded when continuing a saved game

    void Start()
    {
        ShowMainMenu();
    }

    // Loads the main game scene to begin a new game
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Loads the save/load scene for continuing a previous game
    public void LoadGame()
    {
        SceneManager.LoadScene(loadSceneName);
    }

    // Switches from the main menu panel to the options panel
    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    // Returns to the main menu from any sub-panel
    public void BackToMenu()
    {
        ShowMainMenu();
    }

    // Quits the application, with editor support for stopping play mode
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Shows the main menu panel and hides all others
    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }
}