using UnityEngine;

// Attach this to any GameObject in the game scene (e.g. your Canvas).
// Wire button OnClick() events to these methods in the Inspector.
public class UIAudioHook : MonoBehaviour
{
    public void PlayClick()   => AudioManager.Instance?.PlayClick();
    public void PlayDiceRoll() => AudioManager.Instance?.PlayDiceRoll();
    public void PlayCardFlip() => AudioManager.Instance?.PlayCardFlip();
}
