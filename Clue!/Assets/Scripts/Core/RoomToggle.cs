using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class RoomToggle : MonoBehaviour
{
    [Tooltip("The Room card asset this toggle controls.")]
    public CardData roomCard;

    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void Start()
    {
        if (GameSettings.Instance != null)
        {
            toggle.isOn = !GameSettings.Instance.IsRoomDisabled(roomCard);
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        if (GameSettings.Instance == null) return;

        // toggle room 
        if (GameSettings.Instance.IsRoomDisabled(roomCard) == isOn)
        {
            GameSettings.Instance.ToggleRoom(roomCard);
            Debug.Log($"Options: {roomCard.CardName} {(isOn ? "enabled" : "disabled")}");
        }
    }
}
