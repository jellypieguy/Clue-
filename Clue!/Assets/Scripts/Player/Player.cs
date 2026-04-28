using UnityEngine;

// Holds player identity, position, and detective notes.
// Cards are handled by the PlayerHand component on the same GameObject.
// Input and movement are handled by the PlayerController component on the same GameObject.
public class Player : MonoBehaviour
{
    public string PlayerName;
    public Color PlayerColour;

    // Tracks what this player knows: rows = cards, columns: 0 = have it, 1 = shown to me, 2 = not in game
    private bool[,] detectiveNotes;

    // Sets up the player with data loaded at game start
    public void Initialise(string name, Color colour)
    {
        PlayerName = name;
        PlayerColour = colour;

        // 6 persons + 6 weapons + 9 rooms = 21 cards total
        detectiveNotes = new bool[21, 3];
    }

    // Marks a fact in the detective notes grid
    // cardIndex: 0-20 (the card)
    // noteType: 0 = have it, 1 = shown to me, 2 = not in game
    public void MarkNote(int cardIndex, int noteType)
    {
        if (cardIndex < 0 || cardIndex >= 21) return;
        if (noteType < 0 || noteType >= 3) return;
        detectiveNotes[cardIndex, noteType] = true;
    }

    // Reads a fact from the detective notes grid
    public bool ReadNote(int cardIndex, int noteType)
    {
        if (cardIndex < 0 || cardIndex >= 21) return false;
        if (noteType < 0 || noteType >= 3) return false;
        return detectiveNotes[cardIndex, noteType];
    }
}