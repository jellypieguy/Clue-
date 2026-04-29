using UnityEngine;
using TMPro;

public class DifficultySelector : MonoBehaviour
{
    public void OnDifficultyChanged(int index)
    {
        if (GameSettings.Instance == null) return;

        //  dropdown for difficulty 
        // 0 = easy 1 = medium 2 = hard
        AIDifficulty selectedDifficulty = (AIDifficulty)index;

        GameSettings.Instance.SetAIDifficulty(selectedDifficulty);
        Debug.Log($"AI Difficulty set to: {selectedDifficulty}");
    }
}
