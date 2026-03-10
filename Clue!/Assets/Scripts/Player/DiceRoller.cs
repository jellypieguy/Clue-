using UnityEngine;
using System;


/// roll dice 


public class DiceRoller : MonoBehaviour
{
    public static DiceRoller instance { get; private set;}
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

    ///
    /// 
    /// 
    public void RollDice()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CurrentState != GameManager.GameState.WaitingForRoll)
        return;
        
    }



}