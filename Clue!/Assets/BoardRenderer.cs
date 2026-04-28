using UnityEngine;
using System.Collections.Generic;
using ClueGame.Core;
using ClueGame.Data;

public class BoardRenderer : MonoBehaviour
{
    [Header("Prefab — drag your TilePrefab here")]
    public GameObject tilePrefab;

    [Header("Size of each tile in world units")]
    public float tileSize = 0.9f;

    [Header("Cluedo board image (drag your sprite here)")]
    public Sprite boardImage;

    [Header("Fine tuning for the board image position and size")]
    public Vector2 imageOffset = Vector2.zero;
    public float   imageScaleMultiplier = 1f;

    [Header("Show coloured tiles instead of the board image (debug view)")]
    public bool showColouredTiles = false;

    // ── Colours ───────────────────────────────────────────────────────────
    static readonly Color32 C_CORRIDOR  = new(212, 168,  67, 255);
    static readonly Color32 C_DOOR      = new(220, 180,  50, 255);
    static readonly Color32 C_START     = new( 60, 130,  60, 255);
    static readonly Color32 C_XMARK     = new(180,   0,   0, 255);
    static readonly Color32 C_HIGHLIGHT = new(255, 255,  80, 220);
    static readonly Color32 C_HIDDEN    = new(  0,   0,   0,   0); // fully transparent

    /// Each room has a distinct dark colour so players can tell them apart
    /// when running in debug view. Keys match the room names in board.json.
    static readonly Dictionary<string, Color32> ROOM_COLORS = new()
    {
        { "Study",         new( 61,  32,  16, 255) },
        { "Hall",          new( 90,  60,  26, 255) },
        { "Lounge",        new( 42,  26,  13, 255) },
        { "Library",       new( 26,  55,  16, 255) },
        { "Billiard Room", new( 13,  40,  13, 255) },
        { "Dining Room",   new( 52,  13,  13, 255) },
        { "Conservatory",  new( 13,  30,  52, 255) },
        { "Ball Room",     new( 42,  37,  16, 255) },
        { "Kitchen",       new( 30,  26,  13, 255) },
    };

    // ── Internal state ────────────────────────────────────────────────────
    readonly Dictionary<(int, int), GameObject> _tiles = new();
    int[][] _rawGrid;

    // ── Public API ────────────────────────────────────────────────────────

    /// Destroys any previously rendered tiles and draws the full board.
    public void Render(ClueGame.Core.Board board, int[][] rawGrid)
    {
        // Clear any tiles or image from a previous render
        foreach (Transform child in transform) Destroy(child.gameObject);
        _tiles.Clear();
        _rawGrid = rawGrid;

        // Spawn the board image behind the tiles (only if assigned and not in debug view)
        if (boardImage != null && !showColouredTiles)
            SpawnBoardImage(board);

        // Spawn the tile grid
        for (int r = 0; r < board.Rows; r++)
        {
            for (int c = 0; c < board.Cols; c++)
            {
                int v = rawGrid[r][c];
                if (v == BoardLoader.WALL) continue;

                Tile tile = board.GetTile(r, c);
                Vector3 pos = ToWorld(r, c, board.Rows);

                GameObject go = Instantiate(
                    tilePrefab, pos, Quaternion.identity, transform);
                go.name = $"Tile_{r}_{c}";
                go.transform.localScale = Vector3.one * tileSize;

                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                sr.color = showColouredTiles ? GetColor(v, tile) : C_HIDDEN;
                sr.sortingOrder = 1; // above the board image

                _tiles[(r, c)] = go;
            }
        }

        FitCamera(board);
        Debug.Log($"[BoardRenderer] Drew {_tiles.Count} tiles. " +
                  $"Image overlay: {(boardImage != null && !showColouredTiles)}");
    }

    /// Highlights a set of tiles yellow to show valid moves.
    public void Highlight(IEnumerable<Tile> tiles)
    {
        foreach (Tile t in tiles)
        {
            if (_tiles.TryGetValue((t.Row, t.Col), out GameObject go))
                go.GetComponent<SpriteRenderer>().color = C_HIGHLIGHT;
        }
    }

    /// Restores all tiles back to their default appearance after highlighting.
    public void ClearHighlight(ClueGame.Core.Board board)
    {
        foreach (var kv in _tiles)
        {
            int  v    = _rawGrid[kv.Key.Item1][kv.Key.Item2];
            Tile tile = board.GetTile(kv.Key.Item1, kv.Key.Item2);
            kv.Value.GetComponent<SpriteRenderer>().color =
                showColouredTiles ? GetColor(v, tile) : C_HIDDEN;
        }
    }

    // ── Coordinate conversion ─────────────────────────────────────────────

    public Vector3 ToWorld(int row, int col, int totalRows) =>
        new(col * tileSize, (totalRows - 1 - row) * tileSize, 0f);

    public (int row, int col) ToGrid(Vector3 worldPos, int totalRows) =>
        (totalRows - 1 - Mathf.RoundToInt(worldPos.y / tileSize),
         Mathf.RoundToInt(worldPos.x / tileSize));

    // ── Private helpers ───────────────────────────────────────────────────

    /// Creates a single GameObject that displays the Cluedo board image,
    /// scaled and positioned so it lines up with the grid.
    void SpawnBoardImage(ClueGame.Core.Board board)
    {
        GameObject imgObj = new GameObject("BoardImage");
        imgObj.transform.SetParent(transform, false);

        SpriteRenderer sr = imgObj.AddComponent<SpriteRenderer>();
        sr.sprite = boardImage;
        sr.sortingOrder = 0; // behind the tiles

        // Scale the sprite so its world size matches the full grid area
        Bounds b = sr.sprite.bounds;
        float scaleX = (board.Cols * tileSize) / b.size.x * imageScaleMultiplier;
        float scaleY = (board.Rows * tileSize) / b.size.y * imageScaleMultiplier;
        imgObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        // Centre the image on the grid (with optional offset)
        float centreX = (board.Cols - 1) * tileSize / 2f + imageOffset.x;
        float centreY = (board.Rows - 1) * tileSize / 2f + imageOffset.y;
        imgObj.transform.position = new Vector3(centreX, centreY, 0f);
    }

    /// Returns the correct colour for a tile based on its raw grid value.
    Color32 GetColor(int rawValue, Tile tile)
    {
        if (rawValue == BoardLoader.CORRIDOR) return C_CORRIDOR;
        if (rawValue == BoardLoader.DOOR)     return C_DOOR;
        if (rawValue == BoardLoader.START)    return C_START;
        if (rawValue == BoardLoader.ENVELOPE) return C_XMARK;

        if (tile?.RoomName != null &&
            ROOM_COLORS.TryGetValue(tile.RoomName, out Color32 col))
            return col;

        return new Color32(30, 30, 30, 255);
    }

    /// Moves the main camera so the entire board is visible on screen.
    void FitCamera(ClueGame.Core.Board board)
    {
        float centreX = (board.Cols - 1) * tileSize / 2f;
        float centreY = (board.Rows - 1) * tileSize / 2f;
        Camera.main.transform.position = new Vector3(centreX, centreY, -10f);
        Camera.main.orthographicSize   = board.Rows * tileSize / 2f + 1f;
    }
}