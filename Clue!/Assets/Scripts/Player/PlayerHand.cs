using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    private readonly List<CardData> cards = new();
    
    public IReadOnlyList<CardData> Cards => cards;

    public void AddCard(CardData card)
    {
        if (card != null && !cards.Contains(card))
        {
            cards.Add(card);
        }
    }

    public List<CardData> GetHand() => new(cards);
    public CardData TryRefuteSuggestion(CardData suspect, CardData weapon, CardData room)
    {
        if (cards.Contains(suspect)) return suspect;
        if (cards.Contains(weapon)) return weapon;
        if (cards.Contains(room)) return room;
        
        return null;
    }

    // get rid of matching cards we hold so the ui can pick which one to show
    public List<CardData> GetRefutingCards(CardData suspect, CardData weapon, CardData room)
    {
        var hits = new List<CardData>();
        
        if (cards.Contains(suspect)) hits.Add(suspect);
        if (cards.Contains(weapon)) hits.Add(weapon);
        if (cards.Contains(room)) hits.Add(room);
        
        return hits;
    }
}