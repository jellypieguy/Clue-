using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    // figure out where we can walk on roll
    public static HashSet<Tile> GetReachableTiles(Tile startTile, int movementBudget)
    {
        var reachableTiles = new HashSet<Tile>();

        if (movementBudget <= 0 || startTile == null) return reachableTiles;

        var queue = new Queue<KeyValuePair<Tile, int>>();
        var visited = new HashSet<Tile> { startTile };

        queue.Enqueue(new KeyValuePair<Tile, int>(startTile, 0));

        while (queue.Count > 0)
        {
            var (currentTile, currentDistance) = queue.Dequeue();

            if (currentDistance >= movementBudget) continue;

            foreach (var neighbor in currentTile.GetWalkableNeighbors())
            {
                if (visited.Add(neighbor))
                {
                    reachableTiles.Add(neighbor);

                    // doors act as t-nodes you enter your turn is cooked
                    if (neighbor.Type is not Tile.TileType.Room and not Tile.TileType.Door and not Tile.TileType.SecretPassage)
                    {
                        queue.Enqueue(new KeyValuePair<Tile, int>(neighbor, currentDistance + 1));
                    }
                }
            }
        }

        return reachableTiles;
    }
}