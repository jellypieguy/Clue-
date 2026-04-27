using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Singleton instance accessible across all scripts
    public static GameManager Instance { get; private set; }

    // Defines all possible phases of a player's turn
    public enum GameState
    {
        Setup,           // Initial game setup before play begins
        WaitingForRoll,  // Player must roll the dice
        Moving,          // Player is moving to a room
        Suggesting,      // Player can make a suggestion
        Accusing,        // Player is making a final accusation
        EndTurn,         // Current turn is ending, move to next player
        GameOver         // Game has ended
    }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged; // Notifies UI when state changes

    // References to other game systems
    public CardDealer cardDealer;
    public SuggestionSystem suggestionSystem;
    public AIAgent aiAgent;
    public GameDataLoader dataLoader;

    // Player tracking
    public List<Player> Players = new List<Player>();
    public int CurrentPlayerIndex { get; private set; }
    public Player CurrentPlayer { get { return Players[CurrentPlayerIndex]; } }

    // Configurable via main menu slider
    public int NumberOfHumanPlayers = 1;
    public int TotalPlayers = 6;

    // Enforces singleton pattern and persists across scene loads
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

    // Initialises the full game: loads JSON data, creates players, deals cards, starts first turn
    private void SetupGame()
    {
        ChangeState(GameState.Setup);

        // Load all game data from the JSON file
        dataLoader.LoadGameData();
        GameData data = dataLoader.LoadedData;

        cardDealer.LoadNamesFromData(dataLoader);

        // Create players dynamically from JSON character data
        for (int i = 0; i < TotalPlayers && i < data.characters.Length; i++)
        {
            CharacterData charData = data.characters[i];

            GameObject playerObj = new GameObject("Player_" + charData.name);
            Player player = playerObj.AddComponent<Player>();

            // First N players are human, rest are AI
            bool isHuman = i < NumberOfHumanPlayers;
            Color colour = new Color(charData.colour.r, charData.colour.g, charData.colour.b);

            player.Initialise(charData.name, isHuman, colour,
                             charData.startRow, charData.startCol);
            Players.Add(player);
        }

        cardDealer.SetupAndDeal(Players);
        aiAgent.Initialise(cardDealer);

        // Miss Scarlett always goes first per Clue rules
        CurrentPlayerIndex = 0;

        Debug.Log("Game setup complete. " + NumberOfHumanPlayers + " human player(s), "
                  + (TotalPlayers - NumberOfHumanPlayers) + " AI player(s).");

        ChangeState(GameState.WaitingForRoll);
    }

    // Central state machine — controls all turn logic for both human and AI players
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log("State changed to: " + newState + " | Current player: " +
                  (Players.Count > 0 ? CurrentPlayer.PlayerName : "none"));

        switch (newState)
        {
            case GameState.WaitingForRoll:
                // AI rolls automatically, human rolls via UI button
                if (!CurrentPlayer.IsHuman && !CurrentPlayer.IsEliminated)
                {
                    DiceRoller.instance.RollDice();
                }
                break;

            case GameState.Moving:
                // AI picks a target room automatically
                if (!CurrentPlayer.IsHuman)
                {
                    string targetRoom = aiAgent.ChooseTargetRoom(CurrentPlayer);
                    CurrentPlayer.CurrentRoom = targetRoom;
                    Debug.Log(CurrentPlayer.PlayerName + " moves to " + targetRoom);
                    ChangeState(GameState.Suggesting);
                }
                break;

            case GameState.Suggesting:
                // AI makes a suggestion then decides whether to accuse
                if (!CurrentPlayer.IsHuman)
                {
                    aiAgent.MakeSuggestion(CurrentPlayer, suggestionSystem,
                                           CurrentPlayerIndex, Players);

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
                // AI makes accusation and is eliminated if wrong
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

    // Advances to the next non-eliminated player, or ends the game if none remain
    private void NextPlayer()
    {
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

        // Skip eliminated players
        do
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
        while (CurrentPlayer.IsEliminated);

        Debug.Log("Next turn: " + CurrentPlayer.PlayerName);
        ChangeState(GameState.WaitingForRoll);
    }

    // Called by UI when human player submits a suggestion
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
    }

    // Called by UI when human player submits a final accusation
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