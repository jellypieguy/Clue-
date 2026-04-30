using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// controls player input pathfinding and movement rules per Cluedo:
// - walk freely on hallway/spawn tiles
// - stepping on a door = entering the adjacent room (turn ends, switch to Suggesting)
// - cannot walk into a room directly without a door
// - cannot walk on walls or invalid tiles
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
    [Tooltip("Uncheck for AI-controlled players.")]
    public bool IsHuman = true;

    private Tile _currentTile;
    public Tile CurrentTile => _currentTile;

    private int _movementBudget;
    private HashSet<Tile> _reachableTiles = new HashSet<Tile>();
    private bool _isMoving = false;

    public bool IsEliminated { get; private set; } = false;

    // ref when the player makes a wrong accusation
    public void Eliminate()
    {
        IsEliminated = true;
        Debug.Log($"{Character} has been Eliminated.");

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color darkened = sr.color;
            darkened.r *= 0.3f;
            darkened.g *= 0.3f;
            darkened.b *= 0.3f;
            sr.color = darkened;
        }
    }

    public Color GetCharacterColor()
    {
        return Character switch
        {
            CharacterType.MissScarlet => Color.red,
            CharacterType.ColMustard  => new Color(1f, 0.8f, 0f),
            CharacterType.MrsWhite    => Color.white,
            CharacterType.MrGreen     => Color.green,
            CharacterType.MrsPeacock  => new Color(0f, 0.4f, 1f),
            CharacterType.ProfPlum    => new Color(0.5f, 0f, 0.5f),
            _ => Color.white
        };
    }

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

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = GetCharacterColor();
    }

    // secret passage to the linked room triggers a suggestion
    public void UseSecretPassage()
    {
        if (_currentTile?.SecretPassageDestination == null)
        {
            Debug.LogWarning($"{Character}: No secret passage destination set.");
            GameManager.Instance.ChangeState(GameManager.GameState.EndTurn);
            return;
        }

        Tile dest = GridManager.Instance?.GetRoomTile(_currentTile.SecretPassageDestination);
        if (dest == null)
        {
            Debug.LogWarning($"{Character}: Secret passage dest tile not found.");
            GameManager.Instance.ChangeState(GameManager.GameState.EndTurn);
            return;
        }

        _currentTile = dest;
        transform.position = dest.transform.position;
        Debug.Log($"{Character} used the secret passage to {dest.RoomData?.CardName}.");
        GameManager.Instance.ChangeState(GameManager.GameState.Suggesting);
    }

    // teleports char to a tile used when they are named in a suggestion
    public void TeleportToTile(Tile targetTile)
    {
        ClearReachableHighlights();
        if (targetTile != null && !_isMoving)
        {
            _currentTile = targetTile;
            transform.position = targetTile.transform.position + new Vector3(-0.2f, 0.2f, 0);
        }
    }

    public void ClearReachableHighlights()
    {
        foreach (Tile t in _reachableTiles)
            t.RemoveHighlight();
        _reachableTiles.Clear();
    }

    private void HandleDiceRolled(int rollTotal)
    {
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != this) return;

        _movementBudget = rollTotal;

        if (_currentTile != null)
        {
            _reachableTiles = Pathfinder.GetReachableTiles(_currentTile, _movementBudget);
            Debug.Log($"{Character}: {_reachableTiles.Count} reachable tiles for roll of {_movementBudget}.");

            foreach (Tile t in _reachableTiles)
                t.Highlight(Color.green);
        }
    }

    private void Update()
    {
        if (TurnManager.Instance == null || TurnManager.Instance.CurrentPlayer != this) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Moving || _isMoving) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandleMouseClick();
    }

    private void HandleMouseClick()
    {
        Vector2 mousePos = Vector2.zero;
        if (Mouse.current != null)
            mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        Collider2D[] hitColliders = Physics2D.OverlapPointAll(mousePos);

        foreach (Collider2D hit in hitColliders)
        {
            Tile clickedTile = hit.GetComponent<Tile>();
            if (clickedTile != null && _reachableTiles.Contains(clickedTile))
            {
                // Check if another player is already on this tile
                if (IsTileOccupied(clickedTile))
                {
                    Debug.Log($"{Character}: Tile is occupied, pick another.");
                    return;
                }
                ClearReachableHighlights();
                StartCoroutine(MoveToTile(clickedTile));
                return;
            }
        }
    }

    private bool IsTileOccupied(Tile tile)
    {
        foreach (PlayerController player in TurnManager.Instance.GetPlayers())
        {
            if (player == this) continue;
            if (player.CurrentTile == tile && !player.IsEliminated)
                return true;
        }
        return false;
    }

    private IEnumerator MoveToTile(Tile targetTile)
    {
        _isMoving = true;
        _currentTile = targetTile;

        Vector3 startPos = transform.position;
        Vector3 targetPos = targetTile.transform.position;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        _isMoving = false;
        _reachableTiles.Clear();

        // door arrival = entering the adjacent room
        if (_currentTile.Type == Tile.TileType.Room || _currentTile.Type == Tile.TileType.Door)
        {
            string roomName = _currentTile.RoomData != null ? _currentTile.RoomData.CardName : "a room";
            Debug.Log($"{Character} entered {roomName} — switching to Suggesting.");
            GameManager.Instance.ChangeState(GameManager.GameState.Suggesting);
        }
        else if (_currentTile.Type == Tile.TileType.SecretPassage)
        {
            Debug.Log($"{Character} landed on a secret passage — teleporting.");
            UseSecretPassage();
        }
        else
        {
            Debug.Log($"{Character} landed in hallway — turn ends.");
            GameManager.Instance.ChangeState(GameManager.GameState.EndTurn);
        }
    }
}