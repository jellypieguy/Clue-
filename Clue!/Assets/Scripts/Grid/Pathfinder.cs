using System.Collections.Generic;

public static class Pathfinder
{
    private struct Node
    {
        public Tile tile;
        public int distance;

        public Node(Tile t, int d)
        {
            tile = t;
            distance = d;
        }
    }

    public static HashSet<Tile> GetReachableTiles(Tile startTile, int movementBudget)
    {
        HashSet<Tile> reachableTiles = new HashSet<Tile>();

        if (movementBudget <= 0 || startTile == null)
            return reachableTiles;

        Queue<Node> queue = new Queue<Node>();

        HashSet<Tile> visited = new HashSet<Tile>();

        queue.Enqueue(new Node(startTile, 0));
        visited.Add(startTile);

        // Starting tile is always reachable (if not a room)
        if (startTile.Type != Tile.TileType.Room)
            reachableTiles.Add(startTile);

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();

            foreach (Tile neighbor in current.tile.GetWalkableNeighbors())
            {
                if (visited.Contains(neighbor))
                    continue;

                Tile.TileType nextType = neighbor.Type;

                // ----------------------------
                // RULE 1: ROOM ENTRY ONLY FROM DOOR
                // ----------------------------
                if (nextType == Tile.TileType.Room)
                {
                    if (current.tile.Type != Tile.TileType.Door)
                        continue;
                }

                // ----------------------------
                // RULE 2: VALID TILE TYPES ONLY
                // ----------------------------
                if (nextType != Tile.TileType.Hallway &&
                    nextType != Tile.TileType.Door &&
                    nextType != Tile.TileType.Room)
                    continue;

                int newDistance = current.distance + 1;

                if (newDistance > movementBudget)
                    continue;

                visited.Add(neighbor);

                // Always add reachable tile
                reachableTiles.Add(neighbor);

                // IMPORTANT:
                // Do NOT expand FROM rooms (prevents illegal multi-room traversal)
                if (nextType != Tile.TileType.Room)
                {
                    queue.Enqueue(new Node(neighbor, newDistance));
                }
            }
        }

        return reachableTiles;
    }
}