using UnityEngine;
using System.IO;

public class GameDataLoader : MonoBehaviour
{
    public GameData LoadedData { get; private set; }

    public void LoadGameData()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "game_data.json");
        Debug.Log("Looking for game data at: " + filePath);

        // try the standard path first
        if (File.Exists(filePath))
        {
            string jsonText = File.ReadAllText(filePath);
            LoadedData = JsonUtility.FromJson<GameData>(jsonText);
            Debug.Log("Game data loaded from StreamingAssets!");
        }
        else
        {
            // fallback: try loading from Resources folder instead
            TextAsset jsonFile = Resources.Load<TextAsset>("game_data");
            if (jsonFile != null)
            {
                LoadedData = JsonUtility.FromJson<GameData>(jsonFile.text);
                Debug.Log("Game data loaded from Resources!");
            }
            else
            {
                Debug.LogError("Could not find game_data.json in StreamingAssets or Resources!");
                Debug.LogError("StreamingAssets path tried: " + filePath);
                Debug.LogError("Make sure game_data.json is in Assets/StreamingAssets/ OR Assets/Resources/");
                return;
            }
        }

        Debug.Log("Characters: " + LoadedData.characters.Length);
        Debug.Log("Weapons: " + LoadedData.weapons.Length);
        Debug.Log("Rooms: " + LoadedData.rooms.Length);
        Debug.Log("Secret passages: " + LoadedData.secretPassages.Length);
    }

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
