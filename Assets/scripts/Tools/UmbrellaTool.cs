using UnityEngine;

/// <summary>
/// Вешай на префаб umbrella (внутри tools).
/// Открывается при падении с достаточной высоты, замедляет падение в 2 раза, закрывается при приземлении.
/// </summary>
[RequireComponent(typeof(Animator))]
public class UmbrellaTool : PlayerTool
{
    private static readonly int OpenHash  = Animator.StringToHash("Pattern_Open");
    private static readonly int CloseHash = Animator.StringToHash("Pattern_Close");

    [Header("Анимация зонта")]
    [Tooltip("Скорость анимации открытия (2 = вдвое быстрее)")]
    [SerializeField] private float openAnimSpeed = 2f;

    [Tooltip("Зонт открывается когда velocity.y ниже этого значения.\n" +
             "Увеличь (например до 1), чтобы открывался раньше — ещё до пика прыжка.")]
    [SerializeField] private float openVelocityThreshold = 0.5f;

    [Header("Минимальная высота для открытия")]
    [Tooltip("Зонт открывается только когда ноги игрока выше этого множителя × jumpHeight.\n" +
             "1.0 = только выше высоты прыжка (обычные прыжки и маленькие камни НЕ открывают).\n" +
             "0.8 = чуть ниже порога (позволяет открываться чуть раньше).")]
    [SerializeField] private float minHeightMultiplier = 1.0f;

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
                         && player.VerticalVelocity < openVelocityThreshold
                         && IsHighEnoughToOpen();

        if (shouldBeOpen && !isOpen)
        {
            isOpen = true;
            player.fallSpeedMultiplier = 0.5f;
            player.MultiplyVerticalVelocity(0.5f);
            animator.speed = openAnimSpeed;
            animator.Play(OpenHash);
            AudioManager.Instance?.PlaySFX(openSound);
        }
        else if (!shouldBeOpen && isOpen)
        {
            ForceClose();
        }
    }

    /// <summary>
    /// Рейкаст вниз от НОГ игрока (FeetY).
    /// Открывать зонт можно только если земля дальше, чем jumpHeight × minHeightMultiplier.
    /// </summary>
    private bool IsHighEnoughToOpen()
    {
        float minDist = player.jumpHeight * minHeightMultiplier;
        // Стреляем от ног вниз: если земля в пределах minDist — слишком низко
        Vector3 feetPos = new Vector3(
            player.transform.position.x,
            player.FeetY,
            player.transform.position.z);
        return !Physics.Raycast(feetPos, Vector3.down, minDist);
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
