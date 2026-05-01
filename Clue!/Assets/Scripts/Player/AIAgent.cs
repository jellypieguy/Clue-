using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIAgent : MonoBehaviour
{
    // Per-bot notepad — seeded on first use with the bot's own hand so it never suggests cards it holds
    private readonly Dictionary<PlayerController, HashSet<CardData>> _memory = new();

    private AIDifficulty Difficulty =>
        GameSettings.Instance?.AIDifficultyLevel ?? AIDifficulty.Easy;

    private void EnsureMemoryInitialized(PlayerController bot)
    {
        if (_memory.ContainsKey(bot)) return;

        _memory[bot] = new HashSet<CardData>();
        if (bot.TryGetComponent(out PlayerHand hand))
            foreach (var card in hand.Cards) _memory[bot].Add(card);
    }

    public void RecordShownCard(PlayerController bot, CardData card)
    {
        if (bot == null || card == null || bot.IsHuman) return;
        EnsureMemoryInitialized(bot);
        _memory[bot].Add(card);
    }

    public CardData ChooseTargetRoom(PlayerController bot)
    {
        var rooms = DeckManager.Instance.AllActiveRooms;
        if (rooms == null || rooms.Count == 0) return null;

        EnsureMemoryInitialized(bot);
        var currentRoom = bot.CurrentTile?.RoomData;

        List<CardData> candidates;
        if (Difficulty == AIDifficulty.Easy)
        {
            candidates = new List<CardData>(rooms);
        }
        else
        {
            // Filter out rooms already confirmed safe
            candidates = rooms.Where(r => !_memory[bot].Contains(r)).ToList();
            if (candidates.Count == 0) candidates.AddRange(rooms);
        }

        candidates.Remove(currentRoom);
        return candidates.Count == 0 ? currentRoom : candidates[Random.Range(0, candidates.Count)];
    }

    public void MakeSuggestion(PlayerController bot, SuggestionSystem sys, int idx,
                                List<PlayerController> players, System.Action<CardData> onComplete)
    {
        var room = bot.CurrentTile?.RoomData;
        if (room == null) { onComplete(null); return; }

        var suspects = DeckManager.Instance.AllSuspects;
        var weapons  = DeckManager.Instance.AllWeapons;
        if (suspects.Count == 0 || weapons.Count == 0) { onComplete(null); return; }

        EnsureMemoryInitialized(bot);
        var diff = Difficulty;

        var possibleSuspects = diff == AIDifficulty.Easy
            ? new List<CardData>(suspects)
            : suspects.Where(s => !_memory[bot].Contains(s)).ToList();

        var possibleWeapons = diff == AIDifficulty.Easy
            ? new List<CardData>(weapons)
            : weapons.Where(w => !_memory[bot].Contains(w)).ToList();

        if (possibleSuspects.Count == 0) possibleSuspects.AddRange(suspects);
        if (possibleWeapons.Count == 0)  possibleWeapons.AddRange(weapons);

        var chosenSuspect = possibleSuspects[Random.Range(0, possibleSuspects.Count)];
        var chosenWeapon  = possibleWeapons[Random.Range(0, possibleWeapons.Count)];

        // Hard bluff — 20% chance to suggest a card it owns to throw off the table
        if (diff == AIDifficulty.Hard && Random.value < 0.2f && bot.TryGetComponent(out PlayerHand hand))
        {
            var ownedSuspects = hand.Cards.Where(c => c.Type == CardData.CardType.Suspect).ToList();
            if (ownedSuspects.Count > 0)
                chosenSuspect = ownedSuspects[Random.Range(0, ownedSuspects.Count)];
        }

        sys.ProcessSuggestion(chosenSuspect, chosenWeapon, room, idx, players, onComplete);
    }

    public bool ShouldAccuse(PlayerController bot)
    {
        if (Difficulty == AIDifficulty.Easy) return false;

        EnsureMemoryInitialized(bot);
        int unknownSuspects = DeckManager.Instance.AllSuspects.Count(s => !_memory[bot].Contains(s));
        int unknownWeapons  = DeckManager.Instance.AllWeapons.Count(w => !_memory[bot].Contains(w));
        int unknownRooms    = DeckManager.Instance.AllActiveRooms.Count(r => !_memory[bot].Contains(r));

        // Only pull the trigger when exactly one unknown is left in each category
        return unknownSuspects == 1 && unknownWeapons == 1 && unknownRooms == 1;
    }

    public CardData[] MakeAccusation(PlayerController bot)
    {
        EnsureMemoryInitialized(bot);

        var suspects = DeckManager.Instance.AllSuspects;
        var weapons  = DeckManager.Instance.AllWeapons;
        var rooms    = DeckManager.Instance.AllActiveRooms;

        if (Difficulty == AIDifficulty.Easy)
        {
            // Easy just rolls the dice — probably wrong, honestly that's fine
            return new[] {
                suspects[Random.Range(0, suspects.Count)],
                weapons[Random.Range(0, weapons.Count)],
                rooms[Random.Range(0, rooms.Count)]
            };
        }

        var accSuspect = suspects.FirstOrDefault(s => !_memory[bot].Contains(s)) ?? suspects[0];
        var accWeapon  = weapons.FirstOrDefault(w => !_memory[bot].Contains(w))  ?? weapons[0];
        var accRoom    = rooms.FirstOrDefault(r => !_memory[bot].Contains(r))    ?? rooms[0];

        return new[] { accSuspect, accWeapon, accRoom };
    }
}
