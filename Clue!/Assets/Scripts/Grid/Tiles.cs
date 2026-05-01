using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class Tile : MonoBehaviour
{
    public enum TileType { Hallway, Wall, Room, Door, Cellar, Invalid, Spawn, SecretPassage }

    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public bool IsWalkable { get; private set; }
    public TileType Type { get; private set; }

    [Header("Room Setup")]
    public CardData RoomData;
    public CardData SecretPassageDestination;

    private SpriteRenderer spriteRenderer;
    private Color defaultColor;

    public void Setup(int x, int y, TileType type, char mapChar = ' ', Sprite overrideSprite = null)
    {
        GridX = x;
        GridY = y;
        Type = type;

        if (!TryGetComponent<Collider2D>(out _))
            gameObject.AddComponent<BoxCollider2D>();

        // harmless i think
        transform.localScale = new Vector3(0.98f, 0.98f, 1f);

        IsWalkable = type == TileType.Hallway || type == TileType.Door || type == TileType.Spawn || type == TileType.SecretPassage;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (overrideSprite != null) spriteRenderer.sprite = overrideSprite;

            switch (type)
            {
                case TileType.Hallway:
                case TileType.Spawn:
                    spriteRenderer.color = new Color(1f, 1f, 0.85f);
                    break;
                case TileType.Door:
                    spriteRenderer.color = new Color(0.9f, 0.7f, 0.2f);
                    break;
                case TileType.Cellar:
                    spriteRenderer.color = new Color(0.6f, 0.1f, 0.1f);
                    break;
                case TileType.Wall:
                case TileType.SecretPassage:
                    spriteRenderer.color = new Color(0.8f, 0.4f, 0.9f);
                    break;
                case TileType.Room:
                case TileType.Invalid:
                    spriteRenderer.color = new Color(0f, 0f, 0f, 0f); // transparent
                    break;
                default:
                    spriteRenderer.color = Color.white;
                    break;
            }
            defaultColor = spriteRenderer.color;
        }

        var textMesh = GetComponentInChildren<TextMeshPro>();
        if (textMesh != null)
        {
            textMesh.text = mapChar.ToString().ToLower();
            textMesh.GetComponent<MeshRenderer>().sortingOrder = 10;
            textMesh.fontSize = 6f;
            textMesh.color = new Color(0, 0, 0, 0.8f);
    
            // hide text on room and invalid tiles — room images cover them
            if (Type == TileType.Room || Type == TileType.Invalid || mapChar == ' ')
            {
                textMesh.enabled = false;
            }
        }
    }

    public void SetRoomData(CardData data) => RoomData = data;

    public void DisableAsInactive()
    {
        IsWalkable = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.22f, 0.22f, 0.22f);
            defaultColor = spriteRenderer.color;
        }
    }

    public void Highlight(Color color)
    {
        if (spriteRenderer) spriteRenderer.color = color;
    }

    public void RemoveHighlight()
    {
        if (spriteRenderer) spriteRenderer.color = defaultColor;
    }

    public List<Tile> GetWalkableNeighbors() => GridManager.Instance.GetWalkableNeighbors(GridX, GridY);
}