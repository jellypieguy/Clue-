using System.Collections.Generic;
using UnityEngine;

// Handles a suggestion made by the current player.
// Goes round the table clockwise, asks each PlayerHand if it can refute the suggestion,
// and stops at the first player who can show a matching card.
public class SuggestionSystem : MonoBehaviour
{
    // Processes a suggestion by checking each player clockwise from the suggester.
    // Returns the card shown, or null if nobody could disprove the suggestion.
    public CardData ProcessSuggestion(CardData suspect, CardData weapon, CardData room,
                                       int suggestingPlayerIndex, List<PlayerController> allPlayers)
    {
        PlayerController suggester = allPlayers[suggestingPlayerIndex];
        string suggesterName = GetPlayerName(suggester);

        Debug.Log($"{suggesterName} suggests: {suspect.CardName} with {weapon.CardName} in {room.CardName}");

        int playerCount = allPlayers.Count;

        // Check each player clockwise starting from the player to the left
        for (int i = 1; i < playerCount; i++)
        {
            int checkIndex = (suggestingPlayerIndex + i) % playerCount;
            PlayerController playerToCheck = allPlayers[checkIndex];
            string checkName = GetPlayerName(playerToCheck);

            // Skip eliminated players — they still hold cards but the rules say
            // they remain in the game only to refute, so they DO get checked
            // (this is the existing behaviour, eliminated players still show cards)

            PlayerHand hand = playerToCheck.GetComponent<PlayerHand>();
            if (hand == null)
            {
                Debug.LogWarning($"{checkName} has no PlayerHand component.");
                continue;
            }

            // Ask the hand which cards (if any) can refute this suggestion
            List<CardData> refutingCards = hand.GetRefutingCards(suspect, weapon, room);

            if (refutingCards.Count > 0)
            {
                // Pick which card to show
                // AI: random pick from the matching cards
                // Human: TODO show UI for player to choose, for now picks first
                CardData shownCard;
                if (playerToCheck.IsHuman)
                {
                    // TODO: replace with UI choice
                    shownCard = refutingCards[0];
                }
                else
                {
                    shownCard = refutingCards[Random.Range(0, refutingCards.Count)];
                }

                Debug.Log($"{checkName} shows: {shownCard.CardName} to {suggesterName}");
                return shownCard;
            }
            else
            {
                Debug.Log($"{checkName} has nothing to show.");
            }
        }

        // If no player could disprove, the suggestion may be the murder solution
        Debug.Log("Nobody could disprove the suggestion!");
        return null;
    }

    // Helper to safely get a display name for a player.
    // Looks for the Player component first; falls back to the CharacterType enum.
    private string GetPlayerName(PlayerController pc)
    {
        Player playerData = pc.GetComponent<Player>();
        if (playerData != null && !string.IsNullOrEmpty(playerData.PlayerName))
            return playerData.PlayerName;
        return pc.Character.ToString();
    }
}