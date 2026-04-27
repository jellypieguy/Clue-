using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ClueGame.Core;

namespace ClueGame.Data
{
    // ── JSON data classes ─────────────────────────────────────────────────────
    // These mirror the structure of board.json exactly so JsonUtility can
    // deserialise them. Field names must match the JSON keys character-for-character.

    /// Metadata for a single room entry in board.json.
    /// Example JSON: { "id": 2, "name": "Study", "secretPassageTo": "Kitchen", ... }
    [System.Serializable]
    public class RoomMeta
    {
        public int    id;               // Tile value used in the grid array (2-10)
        public string name;             // Display name e.g. "Billiard Room"
        public string secretPassageTo;  // Destination room name, or null
        public int    labelRow;         // Row where the room label is drawn
        public int    labelCol;         // Col where the room label is drawn
    }
    
    /// A character's starting position from board.json.
    /// Example JSON: { "character": "Miss Scarlett", "row": 7, "col": 23 }
    [System.Serializable]
    public class StartPos
    {
        public string character;
        public int    row;
        public int    col;
    }

    ///Simple row/col pair used for the envelope X position
    [System.Serializable]
    public class GridXY
    {
        public int row;
        public int col;
    }
    /// All fields in board.json except the grid array itself.
    /// Unity's JsonUtility cannot deserialise int[][] so we handle
    /// the grid manually in ParseGrid() below.
    [System.Serializable]
    public class BoardJsonFlat
    {
        public int           rows;
        public int           cols;
        public List<RoomMeta> rooms;
        public List<StartPos> startPositions;
        public GridXY         envelopeX;
    }

    // ── BoardLoader ───────────────────────────────────────────────────────────
    
    /// Reads board.json from StreamingAssets/Data/ and builds a Board object.
    /// 
    /// The JSON file defines:
    ///   - rows / cols: board dimensions (25 x 24)
    ///   - grid: 2D int array where each number maps to a tile type
    ///   - rooms: metadata for each of the 9 rooms
    ///   - startPositions: where each character token begins
    ///   - envelopeX: the position of the red X in the center
    /// 
    /// Tile value key (matches board.json tileKey):
    ///   0  = Wall          (impassable, outside or room boundary)
    ///   1  = Corridor      (walkable yellow square)
    ///   2  = Study
    ///   3  = Hall
    ///   4  = Lounge
    ///   5  = Library
    ///   6  = Billiard Room
    ///   7  = Dining Room
    ///   8  = Conservatory
    ///   9  = Ball Room
    ///   10 = Kitchen
    ///   11 = Door          (entry/exit for an adjacent room)
    ///   12 = Start         (player starting square)
    ///   13 = EnvelopeX     (the murder envelope X tile)
    ///   14 = Staircase     (blocked center area, treated as wall)

    public static class BoardLoader
    {
        // ── Tile value constants ──────────────────────────────────────────────
        // These match the numbers in board.json's grid array.
        // Other classes (BoardRenderer, GameBootstrapper) use these constants
        // instead of magic numbers so the meaning is always clear.

        public const int WALL      = 0;
        public const int CORRIDOR  = 1;
        public const int DOOR      = 11;
        public const int START     = 12;
        public const int ENVELOPE  = 13;
        public const int STAIRCASE = 14;

        // ── Public entry point ────────────────────────────────────────────────
        
        /// Loads board.json builds and returns the Board object, the raw int[][] grid, and the room metadata list.
        /// Returns (null, null, null) if the file is missing or malformed.
        /// 
        /// The raw grid is returned alongside the Board so BoardRenderer can
        /// look up the original tile value when restoring colours after highlighting.
        public static (Board board, int[][] rawGrid, List<RoomMeta> rooms) Load()
        {
            // StreamingAssets is a special Unity folder that is included
            // in every build and is always readable at runtime on all platforms.
            string path = Path.Combine(
                Application.streamingAssetsPath, "Data", "board.json");

            if (!File.Exists(path))
            {
                Debug.LogError($"[BoardLoader] board.json not found at: {path}");
                return (null, null, null);
            }

            string raw = File.ReadAllText(path);

            // Parse the non-grid fields with JsonUtility
            BoardJsonFlat flat = JsonUtility.FromJson<BoardJsonFlat>(raw);

            // Parse the grid array manually (JsonUtility can't handle int[][])
            int[][] grid = ParseGrid(raw);

            // Build the Board object from the parsed data
            Board board = BuildBoard(flat, grid);

            return (board, grid, flat.rooms);
        }

        // ── Board construction ────────────────────────────────────────────────
        
