using UnityEngine;

/// <summary>
/// Вызывается когда у игрока заканчивается кислород.
/// Автоматически подписывается на PlayerAir.OnDead.
/// </summary>
public class Dead : MonoBehaviour
{
    void Start()
    {
        var air = GetComponent<PlayerAir>();
        if (air == null) air = GetComponentInParent<PlayerAir>();
        if (air == null) air = FindObjectOfType<PlayerAir>();

        if (air != null)
            air.OnDead += Die;
        else
            Debug.LogWarning("[Dead] PlayerAir не найден!", this);
    }

    public void Die()
    {
        // TODO
    }
}
