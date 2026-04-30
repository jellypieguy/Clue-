using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIAgent : MonoBehaviour
{
    // Each AI bot has its own memory set; tracks cards it knows are NOT the murder cards
    private readonly Dictionary<PlayerController, HashSet<CardData>> aiMemory = new();

    // Reads difficulty from GameSettings; defaults to Easy if GameSettings is missing
    private AIDifficulty CurrentDifficulty => 
        GameSettings.Instance != null ? GameSettings.Instance.AIDifficultyLevel : AIDifficulty.Easy;

    // Sets up memory for a bot the first time it's needed
    private void EnsureMemoryInitialized(PlayerController bot)
    {
        if (aiMemory.ContainsKey(bot)) return;

        aiMemory[bot] = new HashSet<CardData>();
        
        // Add the bot's own hand cards to memory so it knows they can't be murder cards
        if (bot.TryGetComponent(out PlayerHand hand))
        {
            foreach (var card in hand.Cards) aiMemory[bot].Add(card);
        }
    }

    // Called when another player shows a card to this bot during a suggestion refutation
    // Adds the revealed card to memory so the bot knows it's not a murder card
    public void RecordShownCard(PlayerController bot, CardData card)
    {
        if (bot == null || card == null || bot.IsHuman) return;
        
        EnsureMemoryInitialized(bot);
        aiMemory[bot].Add(card);
    }

    // Decides which room the bot should move to this turn
    // Easy AI picks any random room; Medium/Hard avoid rooms already ruled out
    public CardData ChooseTargetRoom(PlayerController bot)
    {
        var rooms = DeckManager.Instance.AllActiveRooms;
        if (rooms == null || rooms.Count == 0) return null;

        var currentRoom = bot.CurrentTile?.RoomData;
        EnsureMemoryInitialized(bot);

        var validRooms = new List<CardData>();

        if (CurrentDifficulty == AIDifficulty.Easy)
        {
            // Easy: pick from all rooms randomly
            validRooms.AddRange(rooms);
        }
        else 
        {
            // Medium/Hard: filter out rooms already known to be safe (not the murder room)
            validRooms.AddRange(rooms.Where(r => !aiMemory[bot].Contains(r)));

            // If all rooms are known (unlikely), fall back to any room
            if (validRooms.Count == 0) validRooms.AddRange(rooms);
        }

        // Don't suggest the room the bot is already in
        validRooms.Remove(currentRoom);

        // Edge case: if no other room is available, stay put
        if (validRooms.Count == 0) return currentRoom;

        return validRooms[Random.Range(0, validRooms.Count)];
    }

    // Builds and submits a suggestion for the current room
    // Returns the card that was shown in refutation (if any)
    public CardData MakeSuggestion(PlayerController bot, SuggestionSystem suggestionSystem, int playerIndex, List<PlayerController> allPlayers)
    {
        var currentRoom = bot.CurrentTile?.RoomData;
        if (currentRoom == null) return null;

        var suspects = DeckManager.Instance.AllSuspects;
        var weapons = DeckManager.Instance.AllWeapons;
        
        if (suspects.Count == 0 || weapons.Count == 0) return null;

        EnsureMemoryInitialized(bot);
        var diff = CurrentDifficulty;

        // Easy: suggest anyone, Medium/Hard: only suggest cards not yet ruled out
        var possibleSuspects = diff == AIDifficulty.Easy 
            ? new List<CardData>(suspects) 
            : suspects.Where(s => !aiMemory[bot].Contains(s)).ToList();

        var possibleWeapons = diff == AIDifficulty.Easy 
            ? new List<CardData>(weapons) 
            : weapons.Where(w => !aiMemory[bot].Contains(w)).ToList();

        // Fallback in case all cards have been ruled out (shouldn't happen normally)
        if (possibleSuspects.Count == 0) possibleSuspects.AddRange(suspects);
        if (possibleWeapons.Count == 0) possibleWeapons.AddRange(weapons);

        var chosenSuspect = possibleSuspects[Random.Range(0, possibleSuspects.Count)];
        var chosenWeapon = possibleWeapons[Random.Range(0, possibleWeapons.Count)];

        // Hard AI bluff: 20% chance to suggest a card it owns to mislead other players
        if (diff == AIDifficulty.Hard && Random.value < 0.2f && bot.TryGetComponent(out PlayerHand hand))
        {
            var ownedSuspects = hand.Cards.Where(c => c.Type == CardData.CardType.Suspect).ToList();
            if (ownedSuspects.Count > 0) 
            {
                chosenSuspect = ownedSuspects[Random.Range(0, ownedSuspects.Count)];
            }
        }

        // Submit the suggestion and return whatever card was shown in refutation
        return suggestionSystem.ProcessSuggestion(chosenSuspect, chosenWeapon, currentRoom, playerIndex, allPlayers);
    }

    // Decides whether the bot is confident enough to make a formal accusation
    // Only accuses when exactly one unknown remains in each category (suspect, weapon, room)
    public bool ShouldAccuse(PlayerController bot)
    {
        // Easy AI never accuses
        if (CurrentDifficulty == AIDifficulty.Easy) return false;

        EnsureMemoryInitialized(bot);

        // Count how many cards in each category are still unaccounted for
        int unknownSuspects = DeckManager.Instance.AllSuspects.Count(s => !aiMemory[bot].Contains(s));
        int unknownWeapons  = DeckManager.Instance.AllWeapons.Count(w => !aiMemory[bot].Contains(w));
        int unknownRooms    = DeckManager.Instance.AllActiveRooms.Count(r => !aiMemory[bot].Contains(r));

        // Only accuse when exactly one unknown remains in each — that must be the murder card
        return unknownSuspects == 1 && unknownWeapons == 1 && unknownRooms == 1;
    }

    // Builds the accusation. the bot's best guess at the murder combination
    public CardData[] MakeAccusation(PlayerController bot)
    {
        EnsureMemoryInitialized(bot);
        
        var suspects = DeckManager.Instance.AllSuspects;
        var weapons  = DeckManager.Instance.AllWeapons;
        var rooms    = DeckManager.Instance.AllActiveRooms;

        if (CurrentDifficulty == AIDifficulty.Easy)
        {
            // Easy AI guesses completely randomly — likely to be wrong
            return new[] {
                suspects[Random.Range(0, suspects.Count)],
                weapons[Random.Range(0, weapons.Count)],
                rooms[Random.Range(0, rooms.Count)]
            };
        }

        // Medium/Hard: pick the one card in each category not yet ruled out
        // Falls back to index 0 just in case deduction somehow fails
        var accSuspect = suspects.FirstOrDefault(s => !aiMemory[bot].Contains(s)) ?? suspects[0];
        var accWeapon  = weapons.FirstOrDefault(w => !aiMemory[bot].Contains(w))  ?? weapons[0];
        var accRoom    = rooms.FirstOrDefault(r => !aiMemory[bot].Contains(r))    ?? rooms[0];

        return new[] { accSuspect, accWeapon, accRoom };
    }
}