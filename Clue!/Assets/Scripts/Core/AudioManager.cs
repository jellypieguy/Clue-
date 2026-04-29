using UnityEngine;

// PLACEHOLDER AudioManager so the project compiles.
// Currently just logs to the Console instead of playing sound effects
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-create sources if missing
        if (_musicSource == null) _musicSource = gameObject.AddComponent<AudioSource>();
        if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

        _musicSource.loop = true;
        _musicSource.playOnAwake = true;
    }

    public void SetMusicVolume(float volume)
    {
        if (_musicSource != null) _musicSource.volume = volume;
    }

    public void ToggleMusic(bool isOn)
    {
        if (_musicSource != null)
        {
            if (isOn) _musicSource.Play();
            else _musicSource.Pause();
        }
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