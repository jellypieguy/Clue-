using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ── Enums & Data ─────────────────────────────────────────────────────────────

public enum ClueState { None, No, Maybe, Yes }

[Serializable]
public class ClueEntry
{
    public string   name;
    public ClueState state;
    public bool     isInHand;   // auto-marked green if human holds this card

    public ClueEntry(string n) { name = n; state = ClueState.None; isInHand = false; }
}

[Serializable]
public class NotepadSaveData
{
    public List<ClueEntry> suspects = new();
    public List<ClueEntry> weapons  = new();
    public List<ClueEntry> rooms    = new();
}

// ── DetectiveNotepad ─────────────────────────────────────────────────────────

public class DetectiveNotepad : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static DetectiveNotepad Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
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

    // ── Static card lists (must match your CardData names exactly) ───────────
    static readonly string[] Suspects = {
        "Miss Scarlett", "Col. Mustard", "Mrs. White",
        "Rev. Green", "Mrs. Peacock", "Prof. Plum"
    };
    static readonly string[] Weapons = {
        "Candlestick", "Knife", "Lead Pipe",
        "Revolver", "Rope", "Wrench"
    };
    static readonly string[] Rooms = {
        "Ballroom", "Billiard Room", "Conservatory",
        "Dining Room", "Hall", "Kitchen",
        "Library", "Lounge", "Study"
    };

    const string SAVE_KEY = "CluedoNotepad_v1";

    // ── Runtime ──────────────────────────────────────────────────────────────
    NotepadSaveData data = new();
    readonly Dictionary<string, CluedoRow> rowLookup = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────
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

    // ── Called by GameManager.AutoMarkHumanHand() ─────────────────────────────
    /// <summary>
    /// Marks a card the human player holds as confirmed (Yes) and flags it
    /// so it can never be accidentally cleared by the player.
    /// </summary>
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

    // ── Build UI ──────────────────────────────────────────────────────────────
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

    // ── Persistence ───────────────────────────────────────────────────────────
    void Save()
    {
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data, true));
        PlayerPrefs.Save();
        ShowToast("Saved!");
    }

    void Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;
        try   { data = JsonUtility.FromJson<NotepadSaveData>(PlayerPrefs.GetString(SAVE_KEY)); }
        catch { data = new NotepadSaveData(); }
    }

    void ClearAll()
    {
        foreach (var e in data.suspects) if (!e.isInHand) e.state = ClueState.None;
        foreach (var e in data.weapons)  if (!e.isInHand) e.state = ClueState.None;
        foreach (var e in data.rooms)    if (!e.isInHand) e.state = ClueState.None;

        foreach (var row in rowLookup.Values) row.Refresh();
        Save();
        ShowToast("Cleared!");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    ClueEntry FindEntry(string cardName)
    {
        ClueEntry e;
        e = data.suspects.Find(x => x.name == cardName); if (e != null) return e;
        e = data.weapons .Find(x => x.name == cardName); if (e != null) return e;
        e = data.rooms   .Find(x => x.name == cardName); if (e != null) return e;
        return null;
    }

    void OnRowChanged() { /* optional: auto-save on every tap */ }

    void TogglePanel()
    {
        if (notepadPanel) notepadPanel.SetActive(!notepadPanel.activeSelf);
    }

    // ── Toast ─────────────────────────────────────────────────────────────────
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

// ── CluedoRow ─────────────────────────────────────────────────────────────────
// Attach to your rowPrefab.
// Prefab needs: TMP_Text labelText, Button noButton, Button maybeButton, Button yesButton.

public class CluedoRow : MonoBehaviour
{
    [Header("Row references")]
    public TMP_Text labelText;
    public Button   noButton;
    public Button   maybeButton;
    public Button   yesButton;

    // Tint colours
    static readonly Color ColNo       = new Color(0.89f, 0.29f, 0.29f, 1f);
    static readonly Color ColMaybe    = new Color(0.22f, 0.54f, 0.87f, 1f);
    static readonly Color ColYes      = new Color(0.39f, 0.60f, 0.13f, 1f);
    static readonly Color ColInactive = new Color(0.82f, 0.82f, 0.82f, 1f);
    static readonly Color ColLocked   = new Color(0.25f, 0.72f, 0.43f, 1f); // darker green for hand cards

    ClueEntry entry;
    Action    onChange;

    public void Init(ClueEntry e, Action onChanged)
    {
        entry    = e;
        onChange = onChanged;

        labelText.text = e.name;

        noButton   .onClick.AddListener(() => Toggle(ClueState.No));
        maybeButton.onClick.AddListener(() => Toggle(ClueState.Maybe));
        yesButton  .onClick.AddListener(() => Toggle(ClueState.Yes));

        Refresh();
    }

    void Toggle(ClueState clicked)
    {
        if (entry.isInHand) return;  // locked — human holds this card

        entry.state = (entry.state == clicked) ? ClueState.None : clicked;
        Refresh();
        onChange?.Invoke();
    }

    public void Refresh()
    {
        if (entry.isInHand)
        {
            // All three buttons tinted locked-green, buttons non-interactive
            SetColor(noButton,    ColLocked);
            SetColor(maybeButton, ColLocked);
            SetColor(yesButton,   ColLocked);
            noButton   .interactable = false;
            maybeButton.interactable = false;
            yesButton  .interactable = false;

            // Add "(in hand)" suffix once
            if (!labelText.text.EndsWith(" ✓"))
                labelText.text += " ✓";
            return;
        }

        SetColor(noButton,    entry.state == ClueState.No    ? ColNo    : ColInactive);
        SetColor(maybeButton, entry.state == ClueState.Maybe ? ColMaybe : ColInactive);
        SetColor(yesButton,   entry.state == ClueState.Yes   ? ColYes   : ColInactive);
    }

    void SetColor(Button btn, Color c)
    {
        var cb = btn.colors;
        cb.normalColor      = c;
        cb.highlightedColor = c * 1.1f;
        cb.selectedColor    = c;
        btn.colors = cb;
    }
}