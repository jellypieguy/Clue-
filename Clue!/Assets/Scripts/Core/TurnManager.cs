using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

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
        if (CurrentPlayer == null) return;

        Debug.Log($"Turn → {CurrentPlayer.Character}");
        OnPlayerTurnChanged?.Invoke(CurrentPlayer);

        if (!CurrentPlayer.IsHuman && AIChatBrain.Instance != null)
        {
            var character = (AIChatBrain.Character)(int)CurrentPlayer.Character;
            AIChatBrain.Instance.TriggerAILine(character, "TurnStart");
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

    public void RegisterPlayer(PlayerController player)
    {
        if (!_playersInGame.Contains(player))
            _playersInGame.Add(player);
    }

    public void ApplyPlayerSettings(int totalPlayers, int humanCount)
    {
        while (_playersInGame.Count > totalPlayers)
        {
            PlayerController extra = _playersInGame[_playersInGame.Count - 1];
            _playersInGame.RemoveAt(_playersInGame.Count - 1);
            extra.gameObject.SetActive(false);
        }

        for (int i = 0; i < _playersInGame.Count; i++)
            _playersInGame[i].IsHuman = (i < humanCount);
    }

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
        Debug.Log("The Murder Envelope has been sealed! The game begins.");
        
        if (CurrentPlayer.IsHuman && GameSettings.Instance != null && GameSettings.Instance.HumanPlayerCount > 1)
            GameManager.Instance.ChangeState(GameManager.GameState.PassingDevice);
        else
            GameManager.Instance.ChangeState(GameManager.GameState.WaitingForRoll);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.EndTurn)
            PassTurnToNextPlayer();
    }

    private void PassTurnToNextPlayer()
    {
        if (_playersInGame.Count == 0) return;

        CurrentPlayer?.ClearReachableHighlights();

        // Search without touching _currentPlayerIndex until we've confirmed a valid target —
        // the setter fires HandleTurnIndexChanged, so mutating it mid-search would spam events
        // for every eliminated player we skip over.
        int start = _currentPlayerIndex;
        int found = -1;

        for (int i = 1; i <= _playersInGame.Count; i++)
        {
            int candidate = (start + i) % _playersInGame.Count;
            if (!_playersInGame[candidate].IsEliminated)
            {
                found = candidate;
                break;
            }
        }

        if (found == -1)
        {
            Debug.LogError("Everyone's dead. The murderer wins by default.");
            GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
            return;
        }

        // Setter fires exactly once for the real next player
        _currentPlayerIndex = found;

        // Defer by one frame so we're clear of the EndTurn event chain before firing WaitingForRoll
        StartCoroutine(BeginNextTurn());
    }
    private IEnumerator BeginNextTurn()
    {
        yield return null; // wait one frame
        
        if (CurrentPlayer.IsHuman && GameSettings.Instance != null && GameSettings.Instance.HumanPlayerCount > 1)
            GameManager.Instance.ChangeState(GameManager.GameState.PassingDevice);
        else
            GameManager.Instance.ChangeState(GameManager.GameState.WaitingForRoll);
    }
}
