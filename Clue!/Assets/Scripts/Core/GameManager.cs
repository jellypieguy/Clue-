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

    // references to other systems
    public CardDealer cardDealer;
    public SuggestionSystem suggestionSystem;
    public AIAgent aiAgent;
    public GameDataLoader dataLoader;

    // player tracking
    public List<Player> Players = new List<Player>();
    public int CurrentPlayerIndex { get; private set; }
    public Player CurrentPlayer { get { return Players[CurrentPlayerIndex]; } }

    // game settings
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

        //load all game data from the JSON file
        dataLoader.LoadGameData();
        GameData data = dataLoader.LoadedData;

        //give the card dealer the loaded names
        cardDealer.LoadNamesFromData(dataLoader);

        // create players using data from the JSON file
        for (int i = 0; i < TotalPlayers && i < data.characters.Length; i++)
        {
            CharacterData charData = data.characters[i];

            GameObject playerObj = new GameObject("Player_" + charData.name);
            Player player = playerObj.AddComponent<Player>();

            bool isHuman = i < NumberOfHumanPlayers;
            Color colour = new Color(charData.colour.r, charData.colour.g, charData.colour.b);

            player.Initialise(charData.name, isHuman, colour,
                             charData.startRow, charData.startCol);
            Players.Add(player);
        }

        // deal cards
        cardDealer.SetupAndDeal(Players);

        // initialise AI
        aiAgent.Initialise(cardDealer);

        // Miss Scarlett always goes first
        CurrentPlayerIndex = 0;

        Debug.Log("Game setup complete. " + NumberOfHumanPlayers + " human player(s), "
                  + (TotalPlayers - NumberOfHumanPlayers) + " AI player(s).");
        Debug.Log("All data loaded from game_data.json");

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
                    DiceRoller.instance.RollDice();
                }
                break;

            case GameState.Moving:
                if (!CurrentPlayer.IsHuman)
                {
                    string targetRoom = aiAgent.ChooseTargetRoom(CurrentPlayer);
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

        do
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
        }
        while (CurrentPlayer.IsEliminated);

        Debug.Log("Next turn: " + CurrentPlayer.PlayerName);
        ChangeState(GameState.WaitingForRoll);
    }

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
