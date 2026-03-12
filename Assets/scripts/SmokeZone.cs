using UnityEngine;

/// <summary>
/// Зона дыма. Добавь на объект с Collider (Is Trigger = true).
/// Когда игрок входит — начинает убывать воздух через PlayerAir.
/// Частицы дыма настраивай сам в Particle System на этом же объекте.
/// </summary>
public class SmokeZone : MonoBehaviour
{
    [Tooltip("Тег игрока (должен совпадать с тегом на объекте игрока).")]
    [SerializeField] private string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        var air = FindPlayerAir(other);
        air?.SetInSmoke(true);
    }

    void OnTriggerExit(Collider other)
    {
        var air = FindPlayerAir(other);
        air?.SetInSmoke(false);
    }

    PlayerAir FindPlayerAir(Collider other)
    {
        if (!other.CompareTag(playerTag)) return null;
        // Ищем на объекте, в детях и в родителях — на случай разных иерархий
        var air = other.GetComponent<PlayerAir>();
        if (air == null) air = other.GetComponentInParent<PlayerAir>();
        if (air == null) air = other.GetComponentInChildren<PlayerAir>();
        return air;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        var col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
#endif
}
