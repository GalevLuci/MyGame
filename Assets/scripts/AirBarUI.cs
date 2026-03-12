using UnityEngine;

/// <summary>
/// Шкала воздуха на HUD — набор отдельных объектов (сегментов).
/// Добавьте любые GameObject в массив Segments в Inspector.
/// Сегменты включаются/выключаются слева направо по мере изменения воздуха.
///
/// Поведение прозрачности (CanvasGroup на barRoot добавляется автоматически):
///  - Воздух меняется или игрок в дыму → полностью виден
///  - Шкала полная, не меняется        → полупрозрачная (idleAlpha)
///  - Не меняется idleHideDelay секунд → скрывается
///  - Изменяется снова                 → появляется с fade-in
/// </summary>
public class AirBarUI : MonoBehaviour
{
    [Header("Сегменты")]
    [Tooltip("Объекты-сегменты в порядке от первого до последнего. " +
             "Первый активируется первым, последний — при полном воздухе.")]
    [SerializeField] private GameObject[] segments;

    [Header("Ссылки")]
    [SerializeField] private PlayerAir  playerAir;

    [Header("Контейнер (для прозрачности)")]
    [Tooltip("Родительский объект всех сегментов. CanvasGroup добавляется автоматически.")]
    [SerializeField] private GameObject barRoot;

    [Header("Прозрачность")]
    [Tooltip("Прозрачность когда шкала полная и не меняется (0 = невидима, 1 = непрозрачна).")]
    [SerializeField] private float idleAlpha      = 0.3f;

    [Tooltip("Скорость плавного изменения прозрачности.")]
    [SerializeField] private float alphaLerpSpeed = 4f;

    [Header("Таймеры")]
    [Tooltip("Через сколько секунд без изменений полностью скрыть шкалу.")]
    [SerializeField] private float idleHideDelay  = 10f;

    // ── приватное состояние ───────────────────────────────────────────────────

    private CanvasGroup canvasGroup;
    private float currentAlpha    = 0f;
    private float targetAlpha     = 0f;
    private float timeSinceChange = 0f;
    private float lastNormalized  = 1f;
    private bool  isVisible       = false;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (playerAir == null)
        {
            Debug.LogWarning("[AirBarUI] PlayerAir не назначен!", this);
            return;
        }

        if (barRoot != null)
        {
            canvasGroup = barRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = barRoot.AddComponent<CanvasGroup>();
        }

        lastNormalized = playerAir.AirNormalized;
        ApplySegments(lastNormalized);
        SetVisible(false);
    }

    void Update()
    {
        if (playerAir == null) return;

        float normalized = playerAir.AirNormalized;
        bool  airChanged = Mathf.Abs(normalized - lastNormalized) > 0.001f;

        // ── Включаем / выключаем сегменты ────────────────────────────────────
        ApplySegments(normalized);

        // ── Логика видимости и прозрачности ──────────────────────────────────
        if (airChanged || playerAir.IsInSmoke)
        {
            lastNormalized  = normalized;
            timeSinceChange = 0f;
            SetVisible(true);
            targetAlpha = 1f;
        }
        else
        {
            timeSinceChange += Time.deltaTime;

            if (normalized >= 0.9999f)
            {
                // Шкала полная — делаем полупрозрачной
                targetAlpha = idleAlpha;
            }

            if (timeSinceChange >= idleHideDelay)
            {
                // Не менялась достаточно долго — скрываем
                SetVisible(false);
            }
        }

        // ── Плавная прозрачность ──────────────────────────────────────────────
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, alphaLerpSpeed * Time.deltaTime);
        if (canvasGroup != null) canvasGroup.alpha = currentAlpha;
    }

    // ── Вспомогательные методы ────────────────────────────────────────────────

    /// <summary>Включает первые N сегментов и выключает остальные.</summary>
    void ApplySegments(float normalized)
    {
        if (segments == null || segments.Length == 0) return;

        int activeCount = Mathf.RoundToInt(normalized * segments.Length);
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null)
                segments[i].SetActive(i < activeCount);
        }
    }

    void SetVisible(bool value)
    {
        if (isVisible == value) return;
        isVisible = value;

        if (value)
        {
            barRoot?.SetActive(true);
            currentAlpha = 0f;   // начинаем fade-in с нуля
            targetAlpha  = 1f;
        }
        else
        {
            barRoot?.SetActive(false);
        }
    }
}
