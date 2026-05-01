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
        NotepadUI.Instance?.ResetNotepad();

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
        if (NotepadUI.Instance == null) return;

        List<PlayerController> players = TurnManager.Instance.GetPlayers();
        foreach (PlayerController player in players)
        {
            if (!player.IsHuman) continue;

            PlayerHand hand = player.GetComponent<PlayerHand>();
            if (hand == null) continue;

            foreach (CardData card in hand.Cards)
                NotepadUI.Instance.AutoMarkCard(card.CardName);

            Debug.Log($"[HandDisplay] Showing {hand.GetHand().Count} cards");
            PlayerHandDisplay.Instance?.ShowHand(hand.GetHand());

            break;
        }
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        PlayerController current = TurnManager.Instance != null
            ? TurnManager.Instance.CurrentPlayer
            : null;

        if (newState == GameState.GameOver) 
        {
            Debug.Log("[GameManager] Game Over.");
            UIManager.Instance?.ShowMurderReveal(
                DeckManager.Instance.Murderer.CardName,
                DeckManager.Instance.MurderWeapon.CardName,
                DeckManager.Instance.MurderRoom.CardName
            );
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
        yield return new WaitForSeconds(1.5f);

        if (aiAgent == null)
        {
            Debug.LogWarning("[GameManager] No AIAgent in scene, ending AI turn.");
            ChangeState(GameState.EndTurn);
            yield break;
        }

        int roll = DiceRoller.Instance.LastRoll;
        HashSet<Tile> reachable = Pathfinder.GetReachableTiles(current.CurrentTile, roll);

        if (reachable.Count == 0)
        {
            Debug.Log($"[AI] {current.Character} has no reachable tiles.");
            ChangeState(GameState.EndTurn);
            yield break;
        }

        // Prefer door/room tiles so AI can make a suggestion
        List<Tile> roomTiles = new List<Tile>();
        List<Tile> hallwayTiles = new List<Tile>();

        foreach (Tile t in reachable)
        {
            if (t.Type == Tile.TileType.Door || t.Type == Tile.TileType.Room)
                roomTiles.Add(t);
            else
                hallwayTiles.Add(t);
        }

        List<Tile> candidates = roomTiles.Count > 0 ? roomTiles : hallwayTiles;

        // Filter out occupied tiles
        List<Tile> freeCandidates = candidates.FindAll(t => !IsTileOccupied(t));
        if (freeCandidates.Count == 0) freeCandidates = candidates;

        Tile chosenTile = freeCandidates[UnityEngine.Random.Range(0, freeCandidates.Count)];

        Debug.Log($"[AI] {current.Character} moving to {chosenTile.name}");

        current.ClearReachableHighlights();
        yield return StartCoroutine(AIMoveToTile(current, chosenTile));
    }

private IEnumerator AIMoveToTile(PlayerController current, Tile targetTile)
{
    Vector3 startPos = current.transform.position;
    Vector3 targetPos = targetTile.transform.position;
    float elapsed = 0f;
    float duration = 0.5f;

    while (elapsed < duration)
    {
        current.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }

    current.transform.position = targetPos;

    // Mirror exactly what MoveToTile does in PlayerController
    if (targetTile.Type == Tile.TileType.Room || targetTile.Type == Tile.TileType.Door)
    {
        string roomName = targetTile.RoomData != null ? targetTile.RoomData.CardName : "a room";
        Debug.Log($"[AI] {current.Character} entered {roomName}.");
        // Manually update CurrentTile via TeleportToTile since _currentTile is private
        current.TeleportToTile(targetTile);
        ChangeState(GameState.Suggesting);
    }
    else if (targetTile.Type == Tile.TileType.SecretPassage)
    {
        current.TeleportToTile(targetTile);
        current.UseSecretPassage();
    }
    else
    {
        current.TeleportToTile(targetTile);
        Debug.Log($"[AI] {current.Character} stuck in hallway.");
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
        Debug.Log($"[Suggestion] Called. CurrentPlayer={TurnManager.Instance?.CurrentPlayer?.Character}");
        PlayerController current = TurnManager.Instance.CurrentPlayer;
        if (current == null) return;
        Debug.Log($"[Suggestion] Tile={current.CurrentTile?.name}, Type={current.CurrentTile?.Type}, RoomData={current.CurrentTile?.RoomData}");
        
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
    private bool IsTileOccupied(Tile tile)
    {
        foreach (PlayerController player in TurnManager.Instance.GetPlayers())
        {
            if (player == TurnManager.Instance.CurrentPlayer) continue;
            if (player.CurrentTile == tile && !player.IsEliminated)
                return true;
        }
        return false;
    }
}