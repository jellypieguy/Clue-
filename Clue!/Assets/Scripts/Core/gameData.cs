using System;


[Serializable]
public class GameData
{
    public CharacterData[] characters;
    public string[] weapons;
    public string[] rooms;
    public SecretPassage[] secretPassages;
}

[Serializable]
public class CharacterData
{
    public string name;
    public int startRow;
    public int startCol;
    public ColourData colour;
}

[Serializable]
public class ColourData
{
    public float r;
    public float g;
    public float b;
}

[Serializable]
public class SecretPassage
{
    public string from;
    public string to;
}
