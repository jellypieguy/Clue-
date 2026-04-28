using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// controls player input pathfinding and movement 
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

        // greys the player to marks removed
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

    // secret passage to the linked room triggers a suggestion
    public void UseSecretPassage()
    {
        if (_currentTile?.SecretPassageDestination == null) return;
        Tile dest = GridManager.Instance?.GetRoomTile(_currentTile.SecretPassageDestination);
        if (dest == null)
        {
            Debug.LogWarning($"{Character}: Secret passage dest tile not found.");
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
        if (targetTile != null && !_isMoving)
        {
            _currentTile = targetTile;
            transform.position = targetTile.transform.position + new Vector3(-0.2f, 0.2f, 0);
        }
    }

    private void HandleDiceRolled(int rollTotal)
    {
        // care the roll if it's currently the player turn
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
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != this) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Moving || _isMoving) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            HandleMouseClick();
    }

    private void HandleMouseClick()
    {
        Vector2 mousePos = Vector2.zero;
        if (Mouse.current != null)
            mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        Debug.Log($"Click at {mousePos}");

        //  handles cases where a player or weapon token collider sits ahove the tile
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(mousePos);

        foreach (Collider2D hit in hitColliders)
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
        AudioManager.Instance?.PlayFootstep();

        if (_currentTile.Type == Tile.TileType.Room)
        {
            string roomName = _currentTile.RoomData != null ? _currentTile.RoomData.CardName : "a room";
            Debug.Log($"Entered {roomName} — switching to Suggesting.");
            GameManager.Instance.ChangeState(GameManager.GameState.Suggesting);
        }
        else
        {
            Debug.Log("Landed in hallway — turn ends.");
            GameManager.Instance.ChangeState(GameManager.GameState.EndTurn);
        }
    }
}
