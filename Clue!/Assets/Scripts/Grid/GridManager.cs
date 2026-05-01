using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public int GridWidth { get; private set; } = 24;
    public int GridHeight { get; private set; } = 25;

    [Header("Level Design Data")]
    public TextAsset boardMapFile;
    public float tileSize = 1f;
    [SerializeField] private Tile tilePrefab;

    [Header("Tile Sprites")]
    [SerializeField] private Sprite hallwaySprite;
    [SerializeField] private Sprite roomSprite;
    [SerializeField] private Sprite doorSprite;
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private Sprite cellarSprite;
    [SerializeField] private Sprite spawnSprite;

    [Header("Board Border & Background")]
    [SerializeField] private Sprite boardBorderSprite;
    [Tooltip("How many tiles thick the border frame is on each side (0.5 = half a tile)")]
    [SerializeField] private float borderThickness = 0.5f;
    [SerializeField] private Sprite sceneBackgroundSprite;
    [Tooltip("How many tiles wide/tall the scene background covers (should be larger than the board)")]
    [SerializeField] private float backgroundSize = 40f;

    [Header("Room Card Data")]
    [Tooltip("0=Conservatory 1=Ballroom 2=Kitchen 3=DiningRoom 4=BilliardRoom 5=Library 6=Lounge 7=Hall 8=Study")]
    [SerializeField] private CardData[] roomCards = new CardData[9];
    [Header("Envelope")]
    [SerializeField] private Sprite envelopeSprite;

    //  boxes for rooms nocollide
    private static readonly int[,] RoomRegions = new int[9, 4]
    {
        {  0,    5,    0,    4 },  // 0: Conservatory its like playing jenga tryna map this shit out
        {  8,    15,   -1,    5 },  // 1: Ballroom
        {  18,   23,   0,    6 },  // 2: Kitchen 
        {  16,   23,   8,    14 }, // 3: Dining Room 
        {  0,    5,    7,    11 }, // 4: Billiard Room 
        {  0,    6,    13,   17 }, // 5: Library 
        {  17,   23,   18,   23 }, // 6: Lounge 
        {  9,    14,   15,   24 }, // 7: Hall
        {  0,    6,    20,   23 }, // 8: Study 
    };

    private Tile[,] grid;
    private Tile[] spawnPoints = new Tile[6];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Prep bounds early so the camera doesn't freak out on Start
        CalculateDimensions();
        GenerateGrid();
    }

    private void CalculateDimensions()
    {
        if (boardMapFile != null)
        {
            var mapLines = boardMapFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            GridHeight = mapLines.Length;

            int maxWidth = 0;
            foreach (var line in mapLines) maxWidth = Mathf.Max(maxWidth, line.Trim().Length);
            GridWidth = maxWidth;
        }
        else
        {
            GridWidth = 24;
            GridHeight = 25;
        }
    }

    private void GenerateGrid()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);

        var mapLines = boardMapFile != null
            ? boardMapFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
            : null;

        grid = new Tile[GridWidth, GridHeight];
        spawnPoints = new Tile[6];

        float startX = (-GridWidth / 2f + 0.5f) * tileSize;
        float startY = (-GridHeight / 2f + 0.5f) * tileSize;

        Sprite tileSprite = null;
        if (tilePrefab != null)
        {
            var sr = tilePrefab.GetComponent<SpriteRenderer>();
            if (sr != null) tileSprite = sr.sprite;
        }

        // Layer 1 (furthest back): scene background fills the whole camera view
        if (sceneBackgroundSprite != null)
            CreateBackgroundLayer("SceneBackground", sceneBackgroundSprite, Color.white, 1f, backgroundSize, backgroundSize, -2002);
        else
            CreateBackgroundLayer("SceneBackground", tileSprite, new Color(0.12f, 0.08f, 0.05f), 1f, backgroundSize, backgroundSize, -2002);

        // Layer 2: border frame — sized to the exact board, plus borderThickness on each side
        // boardBackground (layer 3) is GridWidth+1 wide, so border needs GridWidth+1 + borderThickness*2 to show borderThickness tiles on each edge
        float borderW = GridWidth  + 1f + borderThickness * 2;
        float borderH = GridHeight + 1f + borderThickness * 2;
        if (boardBorderSprite != null)
            CreateBackgroundLayer("BoardBorder", boardBorderSprite, Color.white, 0.6f, borderW, borderH, -2001);
        else
            CreateBackgroundLayer("BoardBorder", tileSprite, Color.black, 0.6f, borderW, borderH, -2001);

        // Layer 3: dark board surface behind the tiles
        CreateBackgroundLayer("BoardBackground", tileSprite, new Color(0.05f, 0.05f, 0.05f), 0.5f, GridWidth + 1f, GridHeight + 1f, -2000);

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var spawnedTile = Instantiate(tilePrefab, transform);
                spawnedTile.transform.localPosition = new Vector3(startX + x * tileSize, startY + y * tileSize, 0);
                spawnedTile.transform.localScale = new Vector3(1f * tileSize, 1f * tileSize, 1f);

                char rawChar = ' ';
                int invertedY = (GridHeight - 1) - y;

                if (mapLines != null && invertedY >= 0 && invertedY < mapLines.Length && x < mapLines[invertedY].Length)
                    rawChar = mapLines[invertedY][x];

                var parsedType = ParseMapData(x, y, mapLines);
                var typeSprite = parsedType switch
                {
                    Tile.TileType.Hallway => hallwaySprite,
                    Tile.TileType.Spawn => spawnSprite != null ? spawnSprite : hallwaySprite,
                    Tile.TileType.Room => roomSprite,
                    Tile.TileType.Door => doorSprite,
                    Tile.TileType.Wall => wallSprite,
                    Tile.TileType.Cellar => cellarSprite,
                    _ => null
                };

                spawnedTile.Setup(x, y, parsedType, rawChar, typeSprite);

                // --- THE TILE OVERLAP FIX ---
                // Forces Unity to draw top-left tiles first, and bottom-right tiles last.
                // Keeps them in negative numbers so Room Images and Text render above them safely.
                if (spawnedTile.TryGetComponent<SpriteRenderer>(out var tileSr))
                {
                    tileSr.sortingOrder = -1000 + (y * 10) - x;
                }

                if (char.IsDigit(rawChar))
                {
                    int spawnIndex = (int)char.GetNumericValue(rawChar) - 1;
                    if (spawnIndex >= 0 && spawnIndex < 6) spawnPoints[spawnIndex] = spawnedTile;
                }

                spawnedTile.name = $"Tile_{x}_{y}_{parsedType}";
                grid[x, y] = spawnedTile;
            }
        }

        ApplySpawnPointHighlights();
        AssignRoomData();
        AssignDoorRoomData();
        AssignSecretPassages();
        AddRoomLabels();
        AddRoomImages();
        ApplyGameSettings();
        AddEnvelopeImage();
    }

    private void CreateBackgroundLayer(string layerName, Sprite sprite, Color color, float zOffset, float scaleX, float scaleY, int sortingOrder = -2000)
    {
        var bg = new GameObject(layerName);
        bg.transform.SetParent(transform);
        bg.transform.localPosition = new Vector3(0, 0, zOffset);

        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.sortingOrder = sortingOrder;

        // Target world-space size
        float targetW = scaleX * tileSize;
        float targetH = scaleY * tileSize;

        // Normalize by sprite's natural world size so any PPU/resolution works correctly
        if (sprite != null && sprite.bounds.size.x > 0 && sprite.bounds.size.y > 0)
            bg.transform.localScale = new Vector3(targetW / sprite.bounds.size.x, targetH / sprite.bounds.size.y, 1);
        else
            bg.transform.localScale = new Vector3(targetW, targetH, 1);
    }

    private void AssignRoomData()
    {
        if (roomCards == null || roomCards.Length < 9) return;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var tile = grid[x, y];
                if (tile == null || tile.Type != Tile.TileType.Room) continue;

                for (int i = 0; i < 9; i++)
                {
                    if (x >= RoomRegions[i, 0] && x <= RoomRegions[i, 1] && y >= RoomRegions[i, 2] && y <= RoomRegions[i, 3])
                    {
                        tile.SetRoomData(roomCards[i]);
                        break;
                    }
                }
            }
        }
    }

    private void AssignDoorRoomData()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var tile = grid[x, y];
                if (tile == null || tile.Type != Tile.TileType.Door) continue;

                var adjacentRoom = FindAdjacentRoomData(x, y);
                if (adjacentRoom != null) tile.SetRoomData(adjacentRoom);
            }
        }
    }

    private CardData FindAdjacentRoomData(int x, int y)
    {
        var offsets = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };
        foreach (var (dx, dy) in offsets)
        {
            int nx = x + dx, ny = y + dy;
            if (nx < 0 || nx >= GridWidth || ny < 0 || ny >= GridHeight) continue;

            var neighbor = grid[nx, ny];
            if (neighbor != null && neighbor.Type == Tile.TileType.Room && neighbor.RoomData != null)
                return neighbor.RoomData;
        }
        return null;
    }

    private void AddRoomLabels()
    {
        if (roomCards == null) return;

        float startX = (-GridWidth / 2f + 0.5f) * tileSize;
        float startY = (-GridHeight / 2f + 0.5f) * tileSize;

        for (int i = 0; i < 9 && i < roomCards.Length; i++)
        {
            var card = roomCards[i];
            if (card == null) continue;

            float cx = (RoomRegions[i, 0] + RoomRegions[i, 1]) * 0.5f;
            float cy = (RoomRegions[i, 2] + RoomRegions[i, 3]) * 0.5f;

            var go = new GameObject($"Label_{card.CardName}");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(startX + cx * tileSize, startY + cy * tileSize, -0.05f);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = card.CardName;
            tmp.fontSize = 2.4f * tileSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0f, 0f, 0f, 0.60f);
            tmp.rectTransform.sizeDelta = new Vector2(5f * tileSize, 3f * tileSize);

            if (go.TryGetComponent<MeshRenderer>(out var mr)) mr.sortingOrder = 6;
        }
    }

    private void AddRoomImages()
    {
        if (roomCards == null) return;

        float startX = (-GridWidth / 2f + 0.5f) * tileSize;
        float startY = (-GridHeight / 2f + 0.5f) * tileSize;

        for (int i = 0; i < 9 && i < roomCards.Length; i++)
        {
            var card = roomCards[i];

            if (card == null || card.BoardSprite == null) continue;

            if (GameSettings.Instance != null && GameSettings.Instance.IsRoomDisabled(card)) continue;

            float cx = (RoomRegions[i, 0] + RoomRegions[i, 1]) * 0.5f;
            float cy = (RoomRegions[i, 2] + RoomRegions[i, 3]) * 0.5f;

            var go = new GameObject($"RoomGraphic_{card.CardName}");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(startX + cx * tileSize, startY + cy * tileSize, 0.1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = card.BoardSprite;
            sr.sortingOrder = 3;

            float roomWidthTiles = (RoomRegions[i, 1] - RoomRegions[i, 0] + 1);
            float roomHeightTiles = (RoomRegions[i, 3] - RoomRegions[i, 2] + 1);

            float spriteWidth = sr.sprite.bounds.size.x;
            float spriteHeight = sr.sprite.bounds.size.y;

            if (spriteWidth > 0 && spriteHeight > 0)
            {
                float targetScaleX = (roomWidthTiles * tileSize) / spriteWidth;
                float targetScaleY = (roomHeightTiles * tileSize) / spriteHeight;
                go.transform.localScale = new Vector3(targetScaleX, targetScaleY, 1f);
            }
        }
    }

    private void AssignSecretPassages()
    {
        if (roomCards == null || roomCards.Length < 9) return;

        var pairs = new[,] { { 2, 8 }, { 8, 2 }, { 0, 6 }, { 6, 0 } };

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var t = grid[x, y];
                if (t == null || t.RoomData == null) continue;

                for (int p = 0; p < pairs.GetLength(0); p++)
                {
                    if (t.RoomData == roomCards[pairs[p, 0]])
                    {
                        t.SecretPassageDestination = roomCards[pairs[p, 1]];
                        break;
                    }
                }
            }
        }
    }

    private void ApplySpawnPointHighlights()
    {
        var colors = new[]
        {
            new Color(1f, 0.2f, 0.2f),    // Scarlet
            new Color(1f, 0.8f, 0f),      // Mustard
            new Color(0.9f, 0.9f, 0.9f),  // White
            new Color(0.2f, 0.8f, 0.2f),  // Green
            new Color(0.2f, 0.2f, 1f),    // Peacock
            new Color(0.6f, 0.2f, 0.8f)   // Plum
        };

        for (int i = 0; i < 6; i++)
        {
            if (spawnPoints[i] != null) spawnPoints[i].Highlight(colors[i]);
        }
    }

    private Tile.TileType ParseMapData(int x, int y, string[] lines)
    {
        if (lines == null || lines.Length == 0) return Tile.TileType.Invalid;

        int invertedY = (GridHeight - 1) - y;
        if (invertedY < 0 || invertedY >= lines.Length || x >= lines[invertedY].Length)
            return Tile.TileType.Invalid;

        char c = char.ToUpper(lines[invertedY][x]);
        if (char.IsDigit(c)) return Tile.TileType.Spawn;

        switch (c)
        {
            case 'W':
            case 'B':
                return Tile.TileType.Wall;
            case 'R':
                return Tile.TileType.Room;
            case 'D':
                return Tile.TileType.Door;
            case 'C':
            case 'F':
                return Tile.TileType.Cellar;
            case 'H':
                return Tile.TileType.Hallway;
            case 'S':
                return Tile.TileType.SecretPassage;
            default:
                return Tile.TileType.Invalid;
        }
    }

    public List<Tile> GetWalkableNeighbors(int x, int y)
    {
        var current = grid[x, y];
        var neighbors = new List<Tile>();

        var offsets = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };
        foreach (var (ox, oy) in offsets)
        {
            int nx = x + ox, ny = y + oy;
            if (!IsValid(nx, ny)) continue;

            var neighbor = grid[nx, ny];
            bool canEnter = false;

            switch (neighbor.Type)
            {
                case Tile.TileType.Hallway:
                case Tile.TileType.Spawn:
                case Tile.TileType.Door:
                case Tile.TileType.SecretPassage:
                    canEnter = true;
                    break;
                case Tile.TileType.Room:
                    canEnter = current.Type == Tile.TileType.Door || current.Type == Tile.TileType.Room;
                    break;
            }

            if (canEnter) neighbors.Add(neighbor);
        }

        return neighbors;
    }
    private void AddEnvelopeImage()
    {
        if (envelopeSprite == null) return;

        float startX = (-GridWidth / 2f + 0.5f) * tileSize;
        float startY = (-GridHeight / 2f + 0.5f) * tileSize;

        float cx = 11.5f;
        float cy = 10.1f;

        var go = new GameObject("EnvelopeGraphic");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(startX + cx * tileSize, startY + cy * tileSize, 0.1f);
        go.transform.rotation = Quaternion.Euler(0, 0, 90f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = envelopeSprite;
        sr.sortingOrder = 5;

        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
    }
    private bool IsValid(int x, int y) => x >= 0 && x < GridWidth && y >= 0 && y < GridHeight && grid[x, y] != null;

    public void ApplyGameSettings()
    {
        if (GameSettings.Instance == null) return;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var tile = grid[x, y];
                if (tile != null && tile.Type == Tile.TileType.Room && tile.RoomData != null && GameSettings.Instance.IsRoomDisabled(tile.RoomData))
                {
                    tile.DisableAsInactive();
                }
            }
        }
    }

    public List<Tile> GetRoomTilesWithData()
    {
        var result = new List<Tile>();
        foreach (var t in grid)
        {
            if (t != null && t.Type == Tile.TileType.Room && t.RoomData != null && t.IsWalkable)
                result.Add(t);
        }
        return result;
    }

    public Tile GetRoomTile(CardData roomData)
    {
        if (roomData == null) return null;
        foreach (var t in grid)
        {
            if (t != null && t.RoomData == roomData && t.IsWalkable) return t;
        }
        return null;
    }

    public Tile GetTileAt(int x, int y) => IsValid(x, y) ? grid[x, y] : null;

    public Tile GetStartingTile(PlayerController.CharacterType character)
    {
        int index = (int)character;
        return (index >= 0 && index < spawnPoints.Length && spawnPoints[index] != null)
            ? spawnPoints[index]
            : GetTileAt(0, 0);
    }
}