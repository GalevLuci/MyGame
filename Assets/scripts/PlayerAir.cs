using UnityEngine;

/// <summary>
/// Вешай на объект игрока.
/// Управляет запасом воздуха: убывает в дыму, пополняется тряпкой.
/// </summary>
public class PlayerAir : MonoBehaviour
{
    [Header("Воздух")]
    [Tooltip("Максимальный запас воздуха.")]
    [SerializeField] private float maxAir = 100f;

    [Tooltip("Скорость убывания воздуха в дыму (единиц/сек).")]
    [SerializeField] private float depletionRate = 15f;

    [Tooltip("Скорость получения урона от удушья при 0 воздуха (HP/сек). 0 = отключить урон.")]
    [SerializeField] private float suffocationDamageRate = 0f;

    // ── Публичное состояние ───────────────────────────────────────────────────
    /// <summary>Воздух от 0 до 1.</summary>
    public float AirNormalized => currentAir / maxAir;
    /// <summary>Текущий воздух.</summary>
    public float CurrentAir    => currentAir;
    public float MaxAir        => maxAir;
    /// <summary>Игрок сейчас в дыму?</summary>
    public bool  IsInSmoke     => isInSmoke;

    // ── События ───────────────────────────────────────────────────────────────
    /// <summary>Вызывается при любом изменении воздуха. Аргумент — нормализованное значение 0–1.</summary>
    public event System.Action<float> OnAirChanged;
    /// <summary>Вызывается при входе в дым.</summary>
    public event System.Action OnEnterSmoke;
    /// <summary>Вызывается при выходе из дыма.</summary>
    public event System.Action OnExitSmoke;
    /// <summary>Вызывается каждый кадр пока воздух = 0 и игрок в дыму.</summary>
    public event System.Action OnSuffocating;

    // ── Приватное ─────────────────────────────────────────────────────────────
    private float currentAir;
    private bool  isInSmoke;

    void Start()
    {
        currentAir = maxAir;
    }

    void Update()
    {
        if (!isInSmoke) return;

        if (currentAir > 0f)
        {
            currentAir = Mathf.Max(0f, currentAir - depletionRate * Time.deltaTime);
            OnAirChanged?.Invoke(AirNormalized);
        }
        else
        {
            OnSuffocating?.Invoke();
            // Урон от удушья (опционально)
            // if (suffocationDamageRate > 0f) ... подключи систему HP здесь
        }
    }

    // ── Публичные методы ──────────────────────────────────────────────────────

    /// <summary>Устанавливает состояние "в дыму". Вызывается SmokeZone.</summary>
    public void SetInSmoke(bool value)
    {
        if (isInSmoke == value) return;
        isInSmoke = value;
        if (value) OnEnterSmoke?.Invoke();
        else       OnExitSmoke?.Invoke();
    }

    /// <summary>Добавляет воздух. Вызывается ClothTool.</summary>
    public void AddAir(float amount)
    {
        float prev = currentAir;
        currentAir = Mathf.Min(maxAir, currentAir + amount);
        if (!Mathf.Approximately(currentAir, prev))
            OnAirChanged?.Invoke(AirNormalized);
    }

    /// <summary>Мгновенно сбрасывает воздух к максимуму (например при респавне).</summary>
    public void ResetAir()
    {
        currentAir = maxAir;
        OnAirChanged?.Invoke(1f);
    }
}
