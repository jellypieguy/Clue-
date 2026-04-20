using UnityEngine;
using System;

public class DiceRoller : MonoBehaviour
{
    public static DiceRoller instance { get; private set; }
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

    public void RollDice()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.WaitingForRoll)
            return;

        int result = UnityEngine.Random.Range(1, 7); // 1 to 6 inclusive
        Debug.Log("Dice rolled: " + result);
        OnDiceRolled?.Invoke(result);

        GameManager.Instance.ChangeState(GameManager.GameState.Moving);
    }
}
