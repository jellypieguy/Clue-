using UnityEngine;

// all the card data took long time to make this
[CreateAssetMenu(fileName = "NewCard", menuName = "Cluedo/CardData")]
public class CardData : ScriptableObject
{
    public enum CardType { Suspect, Weapon, Room }

    [Header("General Info")]
    public string CardName;
    public CardType Type;

    [Header("Visuals")]
    [Tooltip("vertical art for UI, Hands, and Env.")]
    public Sprite CardImage;

    [Tooltip("gameboard pngs")]
    public Sprite BoardSprite;
}