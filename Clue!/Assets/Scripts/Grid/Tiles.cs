using UnityEngine;
using System.Collections.Generic;
using TMPro;

// tile prefab
public class Tile : MonoBehaviour
{
    public enum TileType
    {
        Hallway, Wall, Room, Door, Cellar, Invalid, Spawn
    }

    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public bool IsWalkable { get; private set; }
    public TileType Type { get; private set; }

    [Header("room data")]
    [Tooltip("set card data here.")]
    public CardData RoomData;

    [Tooltip("For corner rooms only — the secret passage destination.")]
    public CardData SecretPassageDestination;

    private SpriteRenderer _sr;
    private Color _originalColor;
    /// tiles coods and data type
    public void Setup(int x, int y, TileType type, char mapChar = ' ')
    {
        GridX = x;
        GridY = y;
        Type = type;

        // tiles have colliders so m1  can be detected
        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>();

        transform.localScale = new Vector3(0.98f, 0.98f, 1f);

        IsWalkable = type == TileType.Hallway || type == TileType.Door || type == TileType.Spawn || type == TileType.Room;

        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            switch (type)
            {
                case TileType.Hallway:
                case TileType.Spawn:
                    _sr.color = new Color(1f, 1f, 0.85f); break; // white
                case TileType.Room:
                    _sr.color = new Color(0.6f, 0.8f, 0.9f); break; //  blue
                case TileType.Door:
                    _sr.color = new Color(0.9f, 0.7f, 0.2f); break; // Gold
                case TileType.Cellar:
                    _sr.color = new Color(0.6f, 0.1f, 0.1f); break; // Maroon
                case TileType.Wall:
                case TileType.Invalid:
                    _sr.color = new Color(0.2f, 0.8f, 0.2f); break; // Green
            }
            _originalColor = _sr.color;
        }

        // logic display the char from the map file 

        TextMeshPro textMesh = GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = mapChar.ToString().ToLower();
            textMesh.GetComponent<MeshRenderer>().sortingOrder = 10;
            textMesh.fontSize = 6f;
            textMesh.color = new Color(0, 0, 0, 0.8f);
            if (Type == TileType.Invalid && mapChar == ' ')
                textMesh.enabled = false;
        }
    }

    // called by the gridman after grid gen hooks up the cluedo room
    public void SetRoomData(CardData roomData)
    {
        RoomData = roomData;
    }

    // removes room from the game
    public void DisableAsInactive()
    {
        IsWalkable = false;
        if (_sr != null)
        {
            _sr.color = new Color(0.22f, 0.22f, 0.22f);
            _originalColor = _sr.color;
        }
    }

    public void Highlight(Color highlightColor)
    {
        if (_sr != null) _sr.color = highlightColor;
    }

    public void RemoveHighlight()
    {
        if (_sr != null) _sr.color = _originalColor;
    }

    public List<Tile> GetWalkableNeighbors()
    {
        return GridManager.Instance.GetWalkableNeighbors(GridX, GridY);
    }
}