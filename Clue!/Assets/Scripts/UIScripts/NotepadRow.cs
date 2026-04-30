using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotepadRow : MonoBehaviour
{
    public TMP_Text cardNameText;
    public Toggle haveToggle;
    public Toggle shownToggle;
    public Toggle xToggle;

    private int boundCardIndex;
    private Player boundPlayer;

    public void Setup(int index, string cardName, Player player)
    {
        boundCardIndex = index;
        boundPlayer = player;
        cardNameText.text = cardName;

        // gets initial state from the players notes so the UI doesnt give fake info 
        haveToggle.isOn = boundPlayer.ReadNote(boundCardIndex, 0);
        shownToggle.isOn = boundPlayer.ReadNote(boundCardIndex, 1);
        xToggle.isOn = boundPlayer.ReadNote(boundCardIndex, 2);

        haveToggle.onValueChanged.AddListener(_ => boundPlayer.MarkNote(boundCardIndex, 0));
        shownToggle.onValueChanged.AddListener(_ => boundPlayer.MarkNote(boundCardIndex, 1));
        xToggle.onValueChanged.AddListener(_ => boundPlayer.MarkNote(boundCardIndex, 2));
    }
}