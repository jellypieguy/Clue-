using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ClueState { None, No, Maybe, Yes }

[Serializable]
public class ClueEntry
{
    public string name;
    public ClueState state;
    public bool isConfirmedInHand;

    public ClueEntry(string name)
    {
        this.name = name;
        state = ClueState.None;
        isConfirmedInHand = false;
    }
}

[Serializable]
public class NotepadSaveData
{
    public List<ClueEntry> suspects = new();
    public List<ClueEntry> weapons = new();
    public List<ClueEntry> rooms = new();
}

public class NotepadUI : MonoBehaviour
{
    public static NotepadUI Instance { get; private set; }

    [SerializeField] private Transform suspectsContainer;
    [SerializeField] private Transform weaponsContainer;
    [SerializeField] private Transform roomsContainer;
    [SerializeField] private GameObject rowPrefab;

    [SerializeField] private Button saveButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button togglePanelButton;
    [SerializeField] private GameObject notepadPanel;

    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TMP_Text toastText;

    // Fallbacks only — real data comes from DeckManager at runtime.
    // If card names in the ScriptableObjects ever change, update these too (or just delete them if DeckManager is always present).
    private static readonly string[] FallbackSuspects = { "Miss Scarlet", "Colonel Mustard", "Mrs. White", "Mr Green", "Mrs. Peacock", "Professor Plum" };
    private static readonly string[] FallbackWeapons  = { "Candlestick", "Dagger", "Lead Pipe", "Revolver", "Rope", "Spanner" };
    private static readonly string[] FallbackRooms    = { "Ballroom", "Billiard Room", "Conservatory", "Dining Room", "Hall", "Kitchen", "Library", "Lounge", "Study" };

    private const string SaveKey = "CluedoNotepad_v1";

    private NotepadSaveData saveState = new();
    private readonly Dictionary<string, NotepadRow> activeRows = new();
    private Coroutine activeToastRoutine;
    private bool _populated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        LoadState();
        if (!_populated) PopulateNotepad();

        saveButton?.onClick.AddListener(SaveState);
        clearButton?.onClick.AddListener(ResetBoard);
        togglePanelButton?.onClick.AddListener(ToggleVisibility);

        toastPanel?.SetActive(false);
        notepadPanel?.SetActive(false);
    }

    private void PopulateNotepad()
    {
        string[] suspects, weapons, rooms;

        if (DeckManager.Instance != null)
        {
            var all = DeckManager.Instance.GetAllCardsOrdered();
            suspects = all.Where(c => c.Type == CardData.CardType.Suspect).Select(c => c.CardName).ToArray();
            weapons  = all.Where(c => c.Type == CardData.CardType.Weapon).Select(c => c.CardName).ToArray();
            rooms    = all.Where(c => c.Type == CardData.CardType.Room).Select(c => c.CardName).ToArray();
        }
        else
        {
            suspects = FallbackSuspects;
            weapons  = FallbackWeapons;
            rooms    = FallbackRooms;
        }

        PopulateSection(suspectsContainer, suspects, saveState.suspects);
        PopulateSection(weaponsContainer,  weapons,  saveState.weapons);
        PopulateSection(roomsContainer,    rooms,    saveState.rooms);
        _populated = true;
    }

    public void AutoMarkCard(string cardName)
    {
        var entry = GetEntryByName(cardName);
        if (entry == null)
        {
            Debug.LogWarning($"[NotepadUI] Missing card definition: {cardName}");
            return;
        }

        entry.state = ClueState.Yes;
        entry.isConfirmedInHand = true;

        if (activeRows.TryGetValue(cardName, out var rowInstance))
        {
            rowInstance.Refresh();
        }
    }

    private void PopulateSection(Transform container, string[] defaultNames, List<ClueEntry> stateList)
    {
        foreach (var itemName in defaultNames)
        {
            if (!stateList.Exists(entry => entry.name == itemName))
            {
                stateList.Add(new ClueEntry(itemName));
            }
        }

        foreach (var entry in stateList)
        {
            var rowObj = Instantiate(rowPrefab, container);
            var rowComponent = rowObj.GetComponent<NotepadRow>();

            rowComponent.Init(entry, OnRowInteracted);
            activeRows[entry.name] = rowComponent;
        }
    }

    private void SaveState()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveState, true));
        PlayerPrefs.Save();
        ShowToast("Saved!");
    }

    private void LoadState()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return;

        try
        {
            saveState = JsonUtility.FromJson<NotepadSaveData>(PlayerPrefs.GetString(SaveKey));

            // Re-lock hands correctly each session
            foreach (var e in saveState.suspects) e.isConfirmedInHand = false;
            foreach (var e in saveState.weapons) e.isConfirmedInHand = false;
            foreach (var e in saveState.rooms) e.isConfirmedInHand = false;
        }
        catch
        {
            saveState = new NotepadSaveData();
        }
    }

    private void ResetBoard()
    {
        Action<List<ClueEntry>> clearList = (list) =>
        {
            foreach (var entry in list.Where(e => !e.isConfirmedInHand))
            {
                entry.state = ClueState.None;
            }
        };

        clearList(saveState.suspects);
        clearList(saveState.weapons);
        clearList(saveState.rooms);

        foreach (var row in activeRows.Values)
        {
            row.Refresh();
        }

        SaveState();
        ShowToast("Cleared!");
    }

    public void ResetNotepad()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        saveState = new NotepadSaveData();

        foreach (Transform child in suspectsContainer) Destroy(child.gameObject);
        foreach (Transform child in weaponsContainer)  Destroy(child.gameObject);
        foreach (Transform child in roomsContainer)    Destroy(child.gameObject);
        activeRows.Clear();

        _populated = false;
        PopulateNotepad();
    }

    private ClueEntry GetEntryByName(string targetName)
    {
        return saveState.suspects.FirstOrDefault(x => x.name == targetName) ??
               saveState.weapons.FirstOrDefault(x => x.name == targetName) ??
               saveState.rooms.FirstOrDefault(x => x.name == targetName);
    }

    private void OnRowInteracted() { }

    private void ToggleVisibility()
    {
        if (notepadPanel)
        {
            notepadPanel.SetActive(!notepadPanel.activeSelf);
        }
    }

    private void ShowToast(string message)
    {
        if (activeToastRoutine != null) StopCoroutine(activeToastRoutine);
        activeToastRoutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        toastPanel.SetActive(true);
        yield return new WaitForSeconds(1.8f);
        toastPanel.SetActive(false);
    }
}