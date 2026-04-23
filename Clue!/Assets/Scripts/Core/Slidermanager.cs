using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class PlayerSelectionSlider : MonoBehaviour
{
    public Slider PlayerSlider;
    public TMP_Text valueLabel;

    public int TotalPlayers =6;

   
    public static int HumanCount;

    void Start()
    {
        UpdateLabel(PlayerSlider.value);
        PlayerSlider.onValueChanged.AddListener(UpdateLabel);
    }

    void UpdateLabel(float value)
    {
        int humans = Mathf.RoundToInt(value);
      

       

        valueLabel.text = humans + " player";
    }

    public void OnStartGame()
    {
        HumanCount = Mathf.RoundToInt(PlayerSlider.value);
        

        
    }
    public int NumberOfHumanPlayers = HumanCount;
}