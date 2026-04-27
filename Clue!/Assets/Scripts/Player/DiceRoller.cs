using UnityEngine;
using System;

public class DiceRoller : MonoBehaviour
{
    // Singleton instance so any script can trigger a dice roll
    public static DiceRoller instance { get; private set; }

    // Broadcasts the dice result to any subscribed listeners (e.g. UI)
    public event Action<int> OnDiceRolled;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    // Rolls a six-sided die and advances the game state to Moving
    public void RollDice()
    {
        // Only allow rolling during the correct game state
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.WaitingForRoll)
            return;

        int result = UnityEngine.Random.Range(1, 7); // 1 to 6 inclusive
        Debug.Log("Dice rolled: " + result);
        OnDiceRolled?.Invoke(result);
        GameManager.Instance.ChangeState(GameManager.GameState.Moving);
    }
}