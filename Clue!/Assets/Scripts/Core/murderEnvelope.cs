using UnityEngine;

public class MurderEnvelope
{
    public Card PersonCard { get;
    private set; }
    public Card WeaponCard { get;
    private set; }
    public Card RoomCard { get;
    private set; }

    public MurderEnvelope(Card person, Card weapon, Card room)
    {
        PersonCard = person;
        WeaponCard = weapon;
        RoomCard = room;
    }

    public bool CheckAccusation(string person, string weapon, string room)
    {
        return PersonCard.Name == person &&
               WeaponCard.Name == weapon &&
               RoomCard.Name == room;
    }
}
