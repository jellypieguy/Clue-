using UnityEngine;
using System.IO;
using System.Linq;

public class GameDataLoader : MonoBehaviour
{
    public GameData LoadedData { get; private set; }

    public void LoadGameData()
    {
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "game_data.json");
        
        if (File.Exists(streamingPath))
        {
            string jsonContent = File.ReadAllText(streamingPath);
            LoadedData = JsonUtility.FromJson<GameData>(jsonContent);
            return;
        }

        var resourceFile = Resources.Load<TextAsset>("game_data");
        if (resourceFile != null)
        {
            LoadedData = JsonUtility.FromJson<GameData>(resourceFile.text);
            return;
        }

        Debug.LogError($"GameDataLoader: mis game_data.json fix in the {streamingPath} ");
    }

    public string[] GetCharacterNames() => 
        LoadedData?.characters?.Select(c => c.name).ToArray() ?? System.Array.Empty<string>();

    public string[] GetWeaponNames() => 
        LoadedData?.weapons ?? System.Array.Empty<string>();

    public string[] GetRoomNames() => 
        LoadedData?.rooms ?? System.Array.Empty<string>();
}