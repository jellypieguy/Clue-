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

    [Header("Board Image")]
    [SerializeField] private Sprite boardImage;

    [Header("Room Card Data")]
    [Tooltip("0=Conservatory 1=Ballroom 2=Kitchen 3=DiningRoom 4=BilliardRoom 5=Library 6=Lounge 7=Hall 8=Study")]
    [SerializeField] private CardData[] roomCards = new CardData[9];

    [Header("Room Images (same order as Room Cards)")]
    [Tooltip("0=Conservatory 1=Ballroom 2=Kitchen 3=DiningRoom 4=BilliardRoom 5=Library 6=Lounge 7=Hall 8=Study")]
    [SerializeField] private Sprite[] roomImages = new Sprite[9];

    // boxes for rooms nocollide. format: xMin, xMax, yMin, yMax.
    private static readonly int[,] RoomRegions = new int[9, 4]
    {
        {  0,    5,    0,    4 },  // 0: Conservatory (Bottom Left)
        {  10,   15,   0,    5 },  // 1: Ballroom (Bottom Center)
        {  18,   23,   0,    5 },  // 2: Kitchen (Bottom Right)
        {  17,   23,   10,   16 }, // 3: Dining Room (Middle Right)
        {  0,    5,    7,    10 }, // 4: Billiard Room (Middle Left Lower)
        {  0,    6,    13,   16 }, // 5: Library (Middle Left Upper)
        {  17,   23,   19,   24 }, // 6: Lounge (Top Right)
        {  9,    14,   18,   24 }, // 7: Hall (Top Center)
        {  0,    6,   21,   24 },  // 8: Study (Top Left)
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

        // Board background image or base layer
        GameObject boardBase = new("BoardBackground");
        boardBase.transform.SetParent(transform);
        boardBase.transform.localPosition = new(0, 0, 0.5f);
        SpriteRenderer baseSR = boardBase.AddComponent<SpriteRenderer>();
        baseSR.sortingOrder = 2;

        if (boardImage != null)
        {
            baseSR.sprite = boardImage;
            baseSR.color = Color.white;
            float spriteW = boardImage.bounds.size.x;
            float spriteH = boardImage.bounds.size.y;
            boardBase.transform.localScale = new Vector3(
                (GridWidth / spriteW) * 1.21f,
                (GridHeight / spriteH) * 1.21f,
                1f
            );
        }
        else
        {
            SpriteRenderer prefabSR = tilePrefab != null ? tilePrefab.GetComponent<SpriteRenderer>() : null;
            baseSR.sprite = prefabSR != null ? prefabSR.sprite : null;
            baseSR.color = new Color(0.05f, 0.05f, 0.05f);
            baseSR.drawMode = SpriteDrawMode.Simple;
            boardBase.transform.localScale = new(GridWidth + 1f, GridHeight + 1f, 1);
        }

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var spawnedTile = Instantiate(tilePrefab, transform);
                spawnedTile.transform.localPosition = new Vector3(startX + x * tileSize, startY + y * tileSize, 0);

                // No visual gaps between grid tiles
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
                    Tile.TileType.SecretPassage => hallwaySprite,
                    _ => null
                };

                spawnedTile.Setup(x, y, parsedType, rawChar, typeSprite);

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
        AddRoomImagesAndLabels();
        ApplyGameSettings();
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

    private void AddRoomImagesAndLabels()
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
            float roomW = RoomRegions[i, 1] - RoomRegions[i, 0];
            float roomH = RoomRegions[i, 3] - RoomRegions[i, 2];

            // Room background image
            if (roomImages != null && i < roomImages.Length && roomImages[i] != null)
            {
                GameObject imgObj = new($"RoomImage_{card.CardName}");
                imgObj.transform.SetParent(transform);
                imgObj.transform.position = new Vector3(startX + cx * tileSize, startY + cy * tileSize, 0.05f);

                SpriteRenderer sr = imgObj.AddComponent<SpriteRenderer>();
                sr.sprite = roomImages[i];
                sr.color = Color.white;
                sr.sortingOrder = 1;

                float spriteW = sr.sprite.bounds.size.x;
                float spriteH = sr.sprite.bounds.size.y;
                imgObj.transform.localScale = new Vector3(
                    ((roomW + 1f) * tileSize) / spriteW,
                    ((roomH + 1f) * tileSize) / spriteH,
                    1f
                );
            }

            // Room name label
            GameObject go = new($"Label_{card.CardName}");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(startX + cx * tileSize, startY + cy * tileSize, -0.05f);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = card.CardName;
            tmp.fontSize = 2.4f * tileSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 0.85f);
            tmp.rectTransform.sizeDelta = new Vector2(5f * tileSize, 3f * tileSize);

            if (go.TryGetComponent<MeshRenderer>(out var mr)) mr.sortingOrder = 6;
        }
    }

    private void AssignSecretPassages()
    {
        if (roomCards == null || roomCards.Length < 9) return;

        // 0=Conservatory, 2=Kitchen, 6=Lounge, 8=Study
        var pairs = new[,] { { 2, 8 }, { 8, 2 }, { 0, 6 }, { 6, 0 } };

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                var t = grid[x, y];
                if (t == null || t.Type != Tile.TileType.SecretPassage) continue;

                // Find which room region this secret passage tile is inside
                for (int i = 0; i < 9; i++)
                {
                    int xMin = RoomRegions[i, 0], xMax = RoomRegions[i, 1];
                    int yMin = RoomRegions[i, 2], yMax = RoomRegions[i, 3];

                    if (x >= xMin && x <= xMax && y >= yMin && y <= yMax)
                    {
                        t.SetRoomData(roomCards[i]);

                        for (int p = 0; p < pairs.GetLength(0); p++)
                        {
                            if (pairs[p, 0] == i)
                            {
                                t.SecretPassageDestination = roomCards[pairs[p, 1]];
                                break;
                            }
                        }
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

        // Check S before digit so secret passages are not misread as spawns
        if (c == 'S') return Tile.TileType.SecretPassage;
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
                    canEnter = current.Type == Tile.TileType.Door || current.Type == Tile.TileType.Room || current.Type == Tile.TileType.SecretPassage;
                    break;
            }

            if (canEnter) neighbors.Add(neighbor);
        }

        return neighbors;
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