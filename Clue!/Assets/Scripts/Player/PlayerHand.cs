using System.Collections.Generic;
using UnityEngine;

// holds the cards given to player 
public class PlayerHand : MonoBehaviour
{
    private List<CardData> _hand = new List<CardData>();
    public IReadOnlyList<CardData> Cards => _hand; 

    public void AddCard(CardData card)
    {
        if (card != null && !_hand.Contains(card))
            _hand.Add(card);
    }

    // copy of the hand
    public List<CardData> GetHand()
    {
        return new List<CardData>(_hand);
    }

    // first card in hand that matches
    public CardData TryRefuteSuggestion(CardData suspect, CardData weapon, CardData room)
    {
        if (_hand.Contains(suspect)) return suspect;
        if (_hand.Contains(weapon))  return weapon;
        if (_hand.Contains(room))    return room;
        return null;
    }

    // returns matching card used to reveal.
    public List<CardData> GetRefutingCards(CardData suspect, CardData weapon, CardData room)
    {
        List<CardData> found = new List<CardData>();
        if (_hand.Contains(suspect)) found.Add(suspect);
        if (_hand.Contains(weapon))  found.Add(weapon);
        if (_hand.Contains(room))    found.Add(room);
        return found;
    }
}