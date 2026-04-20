using System.Collections.Generic;
using UnityEngine;

public class AIAgent : MonoBehaviour
{
    private CardDealer cardDealer;

    public void Initialise(CardDealer dealer)
    {
        cardDealer = dealer;
    }

    // picks a random direction to move towards a room
    // returns a target room name
    public string ChooseTargetRoom(Player aiPlayer)
    {
        string[] rooms = cardDealer.GetRoomNames();
        string target = rooms[Random.Range(0, rooms.Length)];

        // avoid suggesting in the same room if possible
        if (target == aiPlayer.CurrentRoom && rooms.Length > 1)
        {
            while (target == aiPlayer.CurrentRoom)
                target = rooms[Random.Range(0, rooms.Length)];
        }

        return target;
    }

    // makes a random suggestion using the room the AI is currently in
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
        string room = aiPlayer.CurrentRoom;

        suggestionSystem.ProcessSuggestion(chosenPerson, chosenWeapon, room,
                                           playerIndex, allPlayers);
    }

    // decides whether to make an accusation (very conservative: only if AI holds 18+ known cards)
    public bool ShouldAccuse(Player aiPlayer)
    {
        // for random agent, very low chance of accusing unless we add smarter logic later
        // this prevents the AI from eliminating itself early
        return false;
    }

    // makes a random accusation (if ShouldAccuse returns true)
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
