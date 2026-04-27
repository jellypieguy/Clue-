using UnityEngine;

public class MurderEnvelope
{
    // The three secret cards that form the murder solution
    public Card PersonCard { get; private set; }
    public Card WeaponCard { get; private set; }
    public Card RoomCard { get; private set; }

    // Stores one card of each type as the hidden murder solution
    public MurderEnvelope(Card person, Card weapon, Card room)
    {
        PersonCard = person;
        WeaponCard = weapon;
        RoomCard = room;
    }

    // Returns true if the accusation exactly matches all three murder cards
    public bool CheckAccusation(string person, string weapon, string room)
    {
        return PersonCard.Name == person &&
               WeaponCard.Name == weapon &&
               RoomCard.Name == room;
    }
}