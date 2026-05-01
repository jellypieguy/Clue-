using UnityEngine;
using TMPro;

public class ChatMessageUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text messageText;

    public void SetMessage(string characterName, string message, Color nameColor)
    {
        nameText.text = characterName + ":";
        nameText.color = nameColor;
        messageText.text = message;
    }
}