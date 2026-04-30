using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AIDifficulty { Easy, Medium, Hard }

// Persists lobby configs (bot count, banned rooms, etc) across scenes
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    public AIDifficulty AIDifficultyLevel { get; private set; } = AIDifficulty.Easy;

    public int TotalPlayers { get; private set; } = 6;
    public int HumanPlayerCount { get; private set; } = 1;

    private readonly HashSet<CardData> disabledRooms = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetTotalPlayers(int count)
    {
        TotalPlayers = Mathf.Clamp(count, 2, 6);
        // Ensure human count doesn't overflow the new lobby size
        HumanPlayerCount = Mathf.Clamp(HumanPlayerCount, 1, TotalPlayers);
    }

    public void SetHumanCount(int count) => HumanPlayerCount = Mathf.Clamp(count, 1, TotalPlayers);

    public void SetAIDifficulty(AIDifficulty difficulty) => AIDifficultyLevel = difficulty;

    public void ToggleRoom(CardData room)
    {
        if (room == null) return;

        if (!disabledRooms.Add(room))
        {
            disabledRooms.Remove(room);
        }
    }

    public bool IsRoomDisabled(CardData room) => room != null && disabledRooms.Contains(room);

    public int ActiveRoomCount(List<CardData> allRooms) => 
        allRooms.Count(r => !IsRoomDisabled(r));
}