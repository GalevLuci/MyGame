using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоска воздуха на HUD.
/// CanvasGroup добавляется на barRoot автоматически — вручную добавлять не нужно.
///
/// Поведение:
///  - Шкала полная (воздух 100%) → полупрозрачная
///  - Не меняется 10 секунд    → скрывается
///  - Меняется снова            → появляется полностью непрозрачной
/// </summary>
public class AirBarUI : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private GameObject    barRoot;
    [SerializeField] private RectTransform fillRect;

    [Header("Ссылки")]
    [SerializeField] private PlayerAir playerAir;

    [Header("Прозрачность")]
    [Tooltip("Прозрачность когда шкала полная и не меняется (0=невидима, 1=непрозрачна).")]
    [SerializeField] private float idleAlpha      = 0.3f;

    [Tooltip("Скорость плавного изменения прозрачности.")]
    [SerializeField] private float alphaLerpSpeed = 4f;

    [Header("Таймеры")]
    [Tooltip("Через сколько секунд без изменений полностью скрыть шкалу.")]
    [SerializeField] private float idleHideDelay  = 10f;

    [Tooltip("Скорость плавного изменения заливки.")]
    [SerializeField] private float fillSmoothSpeed = 8f;

    private CanvasGroup canvasGroup;
    private float currentFill   = 1f;
    private float currentAlpha  = 0f;
    private float targetAlpha   = 0f;
    private float timeSinceChange = 0f;
    private float lastNormalized  = 1f;
    private bool  isVisible       = false;

    void Start()
    {
        if (playerAir == null)
        {
            Debug.LogWarning("[AirBarUI] PlayerAir не назначен!", this);
            return;
        }

        // Авто-добавляем CanvasGroup если нет
        if (barRoot != null)
        {
            canvasGroup = barRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = barRoot.AddComponent<CanvasGroup>();
        }

        lastNormalized = playerAir.AirNormalized;
        currentFill    = lastNormalized;
        ApplyFill(currentFill);
        SetVisible(false);
    }

    void Update()
    {
        if (playerAir == null) return;

        float normalized = playerAir.AirNormalized;
        bool  airChanged = Mathf.Abs(normalized - lastNormalized) > 0.001f;

        // ── Плавная заливка ───────────────────────────────────────────────────
        currentFill = Mathf.MoveTowards(currentFill, normalized, fillSmoothSpeed * Time.deltaTime);
        ApplyFill(currentFill);

        // ── Логика видимости и прозрачности ──────────────────────────────────
        if (airChanged || playerAir.IsInSmoke)
        {
            // Воздух меняется или игрок в дыму — показать полностью
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
                // Шкала полная и стоит → полупрозрачная
                targetAlpha = idleAlpha;
            }

            if (timeSinceChange >= idleHideDelay)
            {
                // Не менялась 10 секунд → скрыть
                SetVisible(false);
            }
        }

        // ── Плавная прозрачность ──────────────────────────────────────────────
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, alphaLerpSpeed * Time.deltaTime);
        if (canvasGroup != null) canvasGroup.alpha = currentAlpha;
    }

    void ApplyFill(float t)
    {
        if (fillRect == null) return;
        fillRect.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
    }

    void SetVisible(bool value)
    {
        if (isVisible == value) return;
        isVisible = value;
        if (value)
        {
            barRoot?.SetActive(true);
            currentAlpha = 0f; // начинаем fade-in с нуля
            targetAlpha  = 1f;
        }
        else
        {
            barRoot?.SetActive(false);
        }
    }
}
