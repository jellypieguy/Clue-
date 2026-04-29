using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;

// player roster and controls turns
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [SerializeField] private List<PlayerController> _playersInGame = new List<PlayerController>();

    private int _internalPlayerIndex = -1;
    private bool _isAITakingTurn = false;

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
        if (CurrentPlayer != null)
        {
            Debug.Log($"Turn passed to {CurrentPlayer.Character}.");
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
        
        // Don't auto-roll here - just set the state and let the UI handle rolling
        GameManager.Instance.ChangeState(GameManager.GameState.WaitingForRoll);
        
        // If the first player is AI, trigger their turn automatically
        if (CurrentPlayer != null && !CurrentPlayer.IsHuman)
        {
            StartCoroutine(AITurnRoutine());
        }
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.WaitingForRoll)
        {
            // Only trigger AI roll when it's AI's turn and not already taking a turn
            if (CurrentPlayer != null && !CurrentPlayer.IsHuman && !_isAITakingTurn)
            {
                StartCoroutine(AITurnRoutine());
            }
            // Human players will click the dice button - do nothing automatically
        }
        else if (state == GameManager.GameState.EndTurn)
        {
            PassTurnToNextPlayer();
        }
    }

    // AI turn coroutine to handle the entire AI turn sequence
    private IEnumerator AITurnRoutine()
    {
        // Prevent multiple AI turns
        if (_isAITakingTurn || CurrentPlayer == null || CurrentPlayer.IsHuman)
            yield break;
            
        _isAITakingTurn = true;
        
        // Small delay before AI acts
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log($"AI {CurrentPlayer.Character} is taking their turn...");
        
        // Step 1: Roll the dice automatically
        if (DiceRoller.Instance != null)
        {
            // Use the dice roller to roll and get the value
            int rollResult = DiceRoller.Instance.RollDiceForAI(); 
            
            Debug.Log($"AI {CurrentPlayer.Character} rolled: {rollResult}");
            
            // Wait for dice animation
            yield return new WaitForSeconds(1.5f);
            
            // Step 2: Get AI's chosen target room
            AIAgent aiAgent = CurrentPlayer.GetComponent<AIAgent>();
            if (aiAgent != null)
            {
                CardData targetRoom = aiAgent.ChooseTargetRoom(CurrentPlayer, rollResult);
                
                if (targetRoom != null)
                {
                    Tile targetTile = GridManager.Instance.GetRoomTile(targetRoom);
                    
                    if (targetTile != null)
                    {
                        yield return StartCoroutine(MoveAIToTile(CurrentPlayer, targetTile, rollResult));
                    }
                }
                else
                {
                    // No rooms reachable - move to a random reachable tile
                    yield return StartCoroutine(MoveAIToClosestTile(CurrentPlayer, rollResult));
                }
            }
            
            yield return new WaitForSeconds(0.5f);
        }
        
        _isAITakingTurn = false;
        
        // End the turn if player didn't enter a room (room entry triggers suggestion state)
        if (CurrentPlayer != null && CurrentPlayer.CurrentTile.Type != Tile.TileType.Room)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.EndTurn);
        }
    }
    
    private IEnumerator MoveAIToTile(PlayerController aiPlayer, Tile targetTile, int movementBudget)
    {
        if (aiPlayer == null || targetTile == null)
            yield break;
            
        List<Tile> path = GetPathForAI(aiPlayer.CurrentTile, targetTile, movementBudget);
        
        if (path == null || path.Count == 0)
            yield break;
        
        if (path.Count > movementBudget)
            path = path.GetRange(0, movementBudget);
        
        foreach (Tile nextTile in path)
        {
            Vector3 startPos = aiPlayer.transform.position;
            Vector3 targetPos = nextTile.transform.position;
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                aiPlayer.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            aiPlayer.transform.position = targetPos;
            aiPlayer.SetCurrentTile(nextTile);
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private IEnumerator MoveAIToClosestTile(PlayerController aiPlayer, int movementBudget)
    {
        HashSet<Tile> reachableTiles = Pathfinder.GetReachableTiles(aiPlayer.CurrentTile, movementBudget);
        
        if (reachableTiles.Count == 0)
            yield break;
        
        List<Tile> tileList = new List<Tile>(reachableTiles);
        Tile targetTile = tileList[UnityEngine.Random.Range(0, tileList.Count)];
        
        yield return StartCoroutine(MoveAIToTile(aiPlayer, targetTile, movementBudget));
    }
    
    private List<Tile> GetPathForAI(Tile start, Tile target, int budget)
    {
        if (start == null || target == null)
            return null;
            
        Queue<List<Tile>> paths = new Queue<List<Tile>>();
        HashSet<Tile> visited = new HashSet<Tile>();
        
        List<Tile> startPath = new List<Tile>();
        startPath.Add(start);
        paths.Enqueue(startPath);
        visited.Add(start);
        
        while (paths.Count > 0)
        {
            List<Tile> currentPath = paths.Dequeue();
            Tile currentTile = currentPath[currentPath.Count - 1];
            
            if (currentTile == target)
            {
                List<Tile> movementPath = new List<Tile>(currentPath);
                movementPath.RemoveAt(0);
                return movementPath;
            }
            
            if (currentPath.Count - 1 >= budget)
                continue;
            
            foreach (Tile neighbor in currentTile.GetWalkableNeighbors())
            {
                if (!IsValidAIMove(currentTile, neighbor))
                    continue;
                    
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    List<Tile> newPath = new List<Tile>(currentPath);
                    newPath.Add(neighbor);
                    paths.Enqueue(newPath);
                }
            }
        }
        
        return GetClosestPathWithinBudget(start, target, budget);
    }
    
    private List<Tile> GetClosestPathWithinBudget(Tile start, Tile target, int budget)
    {
        Queue<List<Tile>> paths = new Queue<List<Tile>>();
        HashSet<Tile> visited = new HashSet<Tile>();
        List<Tile> bestPath = null;
        float bestDistance = float.MaxValue;
        
        List<Tile> startPath = new List<Tile>();
        startPath.Add(start);
        paths.Enqueue(startPath);
        visited.Add(start);
        
        while (paths.Count > 0)
        {
            List<Tile> currentPath = paths.Dequeue();
            Tile currentTile = currentPath[currentPath.Count - 1];
            
            float distance = Vector3.Distance(currentTile.transform.position, target.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPath = new List<Tile>(currentPath);
            }
            
            if (currentPath.Count - 1 >= budget)
                continue;
            
            foreach (Tile neighbor in currentTile.GetWalkableNeighbors())
            {
                if (!IsValidAIMove(currentTile, neighbor))
                    continue;
                    
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    List<Tile> newPath = new List<Tile>(currentPath);
                    newPath.Add(neighbor);
                    paths.Enqueue(newPath);
                }
            }
        }
        
        if (bestPath != null && bestPath.Count > 1)
        {
            bestPath.RemoveAt(0);
            return bestPath;
        }
        
        return null;
    }
    
    private bool IsValidAIMove(Tile current, Tile next)
    {
        if (current.Type == Tile.TileType.Room && next.Type != Tile.TileType.Door)
            return false;
        
        if (next.Type == Tile.TileType.Room && current.Type != Tile.TileType.Door)
            return false;
        
        if (next.Type != Tile.TileType.Hallway &&
            next.Type != Tile.TileType.Door &&
            next.Type != Tile.TileType.Room)
            return false;
        
        return true;
    }

    private void PassTurnToNextPlayer()
    {
        if (_playersInGame.Count == 0) return;

        int originalIndex = _currentPlayerIndex;
        bool foundActivePlayer = false;

        do
        {
            int nextIndex = (_currentPlayerIndex + 1) % _playersInGame.Count;
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
            GameManager.Instance.ChangeState(GameManager.GameState.GameOver);
            return;
        }

        Debug.Log($"TurnManager: Turn passed to {CurrentPlayer.Character}.");
        GameManager.Instance.ChangeState(GameManager.GameState.WaitingForRoll);
    }
}