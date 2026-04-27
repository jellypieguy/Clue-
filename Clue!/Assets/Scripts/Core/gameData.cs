using System;

// Root data structure mapped from the JSON file
[Serializable]
public class GameData
{
    public CharacterData[] characters;  // All playable characters with start positions
    public string[] weapons;            // List of weapon names
    public string[] rooms;              // List of room names
    public SecretPassage[] secretPassages; // Pairs of rooms connected by secret passages
}

// Holds data for a single character loaded from JSON
[Serializable]
public class CharacterData
{
    public string name;     // Character name e.g. "Miss Scarlett"
    public int startRow;    // Starting row position on the board
    public int startCol;    // Starting column position on the board
    public ColourData colour; // Token colour for this character
}

// RGB colour values for a character token, serialised from JSON
[Serializable]
public class ColourData
{
    public float r; // Red channel (0-1)
    public float g; // Green channel (0-1)
    public float b; // Blue channel (0-1)
}

// Defines a one-way secret passage between two rooms
[Serializable]
public class SecretPassage
{
    public string from; // Room the passage starts from
    public string to;   // Room the passage leads to
}