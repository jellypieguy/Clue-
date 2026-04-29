using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public enum CharacterType
    {
        MissScarlet,
        ColMustard,
        MrsWhite,
        MrGreen,
        MrsPeacock,
        ProfPlum
    }

    [Header("Character Identity")]
    public CharacterType Character;

    [Header("Player Type")]
    public bool IsHuman = true;

    private Tile _currentTile;
    public Tile CurrentTile => _currentTile;

    private int _movementBudget;
    private HashSet<Tile> _reachableTiles = new HashSet<Tile>();
    private bool _isMoving = false;

    public bool IsEliminated { get; private set; } = false;

    // ----------------------------
    // ELIMINATION
    // ----------------------------
    public void Eliminate()
    {
        IsEliminated = true;
        Debug.Log($"{Character} has been Eliminated.");

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color darkened = sr.color;
            darkened *= 0.3f;
            sr.color = darkened;
        }
    }

    // ----------------------------
    // INIT
    // ----------------------------
    private void Start()
    {
        if (DiceRoller.Instance != null)
            DiceRoller.Instance.OnDiceRolled += HandleDiceRolled;
    }

    private void OnDestroy()
    {
        if (DiceRoller.Instance != null)
            DiceRoller.Instance.OnDiceRolled -= HandleDiceRolled;
    }

    public void Initialize(Tile startingTile)
    {
        _currentTile = startingTile;
        transform.position = _currentTile.transform.position;
    }

    // ----------------------------
    // SECRET PASSAGE
    // ----------------------------
    public void UseSecretPassage()
    {
        if (_currentTile?.SecretPassageDestination == null) return;

        Tile dest = GridManager.Instance?.GetRoomTile(_currentTile.SecretPassageDestination);

        if (dest == null) return;

        _currentTile = dest;
        transform.position = dest.transform.position;

        GameManager.Instance.ChangeState(GameManager.GameState.Suggesting);
    }

    // ----------------------------
    // TELEPORT (suggestions)
    // ----------------------------
    public void TeleportToTile(Tile targetTile)
    {
        if (targetTile != null && !_isMoving)
        {
            _currentTile = targetTile;
            transform.position = targetTile.transform.position;
        }
    }

    // ----------------------------
    // DICE RESULT
    // ----------------------------
    private void HandleDiceRolled(int rollTotal)
    {
        if (TurnManager.Instance != null &&
            TurnManager.Instance.CurrentPlayer != this)
            return;

        _movementBudget = rollTotal;

        if (_currentTile != null)
        {
            _reachableTiles = Pathfinder.GetReachableTiles(_currentTile, _movementBudget);

            foreach (Tile t in _reachableTiles)
                t.Highlight(Color.green);
        }
    }

    // ----------------------------
    // INPUT
    // ----------------------------
    private void Update()
    {
        if (TurnManager.Instance != null &&
            TurnManager.Instance.CurrentPlayer != this)
            return;

        if (GameManager.Instance.CurrentState != GameManager.GameState.Moving || _isMoving)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandleMouseClick();
    }

    private void HandleMouseClick()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);

        foreach (Collider2D hit in hits)
        {
            Tile clickedTile = hit.GetComponent<Tile>();

            if (clickedTile != null && _reachableTiles.Contains(clickedTile))
            {
                foreach (Tile t in _reachableTiles)
                    t.RemoveHighlight();

                StartCoroutine(MoveToTile(clickedTile));
                return;
            }
        }
    }

    // ----------------------------
    // MOVEMENT CORE
    // ----------------------------
    private IEnumerator MoveToTile(Tile targetTile)
    {
        _isMoving = true;

        List<Tile> path = GetPathToTile(_currentTile, targetTile);

        if (path == null || path.Count == 0)
        {
            _isMoving = false;
            yield break;
        }

        foreach (Tile nextTile in path)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = nextTile.transform.position;

            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos;
            _currentTile = nextTile;

            yield return new WaitForSeconds(0.03f);
        }

        _isMoving = false;

        foreach (Tile t in _reachableTiles)
            t.RemoveHighlight();

        _reachableTiles.Clear();

        if (_currentTile.Type == Tile.TileType.Room)
            GameManager.Instance.ChangeState(GameManager.GameState.Suggesting);
        else
            GameManager.Instance.ChangeState(GameManager.GameState.EndTurn);
    }

    // ----------------------------
    // PATHFINDING (USES SAME RULE ENGINE)
    // ----------------------------
    private List<Tile> GetPathToTile(Tile start, Tile target)
    {
        Queue<List<Tile>> queue = new Queue<List<Tile>>();
        HashSet<Tile> visited = new HashSet<Tile>();

        queue.Enqueue(new List<Tile> { start });
        visited.Add(start);

        while (queue.Count > 0)
        {
            List<Tile> path = queue.Dequeue();
            Tile current = path[path.Count - 1];

            if (current == target)
            {
                path.RemoveAt(0);
                return path;
            }

            if (path.Count - 1 >= _movementBudget)
                continue;

            foreach (Tile neighbor in current.GetWalkableNeighbors())
            {
                if (!visited.Contains(neighbor) && IsValidMove(current, neighbor))
                {
                    visited.Add(neighbor);

                    List<Tile> newPath = new List<Tile>(path);
                    newPath.Add(neighbor);

                    queue.Enqueue(newPath);
                }
            }
        }

        return null;
    }

    // ----------------------------
    // SINGLE SOURCE OF RULES (CRITICAL FIX)
    // ----------------------------
    private bool IsValidMove(Tile current, Tile next)
    {
        if (next.Type != Tile.TileType.Hallway &&
            next.Type != Tile.TileType.Door &&
            next.Type != Tile.TileType.Room)
            return false;

        // 🚨 ROOM ENTRY RULE (FIXED)
        if (next.Type == Tile.TileType.Room)
        {
            if (current.Type != Tile.TileType.Door)
                return false;
        }

        return true;
    }

    // ----------------------------
    // SET TILE
    // ----------------------------
    public void SetCurrentTile(Tile newTile)
    {
        _currentTile = newTile;
    }
}