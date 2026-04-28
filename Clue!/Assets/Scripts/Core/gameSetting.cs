using System.Collections.Generic;
using UnityEngine;

//  setup screen rooms, player count whem game starts.
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    // NoP and how many of those bots
    public int TotalPlayers     { get; private set; } = 6;
    public int HumanPlayerCount { get; private set; } = 1;

    private readonly HashSet<CardData> _disabledRooms = new HashSet<CardData>();

    public void SetTotalPlayers(int n)
    {
        TotalPlayers = Mathf.Clamp(n, 2, 6);
        // count cann't exceed the new total
        HumanPlayerCount = Mathf.Clamp(HumanPlayerCount, 1, TotalPlayers);
    }

    public void SetHumanCount(int n) => HumanPlayerCount = Mathf.Clamp(n, 1, TotalPlayers);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // toggle in/out room of
    public void ToggleRoom(CardData room)
    {
        if (room == null) return;

        if (_disabledRooms.Contains(room))
            _disabledRooms.Remove(room);
        else
            _disabledRooms.Add(room);
    }

    public bool IsRoomDisabled(CardData room)
    {
        return room != null && _disabledRooms.Contains(room);
    }

    // return num of  rooms from full list that are active
    // stop event of removing every room
    public int ActiveRoomCount(List<CardData> allRooms)
    {
        int count = 0;
        foreach (CardData r in allRooms)
        {
            if (!IsRoomDisabled(r)) count++;
        }
        return count;
    }
}