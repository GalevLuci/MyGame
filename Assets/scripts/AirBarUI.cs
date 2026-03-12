using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоска воздуха на HUD.
///
/// Настройка в Editor:
///  1. Canvas → дочерний GameObject "AirBar" (это barRoot, по умолчанию выключи его).
///  2. Внутри barRoot создай два Image:
///       - Background  (фон, любой цвет — например тёмный)
///       - Fill        (заливка, яркий цвет — например голубой/зелёный)
///     Перетащи Fill в поле fillRect. Source Image и Fill Type НЕ нужны.
///  3. Повесь AirBarUI на любой GameObject (НЕ на barRoot), заполни поля.
/// </summary>
public class AirBarUI : MonoBehaviour
{
    [Header("UI элементы")]
    [Tooltip("Корневой GameObject полоски (будет включаться/выключаться).")]
    [SerializeField] private GameObject barRoot;

    [Tooltip("RectTransform заливки (Fill). Ширина меняется через localScale.x от 0 до 1.")]
    [SerializeField] private RectTransform fillRect;

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
        ApplyFill(currentFill);
        SetVisible(false);
    }

    void Update()
    {
        if (playerAir == null) return;

        float normalized = playerAir.AirNormalized;

        // ── Плавная заливка ───────────────────────────────────────────────────
        currentFill = Mathf.MoveTowards(currentFill, normalized, fillSmoothSpeed * Time.deltaTime);
        ApplyFill(currentFill);

        // ── Логика показа/скрытия ─────────────────────────────────────────────
        bool wantsVisible = playerAir.IsInSmoke || normalized < 0.9999f;

        if (wantsVisible)
        {
            hideTimer = hideDelay;
            SetVisible(true);
        }
        else
        {
            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f)
                    SetVisible(false);
            }
        }
    }

    // Меняем только X-масштаб: 0 = пустая, 1 = полная
    void ApplyFill(float t)
    {
        if (fillRect == null) return;
        fillRect.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
    }

    void SetVisible(bool value)
    {
        if (isVisible == value) return;
        isVisible = value;
        barRoot?.SetActive(value);
    }
}
