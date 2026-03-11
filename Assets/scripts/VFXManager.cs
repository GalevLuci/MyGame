using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Singleton — управляет настройками визуальных эффектов (VFX).
/// Добавьте на пустой GameObject "VFXManager" на сцене.
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    // ─────────────────────────── Volume профиль ────────────────────────────
    [Header("Post Processing")]
    [Tooltip("Global Volume для Post Processing (URP)")]
    public Volume globalVolume;

    // ─────────────────────────── Частицы ──────────────────────────────────
    [Header("Настройки частиц")]
    [Tooltip("Максимальное кол-во частиц (0 = отключены)")]
    [Range(0, 10000)]
    public int maxParticles = 1000;

    // ─────────────────────────── Тени ─────────────────────────────────────
    [Header("Настройки теней")]
    [Tooltip("Текущее качество теней")]
    public ShadowQuality shadowQuality = ShadowQuality.All;

    // ─────────────────────────── Ключи PlayerPrefs ────────────────────────
    private const string VFX_ENABLED_KEY      = "VFXEnabled";
    private const string SHADOW_QUALITY_KEY   = "ShadowQuality";
    private const string MAX_PARTICLES_KEY    = "MaxParticles";
    private const string POST_PROCESS_KEY     = "PostProcessing";

    // ─────────────────────────── Свойства ─────────────────────────────────

    /// <summary>Включены ли VFX (частицы) глобально.</summary>
    public bool VFXEnabled
    {
        get => PlayerPrefs.GetInt(VFX_ENABLED_KEY, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(VFX_ENABLED_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyVFX(value);
        }
    }

    /// <summary>Включён ли Post Processing.</summary>
    public bool PostProcessingEnabled
    {
        get => PlayerPrefs.GetInt(POST_PROCESS_KEY, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(POST_PROCESS_KEY, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyPostProcessing(value);
        }
    }

    /// <summary>Качество теней (0 = выкл, 1 = Hard only, 2 = All).</summary>
    public int ShadowQualityLevel
    {
        get => PlayerPrefs.GetInt(SHADOW_QUALITY_KEY, 2);
        set
        {
            int clamped = Mathf.Clamp(value, 0, 2);
            PlayerPrefs.SetInt(SHADOW_QUALITY_KEY, clamped);
            PlayerPrefs.Save();
            ApplyShadowQuality(clamped);
        }
    }

    /// <summary>Максимум частиц для систем частиц (% от maxParticles).</summary>
    public float ParticleQuality
    {
        get => PlayerPrefs.GetFloat(MAX_PARTICLES_KEY, 1f);
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MAX_PARTICLES_KEY, clamped);
            PlayerPrefs.Save();
            // Применение через VFXEnabled/ручной вызов
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

        LoadAndApplyAll();
    }

    // ─────────────────────────────── Загрузка ─────────────────────────────
    void LoadAndApplyAll()
    {
        ApplyVFX(VFXEnabled);
        ApplyPostProcessing(PostProcessingEnabled);
        ApplyShadowQuality(ShadowQualityLevel);
    }

    // ─────────────────────────────── Применение ───────────────────────────

    void ApplyVFX(bool enabled)
    {
        // Находим все системы частиц на сцене и включаем/выключаем их
        ParticleSystem[] systems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in systems)
        {
            var main = ps.main;
            main.maxParticles = enabled ? maxParticles : 0;

            if (enabled && !ps.isPlaying) ps.Play();
            else if (!enabled) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void ApplyPostProcessing(bool enabled)
    {
        if (globalVolume != null)
            globalVolume.enabled = enabled;
    }

    void ApplyShadowQuality(int level)
    {
        switch (level)
        {
            case 0: QualitySettings.shadows = ShadowQuality.Disable;  break;
            case 1: QualitySettings.shadows = ShadowQuality.HardOnly; break;
            case 2: QualitySettings.shadows = ShadowQuality.All;      break;
        }
    }

    // ─────────────────────────────── Публичное API ────────────────────────

    /// <summary>Переключить VFX (частицы).</summary>
    public void ToggleVFX()     => VFXEnabled = !VFXEnabled;

    /// <summary>Переключить Post Processing.</summary>
    public void TogglePostProcessing() => PostProcessingEnabled = !PostProcessingEnabled;

    /// <summary>Установить качество теней по индексу (0/1/2).</summary>
    public void SetShadowQuality(int level) => ShadowQualityLevel = level;
}
