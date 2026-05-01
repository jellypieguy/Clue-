using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI currentPlayerText;
    [SerializeField] private Button rollDiceButton;
    [SerializeField] private Button endTurnButton;

    [Header("Action Panel")]
    [SerializeField] private Button suggestButton;
    [SerializeField] private Button accuseButton;

    [Header("Suggestion Panel")]
    [SerializeField] private GameObject suggestionPanel;
    [SerializeField] private TMP_Dropdown suspectDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;
    [SerializeField] private TextMeshProUGUI roomLabel;
    [SerializeField] private Button confirmSuggestionButton;
    [SerializeField] private Button cancelSuggestionButton;

    [Header("Accusation Panel")]
    [SerializeField] private GameObject accusationPanel;
    [SerializeField] private TMP_Dropdown accuseSuspectDropdown;
    [SerializeField] private TMP_Dropdown accuseWeaponDropdown;
    [SerializeField] private TMP_Dropdown accuseRoomDropdown;
    [SerializeField] private Button confirmAccusationButton;
    [SerializeField] private Button cancelAccusationButton;

    [Header("Card Reveal Panel")]
    [SerializeField] private GameObject cardRevealPanel;
    [SerializeField] private TextMeshProUGUI cardRevealText;
    [SerializeField] private Image cardRevealImage;
    [SerializeField] private Button cardRevealOKButton;

    [Header("Murder Reveal Panel")]
    [SerializeField] private GameObject murderRevealPanel;
    [SerializeField] private TextMeshProUGUI murdererText;
    [SerializeField] private TextMeshProUGUI weaponText;
    [SerializeField] private TextMeshProUGUI roomText;
    [SerializeField] private Button murderRevealContinueButton;

    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button replayButton;

    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Button resumeButton;

    [Header("Pass Device Panel")]
    [SerializeField] private GameObject passDevicePanel;
    [SerializeField] private TextMeshProUGUI passDeviceText;
    [SerializeField] private Button passDeviceContinueButton;

    [Header("Player Hand")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private GameObject cardPrefab;

    private bool isGamePaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
        BindUIEvents();

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerTurnChanged += HandleTurnChanged;

        TogglePanel(suggestionPanel, false);
        TogglePanel(accusationPanel, false);
        TogglePanel(cardRevealPanel, false);
        TogglePanel(murderRevealPanel, false);
        TogglePanel(gameOverPanel, false);
        TogglePanel(pausePanel, false);
        TogglePanel(passDevicePanel, false);

        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != null)
            HandleTurnChanged(TurnManager.Instance.CurrentPlayer);

        var startingState = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameManager.GameState.Setup;
        RefreshButtonStates(startingState);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePauseMenu();
    }

    private void BindUIEvents()
    {
        if (rollDiceButton != null) rollDiceButton.onClick.AddListener(OnRollDiceClicked);
        if (endTurnButton != null) endTurnButton.onClick.AddListener(OnEndTurnClicked);
        if (suggestButton != null) suggestButton.onClick.AddListener(OnSuggestClicked);
        if (accuseButton != null) accuseButton.onClick.AddListener(OnAccuseClicked);
        if (confirmSuggestionButton != null) confirmSuggestionButton.onClick.AddListener(OnConfirmSuggestion);
        if (cancelSuggestionButton != null) cancelSuggestionButton.onClick.AddListener(() => TogglePanel(suggestionPanel, false));
        if (confirmAccusationButton != null) confirmAccusationButton.onClick.AddListener(OnConfirmAccusation);
        if (cancelAccusationButton != null) cancelAccusationButton.onClick.AddListener(() => TogglePanel(accusationPanel, false));
        if (cardRevealOKButton != null) cardRevealOKButton.onClick.AddListener(OnCardRevealDismissed);
        if (murderRevealContinueButton != null) murderRevealContinueButton.onClick.AddListener(OnMurderRevealContinue);
        if (replayButton != null) replayButton.onClick.AddListener(OnReplayClicked);
        if (resumeButton != null) resumeButton.onClick.AddListener(TogglePauseMenu);
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(val => { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(val); });
        if (muteToggle != null) muteToggle.onValueChanged.AddListener(isMuted => { if (AudioManager.Instance != null) AudioManager.Instance.ToggleMusic(!isMuted); });
        if (passDeviceContinueButton != null) passDeviceContinueButton.onClick.AddListener(OnPassDeviceContinue);
    }

    public void TogglePauseMenu()
    {
        isGamePaused = !isGamePaused;
        TogglePanel(pausePanel, isGamePaused);
        Time.timeScale = isGamePaused ? 0f : 1f;
    }

    public void ShowCardReveal(string reportingPlayerName, CardData revealedCard)
    {
        if (cardRevealText != null)
            cardRevealText.text = $"{reportingPlayerName} showed you:\n{revealedCard.CardName}";

        if (cardRevealImage != null && revealedCard.CardImage != null)
            cardRevealImage.sprite = revealedCard.CardImage;

        if (NotepadUI.Instance != null)
            NotepadUI.Instance.AutoMarkCard(revealedCard.CardName);

        TogglePanel(cardRevealPanel, true);
    }

    public void ShowMurderReveal(string murderer, string weapon, string room)
    {
        if (murdererText != null) murdererText.text = $"Murderer: {murderer}";
        if (weaponText != null) weaponText.text = $"Weapon: {weapon}";
        if (roomText != null) roomText.text = $"Room: {room}";

        TogglePanel(murderRevealPanel, true);
    }

    public void ShowGameOver(string winnerName)
    {
        if (gameOverText != null)
        {
            gameOverText.text = string.IsNullOrEmpty(winnerName)
                ? "Game Over\nThe murderer got away!"
                : $"Game Over\n{winnerName} wins!";
        }
        TogglePanel(gameOverPanel, true);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        RefreshButtonStates(state);

        if (state == GameManager.GameState.PassingDevice)
        {
            if (passDeviceText != null && TurnManager.Instance?.CurrentPlayer != null)
                passDeviceText.text = $"{TurnManager.Instance.CurrentPlayer.Character}'s Turn!\nPass the device.";

            TogglePanel(passDevicePanel, true);
            if (handContainer != null) handContainer.gameObject.SetActive(false);
        }
        else
        {
            TogglePanel(passDevicePanel, false);

            var currentPlayer = TurnManager.Instance?.CurrentPlayer;
            if (currentPlayer != null && currentPlayer.IsHuman)
                UpdateHandDisplay(currentPlayer);
        }

        // GameOver is now handled via ShowMurderReveal -> OnMurderRevealContinue -> ShowGameOver
    }

    private void HandleTurnChanged(PlayerController activePlayer)
    {
        if (currentPlayerText != null)
            currentPlayerText.text = $"Current Player: {activePlayer.Character}";

        UpdateHandDisplay(activePlayer);
    }

    private void RefreshButtonStates(GameManager.GameState state)
    {
        bool isHumanTurn = TurnManager.Instance != null
                        && TurnManager.Instance.CurrentPlayer != null
                        && TurnManager.Instance.CurrentPlayer.IsHuman;

        if (rollDiceButton != null)
            rollDiceButton.interactable = isHumanTurn && state == GameManager.GameState.WaitingForRoll;

        if (suggestButton != null)
            suggestButton.interactable = isHumanTurn && state == GameManager.GameState.Suggesting;

        if (accuseButton != null)
            accuseButton.interactable = isHumanTurn && state != GameManager.GameState.Setup && state != GameManager.GameState.GameOver;

        if (endTurnButton != null)
            endTurnButton.interactable = isHumanTurn && state == GameManager.GameState.Suggesting;
    }

    private void OnRollDiceClicked()
    {
        if (DiceRoller.Instance != null) DiceRoller.Instance.RollDice();
    }

    private void OnEndTurnClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.EndTurn);
    }

    private void OnSuggestClicked()
    {
        if (DeckManager.Instance == null) return;

        PopulateDropdown(suspectDropdown, DeckManager.Instance.AllSuspects);
        PopulateDropdown(weaponDropdown, DeckManager.Instance.AllWeapons);

        var activePlayer = TurnManager.Instance?.CurrentPlayer;
        var currentRoom = activePlayer?.CurrentTile?.RoomData;

        if (roomLabel != null)
            roomLabel.text = currentRoom != null ? $"in {currentRoom.CardName}" : "in [no room]";

        TogglePanel(suggestionPanel, true);
    }

    private void OnAccuseClicked()
    {
        if (DeckManager.Instance == null) return;

        PopulateDropdown(accuseSuspectDropdown, DeckManager.Instance.AllSuspects);
        PopulateDropdown(accuseWeaponDropdown, DeckManager.Instance.AllWeapons);
        PopulateDropdown(accuseRoomDropdown, DeckManager.Instance.AllActiveRooms);

        TogglePanel(accusationPanel, true);
    }

    private void OnConfirmSuggestion()
    {
        if (DeckManager.Instance == null || GameManager.Instance == null) return;

        var targetSuspect = DeckManager.Instance.AllSuspects[suspectDropdown.value];
        var targetWeapon = DeckManager.Instance.AllWeapons[weaponDropdown.value];

        TogglePanel(suggestionPanel, false);
        GameManager.Instance.HumanSuggestion(targetSuspect, targetWeapon);
    }

    private void OnConfirmAccusation()
    {
        if (DeckManager.Instance == null || GameManager.Instance == null) return;

        var targetSuspect = DeckManager.Instance.AllSuspects[accuseSuspectDropdown.value];
        var targetWeapon = DeckManager.Instance.AllWeapons[accuseWeaponDropdown.value];
        var targetRoom = DeckManager.Instance.AllActiveRooms[accuseRoomDropdown.value];

        TogglePanel(accusationPanel, false);
        GameManager.Instance.HumanAccusation(targetSuspect, targetWeapon, targetRoom);
    }

    private void OnCardRevealDismissed()
    {
        TogglePanel(cardRevealPanel, false);
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.EndTurn);
    }

    private void OnMurderRevealContinue()
    {
        TogglePanel(murderRevealPanel, false);
        ShowGameOver(null);
    }

    private void OnReplayClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnPassDeviceContinue()
    {
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.WaitingForRoll);
    }

    private void PopulateDropdown(TMP_Dropdown dropdown, List<CardData> cards)
    {
        if (dropdown == null || cards == null) return;

        dropdown.ClearOptions();
        dropdown.AddOptions(cards.Select(c => c.CardName).ToList());
    }

    private void TogglePanel(GameObject panel, bool isVisible)
    {
        if (panel != null) panel.SetActive(isVisible);
    }

    private void UpdateHandDisplay(PlayerController player)
    {
        if (handContainer == null || cardPrefab == null) return;

        foreach (Transform child in handContainer)
            Destroy(child.gameObject);

        bool isPassingDevice = GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.PassingDevice;

        if (player == null || !player.IsHuman || isPassingDevice)
        {
            handContainer.gameObject.SetActive(false);
            return;
        }

        if (player.TryGetComponent<PlayerHand>(out var hand) && hand.Cards.Count > 0)
        {
            handContainer.gameObject.SetActive(true);
            foreach (var card in hand.Cards)
            {
                var cardObj = Instantiate(cardPrefab, handContainer);

                if (cardObj.TryGetComponent<TextMeshProUGUI>(out var labelText))
                    labelText.text = card.CardName;
                else
                {
                    var childText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
                    if (childText != null) childText.text = card.CardName;
                }

                if (cardObj.TryGetComponent<Image>(out var img) && card.CardImage != null)
                    img.sprite = card.CardImage;
            }
        }
        else
        {
            handContainer.gameObject.SetActive(false);
        }
    }
}