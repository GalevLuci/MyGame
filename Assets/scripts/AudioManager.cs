using UnityEngine;

/// <summary>
/// Singleton — управляет музыкой и звуковыми эффектами (SFX).
/// SFX = всё кроме музыки: шаги, выстрелы, кнопки, меню и т.д.
/// Добавьте на пустой GameObject "AudioManager" на сцене.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─────────────────────────────── Источники ────────────────────────────
    [Header("Источники звука")]
    [Tooltip("AudioSource для фоновой музыки (Loop = true)")]
    public AudioSource musicSource;

    [Tooltip("AudioSource для всех остальных звуков (SFX)")]
    public AudioSource sfxSource;

    // ─────────────────────────────── Звуки меню ───────────────────────────
    [Header("Звуки меню / кнопок")]
    [Tooltip("Звук открытия паузы")]
    public AudioClip pauseOpenSound;

    [Tooltip("Звук закрытия паузы / возврата в игру")]
    public AudioClip pauseCloseSound;

    [Tooltip("Звук нажатия кнопки")]
    public AudioClip buttonClickSound;

    // ─────────────────────────────── Ключи PlayerPrefs ────────────────────
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SFX_VOLUME_KEY   = "SFXVolume";

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

    /// <summary>
    /// Громкость SFX — регулирует ВСЕ звуки кроме музыки:
    /// шаги, выстрелы, кнопки, меню, паузу и т.д.
    /// </summary>
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

    // ──────────────────────────────────────────────────────────────────────
    void Awake()
    {
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
    }

    // ─────────────────────────────── Публичное API ────────────────────────

    /// <summary>Воспроизвести любой SFX звук (шаги, выстрел, кнопка и т.д.).</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    /// <summary>Воспроизвести звук открытия паузы.</summary>
    public void PlayPauseOpen()  => PlaySFX(pauseOpenSound);

    /// <summary>Воспроизвести звук закрытия паузы.</summary>
    public void PlayPauseClose() => PlaySFX(pauseCloseSound);

    /// <summary>Воспроизвести звук нажатия кнопки.</summary>
    public void PlayButtonClick() => PlaySFX(buttonClickSound);

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
