using System.Collections.Generic;
using UnityEngine;

public class SuggestionSystem : MonoBehaviour
{
    // Callback fires with the card shown, or null if nobody could disprove.
    // Goes clockwise from the suggester — first player with a matching card wins.
    public void ProcessSuggestion(CardData suspect, CardData weapon, CardData room,
                                   int suggesterIdx, List<PlayerController> allPlayers,
                                   System.Action<CardData> onComplete)
    {
        var suggester = allPlayers[suggesterIdx];
        Debug.Log($"{GetName(suggester)} suggests: {suspect.CardName} with {weapon.CardName} in {room.CardName}");

        for (int i = 1; i < allPlayers.Count; i++)
        {
            int idx = (suggesterIdx + i) % allPlayers.Count;
            var player = allPlayers[idx];

            var hand = player.GetComponent<PlayerHand>();
            if (hand == null) continue;

            var cards = hand.GetRefutingCards(suspect, weapon, room);
            if (cards.Count == 0)
            {
                Debug.Log($"{GetName(player)} has nothing.");
                continue;
            }

            // Eliminated players still refute per official rules — intentional
            if (player.IsHuman)
            {
                // Pause here and let UIManager handle the card pick, resume via callback
                UIManager.Instance?.ShowRefuterPicker(cards, chosen =>
                {
                    Debug.Log($"{GetName(player)} shows {chosen.CardName}");
                    onComplete(chosen);
                });
                return;
            }

            var shown = cards[Random.Range(0, cards.Count)];
            Debug.Log($"{GetName(player)} shows {shown.CardName}");
            onComplete(shown);
            return;
        }

        Debug.Log("Nobody could disprove it — that's suspicious.");
        onComplete(null);
    }

    private string GetName(PlayerController pc)
    {
        var data = pc.GetComponent<Player>();
        return (data != null && !string.IsNullOrEmpty(data.PlayerName)) ? data.PlayerName : pc.Character.ToString();
    }
}
