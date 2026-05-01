using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ClueState { None, No, Maybe, Yes }

[Serializable]
public class ClueEntry
{
    public string    name;
    public ClueState state;
    public bool      isInHand;

    public ClueEntry(string n) { name = n; state = ClueState.None; isInHand = false; }
}

[Serializable]
public class NotepadSaveData
{
    public List<ClueEntry> suspects = new();
    public List<ClueEntry> weapons  = new();
    public List<ClueEntry> rooms    = new();
}

public class DetectiveNotepad : MonoBehaviour
{
    public static DetectiveNotepad Instance { get; private set; }

    [Header("Section container parents (Vertical Layout Groups)")]
    [SerializeField] private Transform suspectsContainer;
    [SerializeField] private Transform weaponsContainer;
    [SerializeField] private Transform roomsContainer;

    [Header("Row prefab")]
    [SerializeField] private GameObject rowPrefab;

    [Header("Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button togglePanelButton;

    [Header("Panel root (toggled open/closed)")]
    [SerializeField] private GameObject notepadPanel;

    [Header("Toast")]
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TMP_Text   toastText;

    static readonly string[] Suspects = {
        "Miss Scarlet", "Colonel Mustard", "Mrs. White",
        "Mr Green", "Mrs. Peacock", "Professor Plum"
    };
    static readonly string[] Weapons = {
        "Candlestick", "Dagger", "Lead Pipe",
        "Revolver", "Rope", "Spanner"
    };
    static readonly string[] Rooms = {
        "Ballroom", "Billiard Room", "Conservatory",
        "Dining Room", "Hall", "Kitchen",
        "Library", "Lounge", "Study"
    };

    const string SAVE_KEY = "CluedoNotepad_v1";

    NotepadSaveData data = new();
    readonly Dictionary<string, CluedoRow> rowLookup = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        Load();

        BuildSection(suspectsContainer, Suspects, data.suspects);
        BuildSection(weaponsContainer,  Weapons,  data.weapons);
        BuildSection(roomsContainer,    Rooms,    data.rooms);

        saveButton?.onClick.AddListener(Save);
        clearButton?.onClick.AddListener(ClearAll);
        togglePanelButton?.onClick.AddListener(TogglePanel);

        if (toastPanel) toastPanel.SetActive(false);
        if (notepadPanel) notepadPanel.SetActive(false);
    }

    // Called by GameManager.AutoMarkHumanHand()
    public void AutoMarkCard(string cardName)
    {
        ClueEntry entry = FindEntry(cardName);
        if (entry == null)
        {
            Debug.LogWarning($"[DetectiveNotepad] Card not found: {cardName}");
            return;
        }

        entry.state    = ClueState.Yes;
        entry.isInHand = true;

        if (rowLookup.TryGetValue(cardName, out CluedoRow row))
            row.Refresh();
    }

    void BuildSection(Transform container, string[] names, List<ClueEntry> entries)
    {
        foreach (string n in names)
            if (!entries.Exists(e => e.name == n))
                entries.Add(new ClueEntry(n));

        foreach (ClueEntry entry in entries)
        {
            GameObject go  = Instantiate(rowPrefab, container);
            CluedoRow  row = go.GetComponent<CluedoRow>();
            row.Init(entry, OnRowChanged);
            rowLookup[entry.name] = row;
        }
    }

    void Save()
    {
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data, true));
        PlayerPrefs.Save();
        ShowToast("Saved!");
    }

    void Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
        try
        {
            data = JsonUtility.FromJson<NotepadSaveData>(PlayerPrefs.GetString(SAVE_KEY));
            // Reset isInHand flags — re-applied by AutoMarkHumanHand each run
            foreach (var e in data.suspects) e.isInHand = false;
            foreach (var e in data.weapons)  e.isInHand = false;
            foreach (var e in data.rooms)    e.isInHand = false;
        }
        catch { data = new NotepadSaveData(); }
    }

    void ClearAll()
    {
        foreach (var e in data.suspects) e.state = ClueState.None;
        foreach (var e in data.weapons)  e.state = ClueState.None;
        foreach (var e in data.rooms)    e.state = ClueState.None;

        foreach (var row in rowLookup.Values) row.Refresh();
        Save();
        ShowToast("Cleared!");
    }

    ClueEntry FindEntry(string cardName)
    {
        ClueEntry e;
        e = data.suspects.Find(x => x.name == cardName); if (e != null) return e;
        e = data.weapons .Find(x => x.name == cardName); if (e != null) return e;
        e = data.rooms   .Find(x => x.name == cardName); if (e != null) return e;
        return null;
    }

    void OnRowChanged() { }

    void TogglePanel()
    {
        if (notepadPanel) notepadPanel.SetActive(!notepadPanel.activeSelf);
    }

    Coroutine toastCoroutine;

    void ShowToast(string msg)
    {
        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastRoutine(msg));
    }

    IEnumerator ToastRoutine(string msg)
    {
        toastText.text = msg;
        toastPanel.SetActive(true);
        yield return new WaitForSeconds(1.8f);
        toastPanel.SetActive(false);
    }
}