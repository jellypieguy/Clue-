using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach to the notepad panel GameObject.
// Inspector needs: three tab Buttons, and a Content RectTransform inside a ScrollRect.
public class DetectiveNotepad : MonoBehaviour
{
    public static DetectiveNotepad Instance { get; private set; }

    [Header("Tabs")]
    [SerializeField] private Button suspectsTabButton;
    [SerializeField] private Button weaponsTabButton;
    [SerializeField] private Button roomsTabButton;

    [Header("Content")]
    [SerializeField] private Transform contentArea; // ScrollRect → Viewport → Content

    // ── Internal ─────────────────────────────────────────────────────────
    private enum Tab { Suspects, Weapons, Rooms }
    private Tab _currentTab = Tab.Suspects;

    // 0 = blank, 1 = ✓ (safe/seen), 2 = ✗ (eliminated)
    private readonly Dictionary<string, int> _cardStatus = new();

    private List<CardData> _suspects;
    private List<CardData> _weapons;
    private List<CardData> _rooms;

    private static readonly Color ActiveTab   = new Color(0.95f, 0.88f, 0.65f); // parchment
    private static readonly Color InactiveTab = new Color(0.65f, 0.60f, 0.50f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (DeckManager.Instance == null) { Debug.LogError("[DetectiveNotepad] DeckManager missing."); return; }

        _suspects = DeckManager.Instance.AllSuspects;
        _weapons  = DeckManager.Instance.AllWeapons;
        _rooms    = DeckManager.Instance.AllActiveRooms;

        if (_suspects == null || _weapons == null || _rooms == null)
        { Debug.LogError("[DetectiveNotepad] DeckManager lists are null — has SetupGameDeck run yet?"); return; }

        foreach (var c in _suspects) _cardStatus[c.CardName] = 0;
        foreach (var c in _weapons)  _cardStatus[c.CardName] = 0;
        foreach (var c in _rooms)    _cardStatus[c.CardName] = 0;

        if (suspectsTabButton == null) { Debug.LogError("[DetectiveNotepad] suspectsTabButton not wired."); return; }
        if (weaponsTabButton == null)  { Debug.LogError("[DetectiveNotepad] weaponsTabButton not wired."); return; }
        if (roomsTabButton == null)    { Debug.LogError("[DetectiveNotepad] roomsTabButton not wired."); return; }

        suspectsTabButton.onClick.AddListener(() => SwitchTab(Tab.Suspects));
        weaponsTabButton .onClick.AddListener(() => SwitchTab(Tab.Weapons));
        roomsTabButton   .onClick.AddListener(() => SwitchTab(Tab.Rooms));

        SwitchTab(Tab.Suspects);
    }

    // Called automatically by UIManager when an opponent shows the human a card
    public void AutoMarkCard(string cardName)
    {
        if (!_cardStatus.ContainsKey(cardName)) return;
        _cardStatus[cardName] = 1; // mark as seen (✓)
        RefreshCurrentTab();
    }

    // ── Tab switching ─────────────────────────────────────────────────────
    private void SwitchTab(Tab tab)
    {
        _currentTab = tab;

        SetTabColor(suspectsTabButton, tab == Tab.Suspects);
        SetTabColor(weaponsTabButton,  tab == Tab.Weapons);
        SetTabColor(roomsTabButton,    tab == Tab.Rooms);

        RefreshCurrentTab();
    }

    private void SetTabColor(Button btn, bool active)
    {
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? ActiveTab : InactiveTab;
    }

    // ── Build rows ────────────────────────────────────────────────────────
    private void RefreshCurrentTab()
    {
        List<CardData> cards = _currentTab switch
        {
            Tab.Weapons => _weapons,
            Tab.Rooms   => _rooms,
            _           => _suspects
        };

        foreach (Transform child in contentArea)
            Destroy(child.gameObject);

        foreach (CardData card in cards)
            BuildRow(card.CardName);
    }

    private void BuildRow(string cardName)
    {
        // ── Row container ────────────────────────────────────────────────
        var row = new GameObject(cardName + "_Row", typeof(RectTransform));
        row.transform.SetParent(contentArea, false);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing             = 6;
        hlg.padding             = new RectOffset(6, 6, 2, 2);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment      = TextAnchor.MiddleLeft;

        var rowLE = row.AddComponent<LayoutElement>();
        rowLE.minHeight = 32;
        rowLE.preferredHeight = 32;

        // ── Card name label ───────────────────────────────────────────────
        var labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(row.transform, false);

        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text      = cardName;
        label.fontSize  = 13;
        label.color     = Color.black;
        label.alignment = TextAlignmentOptions.MidlineLeft;

        var labelLE = labelObj.AddComponent<LayoutElement>();
        labelLE.flexibleWidth  = 1;
        labelLE.minHeight      = 28;

        // ── Status toggle button ──────────────────────────────────────────
        var btnObj = new GameObject("StatusBtn", typeof(RectTransform));
        btnObj.transform.SetParent(row.transform, false);

        var btnImage  = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.85f, 0.82f, 0.75f);

        var btn = btnObj.AddComponent<Button>();

        var btnLE = btnObj.AddComponent<LayoutElement>();
        btnLE.minWidth       = 34;
        btnLE.preferredWidth = 34;
        btnLE.minHeight      = 28;

        // Button label child
        var btnTextObj = new GameObject("Text", typeof(RectTransform));
        btnTextObj.transform.SetParent(btnObj.transform, false);

        var rt = btnTextObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var btnTMP = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.fontSize  = 18;
        btnTMP.richText  = true;

        UpdateButtonDisplay(btnTMP, _cardStatus[cardName]);

        // Click cycles: blank → ✓ → ✗ → blank
        string captured    = cardName;
        var    capturedTMP = btnTMP;
        btn.onClick.AddListener(() =>
        {
            _cardStatus[captured] = (_cardStatus[captured] + 1) % 3;
            UpdateButtonDisplay(capturedTMP, _cardStatus[captured]);
        });
    }

    private static void UpdateButtonDisplay(TextMeshProUGUI tmp, int status)
    {
        tmp.text = status switch
        {
            1 => "<color=#2a7a2a>✓</color>",
            2 => "<color=#aa2222>✗</color>",
            _ => ""
        };
    }
}