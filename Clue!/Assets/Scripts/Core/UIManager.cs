using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Manages all in-game UI: HUD, popups, event log, panels.
// Wires Inspector-assigned UI elements to game logic via singleton calls.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ── HUD ──────────────────────────────────────────────────────────────
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private Button rollDiceButton;
    [SerializeField] private Button endTurnButton;

    // ── Action panel ─────────────────────────────────────────────────────
    [Header("Action Panel")]
    [SerializeField] private Button suggestButton;
    [SerializeField] private Button accuseButton;

    // ── Suggestion panel ─────────────────────────────────────────────────
    [Header("Suggestion Panel")]
    [SerializeField] private GameObject suggestionPanel;
    [SerializeField] private TMP_Dropdown suspectDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;
    [SerializeField] private TextMeshProUGUI roomLabel;
    [SerializeField] private Button confirmSuggestionButton;
    [SerializeField] private Button cancelSuggestionButton;

    // ── Accusation panel ─────────────────────────────────────────────────
    [Header("Accusation Panel")]
    [SerializeField] private GameObject accusationPanel;
    [SerializeField] private TMP_Dropdown accuseSuspectDropdown;
    [SerializeField] private TMP_Dropdown accuseWeaponDropdown;
    [SerializeField] private TMP_Dropdown accuseRoomDropdown;
    [SerializeField] private Button confirmAccusationButton;
    [SerializeField] private Button cancelAccusationButton;

    // ── Card reveal panel ────────────────────────────────────────────────
    [Header("Card Reveal Panel")]
    [SerializeField] private GameObject cardRevealPanel;
    [SerializeField] private TextMeshProUGUI cardRevealText;
    [SerializeField] private Image cardRevealImage;
    [SerializeField] private Button cardRevealOKButton;

    // ── Game over panel ──────────────────────────────────────────────────
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button replayButton;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerTurnChanged -= HandleTurnChanged;
    }
    

    private void Start()
    {
        if (rollDiceButton != null)              rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        if (endTurnButton != null)               endTurnButton.onClick.AddListener(OnEndTurnClicked);
        if (suggestButton != null)               suggestButton.onClick.AddListener(OnSuggestClicked);
        if (accuseButton != null)                accuseButton.onClick.AddListener(OnAccuseClicked);
        if (confirmSuggestionButton != null)     confirmSuggestionButton.onClick.AddListener(OnConfirmSuggestion);
        if (cancelSuggestionButton != null)      cancelSuggestionButton.onClick.AddListener(OnCancelSuggestion);
        if (confirmAccusationButton != null)     confirmAccusationButton.onClick.AddListener(OnConfirmAccusation);
        if (cancelAccusationButton != null)      cancelAccusationButton.onClick.AddListener(OnCancelAccusation);
        if (cardRevealOKButton != null)          cardRevealOKButton.onClick.AddListener(OnCardRevealOK);
        if (replayButton != null)                replayButton.onClick.AddListener(OnReplayClicked);

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerTurnChanged += HandleTurnChanged;

        SetSuggestionPanel(false);
        SetAccusationPanel(false);
        SetCardRevealPanel(false);
        SetGameOverPanel(false);

        // Initialize display with whatever state the game is already in
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != null)
            HandleTurnChanged(TurnManager.Instance.CurrentPlayer);

        if (GameManager.Instance != null)
            UpdateButtonStates(GameManager.Instance.CurrentState);
        else
            UpdateButtonStates(GameManager.GameState.Setup);
    }

    public void ShowCardReveal(string showerName, CardData card)
    {
        if (cardRevealText != null)
            cardRevealText.text = $"{showerName} showed you:\n{card.CardName}";
        if (cardRevealImage != null && card.CardImage != null)
            cardRevealImage.sprite = card.CardImage;

        // Auto-mark card as seen in detective notepad
        if (DetectiveNotepad.Instance != null)
            DetectiveNotepad.Instance.AutoMarkCard(card.CardName);

        SetCardRevealPanel(true);
    }
    public void ShowGameOver(string winnerName)
    {
        if (gameOverText != null)
        {
            gameOverText.text = string.IsNullOrEmpty(winnerName)
                ? "Game Over\nThe murderer got away!"
                : $"Game Over\n{winnerName} wins!";
        }
        SetGameOverPanel(true);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        UpdateButtonStates(state);

        if (state == GameManager.GameState.GameOver)
            ShowGameOver(null);
    }

    private void HandleTurnChanged(PlayerController newPlayer)
    {
        if (currentPlayerText != null)
            currentPlayerText.text = $"Current Player: {newPlayer.Character}";
    }

    private void UpdateButtonStates(GameManager.GameState state)
    {
        bool isHumanTurn = TurnManager.Instance?.CurrentPlayer != null
                        && TurnManager.Instance.CurrentPlayer.IsHuman;

        if (rollDiceButton != null) rollDiceButton.interactable =
            isHumanTurn && state == GameManager.GameState.WaitingForRoll;

        if (suggestButton != null) suggestButton.interactable =
            isHumanTurn && state == GameManager.GameState.Suggesting;

        if (accuseButton != null) accuseButton.interactable =
            isHumanTurn && state != GameManager.GameState.Setup
                        && state != GameManager.GameState.GameOver;

        if (endTurnButton != null) endTurnButton.interactable =
            isHumanTurn && state == GameManager.GameState.Suggesting;
    }

    private void OnRollDiceClicked()
    {
        if (DiceRoller.Instance != null)
            DiceRoller.Instance.RollDice();
    }

    private void OnEndTurnClicked()
    {
        GameManager.Instance.ChangeState(GameManager.GameState.EndTurn);
    }

    private void OnSuggestClicked()
    {
        PopulateDropdown(suspectDropdown, DeckManager.Instance.AllSuspects);
        PopulateDropdown(weaponDropdown, DeckManager.Instance.AllWeapons);

        PlayerController current = TurnManager.Instance.CurrentPlayer;
        CardData currentRoom = current?.CurrentTile?.RoomData;
        if (roomLabel != null)
            roomLabel.text = currentRoom != null ? $"in {currentRoom.CardName}" : "in [no room]";

        SetSuggestionPanel(true);
    }

    private void OnAccuseClicked()
    {
        PopulateDropdown(accuseSuspectDropdown, DeckManager.Instance.AllSuspects);
        PopulateDropdown(accuseWeaponDropdown, DeckManager.Instance.AllWeapons);
        PopulateDropdown(accuseRoomDropdown, DeckManager.Instance.AllActiveRooms);
        SetAccusationPanel(true);
    }

    private void OnConfirmSuggestion()
    {
        CardData suspect = DeckManager.Instance.AllSuspects[suspectDropdown.value];
        CardData weapon  = DeckManager.Instance.AllWeapons[weaponDropdown.value];
        SetSuggestionPanel(false);
        GameManager.Instance.HumanSuggestion(suspect,weapon);
    }

    private void OnCancelSuggestion()
    {
        SetSuggestionPanel(false);
    }

    private void OnConfirmAccusation()
    {
        CardData suspect = DeckManager.Instance.AllSuspects[accuseSuspectDropdown.value];
        CardData weapon  = DeckManager.Instance.AllWeapons[accuseWeaponDropdown.value];
        CardData room    = DeckManager.Instance.AllActiveRooms[accuseRoomDropdown.value];
        SetAccusationPanel(false);
        GameManager.Instance.HumanAccusation(suspect, weapon, room);
    }

    private void OnCancelAccusation()
    {
        SetAccusationPanel(false);
    }

    private void OnCardRevealOK()
    {
        SetCardRevealPanel(false);
        GameManager.Instance.ChangeState(GameManager.GameState.EndTurn); 
    }

    private void OnReplayClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void PopulateDropdown(TMP_Dropdown dropdown, List<CardData> cards)
    {
        if (dropdown == null) return;
        dropdown.ClearOptions();
        List<string> names = new List<string>();
        foreach (CardData c in cards) names.Add(c.CardName);
        dropdown.AddOptions(names);
    }

    private void SetSuggestionPanel(bool active)
    {
        if (suggestionPanel != null) suggestionPanel.SetActive(active);
    }

    private void SetAccusationPanel(bool active)
    {
        if (accusationPanel != null) accusationPanel.SetActive(active);
    }

    private void SetCardRevealPanel(bool active)
    {
        if (cardRevealPanel != null) cardRevealPanel.SetActive(active);
    }

    private void SetGameOverPanel(bool active)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(active);
    }
}