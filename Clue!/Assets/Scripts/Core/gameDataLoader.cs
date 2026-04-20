using UnityEngine;
using System.IO;

public class GameDataLoader : MonoBehaviour
{
    // this class reads game_data.json and stores everything

    public GameData LoadedData { get; private set; }

    public void LoadGameData()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "game_data.json");

        // check the file actually exists
        if (!File.Exists(filePath))
        {
            Debug.LogError("game_data.json not found at: " + filePath);
            return;
        }

        // read the entire file as a string
        string jsonText = File.ReadAllText(filePath);

        // convert the JSON text into a GameData object
        LoadedData = JsonUtility.FromJson<GameData>(jsonText);

        Debug.Log("Game data loaded successfully!");
        Debug.Log("Characters: " + LoadedData.characters.Length);
        Debug.Log("Weapons: " + LoadedData.weapons.Length);
        Debug.Log("Rooms: " + LoadedData.rooms.Length);
        Debug.Log("Secret passages: " + LoadedData.secretPassages.Length);
    }

    // helper methods so other scripts can easily get what they need

    public string[] GetCharacterNames()
    {
        string[] names = new string[LoadedData.characters.Length];
        for (int i = 0; i < LoadedData.characters.Length; i++)
        {
            names[i] = LoadedData.characters[i].name;
        }
        return names;
    }

    public string[] GetWeaponNames()
    {
        return LoadedData.weapons;
    }

    public string[] GetRoomNames()
    {
        return LoadedData.rooms;
    }
}
