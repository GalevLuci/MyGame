using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоска воздуха на HUD.
/// 
/// Настройка в Editor:
///  1. Создай Canvas → дочерний GameObject "AirBar" (это barRoot).
///  2. Внутри — Image с Fill Type = Filled, Fill Method = Horizontal (это fillImage).
///  3. Повесь AirBarUI на любой GameObject, заполни поля в инспекторе.
/// 
/// Полоска появляется при входе в дым или когда воздух не полный.
/// Скрывается через hideDelay секунд после полного восстановления и выхода из дыма.
/// </summary>
public class AirBarUI : MonoBehaviour
{
    [Header("UI элементы")]
    [Tooltip("Корневой GameObject полоски (будет включаться/выключаться).")]
    [SerializeField] private GameObject barRoot;

    [Tooltip("Image с Fill Type = Filled. fillAmount = текущий воздух.")]
    [SerializeField] private Image fillImage;

    [Header("Настройки")]
    [Tooltip("Компонент PlayerAir на игроке.")]
    [SerializeField] private PlayerAir playerAir;

    [Tooltip("Через сколько секунд скрыть полоску после того как воздух полный и игрок вышел из дыма.")]
    [SerializeField] private float hideDelay = 2f;

    [Tooltip("Скорость плавного изменения fillAmount.")]
    [SerializeField] private float fillSmoothSpeed = 8f;

    private float targetFill  = 1f;
    private float currentFill = 1f;
    private float hideTimer   = 0f;
    private bool  isVisible   = false;

    void Start()
    {
        if (playerAir == null)
        {
            Debug.LogWarning("[AirBarUI] PlayerAir не назначен!", this);
            return;
        }

        playerAir.OnAirChanged   += HandleAirChanged;
        playerAir.OnEnterSmoke   += HandleEnterSmoke;
        playerAir.OnExitSmoke    += HandleExitSmoke;

        // Инициализация
        targetFill  = playerAir.AirNormalized;
        currentFill = targetFill;
        if (fillImage != null) fillImage.fillAmount = currentFill;
        SetVisible(false);
    }

    void OnDestroy()
    {
        if (playerAir == null) return;
        playerAir.OnAirChanged -= HandleAirChanged;
        playerAir.OnEnterSmoke -= HandleEnterSmoke;
        playerAir.OnExitSmoke  -= HandleExitSmoke;
    }

    void Update()
    {
        // Плавное изменение заливки
        if (!Mathf.Approximately(currentFill, targetFill))
        {
            currentFill = Mathf.MoveTowards(currentFill, targetFill,
                                             fillSmoothSpeed * Time.deltaTime);
            if (fillImage != null) fillImage.fillAmount = currentFill;
        }

        // Таймер скрытия
        if (hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
                SetVisible(false);
        }
    }

    // ── Обработчики событий ───────────────────────────────────────────────────

    void HandleAirChanged(float normalized)
    {
        targetFill = normalized;

        // Показать полоску если воздух не полный
        if (normalized < 1f)
        {
            SetVisible(true);
            hideTimer = 0f;
        }
        else if (!playerAir.IsInSmoke)
        {
            // Воздух полон и не в дыму — запустить таймер скрытия
            hideTimer = hideDelay;
        }
    }

    void HandleEnterSmoke()
    {
        SetVisible(true);
        hideTimer = 0f;
    }

    void HandleExitSmoke()
    {
        // Скрыть только если воздух уже полный
        if (playerAir.AirNormalized >= 1f)
            hideTimer = hideDelay;
    }

    // ── Вспомогательное ───────────────────────────────────────────────────────

    void SetVisible(bool value)
    {
        if (isVisible == value) return;
        isVisible = value;
        barRoot?.SetActive(value);
    }
}
