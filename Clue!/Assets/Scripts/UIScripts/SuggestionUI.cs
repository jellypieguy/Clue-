using UnityEngine;
using TMPro;
using System.Linq;

public class SuggestionUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown suspectDropdown;
    [SerializeField] private TMP_Dropdown weaponDropdown;
    [SerializeField] private GameObject suggestionPanel;

    public void Show()
    {
        suggestionPanel.SetActive(true);
        PopulateDropdowns();
    }

    private void PopulateDropdowns()
    {
        suspectDropdown.ClearOptions();
        weaponDropdown.ClearOptions();

        var suspects = DeckManager.Instance.AllSuspects.Select(c => c.CardName).ToList();
        suspectDropdown.AddOptions(suspects);

        var weapons = DeckManager.Instance.AllWeapons.Select(c => c.CardName).ToList();
        weaponDropdown.AddOptions(weapons);
    }

    public void OnConfirmClicked()
    {
        var targetSuspect = DeckManager.Instance.AllSuspects[suspectDropdown.value];
        var targetWeapon = DeckManager.Instance.AllWeapons[weaponDropdown.value];

        GameManager.Instance.HumanSuggestion(targetSuspect, targetWeapon);
        suggestionPanel.SetActive(false);
    }
}