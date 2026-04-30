using UnityEngine;

public class NotepadUI : MonoBehaviour
{
    public GameObject panel;
    public Transform contentParent; 
    public GameObject rowPrefab;

    private void Start()
    {
        panel.SetActive(false);
    }

    public void ToggleNotepad()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf) RefreshNotepad();
    }

    private void RefreshNotepad()
    {
        // nukes the old UI rows before rebuilding
        foreach (Transform child in contentParent) 
        {
            Destroy(child.gameObject);
        }

        if (TurnManager.Instance?.CurrentPlayer == null) return;
        
        if (!TurnManager.Instance.CurrentPlayer.TryGetComponent<Player>(out var humanPlayer)) 
            return;

        // Note: deck manager needs GetAllCardsOrdered() for this
        var allCards = DeckManager.Instance.GetAllCardsOrdered(); 
        
        for (int i = 0; i < allCards.Count; i++)
        {
            var rowObj = Instantiate(rowPrefab, contentParent);
            if (rowObj.TryGetComponent<NotepadRow>(out var rowComponent))
            {
                rowComponent.Setup(i, allCards[i].CardName, humanPlayer);
            }
        }
    }
}