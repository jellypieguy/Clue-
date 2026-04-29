using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotepadRow : MonoBehaviour
{
    public TMP_Text cardNameText;
    public Toggle haveToggle;
    public Toggle shownToggle;
    public Toggle xToggle;

    private int _cardIndex;
    private Player _player;

    public void Setup(int index, string cardName, Player player)
    {
        _cardIndex = index;
        _player = player;
        cardNameText.text = cardName;

        // Sync initial state from player's notes
        haveToggle.isOn = _player.ReadNote(_cardIndex, 0);
        shownToggle.isOn = _player.ReadNote(_cardIndex, 1);
        xToggle.isOn = _player.ReadNote(_cardIndex, 2);

        // Add listeners
        haveToggle.onValueChanged.AddListener((val) => _player.MarkNote(_cardIndex, 0));
        shownToggle.onValueChanged.AddListener((val) => _player.MarkNote(_cardIndex, 1));
        xToggle.onValueChanged.AddListener((val) => _player.MarkNote(_cardIndex, 2));
    }
}
