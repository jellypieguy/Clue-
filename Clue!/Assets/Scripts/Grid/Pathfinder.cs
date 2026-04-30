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

            foreach (Tile neighbor in currentTile.GetWalkableNeighbors())
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    reachableTiles.Add(neighbor);

                    // continue pathfinding through room tiles so player can move freely inside
                    bool isTerminal = neighbor.Type == Tile.TileType.Door 
                                      || neighbor.Type == Tile.TileType.SecretPassage;

                    if (!isTerminal)
                        queue.Enqueue(new KeyValuePair<Tile, int>(neighbor, currentDistance + 1));
                }
            }
        }

        return reachableTiles;
    }
}