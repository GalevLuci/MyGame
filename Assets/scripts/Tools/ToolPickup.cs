using UnityEngine;

/// <summary>
/// Вешай на мировой объект-инструмент (например, зонт на земле).
/// Тег объекта должен быть "Interactable".
/// При взаимодействии игрока инструмент выдаётся и сразу достаётся.
/// </summary>
public class ToolPickup : MonoBehaviour, IInteractable
{
    [Tooltip("Ссылка на PlayerTool внутри игрока (например, UmbrellaTool на объекте tools).")]
    [SerializeField] private PlayerTool tool;

    public void Interaction()
    {
        if (tool == null)
        {
            Debug.LogWarning("[ToolPickup] Ссылка на PlayerTool не назначена!", this);
            return;
        }

        tool.Give();   // выдаём инструмент игроку
        tool.Equip();  // сразу достаём в руки

        gameObject.SetActive(false); // убираем объект с земли
    }
}
