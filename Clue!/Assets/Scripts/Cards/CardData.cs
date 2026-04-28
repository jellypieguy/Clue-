using UnityEngine;

// object data container for suspects, weapons and rooms 
[CreateAssetMenu(fileName = "New Card", menuName = "Cluedo/Card Data")]
public class CardData : ScriptableObject
{
    public enum CardType
    {
        Suspect,
        Weapon,
        Room
    }

    [Header("Card Properties")]
    public CardType Type;
    public string CardName;
    public Sprite CardImage;
}