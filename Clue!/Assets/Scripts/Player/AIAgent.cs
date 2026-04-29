using System.Collections.Generic;
using UnityEngine;

// Random-decision AI for autonomous players.
// Picks a random target room, makes random suggestions, never accuses.
// Sophistication can be added later, the spec only requires it plays the game.
public class AIAgent : MonoBehaviour
{
    // Picks a random room for the AI to move to, avoiding the room it is already in.
    // Returns null if the AI cannot determine its current room or no rooms are available.
    public CardData ChooseTargetRoom(PlayerController aiPlayer)
    {
        List<CardData> rooms = DeckManager.Instance.AllActiveRooms;
        if (rooms == null || rooms.Count == 0) return null;

        // What room is the AI currently in (if any)?
        CardData currentRoom = aiPlayer.CurrentTile != null ? aiPlayer.CurrentTile.RoomData : null;

        CardData target = rooms[Random.Range(0, rooms.Count)];

        // Re-roll if the AI would stay in the same room
        if (target == currentRoom && rooms.Count > 1)
        {
            while (target == currentRoom)
                target = rooms[Random.Range(0, rooms.Count)];
        }

        return target;
    }

    // Makes a random suggestion using a random suspect, random weapon, and the AI's current room.
    // Caller must check the AI is actually in a room first.
    public CardData MakeSuggestion(PlayerController aiPlayer, SuggestionSystem suggestionSystem,
        int playerIndex, List<PlayerController> allPlayers)
    {
        CardData currentRoom = aiPlayer.CurrentTile != null ? aiPlayer.CurrentTile.RoomData : null;

        if (currentRoom == null)
        {
            Debug.Log($"{aiPlayer.Character} is not in a room, cannot suggest.");
            return null;
        }

        List<CardData> suspects = DeckManager.Instance.AllSuspects;
        List<CardData> weapons  = DeckManager.Instance.AllWeapons;

        if (suspects.Count == 0 || weapons.Count == 0)
        {
            Debug.LogWarning("AIAgent: No suspects or weapons available to suggest.");
            return null;
        }

        CardData chosenSuspect = suspects[Random.Range(0, suspects.Count)];
        CardData chosenWeapon  = weapons[Random.Range(0, weapons.Count)];

        return suggestionSystem.ProcessSuggestion(chosenSuspect, chosenWeapon, currentRoom,
            playerIndex, allPlayers);
    }

    // Always returns false for this random agent.
    // Stops the AI from eliminating itself with a wild guess.
    // Smarter accusation logic can be added once detective notes are tracked.
    public bool ShouldAccuse(PlayerController aiPlayer)
    {
        return false;
    }

    // Generates a fully random accusation across all three card categories.
    // Used only if ShouldAccuse returns true.
    public CardData[] MakeAccusation()
    {
        List<CardData> suspects = DeckManager.Instance.AllSuspects;
        List<CardData> weapons  = DeckManager.Instance.AllWeapons;
        List<CardData> rooms    = DeckManager.Instance.AllActiveRooms;

        return new CardData[]
        {
            suspects[Random.Range(0, suspects.Count)],
            weapons[Random.Range(0, weapons.Count)],
            rooms[Random.Range(0, rooms.Count)]
        };
    }
}