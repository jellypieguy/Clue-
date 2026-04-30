using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHandDisplay : MonoBehaviour
{
    public static PlayerHandDisplay Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject handPanel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardSlotPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        handPanel?.SetActive(true);
    }

    public void ShowHand(List<CardData> cards)
    {
        foreach (Transform child in cardContainer)
            Destroy(child.gameObject);

        foreach (CardData card in cards)
        {
            GameObject slot = Instantiate(cardSlotPrefab, cardContainer);

            Image img = slot.GetComponentInChildren<Image>();
            if (img != null && card.CardImage != null)
                img.sprite = card.CardImage;

            TMP_Text label = slot.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = card.CardName;
        }
    }
}