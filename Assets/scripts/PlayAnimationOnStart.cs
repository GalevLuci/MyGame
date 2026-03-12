using UnityEngine;

/// <summary>
/// Воспроизводит указанную анимацию сразу при старте и держит её.
/// Удобно для мировых объектов с фиксированным состоянием (например, закрытый зонт на земле).
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayAnimationOnStart : MonoBehaviour
{
    [Tooltip("Имя состояния аниматора которое нужно воспроизвести (например stay_close).")]
    [SerializeField] private string stateName = "stay_close";

    void Start()
    {
        GetComponent<Animator>().Play(stateName);
    }
}
