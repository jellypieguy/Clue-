using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// AI agent handling autonomous players based on difficulty setting.
public class AIAgent : MonoBehaviour
{
    private Dictionary<PlayerController, HashSet<CardData>> _aiMemory = new Dictionary<PlayerController, HashSet<CardData>>();

    private AIDifficulty GetDifficulty()
    {
        return GameSettings.Instance != null ? GameSettings.Instance.AIDifficultyLevel : AIDifficulty.Easy;
    }

    private void EnsureMemory(PlayerController aiPlayer)
    {
        if (!_aiMemory.ContainsKey(aiPlayer))
        {
            _aiMemory[aiPlayer] = new HashSet<CardData>();
            PlayerHand hand = aiPlayer.GetComponent<PlayerHand>();
            if (hand != null)
            {
                foreach (var card in hand.Cards)
                {
                    _aiMemory[aiPlayer].Add(card);
                }
            }
        }
    }

    public void RecordShownCard(PlayerController aiPlayer, CardData card)
    {
        if (aiPlayer == null || card == null || aiPlayer.IsHuman) return;
        EnsureMemory(aiPlayer);
        _aiMemory[aiPlayer].Add(card);
    }

    // Picks a target room based on difficulty level.
    public CardData ChooseTargetRoom(PlayerController aiPlayer)
    {
        List<CardData> rooms = DeckManager.Instance.AllActiveRooms;
        if (rooms == null || rooms.Count == 0) return null;

        CardData currentRoom = aiPlayer.CurrentTile != null ? aiPlayer.CurrentTile.RoomData : null;
        AIDifficulty diff = GetDifficulty();

        EnsureMemory(aiPlayer);
        List<CardData> validRooms = new List<CardData>();

        if (diff == AIDifficulty.Easy)
        {
            validRooms.AddRange(rooms);
        }
        else // Medium and Hard
        {
            // Avoid rooms already disproved
            foreach (var r in rooms)
            {
                if (!_aiMemory[aiPlayer].Contains(r)) validRooms.Add(r);
            }
            if (validRooms.Count == 0) validRooms.AddRange(rooms); // fallback if all rooms known
        }

        validRooms.Remove(currentRoom);
        if (validRooms.Count == 0) validRooms.AddRange(rooms);
        validRooms.Remove(currentRoom);

        if (validRooms.Count == 0) return currentRoom; // Should not happen with multiple rooms

        return validRooms[Random.Range(0, validRooms.Count)];
    }

    // Makes a suggestion based on difficulty level.
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
        if (suspects.Count == 0 || weapons.Count == 0) return null;

        EnsureMemory(aiPlayer);
        AIDifficulty diff = GetDifficulty();

        List<CardData> possibleSuspects = new List<CardData>();
        List<CardData> possibleWeapons = new List<CardData>();

        if (diff == AIDifficulty.Easy)
        {
            possibleSuspects.AddRange(suspects);
            possibleWeapons.AddRange(weapons);
        }
        else
        {
            foreach (var s in suspects) if (!_aiMemory[aiPlayer].Contains(s)) possibleSuspects.Add(s);
            foreach (var w in weapons) if (!_aiMemory[aiPlayer].Contains(w)) possibleWeapons.Add(w);

            if (possibleSuspects.Count == 0) possibleSuspects.AddRange(suspects);
            if (possibleWeapons.Count == 0) possibleWeapons.AddRange(weapons);
        }

        CardData chosenSuspect = possibleSuspects[Random.Range(0, possibleSuspects.Count)];
        CardData chosenWeapon  = possibleWeapons[Random.Range(0, possibleWeapons.Count)];

        // Hard AI might occasionally bluff and suggest a card it has
        if (diff == AIDifficulty.Hard && Random.value < 0.2f)
        {
            PlayerHand hand = aiPlayer.GetComponent<PlayerHand>();
            if (hand != null && hand.Cards.Count > 0)
            {
                var ownSuspects = hand.Cards.Where(c => c.Type == CardData.CardType.Suspect).ToList();
                if (ownSuspects.Count > 0) chosenSuspect = ownSuspects[Random.Range(0, ownSuspects.Count)];
            }
        }

        return suggestionSystem.ProcessSuggestion(chosenSuspect, chosenWeapon, currentRoom,
            playerIndex, allPlayers);
    }

    // Evaluates if the AI should make an accusation based on its memory and difficulty
    public bool ShouldAccuse(PlayerController aiPlayer)
    {
        AIDifficulty diff = GetDifficulty();
        if (diff == AIDifficulty.Easy) return false;

        EnsureMemory(aiPlayer);

        int unknownSuspects = 0, unknownWeapons = 0, unknownRooms = 0;
        foreach (var s in DeckManager.Instance.AllSuspects) if (!_aiMemory[aiPlayer].Contains(s)) unknownSuspects++;
        foreach (var w in DeckManager.Instance.AllWeapons) if (!_aiMemory[aiPlayer].Contains(w)) unknownWeapons++;
        foreach (var r in DeckManager.Instance.AllActiveRooms) if (!_aiMemory[aiPlayer].Contains(r)) unknownRooms++;

        // Medium/Hard accuse if they've narrowed down to exactly 1 of each card
        if (diff == AIDifficulty.Hard || diff == AIDifficulty.Medium)
        {
            return (unknownSuspects == 1 && unknownWeapons == 1 && unknownRooms == 1);
        }

        return false;
    }

    // Generates an accusation
    public CardData[] MakeAccusation(PlayerController aiPlayer)
    {
        EnsureMemory(aiPlayer);
        AIDifficulty diff = GetDifficulty();

        List<CardData> suspects = DeckManager.Instance.AllSuspects;
        List<CardData> weapons  = DeckManager.Instance.AllWeapons;
        List<CardData> rooms    = DeckManager.Instance.AllActiveRooms;

        if (diff == AIDifficulty.Easy)
        {
            return new CardData[]
            {
                suspects[Random.Range(0, suspects.Count)],
                weapons[Random.Range(0, weapons.Count)],
                rooms[Random.Range(0, rooms.Count)]
            };
        }

        // Medium/Hard selects the unknown cards
        CardData accSuspect = suspects.FirstOrDefault(s => !_aiMemory[aiPlayer].Contains(s)) ?? suspects[0];
        CardData accWeapon = weapons.FirstOrDefault(w => !_aiMemory[aiPlayer].Contains(w)) ?? weapons[0];
        CardData accRoom = rooms.FirstOrDefault(r => !_aiMemory[aiPlayer].Contains(r)) ?? rooms[0];

        return new CardData[] { accSuspect, accWeapon, accRoom };
    }
}