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

    // ── Event log ────────────────────────────────────────────────────────
    [Header("Event Log")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private ScrollRect logScrollRect;

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

    // ── Internal state ───────────────────────────────────────────────────
    private List<string> _logMessages = new List<string>();
    private const int MAX_LOG_LINES = 50;

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

        UpdateButtonStates(GameManager.GameState.Setup);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerTurnChanged -= HandleTurnChanged;
    }

    public void AddLogEvent(string message)
    {
        _logMessages.Add(message);
        if (_logMessages.Count > MAX_LOG_LINES)
            _logMessages.RemoveAt(0);

        if (logText != null)
            logText.text = string.Join("\n", _logMessages);

        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void ShowCardReveal(string showerName, CardData card)
    {
        if (cardRevealText != null)
            cardRevealText.text = $"{showerName} showed you:\n{card.CardName}";
        if (cardRevealImage != null && card.CardImage != null)
            cardRevealImage.sprite = card.CardImage;

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
            isHumanTurn && (state == GameManager.GameState.Suggesting
                         || state == GameManager.GameState.Accusing);

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

        PlayerCon