using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class ClueChatManager : MonoBehaviour
{
    public static ClueChatManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Transform chatContentArea;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private ScrollRect chatScrollRect;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // enter to text
        var kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
        {
            if (chatInputField.isFocused && !string.IsNullOrWhiteSpace(chatInputField.text))
            {
                SendPlayerMessage();
            }
            else
            {
                chatInputField.Select();
                chatInputField.ActivateInputField();
            }
        }
    }

    public void SendPlayerMessage()
    {
        if (string.IsNullOrWhiteSpace(chatInputField.text)) return;

        // player is self
        AddMessageToChat("You", chatInputField.text, Color.white);

        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    public void AddMessageToChat(string senderName, string message, Color nameColor)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayUIKeyPress();

        GameObject newMsg = Instantiate(chatMessagePrefab, chatContentArea);
        TextMeshProUGUI msgText = newMsg.GetComponent<TextMeshProUGUI>();

        string hexColor = ColorUtility.ToHtmlStringRGB(nameColor);
        msgText.text = $"<b><color=#{hexColor}>{senderName}</color></b>: {message}";

        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }
}