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

    [Header("Prefabs")]
    public GameObject playerPrefab;

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

    // Automatically finds manager components on the same GameObject to save you from dragging and dropping
    private void Reset()
    {
        cardDealer = GetComponent<CardDealer>();
        suggestionSystem = GetComponent<SuggestionSystem>();
        aiAgent = GetComponent<AIAgent>();
        dataLoader = GetComponent<GameDataLoader>();
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

        // Setup the DeckManager with cards and pick the solution
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.SetupGameDeck();
        }

        // Create players dynamically from JSON character data
        for (int i = 0; i < TotalPlayers && i < data.characters.Length; i++)
        {
            CharacterData charData = data.characters[i];

            GameObject playerObj = Instantiate(playerPrefab);
            playerObj.name = "Player_" + charData.name;

            Player player = playerObj.GetComponent<Player>();
            PlayerController pc = playerObj.GetComponent<PlayerController>();

            // Ensure PlayerHand exists
            if (playerObj.GetComponent<PlayerHand>() == null)
                playerObj.AddComponent<PlayerHand>();

            // First N players are human, rest are AI
            bool isHuman = i < NumberOfHumanPlayers;
            Color colour = new Color(charData.colour.r, charData.colour.g, charData.colour.b);

            player.Initialise(charData.name, colour);

            pc.IsHuman = isHuman;
            pc.Character = (PlayerController.CharacterType)i;

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RegisterPlayer(pc);
            }
        }

        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.DealCards();
        }

        // TurnManager will place players and start the first turn
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.StartFirstTurn();
        }

        Debug.Log("Game setup complete. " + NumberOfHumanPlayers + " human player(s), "
                  + (TotalPlayers - NumberOfHumanPlayers) + " AI player(s).");
    }

    // Central state machine — controls all turn logic for both human and AI players
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        PlayerController activePlayer = TurnManager.Instance != null ? TurnManager.Instance.CurrentPlayer : null;

        OnGameStateChanged?.Invoke(newState);
        Debug.Log("State changed to: " + newState + " | Current player: " +
                  (activePlayer != null ? activePlayer.Character.ToString() : "none"));

        switch (newState)
        {
            case GameState.WaitingForRoll:
                // AI rolls automatically, human rolls via UI button
                if (activePlayer != null && !activePlayer.IsHuman && !activePlayer.IsEliminated)
                {
                    DiceRoller.Instance.RollDice();
                }
                break;

            case GameState.Moving:
                // AI picks a target room automatically
                if (activePlayer != null && !activePlayer.IsHuman)
                {
                    CardData targetRoom = aiAgent.ChooseTargetRoom(activePlayer);
                    if (targetRoom != null)
                    {
                        Tile targetTile = GridManager.Instance.GetRoomTile(targetRoom);
                        if (targetTile != null)
                        {
                            activePlayer.TeleportToTile(targetTile);
                            Debug.Log(activePlayer.Character + " moves to " + targetRoom.CardName);
                            ChangeState(GameState.Suggesting);
                        }
                    }
                }
                break;

            case GameState.Suggesting:
                // AI makes a suggestion then decides whether to accuse
                if (activePlayer != null && !activePlayer.IsHuman)
                {
                    int playerIndex = TurnManager.Instance.GetPlayers().IndexOf(activePlayer);
                    aiAgent.MakeSuggestion(activePlayer, suggestionSystem,
                                           playerIndex, TurnManager.Instance.GetPlayers());

                    if (aiAgent.ShouldAccuse(activePlayer))
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
                if (activePlayer != null && !activePlayer.IsHuman)
                {
                    CardData[] accusation = aiAgent.MakeAccusation();
                    bool correct = (DeckManager.Instance.Murderer == accusation[0] &&
                                    DeckManager.Instance.MurderWeapon == accusation[1] &&
                                    DeckManager.Instance.MurderRoom == accusation[2]);

                    if (correct)
                    {
                        Debug.Log(activePlayer.Character + " wins! Correct accusation!");
                        ChangeState(GameState.GameOver);
                    }
                    else
                    {
                        Debug.Log(activePlayer.Character + " made wrong accusation. Eliminated.");
                        activePlayer.Eliminate();
                        ChangeState(GameState.EndTurn);
                    }
                }
                break;

            case GameState.EndTurn:
                // TurnManager handles PassTurnToNextPlayer when state changes to EndTurn
                break;

            case GameState.GameOver:
                Debug.Log("Game Over!");
                break;
        }
    }

    // Called by UI when human player submits a suggestion
    public void HumanSuggestion(CardData person, CardData weapon, CardData room)
    {
        PlayerController activePlayer = TurnManager.Instance.CurrentPlayer;
        int playerIndex = TurnManager.Instance.GetPlayers().IndexOf(activePlayer);

        CardData shown = suggestionSystem.ProcessSuggestion(
            person, weapon, room,
            playerIndex, TurnManager.Instance.GetPlayers());
    }

    // Called by UI when human player submits a final accusation
    public void HumanAccusation(CardData person, CardData weapon, CardData room)
    {
        PlayerController activePlayer = TurnManager.Instance.CurrentPlayer;
        bool correct = (DeckManager.Instance.Murderer == person &&
                        DeckManager.Instance.MurderWeapon == weapon &&
                        DeckManager.Instance.MurderRoom == room);

        if (correct)
        {
            Debug.Log("You win! Correct accusation!");
            ChangeState(GameState.GameOver);
        }
        else
        {
            Debug.Log("Wrong accusation. You are eliminated.");
            activePlayer.Eliminate();
            ChangeState(GameState.EndTurn);
        }
    }
}