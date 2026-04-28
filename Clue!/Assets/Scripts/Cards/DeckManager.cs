using System.Collections.Generic;
using UnityEngine;

// manages the poke deck murder env, and card assign
public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance { get; private set; }

    [SerializeField] private List<CardData> allSuspects;
    [SerializeField] private List<CardData> allWeapons;
    [SerializeField] private List<CardData> allRooms;

    // for bot filled by the game deck
    public List<CardData> AllSuspects    => allSuspects;
    public List<CardData> AllWeapons     => allWeapons;
    public List<CardData> AllActiveRooms { get; private set; } = new List<CardData>();

    private CardData _murderer;
    private CardData _murderWeapon;
    private CardData _murderRoom;

    public CardData Murderer => _murderer;
    public CardData MurderWeapon => _murderWeapon;
    public CardData MurderRoom => _murderRoom;

    public List<CardData> ShuffledDeck { get; private set; } = new List<CardData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ref by gameStarter after rooms are set.
    public void SetupGameDeck()
    {
        ShuffledDeck.Clear();

        // skips the rooms that are turned off on the start screen
        List<CardData> activeRooms = new List<CardData>(allRooms);
        if (GameSettings.Instance != null)
            activeRooms.RemoveAll(r => GameSettings.Instance.IsRoomDisabled(r));

        AllActiveRooms = new List<CardData>(activeRooms);

        if (allSuspects.Count == 0 || allWeapons.Count == 0 || activeRooms.Count == 0)
        {
            Debug.LogWarning("DeckManager: Card lists are empty (or all rooms removed) Cannot load the deck.");
            return;
        }

        // one card of each type for the murder env
        _murderer     = allSuspects[Random.Range(0, allSuspects.Count)];
        _murderWeapon = allWeapons[Random.Range(0, allWeapons.Count)];
        _murderRoom   = activeRooms[Random.Range(0, activeRooms.Count)];

        //  all into the deck
        ShuffledDeck.AddRange(allSuspects);
        ShuffledDeck.AddRange(allWeapons);
        ShuffledDeck.AddRange(activeRooms);

        ShuffledDeck.Remove(_murderer);
        ShuffledDeck.Remove(_murderWeapon);
        ShuffledDeck.Remove(_murderRoom);

        FisherYatesShuffle(ShuffledDeck);

        Debug.Log($"DeckManager: Envelope sealed — {_murderer.CardName}, {_murderWeapon.CardName}, {_murderRoom.CardName}.");
    }

    // shuffle
    private void FisherYatesShuffle(List<CardData> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            CardData value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // deals the shuffled cards evenly for all players and bots 
    public void DealCards()
    {
        if (TurnManager.Instance == null || TurnManager.Instance.GetPlayers().Count == 0)
        {
            Debug.LogWarning("DeckManager: Cannot deal cards — no players registered.");
            return;
        }

        List<PlayerController> players = TurnManager.Instance.GetPlayers();
        int playerIndex = 0;

        foreach (CardData card in ShuffledDeck)
        {
            PlayerHand hand = players[playerIndex].GetComponent<PlayerHand>();
            if (hand != null) hand.AddCard(card);
            playerIndex = (playerIndex + 1) % players.Count;
        }

        Debug.Log("DeckManager: All cards dealt.");
    }
}