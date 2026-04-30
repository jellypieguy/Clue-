using UnityEngine;

public class Player : MonoBehaviour
{
    public string PlayerName;
    public Color PlayerColour;

    // rows = card (0-20), cols: 0 = have i 1 = shown to me, 2 = not in play
    private bool[,] notepadData;

    public void Initialize(string name, Color colour)
    {
        PlayerName = name;
        PlayerColour = colour;

        // the standard deck size: 6 suspects + 6 weapons + 9 rooms
        notepadData = new bool[21, 3];
    }

    public void MarkNote(int cardIndex, int noteType)
    {
        if (IsValidNote(cardIndex, noteType))
        {
            notepadData[cardIndex, noteType] = true;
        }
    }

    public bool ReadNote(int cardIndex, int noteType)
    {
        return IsValidNote(cardIndex, noteType) && notepadData[cardIndex, noteType];
    }

    private bool IsValidNote(int cardIndex, int noteType) => 
        cardIndex >= 0 && cardIndex < 21 && noteType >= 0 && noteType < 3;
}