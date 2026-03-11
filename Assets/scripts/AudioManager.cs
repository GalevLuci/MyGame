using UnityEngine;

/// <summary>
/// Singleton — управляет музыкой, звуковыми эффектами и звуками UI.
/// Добавьте на пустой GameObject "AudioManager" на сцене.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─────────────────────────────── Источники ────────────────────────────
    [Header("Источники звука")]
    [Tooltip("AudioSource для фоновой музыки (Loop = true)")]
    public AudioSource musicSource;

    [Tooltip("AudioSource для звуковых эффектов")]
    public AudioSource sfxSource;

    [Tooltip("AudioSource для звуков UI")]
    public AudioSource uiSource;

    // ─────────────────────────────── Звуки UI ─────────────────────────────
    [Header("Звуки UI")]
    [Tooltip("Звук открытия паузы")]
    public AudioClip pauseOpenSound;

    [Tooltip("Звук закрытия паузы / возврата в игру")]
    public AudioClip pauseCloseSound;

    [Tooltip("Звук нажатия кнопки")]
    public AudioClip buttonClickSound;

    // ─────────────────────────────── Ключи PlayerPrefs ────────────────────
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY   = "SFXVolume";
    private const string UI_VOLUME_KEY    = "UIVolume";

    // ─────────────────────────────── Свойства громкости ───────────────────
    public float MusicVolume
    {
        get => musicSource != null ? musicSource.volume : 0f;
        set
        {
            if (musicSource != null) musicSource.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public float SFXVolume
    {
        get => sfxSource != null ? sfxSource.volume : 0f;
        set
        {
            if (sfxSource != null) sfxSource.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public float UIVolume
    {
        get => uiSource != null ? uiSource.volume : 0f;
        set
        {
            if (uiSource != null) uiSource.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(UI_VOLUME_KEY, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Синглтон: один AudioManager на всё время игры
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
    }

    // ─────────────────────────────── Загрузка настроек ────────────────────
    void LoadVolumes()
    {
        if (musicSource != null)
            musicSource.volume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.5f);

        if (sfxSource != null)
            sfxSource.volume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);

        if (uiSource != null)
            uiSource.volume = PlayerPrefs.GetFloat(UI_VOLUME_KEY, 1f);
    }

    // ─────────────────────────────── Публичное API ────────────────────────

    /// <summary>Воспроизвести звуковой эффект (одиночный).</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>Воспроизвести звук интерфейса.</summary>
    public void PlayUI(AudioClip clip)
    {
        if (uiSource == null || clip == null) return;
        uiSource.PlayOneShot(clip);
    }

    /// <summary>Воспроизвести звук открытия паузы.</summary>
    public void PlayPauseOpen()  => PlayUI(pauseOpenSound);

    /// <summary>Воспроизвести звук закрытия паузы.</summary>
    public void PlayPauseClose() => PlayUI(pauseCloseSound);

    /// <summary>Воспроизвести звук нажатия кнопки.</summary>
    public void PlayButtonClick() => PlayUI(buttonClickSound);

    /// <summary>Сменить трек фоновой музыки.</summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    /// <summary>Остановить музыку.</summary>
    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }
}
