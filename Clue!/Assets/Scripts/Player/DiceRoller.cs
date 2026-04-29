using UnityEngine;
using TMPro;
using System;
using System.Collections;

// rolls two d6 with short tumble animation
public class DiceRoller : MonoBehaviour
{
    public static DiceRoller Instance { get; private set; }
    public int CurrentRoll { get; private set; }

    public event Action<int> OnDiceRolled;

    [Tooltip("dice panel text — shows face during the animation.")]
    [SerializeField] private TextMeshProUGUI _diceDisplay;

    [Tooltip("How long the roll lasts before the result (seconds).")]
    [SerializeField] private float _rollDuration = 1.2f;

    // blocks double roll 
    private bool _rolling = false;
    private bool _isAITriggered = false; // Track if AI triggered the roll

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        Instance = this;
    }

    // Method for HUMAN players to roll dice (called by UI button)
    public void RollDiceForHuman()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("DiceRoller: GameManager missing.");
            return;
        }

        if (GameManager.Instance.CurrentState != GameManager.GameState.WaitingForRoll)
        {
            Debug.Log($"DiceRoller: Cannot roll - current state is {GameManager.Instance.CurrentState}, expected WaitingForRoll.");
            return;
        }

        if (_rolling)
        {
            Debug.Log("DiceRoller: Already rolling, please wait.");
            return;
        }

        // Check if it's a human player's turn
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != null)
        {
            if (!TurnManager.Instance.CurrentPlayer.IsHuman)
            {
                Debug.Log("DiceRoller: It's AI's turn, human cannot roll.");
                return;
            }
        }

        _isAITriggered = false;
        int d1 = UnityEngine.Random.Range(1, 7);
        int d2 = UnityEngine.Random.Range(1, 7);
        int total = d1 + d2;

        Debug.Log($"DiceRoller: Human rolled {d1} + {d2} = {total}.");
        
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != null)
        {
            Debug.Log($"{TurnManager.Instance.CurrentPlayer.Character} rolled {total}.");
        }
        
        StartCoroutine(AnimateRoll(d1, d2, total));
    }

    // Method for AI players to roll dice automatically
    public int RollDiceForAI()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("DiceRoller: GameManager missing.");
            return 0;
        }

        if (GameManager.Instance.CurrentState != GameManager.GameState.WaitingForRoll)
        {
            Debug.Log($"DiceRoller: Cannot roll - current state is {GameManager.Instance.CurrentState}");
            return 0;
        }

        if (_rolling)
        {
            Debug.Log("DiceRoller: Already rolling, please wait.");
            return 0;
        }

        // Check if it's an AI player's turn
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != null)
        {
            if (TurnManager.Instance.CurrentPlayer.IsHuman)
            {
                Debug.Log("DiceRoller: It's human's turn, AI cannot roll.");
                return 0;
            }
        }

        _isAITriggered = true;
        int d1 = UnityEngine.Random.Range(1, 7);
        int d2 = UnityEngine.Random.Range(1, 7);
        int total = d1 + d2;

        Debug.Log($"DiceRoller: AI rolled {d1} + {d2} = {total}.");
        
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != null)
        {
            Debug.Log($"{TurnManager.Instance.CurrentPlayer.Character} rolled {total}.");
        }
        
        StartCoroutine(AnimateRoll(d1, d2, total));
        
        return total;
    }

    // Legacy method - kept for compatibility but marked obsolete
    [Obsolete("Use RollDiceForHuman() or RollDiceForAI() instead")]
    public int RollDice()
    {
        Debug.LogWarning("RollDice() is deprecated. Use RollDiceForHuman() or RollDiceForAI() instead.");
        
        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != null)
        {
            if (TurnManager.Instance.CurrentPlayer.IsHuman)
                RollDiceForHuman();
            else
                return RollDiceForAI();
        }
        
        return 0;
    }

    // Roll through random values, then locks to result
    private IEnumerator AnimateRoll(int finalD1, int finalD2, int total)
    {
        _rolling = true;

        float elapsed = 0f;
        float interval = 0.06f; // start fast

        while (elapsed < _rollDuration)
        {
            ShowFaces(UnityEngine.Random.Range(1, 7), UnityEngine.Random.Range(1, 7));
            yield return new WaitForSeconds(interval);
            elapsed += interval;
            // frames get longer for natural slowing effect
            interval = Mathf.Min(interval + 0.012f, 0.22f);
        }

        // Show final dice values
        ShowFaces(finalD1, finalD2);
        
        // Store result for other systems
        CurrentRoll = total;

        _rolling = false;

        // Trigger the event
        OnDiceRolled?.Invoke(total);
        
        // Only change game state if this wasn't triggered by AI
        // (AI state change is handled by TurnManager)
        if (!_isAITriggered && GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Moving);
        }
    }

    // Changes the text display
    private void ShowFaces(int d1, int d2)
    {
        if (_diceDisplay != null)
        {
            _diceDisplay.text = $"[ {d1} ]  [ {d2} ]";
        }
    }

    // Helper method to check if dice is currently rolling
    public bool IsRolling()
    {
        return _rolling;
    }
}