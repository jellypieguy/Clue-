using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip gameMusic;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource ??= gameObject.AddComponent<AudioSource>();
        sfxSource ??= gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;

        if (menuMusic != null)
        {
            musicSource.clip = menuMusic;
            musicSource.Play();
        }
    }

    public void PlayMenuMusic() => SwitchMusic(menuMusic);
    public void PlayGameMusic() => SwitchMusic(gameMusic);

    private void SwitchMusic(AudioClip newClip)
    {
        if (newClip == null || musicSource.clip == newClip) return;

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

    public void PlayFootstep() { }
    public void PlayDiceRoll() { }
    public void PlayClick() { }
}