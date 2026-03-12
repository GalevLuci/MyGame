using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Вешай на объект tools (дочерний от камеры).
/// Инициализирует все инструменты, опрашивает клавиши каждый кадр,
/// компенсирует вертикальный поворот камеры и добавляет покачивание при ходьбе.
/// </summary>
public class ToolHolder : MonoBehaviour
{
    [Header("Компенсация поворота камеры")]
    [Tooltip("Компенсировать вертикальный поворот камеры (pitch),\n" +
             "чтобы инструменты не наклонялись вместе с камерой.")]
    [SerializeField] private bool counteractCameraPitch = true;

    [Header("Звуки")]
    [Tooltip("Звук при достании любого инструмента.")]
    [SerializeField] private AudioClip equipSound;

    [Header("Клавиша действия (одна на все инструменты)")]
    [Tooltip("Клавиша действия — одна для всех инструментов (открыть зонт, применить предмет и т.д.).\n" +
             "Задаётся здесь и автоматически передаётся каждому инструменту.")]
    [SerializeField] private Key actionKey = Key.Q;

    /// <summary>
    /// Единая клавиша действия для всех инструментов.
    /// При изменении автоматически обновляет ActionKey у каждого инструмента.
    /// </summary>
    public Key ActionKey
    {
        get => actionKey;
        set
        {
            actionKey = value;
            if (tools != null)
                foreach (var tool in tools)
                    tool.ActionKey = value;
        }
    }

    [Header("Покачивание при ходьбе")]
    [SerializeField] private bool enableWalkBob = true;

    [Tooltip("Амплитуда покачивания по вертикали (Y)")]
    [SerializeField] private float bobAmplitudeY = 0.04f;

    [Tooltip("Амплитуда покачивания по горизонтали (X)")]
    [SerializeField] private float bobAmplitudeX = 0.02f;

    [Tooltip("Скорость сглаживания покачивания")]
    [SerializeField] private float bobSmoothSpeed = 10f;

    [Tooltip("Скорость возврата к нулю когда игрок стоит")]
    [SerializeField] private float bobReturnSpeed = 8f;

    private PlayerTool[] tools;
    private PlayerController playerController;

    private Vector3 baseLocalPos;
    private Vector3 currentBobOffset = Vector3.zero;
    private Vector3 targetBobOffset  = Vector3.zero;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[ToolHolder] PlayerController не найден в родительских объектах!", this);
            return;
        }

        baseLocalPos = transform.localPosition;

        // includeInactive: true — инструменты начинают выключенными
        tools = GetComponentsInChildren<PlayerTool>(includeInactive: true);
        foreach (var tool in tools)
        {
            tool.Initialize(playerController, this);
            tool.ActionKey    = actionKey;
            tool.onEquipSound = () => AudioManager.Instance?.PlaySFX(equipSound);
        }
    }

    /// <summary>Возвращает true если хоть один инструмент кроме <paramref name="except"/> сейчас достан.</summary>
    public bool AnyOtherEquipped(PlayerTool except)
    {
        foreach (var t in tools)
            if (t != except && t.IsEquipped) return true;
        return false;
    }

    /// <summary>Возвращает инструмент который сейчас в руках, или null если ничего не достано.</summary>
    public PlayerTool GetEquippedTool()
    {
        foreach (var t in tools)
            if (t.IsEquipped) return t;
        return null;
    }

    // Блокирует весь ввод пока идёт анимация свапа инструментов
    private bool isTransitioning = false;

    void Update()
    {
        if (tools == null || isTransitioning) return;

        // Найти уже достанный инструмент
        PlayerTool equipped = null;
        foreach (var t in tools)
            if (t.IsEquipped) { equipped = t; break; }

        foreach (var tool in tools)
        {
            if (!tool.IsOwned) continue;
            if (!Keyboard.current[tool.EquipKey].wasPressedThisFrame) continue;

            if (tool.IsEquipped)
            {
                // Убрать тот же инструмент
                tool.Unequip();
            }
            else if (equipped == null)
            {
                // Ничего не достано — просто достать
                tool.Equip();
            }
            else
            {
                // Другой инструмент уже в руках — свап
                StartCoroutine(SwapTools(equipped, tool));
            }
            break;
        }
    }

    /// <summary>
    /// Убирает <paramref name="from"/>, ждёт конца анимации, затем достаёт <paramref name="to"/>.
    /// Во время свапа весь ввод заблокирован (<see cref="isTransitioning"/> = true).
    /// </summary>
    private System.Collections.IEnumerator SwapTools(PlayerTool from, PlayerTool to)
    {
        isTransitioning = true;
        bool done = false;
        from.Unequip(() => done = true);
        while (!done) yield return null;
        isTransitioning = false;
        to.Equip();
    }

    void LateUpdate()
    {
        if (playerController == null) return;

        // ── 1. Компенсация pitch камеры ──────────────────────────────────────
        // Компенсация pitch камеры: применяем к повороту И позиции,
        // чтобы инструменты не двигались при взгляде вверх/вниз
        Quaternion pitchComp = counteractCameraPitch
            ? Quaternion.Inverse(playerController.cameraTransform.localRotation)
            : Quaternion.identity;

        transform.localRotation = pitchComp;

        // ── 2. Покачивание при ходьбе ─────────────────────────────────────────
        if (enableWalkBob)
        {
            if (playerController.IsMoving)
            {
                float t = playerController.BobTimer;
                float bobY = Mathf.Sin(t * 2f) * bobAmplitudeY;
                float bobX = Mathf.Sin(t)       * bobAmplitudeX;
                targetBobOffset = new Vector3(bobX, bobY, 0f);
            }
            else
            {
                targetBobOffset = Vector3.zero;
            }

            currentBobOffset = Vector3.Lerp(currentBobOffset, targetBobOffset,
                                            Time.deltaTime * (playerController.IsMoving ? bobSmoothSpeed : bobReturnSpeed));
        }
        else
        {
            currentBobOffset = Vector3.zero;
        }

        // Применяем ту же компенсацию к позиции: смещение будет в пространстве игрока (yaw),
        // а не в пространстве камеры (pitch+yaw)
        transform.localPosition = pitchComp * (baseLocalPos + currentBobOffset);
    }
}
