using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Setup,
        PassingDevice,
        WaitingForRoll,
        Moving,
        Suggesting,
        Accusing,
        EndTurn,
        GameOver
    }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    [Header("System References (leave empty to auto-find)")]
    [SerializeField] private SuggestionSystem suggestionSystem;
    [SerializeField] private AIAgent aiAgent;
    [SerializeField] private UIManager uiManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (suggestionSystem == null) suggestionSystem = FindFirstObjectByType<SuggestionSystem>();
        if (aiAgent == null) aiAgent = FindFirstObjectByType<AIAgent>();

        SetupGame();
    }

    private void SetupGame()
    {
        ChangeState(GameState.Setup);

        if (DeckManager.Instance == null)
        {
            Debug.LogError("[GameManager] DeckManager not found in scene.");
            return;
        }
        DeckManager.Instance.SetupGameDeck();

        int totalPlayers = 6;
        int humanCount = 1;
        if (GameSettings.Instance != null)
        {
            totalPlayers = GameSettings.Instance.TotalPlayers;
            humanCount = GameSettings.Instance.HumanPlayerCount;
        }

        if (TurnManager.Instance == null)
        {
            Debug.LogError("[GameManager] TurnManager not found in scene.");
            return;
        }
        TurnManager.Instance.ApplyPlayerSettings(totalPlayers, humanCount);

        DeckManager.Instance.DealCards();

        if (GridManager.Instance != null)
            GridManager.Instance.ApplyGameSettings();

        TurnManager.Instance.StartFirstTurn();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGameMusic();

        AutoMarkHumanHand();
        Debug.Log($"[GameManager] Game setup complete. {humanCount} human(s), {totalPlayers - humanCount} AI.");
    }

    private void AutoMarkHumanHand()
    {
        if (DetectiveNotepad.Instance == null) return;

        List<PlayerController> players = TurnManager.Instance.GetPlayers();
        foreach (PlayerController player in players)
        {
            if (!player.IsHuman) continue;

            PlayerHand hand = player.GetComponent<PlayerHand>();
            if (hand == null) continue;

            foreach (CardData card in hand.Cards)
                DetectiveNotepad.Instance.AutoMarkCard(card.CardName);

            break; // only one human player
        }
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        PlayerController current = TurnManager.Instance != null
            ? TurnManager.Instance.CurrentPlayer
            : null;

        Debug.Log($"[GameManager] State -> {newState} | Player: {(current != null ? current.Character.ToString() : "none")}");

        if (newState == GameState.Setup || newState == GameState.GameOver)
        {
            if (newState == GameState.GameOver) Debug.Log("[GameManager] Game Over.");
            return;
        }

        if (current == null) return;

        switch (newState)
        {
            case GameState.WaitingForRoll:
                if (!current.IsHuman && !current.IsEliminated)
                    DiceRoller.Instance.RollDice();
                break;

            case GameState.Moving:
                if (!current.IsHuman)
                    HandleAIMove(current);
                break;

            case GameState.Suggesting:
                if (!current.IsHuman)
                    HandleAISuggestion(current);
                break;

            case GameState.Accusing:
                if (!current.IsHuman)
                    HandleAIAccusation(current);
                break;

            case GameState.EndTurn:
                break;
        }
    }

    private void HandleAIMove(PlayerController current)
    {
        StartCoroutine(AIMoveRoutine(current));
    }

    private IEnumerator AIMoveRoutine(PlayerController current)
    {
        yield return new WaitForSeconds(1.5f); // Let the player see who's moving

        if (aiAgent == null)
        {
            Debug.LogWarning("[GameManager] No AIAgent in scene, ending AI turn.");
            ChangeState(GameState.EndTurn);
            yield break;
        }

        CardData target = aiAgent.ChooseTargetRoom(current);
        if (target == null)
        {
            ChangeState(GameState.EndTurn);
            yield break;
        }

        Tile destTile = GridManager.Instance.GetRoomTile(target);
        if (destTile != null)
        {
            current.TeleportToTile(destTile);
            Debug.Log($"[GameManager] {current.Character} moves to {target.CardName}.");
            ChangeState(GameState.Suggesting);
        }
        else
        {
            ChangeState(GameState.EndTurn);
        }
    }

    private void HandleAISuggestion(PlayerController current)
    {
        StartCoroutine(AISuggestionRoutine(current));
    }

    private IEnumerator AISuggestionRoutine(PlayerController current)
    {
        yield return new WaitForSeconds(1.0f); // Readability delay

        if (aiAgent == null || suggestionSystem == null)
        {
            ChangeState(GameState.EndTurn);
            yield break;
        }

        int playerIndex = TurnManager.Instance.GetPlayers().IndexOf(current);
        CardData shownCard = aiAgent.MakeSuggestion(current, suggestionSystem,playerIndex,TurnManager.Instance.GetPlayers());
        
        if (shownCard != null)
        {
            aiAgent.RecordShownCard(current, shownCard);
        }

        yield return new WaitForSeconds(1.5f); // Pause so humans can read the suggestion popup

        if (aiAgent.ShouldAccuse(current))
            ChangeState(GameState.Accusing);
        else
            ChangeState(GameState.EndTurn);
    }

    private void HandleAIAccusation(PlayerController current)
    {
        StartCoroutine(AIAccusationRoutine(current));
    }

    private IEnumerator AIAccusationRoutine(PlayerController current)
    {
        yield return new WaitForSeconds(1.0f);

        if (aiAgent == null)
        {
            ChangeState(GameState.EndTurn);
            yield break;
        }

        CardData[] accusation = aiAgent.MakeAccusation(current);
        bool correct = CheckAccusation(accusation[0], accusation[1], accusation[2]);

        if (correct)
        {
            Debug.Log($"[GameManager] {current.Character} wins! Correct accusation!");
            ChangeState(GameState.GameOver);
        }
        else
        {
            Debug.Log($"[GameManager] {current.Character} was wrong. Eliminated.");
            current.Eliminate();
            ChangeState(GameState.EndTurn);
        }
    }

    public void HumanSuggestion(CardData suspect, CardData weapon)
    {
        PlayerController current = TurnManager.Instance.CurrentPlayer;
        if (current == null) return;

        CardData currentRoom = current.CurrentTile?.RoomData;
        if (currentRoom == null)
        {
            Debug.Log("[GameManager] You must be in a room to make a suggestion.");
            return;
        }

        int playerIndex = TurnManager.Instance.GetPlayers().IndexOf(current);
        CardData shownCard = suggestionSystem.ProcessSuggestion(suspect, weapon, currentRoom,
            playerIndex, TurnManager.Instance.GetPlayers());

        if (shownCard != null)
        {
            List<PlayerController> players = TurnManager.Instance.GetPlayers();
            for (int i = 1; i < players.Count; i++)
            {
                int checkIndex = (playerIndex + i) % players.Count;
                PlayerController checker = players[checkIndex];
                PlayerHand hand = checker.GetComponent<PlayerHand>();
                if (hand != null && hand.GetRefutingCards(suspect, weapon, currentRoom).Count > 0)
                {
                    UIManager.Instance?.ShowCardReveal(checker.Character.ToString(), shownCard);
                    break;
                }
            }
        }
    }

    public void HumanAccusation(CardData suspect, CardData weapon, CardData room)
    {
        PlayerController current = TurnManager.Instance.CurrentPlayer;
        if (current == null) return;

        bool correct = CheckAccusation(suspect, weapon, room);

        if (correct)
        {
            Debug.Log($"[GameManager] {current.Character} wins! Correct accusation!");
            ChangeState(GameState.GameOver);
        }
        else
        {
            Debug.Log($"[GameManager] {current.Character} wrong accusation. Eliminated.");
            current.Eliminate();
            ChangeState(GameState.EndTurn);
        }
    }

    private bool CheckAccusation(CardData suspect, CardData weapon, CardData room)
    {
        return suspect == DeckManager.Instance.Murderer
            && weapon == DeckManager.Instance.MurderWeapon
            && room == DeckManager.Instance.MurderRoom;
    }
}