using UnityEngine;

/// Defines the three card categories used in suggestions and accusations.
public enum CardType
{
    Person,  // One of the six suspects
    Weapon,  // One of the six weapons
    Room     // One of the nine rooms
}

/// Represents a single Clue card. Used by CardDealer, MurderEnvelope and SuggestionSystem.
public class Card
{
    public string Name { get; private set; }   // Display name e.g. "Miss Scarlett"
    public CardType Type { get; private set; } // Category of this card

    /// Creates a new card with the given name and type.
    public Card(string name, CardType type)
    {
        Name = name;
        Type = type;
    }
}