using UnityEngine;
using TMPro;
using System;
using System.Collections;

// olls two d6 add short tumble animation first
public class DiceRoller : MonoBehaviour
{
    public static DiceRoller Instance { get; private set; }

    public event Action<int> OnDiceRolled;

    [Tooltip("dice panel text  — shows face during the anime.")]
    [SerializeField] private TextMeshProUGUI _diceDisplay;

    [Tooltip("How long the roll lasts before the result(seconds).")]
    [SerializeField] private float _rollDuration = 1.2f;

    // blocks double roll 
    private bool _rolling = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RollDice()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("DiceRoller: GameManager missing.");
            return;
        }

        if (GameManager.Instance.CurrentState != GameManager.GameState.WaitingForRoll)
        {
            Debug.Log($"DiceRoller: Ignored state is {GameManager.Instance.CurrentState}.");
            return;
        }

        if (_rolling) return;

        int d1 = UnityEngine.Random.Range(1, 7);
        int d2 = UnityEngine.Random.Range(1, 7);
        int total = d1 + d2;

        Debug.Log($"DiceRoller: {d1} + {d2} = {total}.");

        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPlayer != null)
        {
            Debug.Log($"{TurnManager.Instance.CurrentPlayer.Character} rolled {total}.");
        }
        StartCoroutine(AnimateRoll(d1, d2, total));
    }

    // roll through random values, then locks to result
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
            // frames get longer.
            interval = Mathf.Min(interval + 0.012f, 0.22f);
        }

        // shows actual dice val
        ShowFaces(finalD1, finalD2);

        _rolling = false;

        OnDiceRolled?.Invoke(total);
        GameManager.Instance.ChangeState(GameManager.GameState.Moving);
    }

    // changes the text
    private void ShowFaces(int d1, int d2)
    {
        if (_diceDisplay != null)
            _diceDisplay.text = $"[ {d1} ]  [ {d2} ]";
    }
}
