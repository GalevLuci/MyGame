using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоска воздуха на HUD.
///
/// Настройка в Editor:
///  1. Canvas → дочерний GameObject "AirBar" (это barRoot).
///  2. Внутри barRoot — Image с Fill Type = Filled, Method = Horizontal (это fillImage).
///  3. Повесь AirBarUI на любой GameObject (НЕ на barRoot), заполни поля.
///
/// Полоска показывается когда игрок в дыму ИЛИ воздух не полный.
/// Скрывается через hideDelay секунд как только воздух полный и игрок не в дыму.
/// </summary>
public class AirBarUI : MonoBehaviour
{
    [Header("UI элементы")]
    [Tooltip("Корневой GameObject полоски (будет включаться/выключаться).")]
    [SerializeField] private GameObject barRoot;

    [Tooltip("Image с Fill Type = Filled. fillAmount = текущий воздух 0-1.")]
    [SerializeField] private Image fillImage;

    [Header("Настройки")]
    [Tooltip("Компонент PlayerAir на игроке.")]
    [SerializeField] private PlayerAir playerAir;

    [Tooltip("Через сколько секунд скрыть полоску когда воздух полный и не в дыму.")]
    [SerializeField] private float hideDelay = 2f;

    [Tooltip("Скорость плавного изменения заливки.")]
    [SerializeField] private float fillSmoothSpeed = 8f;

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

        currentFill = playerAir.AirNormalized;
        if (fillImage != null) fillImage.fillAmount = currentFill;
        SetVisible(false);
    }

    void Update()
    {
        if (playerAir == null) return;

        float normalized = playerAir.AirNormalized;

        // ── Плавная заливка ───────────────────────────────────────────────────
        currentFill = Mathf.MoveTowards(currentFill, normalized, fillSmoothSpeed * Time.deltaTime);
        if (fillImage != null) fillImage.fillAmount = currentFill;

        // ── Логика показа/скрытия ─────────────────────────────────────────────
        bool wantsVisible = playerAir.IsInSmoke || normalized < 0.9999f;

        if (wantsVisible)
        {
            // Сбросить таймер и показать
            hideTimer = hideDelay;
            SetVisible(true);
        }
        else
        {
            // Воздух полный и не в дыму — отсчёт до скрытия
            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f)
                    SetVisible(false);
            }
        }
    }

    void SetVisible(bool value)
    {
        if (isVisible == value) return;
        isVisible = value;
        barRoot?.SetActive(value);
    }
}
