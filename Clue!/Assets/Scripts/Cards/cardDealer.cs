using System.Collections.Generic;
using UnityEngine;

public class CardDealer : MonoBehaviour
{
    public Card MurderPerson { get; private set; }
    public Card MurderWeapon { get; private set; }
    public Card MurderRoom { get; private set; }

    // filled from the JSON 
    private string[] personNames;
    private string[] weaponNames;
    private string[] roomNames;

    public List<Card> AllCards { get; private set; }

    // load names from the JSON 
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

        MurderPerson = personCards[0];
        MurderWeapon = weaponCards[0];
        MurderRoom = roomCards[0];
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
            // players[playerIndex].AddCardToHand(dealPile[i]);
            playerIndex = (playerIndex + 1) % players.Count;
        }

        for (int i = 0; i < players.Count; i++)
        {
            string cards = "";
            // foreach (Card c in players[i].GetHand())
            //     cards += c.Name + ", ";
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


    public void LoadNamesFromDeckManager()
    {
        if (DeckManager.Instance == null) { Debug.LogWarning("CardDealer: DeckManager not found."); return; }

        var s = DeckManager.Instance.AllSuspects;
        var w = DeckManager.Instance.AllWeapons;
        var r = DeckManager.Instance.AllActiveRooms;

        personNames = new string[s.Count];
        for (int i = 0; i < s.Count; i++) personNames[i] = s[i].CardName;

        weaponNames = new string[w.Count];
        for (int i = 0; i < w.Count; i++) weaponNames[i] = w[i].CardName;

        roomNames = new string[r.Count];
        for (int i = 0; i < r.Count; i++) roomNames[i] = r[i].CardName;

        Debug.Log("CardDealer: names synced from DeckManager.");
    }

    public void DealCardsToPlayers(List<Player> players)
    {
        if (TurnManager.Instance == null) return;
        List<PlayerController> controllers = TurnManager.Instance.GetPlayers();

        for (int i = 0; i < players.Count && i < controllers.Count; i++)
        {
            PlayerHand ph = controllers[i].GetComponent<PlayerHand>();
            if (ph == null) continue;

        }

        AllCards = new List<Card>();
        foreach (CardData cd in DeckManager.Instance.ShuffledDeck)
        {
            CardType ct = cd.Type == CardData.CardType.Weapon ? CardType.Weapon
                        : cd.Type == CardData.CardType.Room ? CardType.Room
                        : CardType.Person;
            AllCards.Add(new Card(cd.CardName, ct));
        }

        Debug.Log("CardDealer: Player hands synced from DeckManager.");
    }
}
