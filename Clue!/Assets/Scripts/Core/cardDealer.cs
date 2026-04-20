using System.Collections.Generic;
using UnityEngine;

public class CardDealer : MonoBehaviour
{
    public MurderEnvelope Envelope { get; private set; }

    private string[] personNames = { "Col Mustard", "Prof Plum", "Rev Green", "Mrs Peacock", "Miss Scarlett", "Mrs White" };
    private string[] weaponNames = { "Dagger", "Candlestick", "Revolver", "Rope", "Lead Piping", "Spanner" };
    private string[] roomNames = { "Kitchen", "Ballroom", "Conservatory", "Billiard Room", "Library", "Study", "Hall", "Lounge", "Dining Room" };

    public List<Card> AllCards { get; private set; }

    public void SetupAndDeal(List<Player> players)
    {
        // creating all 21 cards
        List<Card> personCards = new List<Card>();
        List<Card> weaponCards = new List<Card>();
        List<Card> roomCards = new List<Card>();

        for (int i = 0; i < personNames.Length; i++)
            personCards.Add(new Card(personNames[i], CardType.Person));

        for (int i = 0; i < weaponNames.Length; i++)
            weaponCards.Add(new Card(weaponNames[i], CardType.Weapon));

        for (int i = 0; i < roomNames.Length; i++)
            roomCards.Add(new Card(roomNames[i], CardType.Room));

        // shuffle each category
        Shuffle(personCards);
        Shuffle(weaponCards);
        Shuffle(roomCards);

        // take top card of each for the murder envelope
        Envelope = new MurderEnvelope(personCards[0], weaponCards[0], roomCards[0]);
        Debug.Log("Murder solution: " + personCards[0].Name + " with " + weaponCards[0].Name + " in " + roomCards[0].Name);

        // remove murder cards from their lists
        personCards.RemoveAt(0);
        weaponCards.RemoveAt(0);
        roomCards.RemoveAt(0);

        // combine remaining 18 cards and shuffle
        List<Card> dealPile = new List<Card>();
        dealPile.AddRange(personCards);
        dealPile.AddRange(weaponCards);
        dealPile.AddRange(roomCards);
        Shuffle(dealPile);

        // store all cards for reference
        AllCards = new List<Card>();
        AllCards.AddRange(personCards);
        AllCards.AddRange(weaponCards);
        AllCards.AddRange(roomCards);

        // deal cards one at a time clockwise (some players may get more)
        int playerIndex = 0;
        for (int i = 0; i < dealPile.Count; i++)
        {
            players[playerIndex].AddCardToHand(dealPile[i]);
            playerIndex = (playerIndex + 1) % players.Count;
        }

        // log each player's hand for debugging
        for (int i = 0; i < players.Count; i++)
        {
            string cards = "";
            foreach (Card c in players[i].GetHand())
                cards += c.Name + ", ";
            Debug.Log(players[i].PlayerName + " has: " + cards);
        }
    }

    // fisher-yates shuffle
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
