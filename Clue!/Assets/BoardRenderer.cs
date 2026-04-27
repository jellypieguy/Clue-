using UnityEngine;
using System.Collections.Generic;
using ClueGame.Core;
using ClueGame.Data;


/// Reads the Board object and draws every tile as a coloured square in the scene.
/// How it works:
///   1. GameBootstrapper calls Render(board, rawGrid) on Start
///   2. BoardRenderer loops through every tile in the grid
///   3. For each tile it instantiates a TilePrefab and sets its colour
///   4. Wall tiles are skipped entirely — no GameObject is created for them

public class BoardRenderer : MonoBehaviour
{
    [Header("Prefab — drag your TilePrefab here")]
    public GameObject tilePrefab;

    [Header("Size of each tile in world units")]
    public float tileSize = 0.9f;

    // ── Room colours matching the classic Clue board ──────────────────────
    static readonly Color32 C_CORRIDOR = new(212, 168,  67, 255); // golden yellow
    static readonly Color32 C_DOOR     = new(220, 180,  50, 255); // bright gold
    static readonly Color32 C_START    = new( 60, 130,  60, 255); // muted green
    static readonly Color32 C_XMARK    = new(180,   0,   0, 255); // red

    /// Each room has a distinct dark colour so players can tell them apart.
    /// Keys match the room names in board.json exactly.
    static readonly Dictionary<string, Color32> ROOM_COLORS = new()
    {
        { "Study",         new( 61,  32,  16, 255) }, // dark brown
        { "Hall",          new( 90,  60,  26, 255) }, // mid brown
        { "Lounge",        new( 42,  26,  13, 255) }, // dark wood
        { "Library",       new( 26,  55,  16, 255) }, // dark green
        { "Billiard Room", new( 13,  40,  13, 255) }, // deeper green
        { "Dining Room",   new( 52,  13,  13, 255) }, // dark red
        { "Conservatory",  new( 13,  30,  52, 255) }, // dark blue
        { "Ball Room",     new( 42,  37,  16, 255) }, // warm dark
        { "Kitchen",       new( 30,  26,  13, 255) }, // dark olive
    };

    // ── Internal state ────────────────────────────────────────────────────

    /// Stores every tile GameObject by its (row, col) position.
    /// Used by Highlight() and ClearHighlight() to find tiles quickly
    readonly Dictionary<(int, int), GameObject> _tiles = new();

    // Kept so we can restore colours after highlighting
    int[][] _rawGrid;
    int     _totalRows;

    // ── Public API ────────────────────────────────────────────────────────

    /// Destroys any previously rendered tiles and draws the full board.
    /// Called once by GameBootstrapper when the game starts.
    public void Render(ClueGame.Core.Board board, int[][] rawGrid)
    {
        // Clear any tiles from a previous render
        foreach (Transform child in transform) Destroy(child.gameObject);
        _tiles.Clear();

        _rawGrid   = rawGrid;
        _totalRows = board.Rows;

        for (int r = 0; r < board.Rows; r++)
        {
            for (int c = 0; c < board.Cols; c++)
            {
                int v = rawGrid[r][c];

                // Skip walls — no tile object needed for impassable cells
                if (v == BoardLoader.WALL) continue;

                Tile tile = board.GetTile(r, c);
                Vector3 pos = ToWorld(r, c, board.Rows);

                // Instantiate the tile prefab and parent it to this object
                GameObject go = Instantiate(
                    tilePrefab, pos, Quaternion.identity, transform);
                go.name = $"Tile_{r}_{c}";
                go.transform.localScale = Vector3.one * tileSize;

                // Set the colour based on tile type / room
                go.GetComponent<SpriteRenderer>().color = GetColor(v, tile);

                _tiles[(r, c)] = go;
            }
        }

        FitCamera(board);
        Debug.Log($"[BoardRenderer] Drew {_tiles.Count} tiles.");
    }

    /// Highlights a set of tiles yellow to show valid moves.
    /// Call ClearHighlight() afterwards to restore original colours.
    public void Highlight(IEnumerable<Tile> tiles)
    {
        foreach (Tile t in tiles)
        {
            if (_tiles.TryGetValue((t.Row, t.Col), out GameObject go))
                go.GetComponent<SpriteRenderer>().color =
                    new Color32(255, 255, 80, 220);
        }
    }

    /// Restores all tiles back to their original colour after highlighting.
    public void ClearHighlight(ClueGame.Core.Board board)
    {
        foreach (var kv in _tiles)
        {
            int  v    = _rawGrid[kv.Key.Item1][kv.Key.Item2];
            Tile tile = board.GetTile(kv.Key.Item1, kv.Key.Item2);
            kv.Value.GetComponent<SpriteRenderer>().color = GetColor(v, tile);
        }
    }

    // ── Coordinate conversion ─────────────────────────────────────────────

    /// Converts a grid position to a Unity world position.
    /// Row 0 maps to the top of the screen (highest Y value).
    public Vector3 ToWorld(int row, int col, int totalRows) =>
        new(col * tileSize, (totalRows - 1 - row) * tileSize, 0f);

    /// Converts a Unity world position back to a grid (row, col).
    /// Used when the player clicks a tile to move there.
    public (int row, int col) ToGrid(Vector3 worldPos, int totalRows) =>
        (totalRows - 1 - Mathf.RoundToInt(worldPos.y / tileSize),
         Mathf.RoundToInt(worldPos.x / tileSize));

    // ── Private helpers ───────────────────────────────────────────────────

    /// Returns the correct colour for a tile based on its raw grid value.
    /// Room tiles look up their room name to get the room-specific colour.
    Color32 GetColor(int rawValue, Tile tile)
    {
        if (rawValue == BoardLoader.CORRIDOR) return C_CORRIDOR;
        if (rawValue == BoardLoader.DOOR)     return C_DOOR;
        if (rawValue == BoardLoader.START)    return C_START;
        if (rawValue == BoardLoader.ENVELOPE) return C_XMARK;

        // Room tile — look up by room name
        if (tile?.RoomName != null &&
            ROOM_COLORS.TryGetValue(tile.RoomName, out Color32 col))
            return col;

        // Fallback — should not normally be reached
        return new Color32(30, 30, 30, 255);
    }

    /// Moves the main camera so the entire board is visible on screen.
    /// Sets orthographic size to fit the board height with a small margin.
    void FitCamera(ClueGame.Core.Board board)
    {
        float centreX = (board.Cols - 1) * tileSize / 2f;
        float centreY = (board.Rows - 1) * tileSize / 2f;
        Camera.main.transform.position = new Vector3(centreX, centreY, -10f);
        Camera.main.orthographicSize   = board.Rows * tileSize / 2f + 1f;
    }
}
