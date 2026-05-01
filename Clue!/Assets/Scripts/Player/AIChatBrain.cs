using UnityEngine;

public class AIChatBrain : MonoBehaviour
{
    public static AIChatBrain Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public enum Character { Scarlet, Mustard, White, Green, Peacock, Plum }

    private Color GetCharacterColor(Character character)
    {
        return character switch
        {
            Character.Scarlet => new Color(1f, 0.2f, 0.2f),
            Character.Mustard => new Color(1f, 0.8f, 0f),
            Character.White => new Color(0.9f, 0.9f, 0.9f),
            Character.Green => new Color(0.2f, 0.8f, 0.2f),
            Character.Peacock => new Color(0.2f, 0.2f, 1f),
            Character.Plum => new Color(0.6f, 0.2f, 0.8f),
            _ => Color.white
        };
    }

    // call from turnmanager
    public void TriggerAILine(Character character, string situation)
    {
        string[] possibleLines = GetLinesForCharacter(character, situation);

        if (possibleLines != null && possibleLines.Length > 0)
        {
            string chosenLine = possibleLines[Random.Range(0, possibleLines.Length)];
            Color charColor = GetCharacterColor(character);

            ClueChatManager.Instance.AddMessageToChat(character.ToString(), chosenLine, charColor);
        }
    }

    private string[] GetLinesForCharacter(Character character, string situation)
    {
        // col mustard
        if (character == Character.Mustard)
        {
            if (situation == "TurnStart") return new[] {
                "Right then. Moving out.",
                "Let's get to the bottom of this mess.",
                "Keep your eyes peeled, everyone."
            };
            if (situation == "BadRoll") return new[] {
                "Confound it! Tripped over the rug.",
                "My bad knee is acting up again..."
            };
        }

        // miss scarlett
        if (character == Character.Scarlet)
        {
            if (situation == "TurnStart") return new[] {
                "Darling, let me show you how it's done.",
                "Wheres the maid? So much dust in this house...",
                "Who has a secret they want to share?"
            };
            if (situation == "BadRoll") return new[] {
                "These heels are simply impossible on this flooring.",
                "I'm taking my time, don't rush me."
            };
        }

        // mrs white
        if (character == Character.White)
        {
            if (situation == "TurnStart") return new[] {
                "I just cleaned that hallway, mind your boots!",
                "Always me walking about doing the hard work.",
                "I know what I saw... Lawrence was being shifty"
            };
        }

        // professor plum
        if (character == Character.Plum)
        {
            if (situation == "TurnStart") return new[] {
                "Statistically speaking, the culprit is in the next room.",
                "we should apply the scientific method to this.",
                "keep up with my deductions."
            };
        }

        // Mrs Peacock
        if (character == Character.Peacock)
        {
            if (situation == "TurnStart") return new[] {
                "Oh my! The sheer scandal of it all!",
                "I shouldn't be subjected to this Crap, I really shouldn't.",
                "Did you see how Gracie was looking at me?"
            };
        }

        // Reverend Green
        if (character == Character.Green)
        {
            if (situation == "TurnStart") return new[] {
                "Oh dear... I hope no one else gets hurt.",
                "Excuse me, just passing through.",
                "I really don't like the dark corners of this house..."
            };
        }

        // Default fallback
        return new[] { "..." };
    }
}