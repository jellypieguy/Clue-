using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotepadUI : MonoBehaviour
{
    public GameObject panel;
    public Transform contentParent; // The scroll view content object
    public GameObject rowPrefab;   // A prefab with a text label and 3 toggles

    void Start()
    {
        // Hide by default
        panel.SetActive(false);
    }

    public void ToggleNotepad()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf) RefreshNotepad();
    }

    void RefreshNotepad()
    {
        // Clear old rows
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Get the current human player
        Player human = TurnManager.Instance.CurrentPlayer.GetComponent<Player>();

        // Create a row for every card in the deck
        var allCards = DeckManager.Instance.GetAllCardsOrdered(); // We'll add this helper to DeckManager
        for (int i = 0; i < allCards.Count; i++)
        {
            GameObject row = Instantiate(rowPrefab, contentParent);
            row.GetComponent<NotepadRow>().Setup(i, allCards[i].CardName, human);
        }
    }
}
