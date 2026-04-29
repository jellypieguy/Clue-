using UnityEngine;
using System.Collections.Generic;
using TMPro;

// tile prefab. one instance per cell of the grid
// movement rules:
// - hallway, spawn = walkable freely
// - door = walkable, but stepping on it means entering the room (Pathfinder treats as terminal)
// - room interior = NOT directly walkable (must enter via door)
// - wall, invalid, cellar = blocked
public class Tile : MonoBehaviour
{
    public enum TileType
    {
        Hallway, Wall, Room, Door, Cellar, Invalid, Spawn, SecretPassage
    }
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public bool IsWalkable { get; private set; }
    public TileType Type { get; private set; }

    [Header("room data")]
    [Tooltip("Set card data here. Doors also get RoomData (their adjacent room).")]
    public CardData RoomData;

    [Tooltip("For corner rooms only — the secret passage destination.")]
    public CardData SecretPassageDestination;

    private SpriteRenderer _sr;
    private Color _originalColor;

    public void Setup(int x, int y, TileType type, char mapChar = ' ')
    {
        GridX = x;
        GridY = y;
        Type = type;

        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>();

        transform.localScale = new Vector3(0.98f, 0.98f, 1f);

        // walkable: hallway, door, spawn. room interiors NOT directly walkable.
        IsWalkable = type == TileType.Hallway || type == TileType.Door || type == TileType.Spawn || type == TileType.SecretPassage;

        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            switch (type)
            {
                case TileType.Hallway:
                case TileType.Spawn:
                    _sr.color = new Color(1f, 1f, 0.85f); break;     // cream
                case TileType.Room:
                    _sr.color = new Color(0.6f, 0.8f, 0.9f); break;  // light blue
                case TileType.Door:
                    _sr.color = new Color(0.9f, 0.7f, 0.2f); break;  // gold
                case TileType.Cellar:
                    _sr.color = new Color(0.6f, 0.1f, 0.1f); break;  // maroon
                case TileType.Wall:
                case TileType.SecretPassage:
                    _sr.color = new Color(0.8f, 0.4f, 0.9f); break;  // purple
                case TileType.Invalid:
                    _sr.color = new Color(0.2f, 0.8f, 0.2f); break;  // green (outside board)
            }
            _originalColor = _sr.color;
        }

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

    // called by GridManager after grid generation to hook up the room CardData
    public void SetRoomData(CardData roomData)
    {
        RoomData = roomData;
    }

    // removes room from the game (greyed out)
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