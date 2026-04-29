using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;

    [Header("Scene Names")]
    public string gameSceneName = "GameBoard";   // loads game scence
    public string loadSceneName = "LoadScene";   // loads saved game

    void Start()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuMusic();
        ShowMainMenu();
    }

    //main game scene
    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // continue previous game
    public void LoadGame()
    {
        SceneManager.LoadScene(loadSceneName);
    }

    // options and menu panel
    public void OpenOptions()
    {
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    // return back button
    public void BackToMenu()
    {
        ShowMainMenu();
    }

    //quits game
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // show main menu
    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }
}