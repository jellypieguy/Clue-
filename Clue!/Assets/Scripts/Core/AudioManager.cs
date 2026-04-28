using UnityEngine;

// PLACEHOLDER AudioManager so the project compiles.
// Stubs out the audio calls until someone builds the real audio system.
// Currently just logs to the Console instead of playing sound effects.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Plays a footstep sound for player movement.
    // Stubbed for now — logs to Console until real audio is added.
    public void PlayFootstep()
    {
        // Real implementation would play an AudioClip via an AudioSource
        // For now, just log silently (no Debug.Log to avoid spam every step)
    }

    // Plays a dice roll sound.
    // Stubbed for now.
    public void PlayDiceRoll()
    {
        // Real implementation would play an AudioClip via an AudioSource
    }

    // Plays a generic UI button click sound.
    // Stubbed for now.
    public void PlayClick()
    {
        // Real implementation would play an AudioClip via an AudioSource
    }
}