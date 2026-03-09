using UnityEngine;
using System;


// core of the game, tracks who turn it is and what the game state/phase is in
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        WaitingForRoll,
        Moving,
        Suggesting,
        EndTurn,
        GameOver
    }

    public GameState CurrentState {get; private set; }
    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy (gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // keeps it alive between states

    }
    // updates the current phase of the game
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        //logic for what happens after entering a specific state 
        switch(newState)
        {
            case GameState.WaitingForRoll:
                break; // next turn logic? 
            case GameState.Moving:
                break;
            case GameState.EndTurn: // loop back to waiting for the next roll
                ChangeState(GameState.WaitingForRoll);
                break;
        }
    }


}