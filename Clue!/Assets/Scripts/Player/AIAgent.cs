using System.Collections.Generic;
using UnityEngine;

public class AIAgent : MonoBehaviour
{
    private CardDealer cardDealer;

    // Links the AI agent to the card dealer to access card name lists
    public void Initialise(CardDealer dealer)
    {
        cardDealer = dealer;
    }

    // Picks a random room for the AI to move to, avoiding the room it is already in
    public string ChooseTargetRoom(Player aiPlayer)
    {
        string[] rooms = cardDealer.GetRoomNames();
        string target = rooms[Random.Range(0, rooms.Length)];

        // Re-roll if the AI would stay in the same room
        if (target == aiPlayer.CurrentRoom && rooms.Length > 1)
        {
            while (target == aiPlayer.CurrentRoom)
                target = rooms[Random.Range(0, rooms.Length)];
        }

        return target;
    }

    // Makes a random suggestion using a random person, random weapon, and the AI's current room
    public void MakeSuggestion(Player aiPlayer, SuggestionSystem suggestionSystem,
                                int playerIndex, List<Player> allPlayers)
    {
        if (aiPlayer.CurrentRoom == null)
        {
            Debug.Log(aiPlayer.PlayerName + " is not in a room, cannot suggest.");
            return;
        }

        string[] persons = cardDealer.GetPersonNames();
        string[] weapons = cardDealer.GetWeaponNames();

        string chosenPerson = persons[Random.Range(0, persons.Length)];
        string chosenWeapon = weapons[Random.Range(0, weapons.Length)];

        suggestionSystem.ProcessSuggestion(chosenPerson, chosenWeapon, aiPlayer.CurrentRoom,
                                           playerIndex, allPlayers);
    }

    // Always returns false for this random agent — accusation logic can be extended in future
    // Prevents the AI from eliminating itself with an early random guess
    public bool ShouldAccuse(Player aiPlayer)
    {
        return false;
    }

    // Generates a fully random accusation across all three card categories
    public string[] MakeAccusation()
    {
        string[] persons = cardDealer.GetPersonNames();
        string[] weapons = cardDealer.GetWeaponNames();
        string[] rooms = cardDealer.GetRoomNames();

        return new string[]
        {
            persons[Random.Range(0, persons.Length)],
            weapons[Random.Range(0, weapons.Length)],
            rooms[Random.Range(0, rooms.Length)]
        };
    }
}