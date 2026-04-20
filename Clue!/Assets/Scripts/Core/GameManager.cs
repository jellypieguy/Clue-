using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Setup,
        WaitingForRoll,
        Moving,
        Suggesting,
        Accusing,
        EndTurn,
        GameOver
    }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    // references to other systems (assign in Unity Inspector or find on start)
    public CardDealer cardDealer;
    public SuggestionSystem suggestionSystem;
    public AIAgent aiAgent;

    // player tracking
    public List<Player> Players = new List<Player>();
    public int CurrentPlayerIndex { get; private set; }
    public Player CurrentPlayer { get { return Players[CurrentPlayerIndex]; } }

    // game data
    public int NumberOfHumanPlayers = 1;
    public int TotalPlayers = 6;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SetupGame();
    }

    private void SetupGame()
    {
        ChangeState(GameState.Setup);

        // create players
        string[] characterNames = { "Miss Scarlett", "Col Mustard", "Mrs White",
                                     "Rev Green", "Mrs Peacock", "Prof Plum" };
        Color[] colours = { Color.red, Color.yellow, Color.white,
                            Color.green, Color.blue, new Color(0.5f, 0, 0.5f) };

        // starting positions on the board (row, col) - adjust to match your board layout
        int[,] startPositions = { {24,7}, {0,17}, {24,14}, {19,24}, {6,24}, {0,9} };

        for (int i = 0; i < TotalPlayers; i++)
        {
            GameObject playerObj = new GameObject("Player_" + characterNames[i]);
            Player player = playerObj.AddComponent<Player>();
            bool isHuman = i < NumberOfHumanPlayers;
            player.Initialise(characterNames[i], isHuman, colours[i],
                             startPositions[i, 0], startPositions[i, 1]);
            Players.Add(player);
        }

        // deal cards
        cardDealer.SetupAndDeal(Players);

        // initialise AI
        aiAgent.Initialise(cardDealer);

        // Miss Scarlett goes first (index 0)
        CurrentPlayerIndex = 0;

        Debug.Log("Game setup complete. " + NumberOfHumanPlayers + " human player(s), "
                  + (TotalPlayers - NumberOfHumanPlayers) + " AI player(s).");

        ChangeState(GameState.WaitingForRoll);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log("State changed to: " + newState + " | Current player: " +
                  (Players.Count > 0 ? CurrentPlayer.PlayerName : "none"));

        switch (newState)
        {
            case GameState.WaitingForRoll:
                if (!CurrentPlayer.IsHuman && !CurrentPlayer.IsEliminated)
                {
                    // AI automatically rolls
                    DiceRoller.instance.RollDice();
                }
                break;

            case GameState.Moving:
                if (!CurrentPlayer.IsHuman)
                {
                    // AI picks a target room and moves (simplified)
                    string targetRoom = aiAgent.ChooseTargetRoom(CurrentPlayer);
                    // for now, just teleport AI to the room
                    CurrentPlayer.CurrentRoom = targetRoom;
                    Debug.Log(CurrentPlayer.PlayerName + " moves to " + targetRoom);
                    ChangeState(GameState.Suggesting);
                }
                break;

            case GameState.Suggesting:
                if (!CurrentPlayer.IsHuman)
                {
                    aiAgent.MakeSuggestion(CurrentPlayer, suggestionSystem,
                                           CurrentPlayerIndex, Players);

                    // check if AI wants to accuse
                    if (aiAgent.ShouldAccuse(CurrentPlayer))
                    {
                        ChangeState(GameState.Accusing);
                    }
                    else
                    {
                        ChangeState(GameState.EndTurn);
                    }
                }
                break;

            case GameState.Accusing:
                if (!CurrentPlayer.IsHuman)
                {
                    string[] accusation = aiAgent.MakeAccusation();
                    bool correct = cardDealer.Envelope.CheckAccusation(
                        accusation[0], accusation[1], accusation[2]);

                    if (correct)
                    {
                        Debug.Log(CurrentPlayer.PlayerName + " wins! Correct accusation!");
                        ChangeState(GameState.GameOver);
                    }
                    else
                    {
                        Debug.Log(CurrentPlayer.PlayerName + " made wrong accusation. Eliminated.");
                        CurrentPlayer.IsEliminated = true;
                        ChangeState(GameState.EndTurn);
                    }
                }
                break;

            case GameState.EndTurn:
                NextPlayer();
                break;

            case GameState.GameOver:
                Debug.Log("Game Over!");
                break;
        }
    }

    private void NextPlayer()
    {
        // check if all players are eliminated (no winner)
        int activePlayers = 0;
        for (int i = 0; i < Players.Count; i++)
        {
            if (!Players[i].IsEliminated) activePlayers++;
        }

        if (activePlayers <= 0)
        {
            Debug.Log("All players eliminated. No winner.");
            ChangeState(GameState.GameOver);
            return;
        }

        // move to next non-eliminated player
        do
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
        while (CurrentPlayer.IsEliminated);

        Debug.Log("Next turn: " + CurrentPlayer.PlayerName);
        ChangeState(GameState.WaitingForRoll);
    }

    // called by UI when human player makes a suggestion
    public void HumanSuggestion(string person, string weapon)
    {
        if (CurrentPlayer.CurrentRoom == null)
        {
            Debug.Log("You must be in a room to make a suggestion.");
            return;
        }

        Card shown = suggestionSystem.ProcessSuggestion(
            person, weapon, CurrentPlayer.CurrentRoom,
            CurrentPlayerIndex, Players);

        // TODO: show the revealed card to the human player via UI
    }

    // called by UI when human player makes an accusation
    public void HumanAccusation(string person, string weapon, string room)
    {
        bool correct = cardDealer.Envelope.CheckAccusation(person, weapon, room);

        if (correct)
        {
            Debug.Log("You win! Correct accusation!");
            ChangeState(GameState.GameOver);
        }
        else
        {
            Debug.Log("Wrong accusation. You are eliminated.");
            CurrentPlayer.IsEliminated = true;
            ChangeState(GameState.EndTurn);
        }
    }
}
