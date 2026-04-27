using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class PlayerSelectionSlider : MonoBehaviour
{
    public Slider PlayerSlider;       // UI slider for selecting number of human players
    public TMP_Text valueLabel;       // Label displaying the current slider value

    public int TotalPlayers = 6;      // Maximum number of players in the game

    // Stores the human player count selected before scene transition
    public static int HumanCount;

    void Start()
    {
        UpdateLabel(PlayerSlider.value);
        PlayerSlider.onValueChanged.AddListener(UpdateLabel);
    }

    // Updates the UI label whenever the slider value changes
    void UpdateLabel(float value)
    {
        int humans = Mathf.RoundToInt(value);
        valueLabel.text = humans + " player";
    }

    // Saves the selected human player count when the start button is pressed
    public void OnStartGame()
    {
        HumanCount = Mathf.RoundToInt(PlayerSlider.value);
    }

    // Returns the number of human players selected on the slider
    public int GetHumanPlayerCount()
    {
        return HumanCount;
    }
}