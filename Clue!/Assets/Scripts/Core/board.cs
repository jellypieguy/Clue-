using System.Collections.Generic;

namespace ClueGame.Core
{

    /// Represents a named room on the Clue board
    /// A Room object stores:
    ///   - The room's name (e.g. "Kitchen")
    ///   - Its secret passage destination (if any)
    ///   - The positions of its door tiles (used by movement logic)
    ///   - Which weapons are currently inside it (updated during suggestions)
    /// Room objects are created by BoardLoader and registered with the Board.
    /// via Board.GetRoom("Kitchen") rather than storing a direct reference.

    public class Room
    {
        // ── Identity ──────────────────────────────────────────────────────

        /// The display name of this room. Matches the names in board.json.
        public string Name { get; }


        /// The name of the room connected via secret passage.
        /// Null if this room has no secret passage.
        /// 
        /// Classic passages:
        ///   "Study"    -> "Kitchen"
        ///   "Kitchen"  -> "Study"
        ///   "Lounge"   -> "Conservatory"
        ///   "Conservatory" -> "Lounge"
        public string SecretPassageTo { get; set; }

        /// True if this room has a usable secret passage
        public bool HasSecretPassage => SecretPassageTo != null;

        // ── Door positions ────────────────────────────────────────────────
        
        /// The (row, col) grid positions of this room's door tiles.
        /// Populated by BoardLoader as it reads the grid.
        /// Used by the movement engine to determine valid entry points.
        public List<(int row, int col)> DoorPositions { get; } = new();

        // ── Weapon tracking ───────────────────────────────────────────────

        private readonly List<string> _weapons = new();
        
        /// The names of weapons currently in this room.
        /// Weapons are moved here when a suggestion names this room.
        /// Read-only from outside — use AddWeapon / RemoveWeapon to modify.
        public IReadOnlyList<string> Weapons => _weapons;

        /// Moves a weapon into this room if it isn't already here
        public void AddWeapon(string weaponName)
        {
            if (!_weapons.Contains(weaponName))
                _weapons.Add(weaponName);
        }

        ///Removes a weapon from this room
        public void RemoveWeapon(string weaponName) => _weapons.Remove(weaponName);

        // ── Constructor ──────────────────────────────────────────────────
        /// Creates a room with the given name.
        /// SecretPassageTo and DoorPositions are set afterwards by BoardLoader
        public Room(string name) { Name = name; }

        public override string ToString() =>
            $"Room: {Name}" +
            (HasSecretPassage ? $" (passage -> {SecretPassageTo})" : "");
    }

    // ─────────────────────────────────────────────────────────────────────────
    
    /// The game board — a 25x24 grid of Tile objects plus a registry of rooms.
    public class Board
    {
        // ── Dimensions ────────────────────────────────────────────────────

        ///Total number of rows. Always 25 for the standard Clue board
        public int Rows { get; }

        ///Total number of columns. Always 24 for the standard Clue board
        public int Cols { get; }

        // ── Storage ───────────────────────────────────────────────────────
        
        /// The internal 2D grid of Tile objects.
        /// Index as _grid[row, col]. Row 0 is the top of the board.
        /// All tiles are initialised to TileType.Wall by the constructor,
        /// then overwritten by BoardLoader as it reads the JSON grid.
        private readonly Tile[,] _grid;
        
        /// Room objects keyed by name.
        /// Populated by RegisterRoom() during board loading
        private readonly Dictionary<string, Room> _rooms = new();

        // ── Constructor ───────────────────────────────────────────────────
        
        /// Creates an empty board of the given size.
        /// Every cell starts as TileType.Wall
        public Board(int rows, int cols)
        {
            Rows  = rows;
            Cols  = cols;
            _grid = new Tile[rows, cols];

            // Initialise every cell as a wall so unset tiles are never null
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    _grid[r, c] = new Tile(r, c, TileType.Wall);
        }

        // ── Tile access ───────────────────────────────────────────────────
        
        /// Returns the tile at (row, col), or null if out of bounds.
        /// Always use this instead of accessing _grid directly 
        /// it handles edge tiles safely without throwing an exception.

        public Tile GetTile(int row, int col)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Cols)
                return null;
            return _grid[row, col];
        }
        
        /// Overwrites the tile at its stored (Row, Col) position.
        /// Called by BoardLoader after creating each tile with the correct type.
        public void SetTile(Tile tile) => _grid[tile.Row, tile.Col] = tile;

        // ── Room registry ─────────────────────────────────────────────────


        /// Registers a room by name so it can be retrieved with GetRoom().
        /// Called by BoardLoader once per room as it reads board.json.
        public void RegisterRoom(Room room) => _rooms[room.Name] = room;
        
        /// Returns the room with the given name, or null if not found.
        /// Used throughout the game to look up rooms by name string.
        /// Example: Board.GetRoom("Kitchen")
        public Room GetRoom(string name) =>
            _rooms.TryGetValue(name, out Room r) ? r : null;

        ///All registered rooms — used by the renderer to draw labels
        public IEnumerable<Room> AllRooms => _rooms.Values;

        // ── Movement helpers ──────────────────────────────────────────────
        
        /// Returns all passable tiles directly adjacent (up, down, left, right)
        /// to the tile at (row, col). Diagonal movement is not allowed in Clue.
        /// 
        /// Used by MovementEngine.GetReachableTiles() during BFS pathfinding.
        /// Out-of-bounds and Wall tiles are excluded automatically.
        public List<Tile> GetNeighbours(int row, int col)
        {
            var result = new List<Tile>();

            // Offsets for up, down, left, right — no diagonals
            int[] dr = { -1,  1,  0, 0 };
            int[] dc = {  0,  0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                Tile t = GetTile(row + dr[i], col + dc[i]);
                if (t != null && t.IsPassable)
                    result.Add(t);
            }

            return result;
        }
    }
}