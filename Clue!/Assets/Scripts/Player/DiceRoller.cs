using UnityEngine;
using TMPro;
using System;
using System.Collections;

// Rolls two d6 — spins 3D dice during animation, then settles on result.
public class DiceRoller : MonoBehaviour
{
    public static DiceRoller Instance { get; private set; }

    public event Action<int> OnDiceRolled;

    [Header("UI")]
    [Tooltip("Text element that shows face values during the roll.")]
    [SerializeField] private TextMeshProUGUI _diceDisplay;

    [Tooltip("How long the roll animation lasts (seconds).")]
    [SerializeField] private float _rollDuration = 1.2f;

    [Header("3D Dice")]
    [Tooltip("Transform of the first 3D die in the scene.")]
    [SerializeField] private Transform _dice1;

    [Tooltip("Transform of the second 3D die in the scene.")]
    [SerializeField] private Transform _dice2;

    [Tooltip("Max spin speed in degrees/second at the start of the roll.")]
    [SerializeField] private float _spinSpeed = 720f;

    // blocks double roll
    private bool _rolling = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SetDiceVisible(false);
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
            Debug.Log($"DiceRoller: Ignored — state is {GameManager.Instance.CurrentState}.");
            return;
        }

        if (_rolling) return;

        int d1 = UnityEngine.Random.Range(1, 7);
        int d2 = UnityEngine.Random.Range(1, 7);
        int total = d1 + d2;

        Debug.Log($"DiceRoller: {d1} + {d2} = {total}.");

        if (TurnManager.Instance?.CurrentPlayer != null)
            Debug.Log($"{TurnManager.Instance.CurrentPlayer.Character} rolled {total}.");

        StartCoroutine(AnimateRoll(d1, d2, total));
    }

    private IEnumerator AnimateRoll(int finalD1, int finalD2, int total)
    {
        _rolling = true;
        SetDiceVisible(true);

        float elapsed   = 0f;
        float textTimer = 0f;
        float textInterval = 0.06f; // starts fast, slows down

        while (elapsed < _rollDuration)
        {
            float dt = Time.deltaTime;
            elapsed  += dt;
            textTimer += dt;

            // spin decelerates toward the end
            float t     = elapsed / _rollDuration;
            float speed = Mathf.Lerp(_spinSpeed, 60f, t);

            if (_dice1 != null)
                _dice1.Rotate(speed * dt, speed * 0.7f * dt, speed * 0.4f * dt, Space.Self);
            if (_dice2 != null)
                _dice2.Rotate(speed * 0.9f * dt, speed * dt, speed * 0.5f * dt, Space.Self);

            // shuffle the text display at increasing intervals
            if (textTimer >= textInterval)
            {
                ShowFaces(UnityEngine.Random.Range(1, 7), UnityEngine.Random.Range(1, 7));
                textTimer = 0f;
                textInterval = Mathf.Min(textInterval + 0.012f, 0.22f);
            }

            yield return null;
        }

        // lock to the real result
        ShowFaces(finalD1, finalD2);
        _rolling = false;

        // pause so the player can read the result, then hide and advance
        yield return new WaitForSeconds(1.5f);
        SetDiceVisible(false);

        OnDiceRolled?.Invoke(total);
        GameManager.Instance.ChangeState(GameManager.GameState.Moving);
    }

    private void ShowFaces(int d1, int d2)
    {
        if (_diceDisplay != null)
            _diceDisplay.text = $"[ {d1} ]  [ {d2} ]";
    }

    private void SetDiceVisible(bool visible)
    {
        if (_dice1 != null) _dice1.gameObject.SetActive(visible);
        if (_dice2 != null) _dice2.gameObject.SetActive(visible);
    }
}
