using System.Collections.Generic;
using UnityEngine;

// pathfinder for grid movement.
public static class Pathfinder
{
    // returns tiles reachable from start within the given roll
    // room tiles dest the search wont continue through them
    public static HashSet<Tile> GetReachableTiles(Tile startTile, int movementBudget)
    {
        HashSet<Tile> reachableTiles = new HashSet<Tile>();

        if (movementBudget <= 0 || startTile == null)
            return reachableTiles;

        Queue<KeyValuePair<Tile, int>> queue = new Queue<KeyValuePair<Tile, int>>();
        HashSet<Tile> visited = new HashSet<Tile>();

        queue.Enqueue(new KeyValuePair<Tile, int>(startTile, 0));
        visited.Add(startTile);

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();
            Tile currentTile = currentNode.Key;
            int currentDistance = currentNode.Value;

            if (currentDistance >= movementBudget)
                continue;

            foreach (Tile neighbor in currentTile.GetWalkableNeighbors())
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    reachableTiles.Add(neighbor);

                    // enter a room but cant path thru  it
                    if (neighbor.Type != Tile.TileType.Room)
                        queue.Enqueue(new KeyValuePair<Tile, int>(neighbor, currentDistance + 1));
                }
            }
        }

        return reachableTiles;
    }
}