        /// Walks the raw grid array and creates a Tile object for every cell.
        /// Also registers rooms, assigns door tile room names, and sets
        /// start position data on start tiles.
        private static Board BuildBoard(BoardJsonFlat flat, int[][] grid)
        {
            Board board = new Board(flat.rows, flat.cols);

            // Register all 9 rooms first so door tiles can reference them by name
            var roomById = new Dictionary<int, Room>();
            foreach (RoomMeta rm in flat.rooms)
            {
                Room room = new Room(rm.name)
                {
                    SecretPassageTo = rm.secretPassageTo
                };
                board.RegisterRoom(room);
                roomById[rm.id] = room; // e.g. 2 -> Study room object
            }

            // Walk every cell in the grid
            for (int r = 0; r < flat.rows; r++)
            {
                for (int c = 0; c < flat.cols; c++)
                {
                    int v = grid[r][c];
                    Tile tile;

                    if (v == WALL)
                    {
                        // Outside the board or a room wall — impassable
                        tile = new Tile(r, c, TileType.Wall);
                    }
                    else if (v == CORRIDOR)
                    {
                        // Standard walkable yellow square
                        tile = new Tile(r, c, TileType.Corridor);
                    }
                    else if (v == START)
                    {
                        // Player starting square on the board edge
                        tile = new Tile(r, c, TileType.Start);
                    }
                    else if (v == ENVELOPE)
                    {
                        // The red X in the centre of the board
                        tile = new Tile(r, c, TileType.EnvelopeX);
                    }
                    else if (v == STAIRCASE)
                    {
                        // The blocked staircase area in the centre — treat as wall
                        tile = new Tile(r, c, TileType.Wall);
                    }
                    else if (v == DOOR)
                    {
                        // Door tile — find which room it belongs to by
                        // looking at the adjacent room tiles
                        tile = new Tile(r, c, TileType.Door);
                        string roomName = GetAdjacentRoomName(
                            grid, r, c, flat.rows, flat.cols, roomById);
                        tile.RoomName = roomName;

                        // Register this door position with the room
                        // so the movement engine can find room entry points
                        if (roomName != null)
                            board.GetRoom(roomName)?.DoorPositions.Add((r, c));
                    }
                    else if (roomById.TryGetValue(v, out Room room))
                    {
                        // Room interior tile (values 2-10)
                        tile = new Tile(r, c, TileType.Room);
                        tile.RoomName = room.Name;
                    }
                    else
                    {
                        // Unknown value — treat as wall to be safe
                        tile = new Tile(r, c, TileType.Wall);
                        Debug.LogWarning($"[BoardLoader] Unknown tile value {v} at ({r},{c})");
                    }

                    board.SetTile(tile);
                }
            }

            // Apply start position data to the relevant tiles
            foreach (StartPos sp in flat.startPositions)
            {
                Tile t = board.GetTile(sp.row, sp.col);
                if (t != null)
                    t.StartingPlayer = sp.character;
                else
                    Debug.LogWarning($"[BoardLoader] Start position out of bounds for {sp.character}");
            }

            Debug.Log($"[BoardLoader] Loaded {flat.rows}x{flat.cols} board " +
                      $"with {flat.rooms.Count} rooms.");
            return board;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        
        /// A door tile sits on the boundary between a corridor and a room.
        /// This method looks at the four orthogonal neighbours of a door tile
        /// and returns the name of the first adjacent room it finds.
        /// Returns null if no adjacent room tile exists 
        private static string GetAdjacentRoomName(
            int[][] grid, int r, int c,
            int rows, int cols,
            Dictionary<int, Room> roomById)
        {
            int[] dr = { -1,  1,  0, 0 };
            int[] dc = {  0,  0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int nr = r + dr[i];
                int nc = c + dc[i];

                // Bounds check
                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;

                int neighbourVal = grid[nr][nc];
                if (roomById.TryGetValue(neighbourVal, out Room room))
                    return room.Name;
            }

            return null;
        }
        
        /// Manually parses the "grid" field from the raw JSON string
        private static int[][] ParseGrid(string raw)
        {
            // Find the start of the grid array: "grid": [ ...
            int gridKey   = raw.IndexOf("\"grid\":");
            int arrayOpen = raw.IndexOf('[', gridKey) + 1;

            // Walk forward tracking bracket depth to find the closing ]
            int depth = 1, pos = arrayOpen;
            while (depth > 0 && pos < raw.Length)
            {
                if      (raw[pos] == '[') depth++;
                else if (raw[pos] == ']') depth--;
                pos++;
            }

            // Extract the content between the outer [ and ]
            string gridContent = raw.Substring(arrayOpen, pos - arrayOpen - 1);

            // Split into individual row strings by finding [ ... ] pairs
            var rows = new List<int[]>();
            int rowStart = gridContent.IndexOf('[');

            while (rowStart >= 0)
            {
                int rowEnd = gridContent.IndexOf(']', rowStart);
                string rowContent = gridContent.Substring(
                    rowStart + 1, rowEnd - rowStart - 1);

                // Parse comma-separated integers
                string[] vals = rowContent.Split(',');
                var rowInts = new List<int>();
                foreach (string v in vals)
                {
                    string trimmed = v.Trim();
                    if (trimmed.Length > 0 && int.TryParse(trimmed, out int n))
                        rowInts.Add(n);
                }

                rows.Add(rowInts.ToArray());
                rowStart = gridContent.IndexOf('[', rowEnd);
            }

            return rows.ToArray();
        }
    }
}