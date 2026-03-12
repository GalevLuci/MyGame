using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Базовый класс для всех инструментов игрока.
/// Вешай наследника на каждый префаб-инструмент внутри объекта tools.
/// </summary>
public abstract class PlayerTool : MonoBehaviour
{
    [Header("Владение")]
    [Tooltip("Включи если инструмент уже есть у игрока с самого начала (не нужно подбирать).")]
    [SerializeField] private bool startOwned = false;

    [Header("Клавиша экипировки")]
    [SerializeField] private Key equipKey = Key.Digit1;

    [Header("Анимация достать/убрать")]
    [Tooltip("Время анимации (секунды)")]
    [SerializeField] private float equipAnimDuration = 0.25f;
    [Tooltip("Смещение позиции в спрятанном состоянии (локальные координаты игрока).\n" +
             "(0, 0, -0.8) = из-за спины   |   (0, -0.8, 0) = снизу")]
    [SerializeField] private Vector3 hideOffset = new Vector3(0f, 0f, -0.8f);
    [Tooltip("Поворот в спрятанном состоянии (Euler, относительно нормального угла).\n" +
             "(90, 0, 0) = наклонён на 90° — как за спиной")]
    [SerializeField] private Vector3 hideRotationOffset = new Vector3(90f, 0f, 0f);

    protected PlayerController player;
    public bool IsEquipped { get; private set; }
    public bool IsOwned    { get; private set; }
    /// <summary>Клавиша экипировки — читается и задаётся из SettingsMenu.</summary>
    public Key EquipKey  { get => equipKey;  set => equipKey  = value; }
    /// <summary>Универсальная клавиша действия (Q по умолчанию) — одна на все инструменты, задаётся ToolHolder'ом.</summary>
    public Key ActionKey { get; set; } = Key.Q;

    /// <summary>Даёт инструмент игроку (например при подборе с земли).</summary>
    public void Give() => IsOwned = true;

    private Vector3    equippedLocalPos;
    private Quaternion equippedLocalRot;
    private Quaternion hiddenLocalRot;
    private Coroutine  animCoroutine;

    /// <summary>Вызывается ToolHolder'ом при старте.</summary>
    public virtual void Initialize(PlayerController playerController)
    {
        player = playerController;
        equippedLocalPos = transform.localPosition;
        equippedLocalRot = transform.localRotation;
        hiddenLocalRot   = equippedLocalRot * Quaternion.Euler(hideRotationOffset);
        IsOwned = startOwned;
        gameObject.SetActive(false);
    }

    /// <summary>Звук достания — задаётся ToolHolder'ом (один звук на все инструменты).</summary>
    public System.Action onEquipSound;

    /// <summary>Вызывается ToolHolder'ом каждый кадр (даже когда GameObject выключен).</summary>
    /// <param name="blockEquip">Запретить достать, если уже достан другой инструмент.</param>
    public void HandleEquipInput(bool blockEquip = false)
    {
        if (!IsOwned) return;
        if (!Keyboard.current[equipKey].wasPressedThisFrame) return;
        if (IsEquipped) Unequip();
        else if (!blockEquip) Equip();
    }

    public void Toggle() { if (IsEquipped) Unequip(); else Equip(); }

    public void Equip()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        IsEquipped = true;
        gameObject.SetActive(true);
        // Начинаем из спрятанной позиции + повёрнутым
        transform.localPosition = GetHiddenLocalPos();
        transform.localRotation = hiddenLocalRot;
        animCoroutine = StartCoroutine(SlideTo(equippedLocalPos, equippedLocalRot));
        onEquipSound?.Invoke();
        OnEquipped();
    }

    public void Unequip()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        IsEquipped = false;
        OnUnequipped();
        animCoroutine = StartCoroutine(SlideToThenDisable(
            GetHiddenLocalPos(), hiddenLocalRot));
    }

    /// <summary>Переопредели для логики при достании.</summary>
    protected virtual void OnEquipped() { }

    /// <summary>Переопредели для логики при уборке (до начала анимации).</summary>
    protected virtual void OnUnequipped() { }

    // ──────────────── Спрятанная позиция ────────────────

    /// <summary>
    /// Считает спрятанную локальную позицию применяя hideOffset в горизонтальном
    /// пространстве игрока (без питча камеры), чтобы анимация не искажалась
    /// при взгляде вниз/вверх.
    /// </summary>
    private Vector3 GetHiddenLocalPos()
    {
        if (player == null || transform.parent == null)
            return equippedLocalPos + hideOffset;

        // Мировая позиция экипированной точки
        Vector3 worldEquipped = transform.parent.TransformPoint(equippedLocalPos);

        // Применяем offset только по горизонтальному направлению игрока (yaw без pitch)
        Quaternion playerYaw = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
        Vector3 worldHidden  = worldEquipped + playerYaw * hideOffset;

        return transform.parent.InverseTransformPoint(worldHidden);
    }

    // ──────────────── Анимация ────────────────

    private IEnumerator SlideTo(Vector3 targetPos, Quaternion targetRot)
    {
        Vector3    startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        float elapsed = 0f;
        while (elapsed < equipAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / equipAnimDuration;
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        transform.localPosition = targetPos;
        transform.localRotation = targetRot;
    }

    private IEnumerator SlideToThenDisable(Vector3 targetPos, Quaternion targetRot)
    {
        yield return StartCoroutine(SlideTo(targetPos, targetRot));
        gameObject.SetActive(false);
        // Сброс для следующего достания
        transform.localPosition = equippedLocalPos;
        transform.localRotation = equippedLocalRot;
    }
}
