using System.Collections.Generic;
using UnityEngine;
using System;

// player roster and controls turns
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [SerializeField] private List<PlayerController> _playersInGame = new List<PlayerController>();

    private int _internalPlayerIndex = -1;

    private int _currentPlayerIndex
    {
        get => _internalPlayerIndex;
        set
        {
            if (_internalPlayerIndex != value)
            {
                int oldIndex = _internalPlayerIndex;
                _internalPlayerIndex = value;
                HandleTurnIndexChanged(oldIndex, value);
            }
        }
    }

    // fires when the active player change
    public event Action<PlayerController> OnPlayerTurnChanged;

    public PlayerController CurrentPlayer => _playersInGame.Count > 0 && _currentPlayerIndex >= 0 ? _playersInGame[_currentPlayerIndex] : null;

    public List<PlayerController> GetPlayers() => _playersInGame;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void HandleTurnIndexChanged(int oldIndex, int newIndex)
    {
        if (CurrentPlayer != null)
        {
            UIManager.Instance?.AddLogEvent($"Turn passed to {CurrentPlayer.Character}.");
            OnPlayerTurnChanged?.Invoke(CurrentPlayer);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    // adds a player to the roster 
    public void RegisterPlayer(PlayerController player)
    {
        if (!_playersInGame.Contains(player))
            _playersInGame.Add(player);
    }

    // ccall this before first turn so skipped players are hidden from the board
    public void ApplyPlayerSettings(int totalPlayers, int humanCount)
    {
        // Remove any extra players from the back of the list and hide their tokens.
        while (_playersInGame.Count > totalPlayers)
        {
            PlayerController extra = _playersInGame[_playersInGame.Count - 1];
            _playersInGame.RemoveAt(_playersInGame.Count - 1);
            extra.gameObject.SetActive(false);
        }

        // sets player as human orbot based on their position
        for (int i = 0; i < _playersInGame.Count; i++)
            _playersInGame[i].IsHuman = (i < humanCount);
    }

    // spawn points
    public void StartFirstTurn()
    {
        if (_playersInGame.Count == 0)
        {
            Debug.LogWarning("TurnManager: No players registered.");
            return;
        }

        foreach (PlayerController player in _playersInGame)
        {
            Tile startTile = GridManager.Instance.GetStartingTile(player.Character);
            if (startTile != null)
            {
                Debug.Log($"TurnManager: Placing {player.Character} at ({startTile.GridX},{startTile.GridY})");
                player.Initialize(startTile);
            }
            else
            {
                Debug.LogError($"TurnManager: No starting tile found for {player.Character}.");
            }
        }

        _currentPlayerIndex = 0;
        UIManager.Instance?.AddLogEvent("The Murder Envelope has been sealed! The game begins.");
        GameManager.Instance.ChangeState(GameManager.GameState.WaitingForRoll);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.EndTurn)
            PassTurnToNextPlayer();
    }

    // advance to the next player if not elim
    private void PassTurnToNextPlayer()
    {
        if (_playersInGame.Count == 0) return;

        int originalIndex = _currentPlayerIndex;
        bool foundActivePlayer = false;

        do
        {
            int nextIndex = (_currentPlayerIndex + 1) % _playersInGame.Count;
            // udates player index so player reflects the check
            _currentPlayerIndex = nextIndex;

            if (!CurrentPlayer.IsEliminated)
            {
                foundActivePlayer = true;
                break;
            }
        } while (_currentPlayerIndex != originalIndex);

        if (!foundActivePlayer)
        {
            Debug.LogError("ALL PLAYERS ELIMINATED! The murderer got away with it!");
            UIManager.Instance?.AddLogEvent("ALL PLAYERS ELIMINATED! The murderer got away with it!");
            GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
            return;
        }

        Debug.Log($"TurnManager: Turn passed to {CurrentPlayer.Character}.");
        // UI + Event handled by Index
        GameManager.Instance.ChangeState(GameManager.GameState.WaitingForRoll);
    }
}
