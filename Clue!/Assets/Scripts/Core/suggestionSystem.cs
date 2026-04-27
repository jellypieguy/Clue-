using System.Collections.Generic;
using UnityEngine;

public class SuggestionSystem : MonoBehaviour
{
    // Processes a suggestion by checking each player clockwise from the suggester.
    // Each player reveals a matching card if they have one, then the round stops.
    // Returns the card shown, or null if nobody could disprove the suggestion.
    public Card ProcessSuggestion(string person, string weapon, string room,
                                   int suggestingPlayerIndex, List<Player> allPlayers)
    {
        Debug.Log(allPlayers[suggestingPlayerIndex].PlayerName +
                  " suggests: " + person + " with " + weapon + " in " + room);

        int playerCount = allPlayers.Count;

        // Check each player clockwise starting from the player to the left
        for (int i = 1; i < playerCount; i++)
        {
            int checkIndex = (suggestingPlayerIndex + i) % playerCount;
            Player playerToCheck = allPlayers[checkIndex];

            // Ask player if they hold any of the suggested cards
            Card shownCard = playerToCheck.GetCardToShow(person, weapon, room);

            if (shownCard != null)
            {
                // Only the suggesting player sees the shown card
                Debug.Log(playerToCheck.PlayerName + " shows: " + shownCard.Name +
                          " to " + allPlayers[suggestingPlayerIndex].PlayerName);
                return shownCard;
            }
            else
            {
                Debug.Log(playerToCheck.PlayerName + " has nothing to show.");
            }
        }

        // If no player could disprove, the suggestion may be the murder solution
        Debug.Log("Nobody could disprove the suggestion!");
        return null;
    }
}