using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SuggestionUI : MonoBehaviour
{
    public TMP_Dropdown suspectDropdown;
    public TMP_Dropdown weaponDropdown;
    public GameObject panel;

    public void Show()
    {
        panel.SetActive(true);
        PopulateDropdowns();
    }

    void PopulateDropdowns()
    {
        suspectDropdown.ClearOptions();
        weaponDropdown.ClearOptions();

        List<string> suspects = new List<string>();
        foreach (var card in DeckManager.Instance.AllSuspects) suspects.Add(card.CardName);
        suspectDropdown.AddOptions(suspects);

        List<string> weapons = new List<string>();
        foreach (var card in DeckManager.Instance.AllWeapons) weapons.Add(card.CardName);
        weaponDropdown.AddOptions(weapons);
    }

    public void OnConfirmClicked()
    {
        CardData selectedSuspect = DeckManager.Instance.AllSuspects[suspectDropdown.value];
        CardData selectedWeapon = DeckManager.Instance.AllWeapons[weaponDropdown.value];

        // The GameManager handles pulling the room from the player's current tile
        GameManager.Instance.HumanSuggestion(selectedSuspect, selectedWeapon);
        panel.SetActive(false);
    }
}
