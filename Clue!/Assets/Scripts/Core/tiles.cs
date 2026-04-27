namespace ClueGame.Core
{

    /// The type of a single tile on the board.
    /// Tells the game and renderer how to treat each cell.

    public enum TileType
    {
        Wall,       // Impassable — room boundaries, outside the board edge
        Corridor,   // Standard walkable yellow square
        Room,       // Interior tile of a named room
        Door,       // Entry/exit point between a corridor and a room
        Start,      // A player's starting position on the edge of the board
        EnvelopeX   // The X mark in the centre where the murder envelope sits
    }

    /// Represents a single cell on the 25x24 Clue board grid.
    /// Each tile knows:
    ///   - Its position (row, col)
    ///   - What type it is (corridor, room, door, etc)
    ///   - Which room it belongs to (if any)
    ///   - Which player starts here (if any)
    ///   - Whether it connects to a secret passage
    /// 
    /// Tile objects are created by BoardLoader and stored inside Board.
    /// BoardRenderer reads them to decide what colour to draw each cell.
    /// MovementEngine reads IsPassable to decide which tiles a player can reach.
    public class Tile
    {
        // ── Position ──────────────────────────────────────────────────────
        
        /// Row index from the top of the board.
        /// Row 0 is the top row, row 24 is the bottom row.
        
        public int Row { get; }

        
        /// Column index from the left of the board.
        /// Col 0 is the leftmost column, col 23 is the rightmost.
        
        public int Col { get; }

        // ── Type ──────────────────────────────────────────────────────────
        
        /// What kind of tile this is.
        /// This is the main property the renderer and movement engine check.
        /// Set during construction, can be updated by BoardLoader if needed.

        public TileType Type { get; set; }

        // ── Room data ─────────────────────────────────────────────────────
        
        /// The name of the room this tile belongs to.
        /// 
        /// Set on:
        ///   - TileType.Room tiles (interior squares inside a named room)
        ///   - TileType.Door tiles (the entry/exit squares on the room boundary)
        /// 
        /// Null on corridors, walls, start positions and the envelope X.
        /// 
        /// Example values: "Study", "Kitchen", "Ball Room"
        public string RoomName { get; set; }
        
        /// If this tile is inside a room that has a secret passage,
        /// this holds the name of the destination room.
        /// 
        /// Secret passages in classic Clue connect diagonally opposite rooms:
        ///   Study      <-> Kitchen
        ///   Lounge     <-> Conservatory
        /// 
        /// Using a secret passage counts as the whole move for that turn.
        /// Null if this room has no secret passage.

        public string SecretPassageTo { get; set; }

        // ── Start position ────────────────────────────────────────────────
        
        /// The character name of the player who starts on this tile.
        /// Only set on TileType.Start tiles. Null everywhere else.
        /// 
        /// Example: tile at row 7, col 23 has StartingPlayer = "Miss Scarlett"
        public string StartingPlayer { get; set; }

        // ── Computed helpers ──────────────────────────────────────────────
        
        /// True if a player token can stand on or pass through this tile.
        /// Walls are the only tile type that block movement.
        /// Used by MovementEngine when calculating reachable tiles.
        public bool IsPassable => Type != TileType.Wall;
        
        /// True if this tile is inside a named room.
        /// A player must be on a room tile to make a suggestion.
        /// Both Room interior tiles and Door tiles count as inside the room.
        public bool IsInsideRoom =>
            Type == TileType.Room || Type == TileType.Door;

        // ── Constructor ───────────────────────────────────────────────────
        
        /// Creates a tile at the given grid position with the given type.
        /// 
        /// After construction, BoardLoader sets the optional fields:
        ///   tile.RoomName       = "Kitchen"
        ///   tile.StartingPlayer = "Miss Scarlett"
        ///   tile.SecretPassageTo = "Study"
        /// <param name="row">Row from the top (0-indexed).</param>
        /// <param name="col">Column from the left (0-indexed).</param>
        /// <param name="type">The tile type — determines passability and rendering.</param>
        public Tile(int row, int col, TileType type)
        {
            Row  = row;
            Col  = col;
            Type = type;
        }
        
        /// Returns a readable description of this tile for debug logging.
        /// Example: "Tile(7,23) [Start] Miss Scarlett"
        public override string ToString() =>
            $"Tile({Row},{Col}) [{Type}]" +
            (RoomName       != null ? $" {RoomName}"       : "") +
            (StartingPlayer != null ? $" [{StartingPlayer}]" : "");
    }
}