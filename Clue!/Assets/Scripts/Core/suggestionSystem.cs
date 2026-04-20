using System.Collections.Generic;
using UnityEngine;

public class SuggestionSystem : MonoBehaviour
{
    // processes a suggestion: checks players clockwise from the suggester
    // returns the card shown (or null if nobody could disprove)
    public Card ProcessSuggestion(string person, string weapon, string room,
                                   int suggestingPlayerIndex, List<Player> allPlayers)
    {
        Debug.Log(allPlayers[suggestingPlayerIndex].PlayerName +
                  " suggests: " + person + " with " + weapon + " in " + room);

        int playerCount = allPlayers.Count;

        // start from the player to the left (clockwise)
        for (int i = 1; i < playerCount; i++)
        {
            int checkIndex = (suggestingPlayerIndex + i) % playerCount;
            Player playerToCheck = allPlayers[checkIndex];

            Card shownCard = playerToCheck.GetCardToShow(person, weapon, room);

            if (shownCard != null)
            {
                Debug.Log(playerToCheck.PlayerName + " shows: " + shownCard.Name +
                          " to " + allPlayers[suggestingPlayerIndex].PlayerName);
                return shownCard;
            }
            else
            {
                Debug.Log(playerToCheck.PlayerName + " has nothing to show.");
            }
        }

        Debug.Log("Nobody could disprove the suggestion!");
        return null;
    }
}
