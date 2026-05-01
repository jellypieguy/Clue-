using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    [Header("UI References")]
    public ScrollRect scrollRect;
    public Transform messageContainer;
    public GameObject messagePrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddMessageToChat(string characterName, string message, Color nameColor)
    {
        if (messagePrefab == null || messageContainer == null) return;

        GameObject msgObj = Instantiate(messagePrefab, messageContainer);
        ChatMessageUI msgUI = msgObj.GetComponent<ChatMessageUI>();

        if (msgUI != null)
            msgUI.SetMessage(characterName, message, nameColor);

        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}