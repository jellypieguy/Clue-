using UnityEngine;
using System;
using Unity.Netcode;

// The brain of the game 
// mode logic JSON and Board is handled by the JSONGameManager and BoardGameFlow.
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Setup,
        WaitingForRoll, //  roll
        Moving,         // dest
        Suggesting,     // suggestion
        Accusing,       // accusation
        EndTurn,        // pass next
        GameOver        // winner winner chicken dinner
    }

    public NetworkVariable<GameState> NetworkCurrentState = new NetworkVariable<GameState>(GameState.Setup);
    public GameState CurrentState => NetworkCurrentState.Value;

    // all that care about state changes
    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
        else
            Debug.LogWarning("GameManager not a root object Dont Destroy OnLoad skipped");
    }

    public override void OnNetworkSpawn()
    {
        NetworkCurrentState.OnValueChanged += HandleStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        NetworkCurrentState.OnValueChanged -= HandleStateChanged;
    }

    public void ChangeState(GameState newState)
    {
        if (!IsServer)
        {
            Debug.LogWarning("ChangeState called by client! Only The Server can change state.");
            return;
        }

        if (NetworkCurrentState.Value == newState) return;
        NetworkCurrentState.Value = newState;
    }

    private void HandleStateChanged(GameState oldState, GameState newState)
    {
        Debug.Log($"<color=cyan>GameManager (Net):</color> {oldState} → {newState}");
        OnGameStateChanged?.Invoke(newState);
    }

    //  action points
    // UI button bindings for making a suggestion and accusation.

    public void HumanSuggestion(CardData suspect, CardData weapon)
    {
        PlayerController active = TurnManager.Instance?.CurrentPlayer;
        if (active?.CurrentTile?.RoomData == null)
        {
            Debug.Log("You must be inside a room to make a suggestion.");
            return;
        }
        SuggestionManager.Instance?.MakeSuggestion(suspect, weapon, active.CurrentTile.RoomData);
    }

    public void HumanAccusation(CardData suspect, CardData weapon, CardData room)
    {
        SuggestionManager.Instance?.MakeAccusation(suspect, weapon, room);
    }

    public void HumanSuggestion(string person, string weapon)
    {
        if (JSONGameManager.Instance?.CurrentPlayer?.CurrentRoom == null) { Debug.Log("You must be inside a room."); return; }
        JSONGameManager.Instance?.suggestionSystem?.ProcessSuggestion(person, weapon, JSONGameManager.Instance.CurrentPlayer.CurrentRoom,
                                            JSONGameManager.Instance.CurrentPlayerIndex, JSONGameManager.Instance.Players);
    }

    public void HumanAccusation(string person, string weapon, string room)
    {
        bool win = JSONGameManager.Instance?.cardDealer?.Envelope.CheckAccusation(person, weapon, room) ?? false;
        if (win)
        {
            Debug.Log("Correct accusation | you win!");
            ChangeState(GameState.GameOver);
        }
        else
        {
            Debug.Log("Wrong accusation | eliminated.");
            if (JSONGameManager.Instance?.CurrentPlayer != null) JSONGameManager.Instance.CurrentPlayer.IsEliminated = true;
            ChangeState(GameState.EndTurn);
        }
    }
}
