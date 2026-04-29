using System.Collections.Generic;
using UnityEngine;

// Basic AI agent for automated players.
// Keeps behaviour simple (random suggestions, no accusations).
public class AIAgent : MonoBehaviour
{
    // Chooses a room that the AI can actually reach with its current roll.
    // Falls back to null if no rooms are reachable this turn.
    public CardData ChooseTargetRoom(PlayerController aiPlayer, int roll)
    {
        // Safety checks
        if (aiPlayer == null || aiPlayer.CurrentTile == null)
            return null;

        // DEBUG: Log starting position to help debug movement issues
        Debug.Log($"AI {aiPlayer.Character} starting at {aiPlayer.CurrentTile.Type} with movement budget: {roll}");

        // Get all tiles the AI can move to this turn using the pathfinder
        HashSet<Tile> reachableTiles = Pathfinder.GetReachableTiles(aiPlayer.CurrentTile, roll);
        
        // DEBUG: Log how many tiles are reachable
        Debug.Log($"AI {aiPlayer.Character}: {reachableTiles.Count} reachable tiles found");

        List<CardData> reachableRooms = new List<CardData>();

        // Filter only tiles that are rooms
        foreach (Tile tile in reachableTiles)
        {
            if (tile.Type == Tile.TileType.Room && tile.RoomData != null)
            {
                reachableRooms.Add(tile.RoomData);
                Debug.Log($"AI {aiPlayer.Character}: Can reach room {tile.RoomData.CardName}");
            }
        }

        // No valid rooms this turn - AI will just move to a hallway or stay put
        if (reachableRooms.Count == 0)
        {
            Debug.Log($"AI {aiPlayer.Character}: No reachable rooms this turn");
            return null;
        }

        // Pick a random reachable room (simple AI behavior)
        CardData selectedRoom = reachableRooms[Random.Range(0, reachableRooms.Count)];
        Debug.Log($"AI {aiPlayer.Character}: Selected room {selectedRoom.CardName}");
        return selectedRoom;
    }

    // Makes a suggestion when inside a room.
    // Uses random suspect + weapon + current room.
    public void MakeSuggestion(PlayerController aiPlayer,
                               SuggestionSystem suggestionSystem,
                               int playerIndex,
                               List<PlayerController> allPlayers)
    {
        // Safety checks
        if (aiPlayer == null || suggestionSystem == null)
            return;

        // AI must be standing in a room to suggest
        CardData currentRoom = aiPlayer.CurrentTile != null
            ? aiPlayer.CurrentTile.RoomData
            : null;

        if (currentRoom == null)
        {
            Debug.Log($"{aiPlayer.Character} is not in a room, cannot make a suggestion.");
            return;
        }

        // Get all available cards from the DeckManager
        List<CardData> suspects = DeckManager.Instance.AllSuspects;
        List<CardData> weapons  = DeckManager.Instance.AllWeapons;

        if (suspects == null || weapons == null ||
            suspects.Count == 0 || weapons.Count == 0)
        {
            Debug.LogWarning("AIAgent: Missing suspects or weapons.");
            return;
        }

        // Pick randomly for now (no deduction logic yet)
        // Future improvement: Use process of elimination to make smarter suggestions
        CardData chosenSuspect = suspects[Random.Range(0, suspects.Count)];
        CardData chosenWeapon  = weapons[Random.Range(0, weapons.Count)];

        Debug.Log($"AI {aiPlayer.Character} suggests: {chosenSuspect.CardName} with {chosenWeapon.CardName} in {currentRoom.CardName}");

        // Process the suggestion through the game's suggestion system
        suggestionSystem.ProcessSuggestion(
            chosenSuspect,
            chosenWeapon,
            currentRoom,
            playerIndex,
            allPlayers
        );
    }

    // This AI never accuses.
    // Prevents it from randomly losing the game.
    // Change this to true if you want AI to make accusations (with random guesses)
    public bool ShouldAccuse(PlayerController aiPlayer)
    {
        // AI never accuses to avoid accidentally losing
        // In a more advanced AI, this would check if the AI knows the solution
        return false;
    }

    // Generates a random accusation.
    // Only used if ShouldAccuse is changed to return true later.
    public CardData[] MakeAccusation()
    {
        // Get all available cards from the DeckManager
        List<CardData> suspects = DeckManager.Instance.AllSuspects;
        List<CardData> weapons  = DeckManager.Instance.AllWeapons;
        List<CardData> rooms    = DeckManager.Instance.AllActiveRooms;

        // Basic safety checks
        if (suspects.Count == 0 || weapons.Count == 0 || rooms.Count == 0)
        {
            Debug.LogWarning("AIAgent: Cannot make accusation - missing card data");
            return null;
        }

        // Pick random cards for the accusation
        CardData[] accusation = new CardData[]
        {
            suspects[Random.Range(0, suspects.Count)],
            weapons[Random.Range(0, weapons.Count)],
            rooms[Random.Range(0, rooms.Count)]
        };

        Debug.Log($"AI made random accusation: {accusation[0].CardName}, {accusation[1].CardName}, {accusation[2].CardName}");
        
        return accusation;
    }

    // Optional: Helper method to get the closest reachable room
    // Useful for smarter AI behavior
    public CardData GetClosestReachableRoom(PlayerController aiPlayer, int roll)
    {
        if (aiPlayer == null || aiPlayer.CurrentTile == null)
            return null;

        HashSet<Tile> reachableTiles = Pathfinder.GetReachableTiles(aiPlayer.CurrentTile, roll);
        
        Tile closestRoom = null;
        float closestDistance = float.MaxValue;
        Vector3 playerPos = aiPlayer.transform.position;

        foreach (Tile tile in reachableTiles)
        {
            if (tile.Type == Tile.TileType.Room && tile.RoomData != null)
            {
                float distance = Vector3.Distance(playerPos, tile.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestRoom = tile;
                }
            }
        }

        return closestRoom != null ? closestRoom.RoomData : null;
    }

    // Optional: Check if AI can reach any room at all
    public bool CanReachAnyRoom(PlayerController aiPlayer, int roll)
    {
        if (aiPlayer == null || aiPlayer.CurrentTile == null)
            return false;

        HashSet<Tile> reachableTiles = Pathfinder.GetReachableTiles(aiPlayer.CurrentTile, roll);
        
        foreach (Tile tile in reachableTiles)
        {
            if (tile.Type == Tile.TileType.Room && tile.RoomData != null)
                return true;
        }
        
        return false;
    }
}