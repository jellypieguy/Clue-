using UnityEngine;
using UnityEngine.UI;

// Helper script to connect a UI Toggle to the GameSettings room exclusion list
[RequireComponent(typeof(Toggle))]
public class RoomToggle : MonoBehaviour
{
    [Tooltip("The Room card asset this toggle controls.")]
    public CardData roomCard;

    private Toggle _toggle;

    void Awake()
    {
        _toggle = GetComponent<Toggle>();
        _toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    void Start()
    {
        // Sync the toggle state with current settings if they already exist
        if (GameSettings.Instance != null)
        {
            _toggle.isOn = !GameSettings.Instance.IsRoomDisabled(roomCard);
        }
    }

    void OnToggleChanged(bool isOn)
    {
        if (GameSettings.Instance == null) return;

        // If the toggle is OFF, we want to DISABLE the room
        // If the toggle is ON, we want to ENABLE the room
        // ToggleRoom flips the state, so we check the setting first
        if (GameSettings.Instance.IsRoomDisabled(roomCard) == isOn)
        {
            GameSettings.Instance.ToggleRoom(roomCard);
            Debug.Log($"Options: Room {roomCard.CardName} is now {(isOn ? "Enabled" : "Disabled")}");
        }
    }
}
