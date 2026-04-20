using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string PlayerName;
    public bool IsHuman;
    public bool IsEliminated; // failed accusation, still shows cards but no turns
    public Color PlayerColour;

    private List<Card> hand = new List<Card>();
    private bool[,] detectiveNotes; // tracks what this player knows

    // board position
    public int CurrentRow;
    public int CurrentCol;
    public string CurrentRoom; // null if in a corridor

    public void Initialise(string name, bool isHuman, Color colour, int startRow, int startCol)
    {
        PlayerName = name;
        IsHuman = isHuman;
        PlayerColour = colour;
        CurrentRow = startRow;
        CurrentCol = startCol;
        CurrentRoom = null;
        IsEliminated = false;

        // 6 persons + 6 weapons + 9 rooms = 21 cards
        // rows = cards, columns: 0 = have it, 1 = shown to me, 2 = not in game
        detectiveNotes = new bool[21, 3];
    }

    public void AddCardToHand(Card card)
    {
        hand.Add(card);
    }

    public List<Card> GetHand()
    {
        return hand;
    }

    public bool HasCard(string cardName)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Name == cardName) return true;
        }
        return false;
    }

    // returns the first matching card this player can show, or null
    public Card GetCardToShow(string person, string weapon, string room)
    {
        List<Card> matchingCards = new List<Card>();

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Name == person || hand[i].Name == weapon || hand[i].Name == room)
            {
                matchingCards.Add(hand[i]);
            }
        }

        if (matchingCards.Count == 0) return null;

        // AI picks randomly, human would choose via UI
        if (!IsHuman)
        {
            return matchingCards[Random.Range(0, matchingCards.Count)];
        }

        // for human players, return the first one for now
        // TODO: let human choose which card to show via UI
        return matchingCards[0];
    }
}
