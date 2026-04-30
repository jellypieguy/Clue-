using UnityEngine;

public class DifficultySelector : MonoBehaviour
{
    public void OnDifficultyChanged(int index)
    {
        if (GameSettings.Instance == null) return;

        // UI difficulty  (0=ez, 1=med, 2=hard)
        var selectedDifficulty = (AIDifficulty)index;

        GameSettings.Instance.SetAIDifficulty(selectedDifficulty);
        Debug.Log($"[DifficultySelector] AI difficulty cranked to: {selectedDifficulty}");
    }
}