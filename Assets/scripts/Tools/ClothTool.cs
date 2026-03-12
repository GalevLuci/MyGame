using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Тряпка — инструмент для пополнения воздуха.
/// Зажми ActionKey (Q) чтобы восстанавливать воздух.
/// Работает как снаружи так и внутри дымовой зоны.
/// </summary>
public class ClothTool : PlayerTool
{
    [Header("Тряпка")]
    [Tooltip("Скорость восстановления воздуха пока зажата ActionKey (единиц/сек).")]
    [SerializeField] private float restoreRate = 25f;

    [Tooltip("Звук при использовании тряпки (опционально).")]
    [SerializeField] private AudioClip useSound;

    private PlayerAir playerAir;
    private bool      wasRestoring = false;

    protected override void OnEquipped()
    {
        // Ищем PlayerAir на игроке
        playerAir = player.GetComponent<PlayerAir>();
        if (playerAir == null)
            playerAir = player.GetComponentInChildren<PlayerAir>();
        if (playerAir == null)
            Debug.LogWarning("[ClothTool] PlayerAir не найден на игроке!", this);
    }

    protected override void OnUnequipped()
    {
        wasRestoring = false;
    }

    void Update()
    {
        if (!IsEquipped || playerAir == null) return;

        bool isRestoring = Keyboard.current[ActionKey].isPressed;

        if (isRestoring)
        {
            playerAir.AddAir(restoreRate * Time.deltaTime);

            // Звук при начале использования
            if (!wasRestoring && useSound != null)
                AudioManager.Instance?.PlaySFX(useSound);
        }

        wasRestoring = isRestoring;
    }
}
