using System.Collections.Generic;
using UnityEngine;

public class CardDealer : MonoBehaviour
{
    public MurderEnvelope Envelope { get; private set; }

    //they get filled from the JSON file
    private string[] personNames;
    private string[] weaponNames;
    private string[] roomNames;

    public List<Card> AllCards { get; private set; }

    // call this to load names from the JSON data
    public void LoadNamesFromData(GameDataLoader dataLoader)
    {
        personNames = dataLoader.GetCharacterNames();
        weaponNames = dataLoader.GetWeaponNames();
        roomNames = dataLoader.GetRoomNames();

        Debug.Log("CardDealer loaded " + personNames.Length + " characters, "
                  + weaponNames.Length + " weapons, "
                  + roomNames.Length + " rooms from file.");
    }

    public void SetupAndDeal(List<Player> players)
    {
        List<Card> personCards = new List<Card>();
        List<Card> weaponCards = new List<Card>();
        List<Card> roomCards = new List<Card>();

        for (int i = 0; i < personNames.Length; i++)
            personCards.Add(new Card(personNames[i], CardType.Person));

        for (int i = 0; i < weaponNames.Length; i++)
            weaponCards.Add(new Card(weaponNames[i], CardType.Weapon));

        for (int i = 0; i < roomNames.Length; i++)
            roomCards.Add(new Card(roomNames[i], CardType.Room));

        Shuffle(personCards);
        Shuffle(weaponCards);
        Shuffle(roomCards);

        Envelope = new MurderEnvelope(personCards[0], weaponCards[0], roomCards[0]);
        Debug.Log("Murder solution: " + personCards[0].Name + " with "
                  + weaponCards[0].Name + " in " + roomCards[0].Name);

        personCards.RemoveAt(0);
        weaponCards.RemoveAt(0);
        roomCards.RemoveAt(0);

        List<Card> dealPile = new List<Card>();
        dealPile.AddRange(personCards);
        dealPile.AddRange(weaponCards);
        dealPile.AddRange(roomCards);
        Shuffle(dealPile);

        AllCards = new List<Card>();
        AllCards.AddRange(personCards);
        AllCards.AddRange(weaponCards);
        AllCards.AddRange(roomCards);

        int playerIndex = 0;
        for (int i = 0; i < dealPile.Count; i++)
        {
            players[playerIndex].AddCardToHand(dealPile[i]);
            playerIndex = (playerIndex + 1) % players.Count;
        }

        for (int i = 0; i < players.Count; i++)
        {
            string cards = "";
            foreach (Card c in players[i].GetHand())
                cards += c.Name + ", ";
            Debug.Log(players[i].PlayerName + " has: " + cards);
        }
    }

    private void Shuffle(List<Card> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    public string[] GetPersonNames() { return personNames; }
    public string[] GetWeaponNames() { return weaponNames; }
    public string[] GetRoomNames() { return roomNames; }
}
