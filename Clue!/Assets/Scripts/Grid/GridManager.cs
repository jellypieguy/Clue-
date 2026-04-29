using UnityEngine;
using System.Collections.Generic;
using TMPro;

// gens owns the 2D tile array for the cluedo board
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public int GridWidth { get; private set; } = 24;
    public int GridHeight { get; private set; } = 25;

    [Header("Level Design Data")]
    [Tooltip("file with the grid layout using char: W, R, D, C, X, H, F, 1-6.")]
    public TextAsset boardMapFile;

    [SerializeField] private Tile tilePrefab;

    [Header("Room Card Data (assign in Inspector)")]
    [Tooltip("0=Conservatory 1=Ballroom 2=Kitchen 3=DiningRoom 4=BilliardRoom 5=Library 6=Lounge 7=Hall 8=Study")]
    [SerializeField] private CardData[] _roomCards = new CardData[9];

    // bunding boxes for each room Format xMin, xMax, yMin, yMax Y=0 is the bottom.
    private static readonly int[,] RoomRegions = new int[9, 4]
    {
        // xMin  xMax  yMin  yMax
        {  1,    6,    20,   25 },  // 0: consarvatory
        {  9,    16,   19,   25 },  // 1: ballroom
        {  19,   24,   20,   25 },  // 2: kitchen
        {  1,    9,    8,    18 },  // 3: dining room
        {  17,   24,   13,   18 },  // 4: billiard room
        {  17,   24,   5,    12 },  // 5: library
        {  1,    8,    1,    6  },  // 6: lounge
        {  9,    16,   1,    6  },  // 7: hall
        {  17,   24,   1,    6  },  // 8: study
    };

    private Tile[,] _grid;
    private Tile[] _spawnPoints = new Tile[6];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Calc dimension early so CameraFit can use them in Start
        CalculateDimensions();
        GenerateGrid();
    }

    private void CalculateDimensions()
    {
        if (boardMapFile != null)
        {
            string[] mapLines = boardMapFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            GridHeight = mapLines.Length;
            int maxWidth = 0;
            foreach (string line in mapLines) maxWidth = Mathf.Max(maxWidth, line.Trim().Length);
            GridWidth = maxWidth;
            Debug.Log($"GridManager: Board size {GridWidth}x{GridHeight}");
        }
        else
        {
            GridWidth = 24;
            GridHeight = 25;
        }
    }

    private void Start()
    {
    }

    private void GenerateGrid()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        string[] mapLines = null;
        if (boardMapFile != null)
            mapLines = boardMapFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        _grid = new Tile[GridWidth, GridHeight];
        _spawnPoints = new Tile[6];

        float startX = -GridWidth / 2f + 0.5f;
        float startY = -GridHeight / 2f + 0.5f;

        SpriteRenderer prefabSR = tilePrefab != null ? tilePrefab.GetComponent<SpriteRenderer>() : null;
        Sprite tileSprite = prefabSR != null ? prefabSR.sprite : null;

        // background nd grid
        GameObject boardBase = new("BoardBackground");
        boardBase.transform.SetParent(transform);
        boardBase.transform.localPosition = new(0, 0, 0.5f);
        SpriteRenderer baseSR = boardBase.AddComponent<SpriteRenderer>();
        baseSR.sprite = tileSprite;
        baseSR.color = new(0.05f, 0.05f, 0.05f);
        baseSR.drawMode = SpriteDrawMode.Simple;
        boardBase.transform.localScale = new(GridWidth + 1f, GridHeight + 1f, 1);

        GameObject borderBase = new("BlackBorders");
        borderBase.transform.SetParent(transform);
        borderBase.transform.localPosition = new(0, 0, 0.25f);
        SpriteRenderer borderSR = borderBase.AddComponent<SpriteRenderer>();
        borderSR.sprite = tileSprite;
        borderSR.color = Color.black;
        borderSR.drawMode = SpriteDrawMode.Simple;
        borderBase.transform.localScale = new(GridWidth, GridHeight, 1);

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Tile spawnedTile = Instantiate(tilePrefab, transform);
                spawnedTile.transform.localPosition = new Vector3(startX + x, startY + y, 0);

                char rawChar = ' ';
                int invertedY = (GridHeight - 1) - y;
                if (mapLines != null && invertedY >= 0 && invertedY < mapLines.Length && x < mapLines[invertedY].Length)
                    rawChar = mapLines[invertedY][x];

                Tile.TileType parsedType = ParseMapData(x, y, mapLines);
                spawnedTile.Setup(x, y, parsedType, rawChar);

                if (char.IsDigit(rawChar))
                {
                    int spawnIndex = (int)char.GetNumericValue(rawChar) - 1;
                    if (spawnIndex >= 0 && spawnIndex < 6)
                        _spawnPoints[spawnIndex] = spawnedTile;
                }

                spawnedTile.name = $"Tile_{x}_{y}_{parsedType}";
                _grid[x, y] = spawnedTile;
            }
        }

        ApplySpawnPointHighlights();
        AssignRoomData();
        AssignDoorRoomData();
        AssignSecretPassages();
        AddRoomLabels();
        ApplyGameSettings();
    }

    // 2nd pass after gen tags each room tile with its data via grid pos
    private void AssignRoomData()
    {
        if (_roomCards == null || _roomCards.Length < 9) return;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Tile tile = _grid[x, y];
                if (tile == null || tile.Type != Tile.TileType.Room) continue;

                for (int i = 0; i < 9; i++)
                {
                    int xMin = RoomRegions[i, 0], xMax = RoomRegions[i, 1];
                    int yMin = RoomRegions[i, 2], yMax = RoomRegions[i, 3];

                    if (x >= xMin && x <= xMax && y >= yMin && y <= yMax)
                    {
                        tile.SetRoomData(_roomCards[i]);
                        break;
                    }
                }
            }
        }
    }

    // tags each door tile with the RoomData of an adjacent room tile
    // so stepping on a door is recognised as entering that room
    private void AssignDoorRoomData()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Tile tile = _grid[x, y];
                if (tile == null || tile.Type != Tile.TileType.Door) continue;

                CardData adjacentRoom = FindAdjacentRoomData(x, y);
                if (adjacentRoom != null)
                    tile.SetRoomData(adjacentRoom);
                else
                    Debug.LogWarning($"GridManager: Door at ({x},{y}) has no adjacent room.");
            }
        }
    }

    private CardData FindAdjacentRoomData(int x, int y)
    {
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (nx < 0 || nx >= GridWidth || ny < 0 || ny >= GridHeight) continue;

            Tile neighbour = _grid[nx, ny];
            if (neighbour != null && neighbour.Type == Tile.TileType.Room && neighbour.RoomData != null)
                return neighbour.RoomData;
        }
        return null;
    }

    // Spawns a cent space w/ text label for each room
    private void AddRoomLabels()
    {
        if (_roomCards == null) return;

        float startX = -GridWidth / 2f + 0.5f;
        float startY = -GridHeight / 2f + 0.5f;

        for (int i = 0; i < 9 && i < _roomCards.Length; i++)
        {
            CardData card = _roomCards[i];
            if (card == null) continue;

            // @grace put the label at the centre of the room's box.
            float cx = (RoomRegions[i, 0] + RoomRegions[i, 1]) * 0.5f;
            float cy = (RoomRegions[i, 2] + RoomRegions[i, 3]) * 0.5f;

            GameObject go = new($"Label_{card.CardName}");
            go.transform.SetParent(transform);
            go.transform.position = new(startX + cx, startY + cy, -0.05f);

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text = card.CardName;
            tmp.fontSize = 2.4f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new(0f, 0f, 0f, 0.60f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.rectTransform.sizeDelta = new(5f, 3f);

            // sits above the tile below player tokens.
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 6;
        }
    }

    // for the diagonal secret passage between the corner rooms
    // rooom card indices 0=Conservatory, 2=Kitchen, 6=Lounge, 8=Study
    private void AssignSecretPassages()
    {
        if (_roomCards == null || _roomCards.Length < 9) return;

        // each pair tiles tagged with room cards get dest for room cards
        int[,] pairs = { { 2, 8 }, { 8, 2 }, { 0, 6 }, { 6, 0 } }; 

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Tile t = _grid[x, y];
                if (t?.RoomData == null) continue;

                for (int p = 0; p < pairs.GetLength(0); p++)
                {
                    if (t.RoomData == _roomCards[pairs[p, 0]])
                    {
                        t.SecretPassageDestination = _roomCards[pairs[p, 1]];
                        break;
                    }
                }
            }
        }
    }

    private void ApplySpawnPointHighlights()
    {
        Color[] characterColors =
        {
            new(1f, 0.2f, 0.2f),    // Miss Scarlet
            new(1f, 0.8f, 0f),      // Col Mustard
            new(0.9f, 0.9f, 0.9f),  // Mrs White
            new(0.2f, 0.8f, 0.2f),  // Mr Green
            new(0.2f, 0.2f, 1f),    // Mrs Peacock
            new(0.6f, 0.2f, 0.8f),  // Prof Plum
        };

        for (int i = 0; i < 6; i++)
        {
            if (_spawnPoints[i] != null) _spawnPoints[i].Highlight(characterColors[i]);
        }
    }

    private Tile.TileType ParseMapData(int x, int y, string[] lines)
    {
        if (lines == null || lines.Length == 0) return Tile.TileType.Invalid;

        int invertedY = (GridHeight - 1) - y;
        if (invertedY < 0 || invertedY >= lines.Length || x >= lines[invertedY].Length) return Tile.TileType.Invalid;

        char c = char.ToUpper(lines[invertedY][x]);
        if (char.IsDigit(c)) return Tile.TileType.Spawn;

        return c switch
        {
            'W' or 'B' => Tile.TileType.Wall,
            'R' => Tile.TileType.Room,
            'D' => Tile.TileType.Door,
            'C' or 'F' => Tile.TileType.Cellar,
            'X' => Tile.TileType.Invalid,
            'H' => Tile.TileType.Hallway,
            'S' => Tile.TileType.SecretPassage,
            _ => Tile.TileType.Invalid
        };
    }

    public List<Tile> GetWalkableNeighbors(int x, int y)
    {
        Tile current = _grid[x, y];
        List<Tile> neighbors = new();

        foreach (var (ox, oy) in new[] { (0, 1), (0, -1), (1, 0), (-1, 0) })
        {
            int nx = x + ox, ny = y + oy;
            if (!IsValid(nx, ny)) continue;

            Tile neighbor = _grid[nx, ny];

            bool canEnter = neighbor.Type switch
            {
                Tile.TileType.Hallway => true,
                Tile.TileType.Spawn   => true,
                Tile.TileType.Door    => true,
                Tile.TileType.Room    => current.Type is Tile.TileType.Door or Tile.TileType.Room,
                Tile.TileType.SecretPassage => true,
                Tile.TileType.Wall    => false,
                Tile.TileType.Cellar  => false,
                Tile.TileType.Invalid => false,
                _                     => false
            };

            if (canEnter) neighbors.Add(neighbor);
        }

        return neighbors;
    }

    private bool IsValid(int x, int y) =>
        x >= 0 && x < GridWidth && y >= 0 && y < GridHeight && _grid[x, y] != null;

    // greys out rooms not in play
    public void ApplyGameSettings()
    {
        if (GameSettings.Instance == null) return;

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Tile tile = _grid[x, y];
                if (tile == null || tile.Type != Tile.TileType.Room) continue;
                if (tile.RoomData != null && GameSettings.Instance.IsRoomDisabled(tile.RoomData))
                    tile.DisableAsInactive();
            }
        }

        Debug.Log("GridManager: Room exclusions applied.");
    }

    // RE: movement room tiles that have Card data assigned - for a.i
    public List<Tile> GetRoomTilesWithData()
    {
        List<Tile> result = new();
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
            {
                Tile t = _grid[x, y];
                if (t != null && t.Type == Tile.TileType.Room && t.RoomData != null && t.IsWalkable)
                    result.Add(t);
            }
        return result;
    }

    // RE: movement into room tile assign w the Card ref secret passage 
    public Tile GetRoomTile(CardData roomData)
    {
        if (roomData == null) return null;
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
            {
                Tile t = _grid[x, y];
                if (t != null && t.RoomData == roomData && t.IsWalkable)
                    return t;
            }
        return null;
    }

    public Tile GetTileAt(int x, int y)
    {
        if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight) return _grid[x, y];
        return null;
    }

    public Tile GetStartingTile(PlayerController.CharacterType character)
    {
        int index = (int)character;
        if (index >= 0 && index < _spawnPoints.Length && _spawnPoints[index] != null) return _spawnPoints[index];
        return GetTileAt(0, 0);
    }
}