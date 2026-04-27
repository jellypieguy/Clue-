using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string PlayerName;
    public bool IsHuman;
    public bool IsEliminated; // True if player made a wrong accusation — still shows cards but skips turns
    public Color PlayerColour;

    private List<Card> hand = new List<Card>();

    // Tracks what this player knows: rows = cards, columns: 0 = have it, 1 = shown to me, 2 = not in game
    private bool[,] detectiveNotes;

    // Board position in grid coordinates
    public int CurrentRow;
    public int CurrentCol;
    public string CurrentRoom; // Null if player is in a corridor rather than a named room

    // Sets up the player with data loaded from JSON at game start
    public void Initialise(string name, bool isHuman, Color colour, int startRow, int startCol)
    {
        PlayerName = name;
        IsHuman = isHuman;
        PlayerColour = colour;
        CurrentRow = startRow;
        CurrentCol = startCol;
        CurrentRoom = null;
        IsEliminated = false;

        // 6 persons + 6 weapons + 9 rooms = 21 cards total
        detectiveNotes = new bool[21, 3];
    }

    // Adds a dealt card to this player's hand
    public void AddCardToHand(Card card)
    {
        hand.Add(card);
    }

    public List<Card> GetHand()
    {
        return hand;
    }

    // Returns true if this player holds the named card in their hand
    public bool HasCard(string cardName)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Name == cardName) return true;
        }
        return false;
    }

    // Returns a card from this player's hand that matches the suggestion, or null if none match.
    // AI picks randomly from matching cards; human currently returns the first match.
    public Card GetCardToShow(string person, string weapon, string room)
    {
        List<Card> matchingCards = new List<Card>();

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Name == person || hand[i].Name == weapon || hand[i].Name == room)
                matchingCards.Add(hand[i]);
        }

        if (matchingCards.Count == 0) return null;

        // AI randomly selects which matching card to reveal
        if (!IsHuman)
            return matchingCards[Random.Range(0, matchingCards.Count)];

        // TODO: let human choose which card to show via UI
        return matchingCards[0];
    }
}