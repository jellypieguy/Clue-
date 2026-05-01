using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

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
    public string PendingWinner { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    [Header("System References (leave empty to auto-find)")]
    [SerializeField] private SuggestionSystem suggestionSystem;
    [SerializeField] private AIAgent aiAgent;
    [SerializeField] private UIManager uiManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

        if (DeckManager.Instance == null) { Debug.LogError("[GameManager] DeckManager not found."); return; }
        DeckManager.Instance.SetupGameDeck();

        int totalPlayers = 6, humanCount = 1;
        if (GameSettings.Instance != null)
        {
            totalPlayers = GameSettings.Instance.TotalPlayers;
            humanCount   = GameSettings.Instance.HumanPlayerCount;
        }

        if (TurnManager.Instance == null) { Debug.LogError("[GameManager] TurnManager not found."); return; }
        TurnManager.Instance.ApplyPlayerSettings(totalPlayers, humanCount);

        DeckManager.Instance.DealCards();

        GridManager.Instance?.ApplyGameSettings();
        TurnManager.Instance.StartFirstTurn();
        AudioManager.Instance?.PlayGameMusic();

        AutoMarkHumanHand();
        Debug.Log($"[GameManager] Ready. {humanCount} human(s), {totalPlayers - humanCount} AI.");
    }

    private void AutoMarkHumanHand()
    {
        if (NotepadUI.Instance == null) return;

        foreach (PlayerController player in TurnManager.Instance.GetPlayers())
        {
            if (!player.IsHuman) continue;

            PlayerHand hand = player.GetComponent<PlayerHand>();
            if (hand == null) continue;

            foreach (CardData card in hand.Cards)
                NotepadUI.Instance.AutoMarkCard(card.CardName);

            break;
        }
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        var current = TurnManager.Instance?.CurrentPlayer;

        Debug.Log($"[GameManager] → {newState} | {current?.Character.ToString() ?? "none"}");

        if (newState == GameState.GameOver)
        {
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
                    StartCoroutine(AIMoveRoutine(current));
                break;
            case GameState.Suggesting:
                if (!current.IsHuman)
                    StartCoroutine(AISuggestionRoutine(current));
                break;
            case GameState.Accusing:
                if (!current.IsHuman)
                    StartCoroutine(AIAccusationRoutine(current));
                break;
        }
    }

    // ── AI Routines ──────────────────────────────────────────────────────────

    private IEnumerator AIMoveRoutine(PlayerController current)
    {
        if (current.IsEliminated) { ChangeState(GameState.EndTurn); yield break; }

        yield return new WaitForSeconds(1.5f);

        if (aiAgent == null) { ChangeState(GameState.EndTurn); yield break; }

        int roll = DiceRoller.Instance.LastRoll;
        var reachable = Pathfinder.GetReachableTiles(current.CurrentTile, roll);

        if (reachable.Count == 0)
        {
            Debug.Log($"[AI] {current.Character} is boxed in.");
            ChangeState(GameState.EndTurn);
            yield break;
        }

        // Agent picks a target room based on its memory, then we try to reach it this turn
        var targetRoom = aiAgent.ChooseTargetRoom(current);

        var roomTiles    = reachable.Where(t => t.Type == Tile.TileType.Door || t.Type == Tile.TileType.Room).ToList();
        var hallwayTiles = reachable.Where(t => t.Type != Tile.TileType.Door && t.Type != Tile.TileType.Room).ToList();

        List<Tile> candidates;
        if (targetRoom != null && roomTiles.Count > 0)
        {
            var preferred = roomTiles.Where(t => t.RoomData == targetRoom).ToList();
            candidates = preferred.Count > 0 ? preferred : roomTiles;
        }
        else
        {
            candidates = roomTiles.Count > 0 ? roomTiles : hallwayTiles;
        }

        var free = candidates.Where(t => !IsTileOccupied(t)).ToList();
        if (free.Count == 0) free = candidates;

        var chosen = free[UnityEngine.Random.Range(0, free.Count)];
        current.ClearReachableHighlights();
        yield return StartCoroutine(AIMoveToTile(current, chosen));
    }

    private IEnumerator AIMoveToTile(PlayerController current, Tile target)
    {
        Vector3 start = current.transform.position;
        float elapsed = 0f, duration = 0.5f;

        while (elapsed < duration)
        {
            current.transform.position = Vector3.Lerp(start, target.transform.position, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        current.transform.position = target.transform.position;

        // Mirror the state transitions from PlayerController.MoveToTile
        if (target.Type == Tile.TileType.Room || target.Type == Tile.TileType.Door)
        {
            current.TeleportToTile(target);
            ChangeState(GameState.Suggesting);
        }
        else if (target.Type == Tile.TileType.SecretPassage)
        {
            current.TeleportToTile(target);
            current.UseSecretPassage();
        }
        else
        {
            current.TeleportToTile(target);
            ChangeState(GameState.EndTurn);
        }
    }

    private IEnumerator AISuggestionRoutine(PlayerController current)
    {
        if (current.IsEliminated) { ChangeState(GameState.EndTurn); yield break; }

        yield return new WaitForSeconds(1.0f);

        if (aiAgent == null || suggestionSystem == null) { ChangeState(GameState.EndTurn); yield break; }

        int playerIndex = TurnManager.Instance.GetPlayers().IndexOf(current);
        bool done = false;

        aiAgent.MakeSuggestion(current, suggestionSystem, playerIndex, TurnManager.Instance.GetPlayers(), shownCard =>
        {
            if (shownCard != null) aiAgent.RecordShownCard(current, shownCard);
            done = true;
        });

        yield return new WaitUntil(() => done);
        yield return new WaitForSeconds(1.5f);

        ChangeState(aiAgent.ShouldAccuse(current) ? GameState.Accusing : GameState.EndTurn);
    }

    private IEnumerator AIAccusationRoutine(PlayerController current)
    {
        if (current.IsEliminated) { ChangeState(GameState.EndTurn); yield break; }

        yield return new WaitForSeconds(1.0f);

        if (aiAgent == null) { ChangeState(GameState.EndTurn); yield break; }

        var accusation = aiAgent.MakeAccusation(current);
        if (CheckAccusation(accusation[0], accusation[1], accusation[2]))
        {
            PendingWinner = current.Character.ToString();
            ChangeState(GameState.GameOver);
        }
        else
        {
            Debug.Log($"[AI] {current.Character} got it wrong — eliminated.");
            current.Eliminate();
            ChangeState(GameState.EndTurn);
        }
    }

    // ── Human Actions ─────────────────────────────────────────────────────────

    public void HumanSuggestion(CardData suspect, CardData weapon)
    {
        var current = TurnManager.Instance.CurrentPlayer;
        if (current == null) return;

        var currentRoom = current.CurrentTile?.RoomData;
        if (currentRoom == null) { Debug.Log("[GameManager] Must be in a room to suggest."); return; }

        // Official rule: drag the named suspect into the room no matter where they are
        TeleportSuspectToRoom(suspect, currentRoom);

        int playerIndex = TurnManager.Instance.GetPlayers().IndexOf(current);
        var players = TurnManager.Instance.GetPlayers();

        suggestionSystem.ProcessSuggestion(suspect, weapon, currentRoom, playerIndex, players, shownCard =>
        {
            if (shownCard == null)
            {
                UIManager.Instance?.ShowNobodyDisproved();
                return;
            }

            // Find who showed it so we can name them in the reveal panel
            for (int i = 1; i < players.Count; i++)
            {
                int idx = (playerIndex + i) % players.Count;
                var hand = players[idx].GetComponent<PlayerHand>();
                if (hand != null && hand.GetRefutingCards(suspect, weapon, currentRoom).Count > 0)
                {
                    UIManager.Instance?.ShowCardReveal(players[idx].Character.ToString(), shownCard);
                    break;
                }
            }
        });
    }

    public void HumanAccusation(CardData suspect, CardData weapon, CardData room)
    {
        var current = TurnManager.Instance.CurrentPlayer;
        if (current == null) return;

        if (CheckAccusation(suspect, weapon, room))
        {
            PendingWinner = current.Character.ToString();
            ChangeState(GameState.GameOver);
        }
        else
        {
            Debug.Log($"[GameManager] {current.Character} wrong — eliminated.");
            current.Eliminate();
            ChangeState(GameState.EndTurn);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool CheckAccusation(CardData suspect, CardData weapon, CardData room) =>
        suspect == DeckManager.Instance.Murderer
        && weapon == DeckManager.Instance.MurderWeapon
        && room   == DeckManager.Instance.MurderRoom;

    private void TeleportSuspectToRoom(CardData suspect, CardData room)
    {
        var player = FindPlayerForSuspect(suspect);
        if (player == null) return;
        var tile = GridManager.Instance?.GetRoomTile(room);
        if (tile != null) player.TeleportToTile(tile);
    }

    private PlayerController FindPlayerForSuspect(CardData suspect)
    {
        // Card names → CharacterType — hardcoded because there's no reverse lookup on the enum
        var nameMap = new Dictionary<string, PlayerController.CharacterType>
        {
            { "Miss Scarlet",    PlayerController.CharacterType.MissScarlet },
            { "Colonel Mustard", PlayerController.CharacterType.ColMustard  },
            { "Mrs. White",      PlayerController.CharacterType.MrsWhite    },
            { "Mr Green",        PlayerController.CharacterType.MrGreen     },
            { "Mrs. Peacock",    PlayerController.CharacterType.MrsPeacock  },
            { "Professor Plum",  PlayerController.CharacterType.ProfPlum    },
        };

        if (!nameMap.TryGetValue(suspect.CardName, out var charType)) return null;
        return TurnManager.Instance.GetPlayers().FirstOrDefault(p => p.Character == charType);
    }

    private bool IsTileOccupied(Tile tile)
    {
        foreach (var player in TurnManager.Instance.GetPlayers())
        {
            if (player == TurnManager.Instance.CurrentPlayer) continue;
            if (player.CurrentTile == tile && !player.IsEliminated) return true;
        }
        return false;
    }
}
