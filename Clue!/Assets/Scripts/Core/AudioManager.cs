using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Music Clips")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;

    [Header("SFX Clips - Movement & Dice")]
    [SerializeField] private AudioClip footstepClip;    // board movement
    [SerializeField] private AudioClip diceRollClip;    // diceroll

    [Header("SFX Clips - Cards & UI")]
    [SerializeField] private AudioClip cardDealClip;    // carddeal
    [SerializeField] private AudioClip cardFlipClip;    //flippingcards
    [SerializeField] private AudioClip uiKeyPressClip;  // ui  sound
    [SerializeField] private AudioClip genericClickClip; // buttons

    [Header("Settings")]
    [Range(0.05f, 0.2f)] public float pitchVariation = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitialiseAudio();
    }

    private void InitialiseAudio()
    {
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        if (menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.Play();
        }
    }

    // music
    public void PlayMenuMusic() => SwitchMusic(menuMusic);
    public void PlayGameMusic() => SwitchMusic(gameMusic);

    private void SwitchMusic(AudioClip newClip)
    {
        if (newClip == null || musicSource == null) return;
        if (musicSource.clip == newClip) return;

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
    }

    public void ToggleMusic(bool isEnabled)
    {
        if (musicSource == null) return;
        if (isEnabled) musicSource.Play();
        else musicSource.Pause();
    }


    // movement every time a player moves a tile
    public void PlayFootstep() => PlayRandomizedSFX(footstepClip, 0.6f);

    // dice when rolled
    public void PlayDiceRoll() => PlayRandomizedSFX(diceRollClip, 1.0f);

    public void PlayClick() => PlayRandomizedSFX(genericClickClip, 1.0f);

    public void PlayUIKeyPress() => PlayRandomizedSFX(uiKeyPressClip, 0.8f);

    public void PlayCardDeal() => PlayRandomizedSFX(cardDealClip, 0.7f);

    public void PlayCardFlip() => PlayRandomizedSFX(cardFlipClip, 0.8f);

    private void PlayRandomizedSFX(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;

        float randomPitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);

        sfxSource.pitch = randomPitch;
        sfxSource.PlayOneShot(clip, volume);
    }
}