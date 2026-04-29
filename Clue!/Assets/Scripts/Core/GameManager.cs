using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Game state enum - defines all possible states of the game
    public enum GameState
    {
        Setup,          // Initial game setup
        WaitingForRoll, // Waiting for player to roll dice
        Moving,         // Player is moving on the board
        Suggesting,     // Player is making a suggestion
        Accusing,       // Player is making an accusation
        EndTurn,        // Ending current player's turn
        GameOver        // Game has ended
    }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    [Header("System References (leave empty to auto-find)")]
    [SerializeField] private SuggestionSystem suggestionSystem;
    [SerializeField] private AIAgent aiAgent;

    private void Awake()
    {
        // Singleton pattern - ensure only one GameManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Auto-find system references if not assigned in inspector
        if (suggestionSystem == null) suggestionSystem = FindObjectOfType<SuggestionSystem>();
        if (aiAgent == null) aiAgent = FindObjectOfType<AIAgent>();

        SetupGame();
    }

    // Initial game setup - called at the start of the game
    private void SetupGame()
    {
        ChangeState(GameState.Setup);

        // Verify DeckManager exists
        if (DeckManager.Instance == null)
        {
            Debug.LogError("[GameManager] DeckManager not found in scene.");
            return;
        }
        DeckManager.Instance.SetupGameDeck();

        // Get player settings from GameSettings (default to 6 players, 1 human if not found)
        int totalPlayers = 6;
        int humanCount   = 1;
        if (GameSettings.Instance != null)
        {
            totalPlayers = GameSettings.Instance.TotalPlayers;
            humanCount   = GameSettings.Instance.HumanPlayerCount;
        }

        // Verify TurnManager exists
        if (TurnManager.Instance == null)
        {
            Debug.LogError("[GameManager] TurnManager not found in scene.");
            return;
        }
        TurnManager.Instance.ApplyPlayerSettings(totalPlayers, humanCount);

        // Deal cards to all players
        DeckManager.Instance.DealCards();

        // Apply grid settings if GridManager exists
        if (GridManager.Instance != null)
            GridManager.Instance.ApplyGameSettings();

        // Start the first player's turn
        TurnManager.Instance.StartFirstTurn();

        Debug.Log($"[GameManager] Game setup complete. {humanCount} human(s), {totalPlayers - humanCount} AI.");
    }

    // Change the game state and trigger appropriate actions
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);

        PlayerController current = TurnManager.Instance != null
            ? TurnManager.Instance.CurrentPlayer
            : null;

        Debug.Log($"[GameManager] State -> {newState} | Player: {(current != null ? current.Character.ToString() : "none")}");

        // Don't need to handle Setup or GameOver states
        if (newState == GameState.Setup || newState == GameState.GameOver)
        {
            if (newState == GameState.GameOver) Debug.Log("[GameManager] Game Over.");
            return;
        }

        if (current == null) return;

        // Handle different game states based on whether player is human or AI
        switch (newState)
        {
            case GameState.WaitingForRoll:
                // AI automatically rolls dice, humans wait for UI button
                if (!current.IsHuman && !current.IsEliminated)
                {
                    // Use the correct method for AI rolling
                    DiceRoller.Instance.RollDiceForAI();
                }
                break;

            case GameState.Moving:
                // AI needs to move along the path
                if (!current.IsHuman && !current.IsEliminated)
                {
                    StartCoroutine(HandleAIMove(current));
                }
                break;

            case GameState.Suggesting:
                // AI makes a suggestion in the room they entered
                if (!current.IsHuman && !current.IsEliminated)
                {
                    HandleAISuggestion(current);
                }
                break;

            case GameState.Accusing:
                // AI makes an accusation (rare, they usually don't)
                if (!current.IsHuman && !current.IsEliminated)
                {
                    HandleAIAccusation(current);
                }
                break;

            case GameState.EndTurn:
                // Turn will be passed by TurnManager
                break;
        }
    }

    // Handle AI movement using proper pathfinding instead of teleportation
    private IEnumerator HandleAIMove(PlayerController current)
    {
        if (aiAgent == null)
        {
            Debug.LogWarning("[GameManager] No AIAgent in scene, ending AI turn.");
            ChangeState(GameState.EndTurn);
            yield break;
        }

        // Get the dice roll value
        int roll = DiceRoller.Instance.CurrentRoll;
        
        // If roll is 0, something went wrong with dice rolling
        if (roll <= 0)
        {
            Debug.LogWarning($"[GameManager] Invalid roll value: {roll}, ending turn.");
            ChangeState(GameState.EndTurn);
            yield break;
        }
        
        // Let AI choose a target room based on current position and roll
        CardData targetRoomCard = aiAgent.ChooseTargetRoom(current, roll);
        
        if (targetRoomCard == null)
        {
            Debug.Log($"[GameManager] {current.Character} has no reachable rooms. Ending turn.");
            ChangeState(GameState.EndTurn);
            yield break;
        }

        // Find the actual tile for the chosen room
        Tile targetTile = GridManager.Instance.GetRoomTile(targetRoomCard);
        
        if (targetTile == null)
        {
            Debug.LogWarning($"[GameManager] Could not find tile for room {targetRoomCard.CardName}");
            ChangeState(GameState.EndTurn);
            yield break;
        }

        Debug.Log($"[GameManager] {current.Character} moving to {targetRoomCard.CardName} with roll {roll}");
        
        // Move the AI along the path instead of teleporting
        yield return StartCoroutine(MoveAIAlongPath(current, targetTile, roll));
        
        // After movement, if player is now in a room, switch to suggesting state
        if (current.CurrentTile != null && current.CurrentTile.Type == Tile.TileType.Room)
        {
            Debug.Log($"[GameManager] {current.Character} entered {current.CurrentTile.RoomData.CardName}. Moving to suggestion.");
            ChangeState(GameState.Suggesting);
        }
        else
        {
            Debug.Log($"[GameManager] {current.Character} did not enter a room. Ending turn.");
            ChangeState(GameState.EndTurn);
        }
    }

    // Coroutine to move AI step by step along the path
    private IEnumerator MoveAIAlongPath(PlayerController aiPlayer, Tile targetTile, int movementBudget)
    {
        if (aiPlayer == null || targetTile == null)
            yield break;
        
        // Get the path from current position to target
        List<Tile> path = CalculatePathForAI(aiPlayer.CurrentTile, targetTile, movementBudget);
        
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[GameManager] No valid path found for {aiPlayer.Character}");
            yield break;
        }
        
        // Cap path length to movement budget
        if (path.Count > movementBudget)
            path = path.GetRange(0, movementBudget);
        
        Debug.Log($"[GameManager] {aiPlayer.Character} moving along {path.Count} tiles");
        
        // Move step by step along the path
        foreach (Tile nextTile in path)
        {
            Vector3 startPos = aiPlayer.transform.position;
            Vector3 targetPos = nextTile.transform.position;
            float elapsed = 0f;
            float duration = 0.3f; // Movement speed between tiles
            
            // Smooth movement animation
            while (elapsed < duration)
            {
                aiPlayer.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Ensure final position is exact
            aiPlayer.transform.position = targetPos;
            
            // Update the player's current tile reference
            aiPlayer.SetCurrentTile(nextTile);
            
            // Small delay between steps for visual clarity
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log($"[GameManager] {aiPlayer.Character} finished moving. Final tile: {aiPlayer.CurrentTile.Type}");
    }

    // Calculate a valid path from start to target within budget
    private List<Tile> CalculatePathForAI(Tile start, Tile target, int budget)
    {
        if (start == null || target == null)
            return null;
        
        // Simple BFS to find shortest path
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
            
            // If we reached the target, return the path (excluding start tile)
            if (currentTile == target)
            {
                List<Tile> movementPath = new List<Tile>(currentPath);
                movementPath.RemoveAt(0); // Remove the starting tile
                return movementPath;
            }
            
            // Stop exploring if we've used all movement budget
            if (currentPath.Count - 1 >= budget)
                continue;
            
            // Check all valid neighbors
            foreach (Tile neighbor in currentTile.GetWalkableNeighbors())
            {
                // Validate movement rules
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
        
        // If can't reach target, try to get as close as possible
        return GetClosestPathWithinBudget(start, target, budget);
    }

    // Fallback: Get as close as possible to target within budget
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
            
            // Calculate distance to target
            float distance = Vector3.Distance(currentTile.transform.position, target.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPath = new List<Tile>(currentPath);
            }
            
            // Stop if we've used all budget
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
        
        // Return path excluding start tile
        if (bestPath != null && bestPath.Count > 1)
        {
            bestPath.RemoveAt(0);
            return bestPath;
        }
        
        return null;
    }

    // Validate movement rules (same as Pathfinder)
    private bool IsValidAIMove(Tile current, Tile next)
    {
        // Can't move from room to non-door
        if (current.Type == Tile.TileType.Room && next.Type != Tile.TileType.Door)
            return false;
        
        // Can't move to room unless from door
        if (next.Type == Tile.TileType.Room && current.Type != Tile.TileType.Door)
            return false;
        
        // Only allow Hallway, Door, Room
        if (next.Type != Tile.TileType.Hallway &&
            next.Type != Tile.TileType.Door &&
            next.Type != Tile.TileType.Room)
            return false;
        
        return true;
    }

    // Handle AI making a suggestion
    private void HandleAISuggestion(PlayerController current)
    {
        if (aiAgent == null || suggestionSystem == null)
        {
            ChangeState(GameState.EndTurn);
            return;
        }

        // Get the index of the current player
        int playerIndex = TurnManager.Instance.GetPlayers().IndexOf(current);
        
        // Make the suggestion
        aiAgent.MakeSuggestion(current, suggestionSystem,
                               playerIndex, TurnManager.Instance.GetPlayers());

        // Decide whether to accuse or end turn
        if (aiAgent.ShouldAccuse(current))
            ChangeState(GameState.Accusing);
        else
            ChangeState(GameState.EndTurn);
    }

    // Handle AI making an accusation
    private void HandleAIAccusation(PlayerController current)
    {
        if (aiAgent == null)
        {
            ChangeState(GameState.EndTurn);
            return;
        }

        // Get random accusation from AI
        CardData[] accusation = aiAgent.MakeAccusation();
        
        if (accusation == null || accusation.Length < 3)
        {
            ChangeState(GameState.EndTurn);
            return;
        }
        
        // Check if accusation is correct
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

    // Public method for humans to make a suggestion
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
        suggestionSystem.ProcessSuggestion(suspect, weapon, currentRoom,
                                           playerIndex, TurnManager.Instance.GetPlayers());
    }

    // Public method for humans to make an accusation
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

    // Check if accusation matches the murder solution
    private bool CheckAccusation(CardData suspect, CardData weapon, CardData room)
    {
        return suspect == DeckManager.Instance.Murderer
            && weapon  == DeckManager.Instance.MurderWeapon
            && room    == DeckManager.Instance.MurderRoom;
    }
}