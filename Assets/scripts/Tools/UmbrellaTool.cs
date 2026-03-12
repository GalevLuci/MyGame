using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Вешай на префаб umbrella (внутри tools).
/// Зонт открывается когда игрок в воздухе И зажата ActionKey (задаётся ToolHolder'ом, по умолчанию Q).
/// Замедляет падение, закрывается при отпускании клавиши или приземлении.
/// </summary>
[RequireComponent(typeof(Animator))]
public class UmbrellaTool : PlayerTool
{
    private static readonly int OpenHash  = Animator.StringToHash("Pattern_Open");
    private static readonly int CloseHash = Animator.StringToHash("Pattern_Close");

    [Header("Анимация зонта")]
    [Tooltip("Скорость анимации открытия (2 = вдвое быстрее)")]
    [SerializeField] private float openAnimSpeed = 2f;

    [Tooltip("Множитель скорости падения когда зонт открыт.\n" +
             "0.5 = падаешь вдвое медленнее, 0.25 = вчетверо медленнее.")]
    [SerializeField] private float openFallSpeedMultiplier = 0.2f;

    [Header("Звуки")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    private Animator animator;
    private bool isOpen = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        bool shouldBeOpen = !player.IsGrounded
                         && Keyboard.current[ActionKey].isPressed;

        if (shouldBeOpen && !isOpen)
        {
            isOpen = true;
            player.fallSpeedMultiplier = openFallSpeedMultiplier;
            player.MultiplyVerticalVelocity(openFallSpeedMultiplier);
            animator.speed = openAnimSpeed;
            animator.Play(OpenHash);
            AudioManager.Instance?.PlaySFX(openSound);
        }
        else if (!shouldBeOpen && isOpen)
        {
            ForceClose();
        }
    }

    protected override void OnEquipped()
    {
        // Принудительно ставим зонт в закрытое состояние при достании
        isOpen = false;
        animator.speed = 1f;
        animator.Play(CloseHash, -1, 1f); // normalizedTime=1f → сразу конец анимации = закрыт
    }

    protected override void OnUnequipped()
    {
        if (isOpen) ForceClose();
    }

    void OnDisable()
    {
        if (isOpen && player != null)
        {
            isOpen = false;
            player.fallSpeedMultiplier = 1f;
            animator.speed = 1f;
        }
    }

    private void ForceClose()
    {
        isOpen = false;
        player.fallSpeedMultiplier = 1f;
        animator.speed = 1f;
        animator.Play(CloseHash);
        AudioManager.Instance?.PlaySFX(closeSound);
    }
}
