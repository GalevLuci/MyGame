using UnityEngine;

/// <summary>
/// Вешай на объект tools (дочерний от игрока).
/// Инициализирует все инструменты и опрашивает их клавиши каждый кадр.
/// </summary>
public class ToolHolder : MonoBehaviour
{
    private PlayerTool[] tools;

    void Start()
    {
        var playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[ToolHolder] PlayerController не найден в родительских объектах!", this);
            return;
        }

        // includeInactive: true — инструменты начинают выключенными
        tools = GetComponentsInChildren<PlayerTool>(includeInactive: true);
        foreach (var tool in tools)
            tool.Initialize(playerController);
    }

    void Update()
    {
        if (tools == null) return;
        // Опрашиваем клавиши у всех инструментов, даже если их GameObject выключен
        foreach (var tool in tools)
            tool.HandleEquipInput();
    }
}
